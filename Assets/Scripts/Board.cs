using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет игровым полем.
/// Внутренняя сетка имеет размеры (width) x (height + extraRows).
/// Дно поля поднято на bottomOffset единиц – допустимые мировые Y начинаются с bottomOffset.
/// При фиксации тайлов в сетке индекс строки = (worldY - bottomOffset).
/// Реализованы проверка совпадений (3+ подряд тайлов одного цвета) и гравитация для связных групп.
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;       // Число столбцов
    public static int height = 24;      // Число видимых строк

    public int extraRows = 4;           // Дополнительные строки (для висящих тайлов)
    public int bottomOffset = 1;        // Подъём дна поля (допустимые мировые Y ≥ bottomOffset)

    // Полная высота сетки
    public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height + extraRows];
    }

    /// <summary>
    /// Определяет ячейку, в которую попадает позиция.
    /// При условии, что pivot спрайта = (0.5, 0.5), клетка вычисляется как Floor(pos + 0.5)
    /// (то есть, если позиция равна (3.2, 5.7), ячейка будет (Floor(3.7)=3, Floor(6.2)=6)).
    /// </summary>
    public Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, находится ли мировая позиция pos внутри сетки.
    /// Допустимые X: 0 ... width-1.
    /// Допустимые Y: от bottomOffset до bottomOffset + gridHeight - 1.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        Vector2 cell = GetCellCoordinates(pos);
        int x = (int)cell.x;
        int y = (int)cell.y;
        return (x >= 0 && x < width && y >= bottomOffset && y < bottomOffset + grid.GetLength(1));
    }

    /// <summary>
    /// Фиксирует тайл в сетке. Индекс строки = (cell.y - bottomOffset).
    /// </summary>
    public void AddToGrid(Transform tile)
    {
        Vector2 cell = GetCellCoordinates(tile.position);
        int x = (int)cell.x;
        int y = (int)cell.y - bottomOffset;
        if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
            grid[x, y] = tile;
    }

    /// <summary>
    /// Проверяет совпадения по горизонтали и вертикали для всей сетки.
    /// Удаляются только группы, где 3 и более подряд тайлов имеют один и тот же цвет.
    /// После удаления запускается гравитация, а через небольшую задержку – повторная проверка.
    /// </summary>
    public void CheckMatches()
    {
        List<Transform> tilesToRemove = new List<Transform>();
        int gridHeight = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Transform tile = grid[x, y];
                if (tile != null)
                {
                    Color col = tile.GetComponent<SpriteRenderer>().color;

                    // Горизонтальная проверка
                    List<Transform> matchH = new List<Transform>();
                    matchH.Add(tile);
                    int xt = x + 1;
                    while (xt < width && grid[xt, y] != null &&
                           grid[xt, y].GetComponent<SpriteRenderer>().color == col)
                    {
                        matchH.Add(grid[xt, y]);
                        xt++;
                    }
                    if (matchH.Count >= 3)
                    {
                        foreach (Transform t in matchH)
                        {
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                        }
                    }

                    // Вертикальная проверка
                    List<Transform> matchV = new List<Transform>();
                    matchV.Add(tile);
                    int yt = y + 1;
                    while (yt < gridHeight && grid[x, yt] != null &&
                           grid[x, yt].GetComponent<SpriteRenderer>().color == col)
                    {
                        matchV.Add(grid[x, yt]);
                        yt++;
                    }
                    if (matchV.Count >= 3)
                    {
                        foreach (Transform t in matchV)
                        {
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                        }
                    }
                }
            }
        }

        if (tilesToRemove.Count > 0)
        {
            foreach (Transform t in tilesToRemove)
            {
                Vector2 cell = GetCellCoordinates(t.position);
                int x = (int)cell.x;
                int y = (int)cell.y - bottomOffset;
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
    /// Гравитация: опускает тайлы (или связные группы) вниз, пока под ними пусто.
    /// </summary>
    public IEnumerator FillEmptySpaces()
    {
        bool moved;
        do
        {
            moved = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] == null)
                    {
                        for (int k = y + 1; k < grid.GetLength(1); k++)
                        {
                            if (grid[x, k] != null)
                            {
                                // Перемещаем тайл вниз
                                grid[x, y] = grid[x, k];
                                grid[x, k] = null;
                                grid[x, y].position = new Vector2(x, k - 1 + bottomOffset);
                                moved = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (moved)
                yield return new WaitForSeconds(0.1f);
        } while (moved);
        yield break;
    }
}
