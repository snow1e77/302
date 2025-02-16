using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет игровым полем. Видимая область имеет размеры width x height ячеек,
/// но внутренняя сетка расширена на extraRows сверху для хранения висящих фигур.
/// Дно поля поднято на bottomOffset единиц – допустимые мировые Y начинаются с bottomOffset.
/// При фиксации тайлов в сетке индекс строки вычисляется как (worldY - bottomOffset).
/// Также реализована механика совпадений (удаляются группы из 3+ подряд тайлов одного цвета)
/// и гравитация, когда связные группы опускаются вниз.
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;       // Число столбцов
    public static int height = 24;      // Число видимых строк

    public int extraRows = 4;           // Дополнительные строки сверху
    public int bottomOffset = 1;        // Подъём дна поля (допустимые мировые Y ≥ bottomOffset)

    // Полная высота сетки = height + extraRows
    public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height + extraRows];
    }

    /// <summary>
    /// Преобразует мировую позицию в координаты ячейки с округлением до ближайшего целого.
    /// </summary>
    public Vector2 Round(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// Фиксирует тайл в сетке.
    /// Индекс строки = (worldY - bottomOffset).
    /// </summary>
    public void AddToGrid(Transform tile)
    {
        Vector2 pos = Round(tile.position);
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y) - bottomOffset;
        if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
            grid[x, y] = tile;
    }

    /// <summary>
    /// Проверяет совпадения по горизонтали и вертикали во всей сетке.
    /// Удаляются только группы, где 3 или более подряд тайлов имеют одинаковый цвет.
    /// После удаления запускается гравитация и через небольшую задержку повторно проверяются совпадения.
    /// </summary>
    public void CheckMatches()
    {
        List<Transform> tilesToRemove = new List<Transform>();
        int gridHeight = grid.GetLength(1);

        // Проходим по всей сетке
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Transform tile = grid[x, y];
                if (tile != null)
                {
                    Color tileColor = tile.GetComponent<SpriteRenderer>().color;

                    // Горизонтальная проверка
                    List<Transform> matchHorizontal = new List<Transform>();
                    matchHorizontal.Add(tile);
                    int xTemp = x + 1;
                    while (xTemp < width && grid[xTemp, y] != null &&
                           grid[xTemp, y].GetComponent<SpriteRenderer>().color == tileColor)
                    {
                        matchHorizontal.Add(grid[xTemp, y]);
                        xTemp++;
                    }
                    if (matchHorizontal.Count >= 3)
                    {
                        foreach (Transform t in matchHorizontal)
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                    }

                    // Вертикальная проверка
                    List<Transform> matchVertical = new List<Transform>();
                    matchVertical.Add(tile);
                    int yTemp = y + 1;
                    while (yTemp < gridHeight && grid[x, yTemp] != null &&
                           grid[x, yTemp].GetComponent<SpriteRenderer>().color == tileColor)
                    {
                        matchVertical.Add(grid[x, yTemp]);
                        yTemp++;
                    }
                    if (matchVertical.Count >= 3)
                    {
                        foreach (Transform t in matchVertical)
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                    }
                }
            }
        }

        if (tilesToRemove.Count > 0)
        {
            foreach (Transform t in tilesToRemove)
            {
                Vector2 pos = Round(t.position);
                int x = Mathf.RoundToInt(pos.x);
                int y = Mathf.RoundToInt(pos.y) - bottomOffset;
                if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
                    grid[x, y] = null;
                Destroy(t.gameObject);
            }
            StartCoroutine(FillEmptySpaces());
            Invoke("DelayedCheckMatches", 0.2f);
        }
    }

    void DelayedCheckMatches()
    {
        CheckMatches();
    }

    /// <summary>
    /// Гравитация: опускает тайлы, чтобы заполнить пустые места.
    /// Если связная группа тайлов может опуститься (для всех ячеек ниже пусто), группа опускается на 1 ячейку.
    /// </summary>
    public IEnumerator FillEmptySpaces()
    {
        bool movedAny;
        do
        {
            movedAny = false;
            bool[,] visited = new bool[width, grid.GetLength(1)];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (!visited[x, y] && grid[x, y] != null)
                    {
                        List<Vector2Int> group = new List<Vector2Int>();
                        Queue<Vector2Int> queue = new Queue<Vector2Int>();
                        queue.Enqueue(new Vector2Int(x, y));
                        visited[x, y] = true;
                        while (queue.Count > 0)
                        {
                            Vector2Int cell = queue.Dequeue();
                            group.Add(cell);
                            Vector2Int[] neighbors = new Vector2Int[]
                            {
                                new Vector2Int(cell.x + 1, cell.y),
                                new Vector2Int(cell.x - 1, cell.y),
                                new Vector2Int(cell.x, cell.y + 1),
                                new Vector2Int(cell.x, cell.y - 1)
                            };
                            foreach (Vector2Int nb in neighbors)
                            {
                                if (nb.x >= 0 && nb.x < width && nb.y >= 0 && nb.y < grid.GetLength(1))
                                {
                                    if (!visited[nb.x, nb.y] && grid[nb.x, nb.y] != null)
                                    {
                                        visited[nb.x, nb.y] = true;
                                        queue.Enqueue(nb);
                                    }
                                }
                            }
                        }
                        bool canFall = true;
                        foreach (Vector2Int cell in group)
                        {
                            if (cell.y == 0)
                            {
                                canFall = false;
                                break;
                            }
                            if (grid[cell.x, cell.y - 1] != null && !group.Contains(new Vector2Int(cell.x, cell.y - 1)))
                            {
                                canFall = false;
                                break;
                            }
                        }
                        if (canFall)
                        {
                            group.Sort((a, b) => a.y.CompareTo(b.y));
                            foreach (Vector2Int cell in group)
                            {
                                Transform t = grid[cell.x, cell.y];
                                grid[cell.x, cell.y] = null;
                                grid[cell.x, cell.y - 1] = t;
                                t.position = new Vector2(cell.x, cell.y - 1 + bottomOffset);
                            }
                            movedAny = true;
                        }
                    }
                }
            }
            if (movedAny)
                yield return new WaitForSeconds(0.1f);
        } while (movedAny);
        yield break;
    }
}
