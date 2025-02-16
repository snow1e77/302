using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет игровым полем.
/// Видимая область имеет размеры width x height, но внутренняя сетка расширена 
/// на extraRows сверху для обработки висящих фигур.
/// Нижняя граница (дно) поднята на bottomOffset единиц.
/// Индекс строки в сетке вычисляется как (worldY - bottomOffset).
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;    // Число столбцов
    public static int height = 24;   // Число видимых строк

    // Дополнительные строки (например, 4) для хранения висящих фигур
    public int extraRows = 4;

    // Поднимаем дно поля на 1 единицу (например, если bottomOffset = 1, то допустимые Y ≥ 1)
    public int bottomOffset = 1;

    // Полная высота сетки = height + extraRows
    public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height + extraRows];
    }

    /// <summary>
    /// Проверяет, находится ли мировая позиция pos внутри сетки.
    /// Допустимые X: от 0 до width-1, допустимые Y: от bottomOffset до bottomOffset + gridHeight - 1.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        return (x >= 0 && x < width && y >= bottomOffset && y < bottomOffset + grid.GetLength(1));
    }

    /// <summary>
    /// Округляет позицию до целых значений.
    /// </summary>
    public Vector2 Round(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// Фиксирует блок, переводя мировую координату в индекс строки: gridY = worldY - bottomOffset.
    /// </summary>
    public void AddToGrid(Transform block)
    {
        Vector2 pos = Round(block.position);
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y) - bottomOffset;
        if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
            grid[x, y] = block;
    }

    /// <summary>
    /// Проверяет совпадения по горизонтали и вертикали для всей сетки.
    /// Если найдено 3 и более смежных блока одного цвета, они удаляются.
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
                    // Горизонтальная проверка: собираем подряд идущие тайлы по горизонтали
                    List<Transform> matchHorizontal = new List<Transform>();
                    matchHorizontal.Add(tile);
                    int xTemp = x + 1;
                    while (xTemp < width && grid[xTemp, y] != null)
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

                    // Вертикальная проверка: собираем подряд тайлы по вертикали
                    List<Transform> matchVertical = new List<Transform>();
                    matchVertical.Add(tile);
                    int yTemp = y + 1;
                    while (yTemp < gridHeight && grid[x, yTemp] != null)
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

        // Удаляем найденные тайлы
        foreach (Transform t in tilesToRemove)
        {
            Vector2 pos = Round(t.position);
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y) - bottomOffset;
            if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
                grid[x, y] = null;
            Destroy(t.gameObject);
        }

        // После удаления запускаем гравитацию, чтобы оставшиеся тайлы опустились вниз
        StartCoroutine(FillEmptySpaces());
    }

    /// <summary>
    /// Гравитация: опускает блоки, чтобы заполнить пустые места.
    /// Если смежные блоки образуют группу, она падает вместе, если для всех клеток ниже пусто.
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
