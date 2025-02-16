using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// После того, как фигура не может опускаться дальше, её тайлы фиксируются в сетке,
/// затем запускается проверка совпадений, гравитация и спавнится новая фигура.
/// Теперь каждая фигура составлена из тайлов, каждому из которых назначается свой случайный цвет
/// из набора ровно из 4 цветов. При этом алгоритм пытается так назначать цвета, чтобы
/// соседние (по горизонтали или вертикали) тайлы не имели одного цвета более двух раз подряд.
/// Реализованы hard drop (по пробелу) и wall kicks при повороте.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f; // Интервал автоматического падения
    private float fallTimer = 0f;

    // Размер одного тайла (при использовании спрайта 256×256 с Pixels Per Unit = 256, blockSize = 1)
    public float blockSize = 1f;

    // Массив доступных цветов (ровно 4 цвета)
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();
        AssignColorsToTiles();
    }

    /// <summary>
    /// Назначает каждому тайлу внутри фигуры свой цвет.
    /// Алгоритм проходит по всем дочерним объектам (тайлам) в порядке сортировки по локальной позиции.
    /// Для каждого тайла проверяются уже обработанные соседи (по горизонтали и вертикали).
    /// Если среди соседей уже назначен один и тот же цвет два раза, этот цвет исключается из кандидатов.
    /// Если кандидатных цветов не осталось, выбирается случайный цвет из всех.
    /// </summary>
    void AssignColorsToTiles()
    {
        // Получаем список всех дочерних тайлов (исключая сам объект фигуры)
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        List<Transform> tiles = new List<Transform>();
        foreach (Transform t in allChildren)
        {
            if (t != transform)
                tiles.Add(t);
        }
        // Сортируем тайлы по локальным координатам (сначала по Y, затем по X)
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });

        // Для каждого тайла выбираем цвет с учётом соседей
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            List<Color> neighborColors = new List<Color>();

            // Проверяем уже обработанных соседей (по локальной позиции)
            for (int j = 0; j < i; j++)
            {
                Transform other = tiles[j];
                if (AreNeighbors(tile, other))
                {
                    SpriteRenderer srOther = other.GetComponent<SpriteRenderer>();
                    if (srOther != null)
                        neighborColors.Add(srOther.color);
                }
            }

            // Подсчитываем, сколько раз каждый цвет встречается среди соседей
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            foreach (Color col in availableColors)
                colorCount[col] = 0;
            foreach (Color col in neighborColors)
            {
                if (colorCount.ContainsKey(col))
                    colorCount[col]++;
            }

            // Формируем список кандидатов: разрешаем тот цвет, если он встречается меньше 2 раз
            List<Color> candidates = new List<Color>();
            foreach (Color col in availableColors)
            {
                if (colorCount[col] < 2)
                    candidates.Add(col);
            }

            Color chosen;
            if (candidates.Count > 0)
                chosen = candidates[Random.Range(0, candidates.Count)];
            else
                chosen = availableColors[Random.Range(0, availableColors.Length)];

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = chosen;
        }
    }

    // Метод для проверки, являются ли два тайла соседями (по горизонтали или вертикали) в фигуре.
    bool AreNeighbors(Transform a, Transform b)
    {
        Vector2 posA = a.localPosition;
        Vector2 posB = b.localPosition;
        float dx = Mathf.Abs(posA.x - posB.x);
        float dy = Mathf.Abs(posA.y - posB.y);
        // Соседи должны находиться на расстоянии blockSize по одной из осей и практически совпадать по другой.
        return (Mathf.Approximately(dx, blockSize) && Mathf.Approximately(dy, 0f)) ||
               (Mathf.Approximately(dy, blockSize) && Mathf.Approximately(dx, 0f));
    }

    void Update()
    {
        // Перемещение влево
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += Vector3.left;
            if (!IsValidPosition())
                transform.position += Vector3.right;
        }
        // Перемещение вправо
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += Vector3.right;
            if (!IsValidPosition())
                transform.position += Vector3.left;
        }
        // Поворот с wall-kick
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Vector3 originalPos = transform.position;
            Quaternion originalRot = transform.rotation;
            transform.Rotate(0, 0, -90);
            if (!IsValidPosition())
            {
                // Пробуем несколько смещений
                Vector3[] kicks = new Vector3[]
                {
                    new Vector3(1,0,0),
                    new Vector3(-1,0,0),
                    new Vector3(0,1,0),
                    new Vector3(0,-1,0)
                };
                bool valid = false;
                foreach (Vector3 kick in kicks)
                {
                    transform.position = originalPos + kick;
                    if (IsValidPosition())
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    transform.position = originalPos;
                    transform.rotation = originalRot;
                }
            }
        }
        // Hard drop по пробелу
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (IsValidPosition())
            {
                transform.position += Vector3.down;
            }
            transform.position += Vector3.up;
            AddToBoard();
            board.CheckMatches();
            FindObjectOfType<GameManager>().SpawnNextPiece();
            enabled = false;
        }
        // Автоматическое падение
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallTime)
        {
            transform.position += Vector3.down;
            if (!IsValidPosition())
            {
                transform.position += Vector3.up;
                AddToBoard();
                board.CheckMatches();
                FindObjectOfType<GameManager>().SpawnNextPiece();
                enabled = false;
            }
            fallTimer = 0f;
        }
    }

    /// <summary>
    /// Получает координаты ячейки для позиции, учитывая, что pivot тайла = (0.5, 0.5).
    /// Вычисление происходит как Floor(pos + 0.5).
    /// </summary>
    Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, что каждый тайл (его позиция, приведённая к ячейке) находится внутри поля.
    /// Допустимые X: от 0 до Board.width - 1.
    /// Допустимые Y: >= board.bottomOffset.
    /// Также проверяется, что соответствующая ячейка сетки не занята.
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 cell = GetCellCoordinates(child.position);
            if (cell.x < 0 || cell.x >= Board.width)
                return false;
            if (cell.y < board.bottomOffset)
                return false;
            if (cell.y < board.bottomOffset + board.grid.GetLength(1))
            {
                int gridY = (int)cell.y - board.bottomOffset;
                if (board.grid[(int)cell.x, gridY] != null)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Фиксирует все тайлы фигуры в сетке.
    /// Индекс строки вычисляется как (cell.y - board.bottomOffset).
    /// </summary>
    void AddToBoard()
    {
        foreach (Transform child in transform)
        {
            Vector2 cell = GetCellCoordinates(child.position);
            int x = (int)cell.x;
            int y = (int)cell.y - board.bottomOffset;
            if (x >= 0 && x < Board.width && y >= 0 && y < board.grid.GetLength(1))
                board.grid[x, y] = child;
        }
    }
}
