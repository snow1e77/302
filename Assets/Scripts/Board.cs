using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет игровым полем. Сетка имеет размеры width x (height + extraRows).
/// Дно поля поднято на bottomOffset единиц – допустимые мировые Y начинаются с bottomOffset.
/// При фиксации тайлов индекс строки вычисляется как (worldY - bottomOffset).
/// Реализованы:
/// - Предгенерация рядов тайлов в нижней части поля (pregenRows, pregenTilePrefab, pregenColors)
/// - Проверка совпадений (3+ подряд тайлов одного цвета)
/// - Расширенная логика удаления всей связной области тайлов одного цвета (без диагоналей)
/// - Гравитация: связные группы тайлов опускаются максимально вниз за один шаг
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;       // Число столбцов
    public static int height = 24;      // Число видимых строк

    public int extraRows = 4;           // Дополнительные строки (для висящих тайлов)
    public int bottomOffset = 1;        // Подъём дна поля (мировые Y >= bottomOffset)

    // Параметры предгенерации (нижние ряды, как будто уже накоплен мусор)
    public int pregenRows = 2;          // Количество предгенерированных рядов
    public GameObject pregenTilePrefab; // Префаб тайла для предгенерации
    public Color[] pregenColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Сетка: общая высота = height + extraRows
    public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height + extraRows];
    }

    void Start()
    {
        // Генерируем предсгенерированные ряды в нижней части поля
        GeneratePreGeneratedRows();
    }

    /// <summary>
    /// Вычисляет координаты ячейки для мировой позиции.
    /// При pivot = (0.5,0.5) позиция ячейки вычисляется как Floor(pos + 0.5)
    /// </summary>
    public Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, находится ли мировая позиция pos внутри сетки.
    /// Допустимые X: от 0 до width-1; Y: от bottomOffset до bottomOffset + gridHeight - 1.
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
    /// Генерирует предсгенерированные ряды тайлов в нижней части поля.
    /// Ряды заполняются ячейками с Y от bottomOffset до bottomOffset + pregenRows - 1.
    /// Для каждого тайла выбирается случайный цвет из pregenColors. Если в ряду уже два подряд одинаковых цвета, выбирается другой.
    /// </summary>
    public void GeneratePreGeneratedRows()
    {
        if (pregenTilePrefab == null)
        {
            Debug.LogWarning("PregenTilePrefab не задан!");
            return;
        }
        if (pregenColors == null || pregenColors.Length == 0)
        {
            Debug.LogWarning("PregenColors не заданы!");
            return;
        }

        for (int y = bottomOffset; y < bottomOffset + pregenRows; y++)
        {
            List<Color> currentRowColors = new List<Color>();
            for (int x = 0; x < width; x++)
            {
                Vector3 spawnPos = new Vector3(x, y, 0);
                GameObject tileObj = Instantiate(pregenTilePrefab, spawnPos, Quaternion.identity, transform);
                SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();

                Color chosenColor = pregenColors[Random.Range(0, pregenColors.Length)];
                if (x >= 2)
                {
                    if (currentRowColors[x - 1] == currentRowColors[x - 2])
                    {
                        Color prevColor = currentRowColors[x - 1];
                        List<Color> candidateColors = new List<Color>();
                        foreach (Color c in pregenColors)
                        {
                            if (c != prevColor)
                                candidateColors.Add(c);
                        }
                        if (candidateColors.Count > 0)
                        {
                            chosenColor = candidateColors[Random.Range(0, candidateColors.Count)];
                        }
                    }
                }
                if (sr != null)
                {
                    sr.color = chosenColor;
                }
                currentRowColors.Add(chosenColor);

                int gridY = y - bottomOffset;
                if (x >= 0 && x < width && gridY >= 0 && gridY < grid.GetLength(1))
                {
                    grid[x, gridY] = tileObj.transform;
                }
            }
        }
    }

    /// <summary>
    /// Проверяет совпадения по горизонтали и вертикали для всей сетки.
    /// Если обнаружена цепочка (>=3 подряд тайлов одного цвета), запускается расширенная логика удаления:
    /// вызывается RemoveConnectedTiles, которая удаляет всю связную область тайлов данного цвета (без диагоналей).
    /// После удаления запускается гравитация (FillEmptySpaces) и через небольшую задержку повторная проверка совпадений.
    /// </summary>
    public void CheckMatches()
    {
        bool foundMatch = false;
        int gridHeight = grid.GetLength(1);
        for (int x = 0; x < width && !foundMatch; x++)
        {
            for (int y = 0; y < gridHeight && !foundMatch; y++)
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
                        RemoveConnectedTiles(matchH[0], col);
                        foundMatch = true;
                        break;
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
                        RemoveConnectedTiles(matchV[0], col);
                        foundMatch = true;
                        break;
                    }
                }
            }
        }

        if (foundMatch)
        {
            StartCoroutine(FillEmptySpaces());
            Invoke("DelayedCheckMatches", 0.2f);
        }
    }

    void DelayedCheckMatches()
    {
        CheckMatches();
    }

    /// <summary>
    /// Удаляет всю связную область тайлов, смежных по вертикали и горизонтали (без диагоналей),
    /// имеющих заданный цвет, начиная с стартового тайла.
    /// </summary>
    private void RemoveConnectedTiles(Transform start, Color color)
    {
        Vector2 cell = GetCellCoordinates(start.position);
        int startX = (int)cell.x;
        int startY = (int)cell.y - bottomOffset;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, grid.GetLength(1)];
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Transform t = grid[current.x, current.y];
            if (t != null)
            {
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.color.Equals(color))
                {
                    grid[current.x, current.y] = null;
                    Destroy(t.gameObject);

                    Vector2Int[] neighbors = new Vector2Int[]
                    {
                        new Vector2Int(current.x + 1, current.y),
                        new Vector2Int(current.x - 1, current.y),
                        new Vector2Int(current.x, current.y + 1),
                        new Vector2Int(current.x, current.y - 1)
                    };

                    foreach (Vector2Int nb in neighbors)
                    {
                        if (nb.x >= 0 && nb.x < width && nb.y >= 0 && nb.y < grid.GetLength(1))
                        {
                            if (!visited[nb.x, nb.y] && grid[nb.x, nb.y] != null)
                            {
                                SpriteRenderer nsr = grid[nb.x, nb.y].GetComponent<SpriteRenderer>();
                                if (nsr != null && nsr.color.Equals(color))
                                {
                                    visited[nb.x, nb.y] = true;
                                    queue.Enqueue(nb);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Гравитация: каждая связная группа тайлов опускается максимально вниз за один шаг.
    /// Используется BFS для нахождения группы, затем вычисляется максимально возможное падение для группы,
    /// и группа перемещается вниз на это число ячеек.
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
                        // Собираем связную группу тайлов через BFS
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

                        // Вычисляем максимально возможное падение для всей группы
                        int maxFall = int.MaxValue;
                        Dictionary<Vector2Int, Transform> tileMap = new Dictionary<Vector2Int, Transform>();
                        foreach (Vector2Int cell in group)
                        {
                            tileMap[cell] = grid[cell.x, cell.y];
                        }
                        foreach (Vector2Int cell in group)
                        {
                            grid[cell.x, cell.y] = null;
                        }
                        foreach (Vector2Int cell in group)
                        {
                            int fallDist = 0;
                            int checkY = cell.y - 1;
                            while (checkY >= 0)
                            {
                                if (grid[cell.x, checkY] == null || group.Contains(new Vector2Int(cell.x, checkY)))
                                {
                                    fallDist++;
                                    checkY--;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (fallDist < maxFall)
                                maxFall = fallDist;
                            if (maxFall == 0)
                                break;
                        }
                        if (maxFall > 0 && maxFall != int.MaxValue)
                        {
                            group.Sort((a, b) => a.y.CompareTo(b.y));
                            foreach (Vector2Int cell in group)
                            {
                                Vector2Int newCell = new Vector2Int(cell.x, cell.y - maxFall);
                                grid[newCell.x, newCell.y] = tileMap[cell];
                                tileMap[cell].position = new Vector2(newCell.x, newCell.y + bottomOffset);
                            }
                            moved = true;
                        }
                        else
                        {
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
