﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет игровым полем.
/// Сетка имеет размеры width x (height + extraRows).
/// Дно поля поднято на bottomOffset единиц – допустимые мировые Y начинаются с bottomOffset.
/// При фиксации тайлов индекс строки = (worldY - bottomOffset).
/// Реализована логика совпадений (3+ подряд тайлов одного цвета) и гравитация связных групп.
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;      // Число столбцов
    public static int height = 24;     // Число видимых строк

    public int extraRows = 4;          // Дополнительные строки (для «висящих» тайлов)
    public int bottomOffset = 1;       // Подъём дна поля (допустимые Y ≥ bottomOffset)

    // Полная высота сетки = height + extraRows
    public Transform[,] grid;

    void Awake()
    {
        // Создаём сетку, где height + extraRows – общее число строк
        grid = new Transform[width, height + extraRows];
    }

    /// <summary>
    /// Для спрайтов с pivot = (0.5, 0.5) координата ячейки вычисляется как Floor(pos + 0.5).
    /// </summary>
    public Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, находится ли мировая позиция pos внутри сетки.
    /// Допустимые X: 0..width-1; Y: от bottomOffset до bottomOffset + gridHeight - 1.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        Vector2 cell = GetCellCoordinates(pos);
        int x = (int)cell.x;
        int y = (int)cell.y;
        return (x >= 0 && x < width && y >= bottomOffset && y < bottomOffset + grid.GetLength(1));
    }

    /// <summary>
    /// Фиксирует тайл в сетке: индекс строки = (cell.y - bottomOffset).
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
    /// После удаления запускается гравитация (FillEmptySpaces), затем повторная проверка совпадений.
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
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
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
                Vector2 cell = GetCellCoordinates(t.position);
                int x = (int)cell.x;
                int y = (int)cell.y - bottomOffset;
                if (x >= 0 && x < width && y >= 0 && y < grid.GetLength(1))
                    grid[x, y] = null;
                Destroy(t.gameObject);
            }
            // Запускаем гравитацию
            StartCoroutine(FillEmptySpaces());
            // Через небольшую задержку повторяем проверку совпадений
            Invoke("DelayedCheckMatches", 0.2f);
        }
    }

    void DelayedCheckMatches()
    {
        CheckMatches();
    }

    /// <summary>
    /// Гравитация с помощью BFS:
    /// Ищем связные группы тайлов (flood fill), проверяем, может ли вся группа опуститься на 1.
    /// Если да – опускаем, и повторяем, пока что-нибудь двигается.
    /// Таким образом, блоки не «висят» в воздухе.
    /// </summary>
    /// <summary>
    /// Гравитация: каждая связная группа тайлов падает максимально вниз за один шаг.
    /// Если после удаления совпадений тайлы "висят в воздухе", они упадут до нижнего препятствия.
    /// </summary>
    public IEnumerator FillEmptySpaces()
    {
        bool moved;
        do
        {
            moved = false;
            bool[,] visited = new bool[width, grid.GetLength(1)];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (!visited[x, y] && grid[x, y] != null)
                    {
                        // Находим связную группу BFS
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
                            new Vector2Int(cell.x+1, cell.y),
                            new Vector2Int(cell.x-1, cell.y),
                            new Vector2Int(cell.x, cell.y+1),
                            new Vector2Int(cell.x, cell.y-1)
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

                        // Находим максимальное расстояние, на которое группа может упасть
                        int maxFall = int.MaxValue;
                        // Сохраняем тайлы во временный словарь, чтобы не потерять Transform после очистки
                        Dictionary<Vector2Int, Transform> tileMap = new Dictionary<Vector2Int, Transform>();
                        foreach (Vector2Int cell in group)
                        {
                            tileMap[cell] = grid[cell.x, cell.y];
                        }

                        // Очищаем старые ячейки в grid
                        foreach (Vector2Int cell in group)
                        {
                            grid[cell.x, cell.y] = null;
                        }

                        // Вычисляем расстояние падения
                        foreach (Vector2Int cell in group)
                        {
                            int dist = 0;
                            int checkY = cell.y - 1;
                            while (checkY >= 0)
                            {
                                // Если клетка ниже пуста или входит в ту же группу, можно упасть дальше
                                if (grid[cell.x, checkY] == null || group.Contains(new Vector2Int(cell.x, checkY)))
                                {
                                    dist++;
                                    checkY--;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (dist < maxFall)
                                maxFall = dist;
                            if (maxFall == 0)
                                break;
                        }

                        if (maxFall > 0 && maxFall != int.MaxValue)
                        {
                            // Двигаем группу вниз на maxFall
                            moved = true;
                            // Сортируем группу по возрастанию y (снизу вверх)
                            group.Sort((a, b) => a.y.CompareTo(b.y));
                            foreach (Vector2Int cell in group)
                            {
                                Vector2Int newCell = new Vector2Int(cell.x, cell.y - maxFall);
                                Transform t = tileMap[cell];
                                grid[newCell.x, newCell.y] = t;
                                // Обновляем мировую позицию
                                t.position = new Vector2(newCell.x, newCell.y + bottomOffset);
                            }
                        }
                        else
                        {
                            // Если упасть нельзя, просто возвращаем группу на место
                            foreach (Vector2Int cell in group)
                            {
                                grid[cell.x, cell.y] = tileMap[cell];
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
