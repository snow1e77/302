using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой, как в Тетрисе.
/// После того, как фигура не может дальше падать, её тайлы фиксируются в сетке,
/// затем вызывается проверка совпадений, запускается гравитация и спавнится новая фигура.
/// Каждая фигура получает один случайный цвет для всех своих тайлов, 
/// выбранный из набора ровно из 4 цветов, при этом алгоритм пытается, чтобы соседние тайлы в фигуре не имели одинаковый цвет более двух раз подряд.
/// Реализована также возможность hard drop по пробелу.
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

        // Получаем список всех дочерних тайлов (исключая корневой объект фигуры)
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

        // Назначаем цвет каждому тайлу с учетом того, чтобы соседние тайлы не имели более двух одинаковых цветов подряд.
        // Для этого, для каждого тайла, смотрим уже обработанных соседей (по локальным координатам, где расстояние равно blockSize)
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            List<Color> neighborColors = new List<Color>();
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
            // Подсчитываем количество вхождений каждого цвета среди соседей
            Dictionary<Color, int> colorFreq = new Dictionary<Color, int>();
            foreach (Color col in availableColors)
                colorFreq[col] = 0;
            foreach (Color col in neighborColors)
            {
                if (colorFreq.ContainsKey(col))
                    colorFreq[col]++;
            }
            // Формируем список кандидатов: разрешаем тот цвет, который встречается меньше 2 раз
            List<Color> candidates = new List<Color>();
            foreach (Color col in availableColors)
            {
                if (colorFreq[col] < 2)
                    candidates.Add(col);
            }
            Color chosen;
            if (candidates.Count > 0)
                chosen = candidates[Random.Range(0, candidates.Count)];
            else
            {
                // Если кандидатов нет, выбираем цвет с минимальной частотой
                int minCount = int.MaxValue;
                chosen = availableColors[0];
                foreach (Color col in availableColors)
                {
                    if (colorFreq[col] < minCount)
                    {
                        minCount = colorFreq[col];
                        chosen = col;
                    }
                }
            }
            // Назначаем выбранный цвет тайлу
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = chosen;
        }
    }

    // Метод для проверки, являются ли два тайла соседями (по горизонтали или вертикали) в фигуре
    bool AreNeighbors(Transform a, Transform b)
    {
        Vector2 posA = a.localPosition;
        Vector2 posB = b.localPosition;
        Vector2 diff = new Vector2(Mathf.Abs(posA.x - posB.x), Mathf.Abs(posA.y - posB.y));
        return (Mathf.Approximately(diff.x, blockSize) && Mathf.Approximately(diff.y, 0f)) ||
               (Mathf.Approximately(diff.y, blockSize) && Mathf.Approximately(diff.x, 0f));
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
        // Поворот на -90° (по часовой стрелке)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (!IsValidPosition())
                transform.Rotate(0, 0, 90);
        }
        // Hard drop (мгновенное опускание) по пробелу
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
    /// Проверяет, что каждый тайл (позиция тайла – его нижний левый угол) находится внутри поля.
    /// Допустимые X: от 0 до Board.width - blockSize.
    /// Допустимые Y: должны быть >= board.bottomOffset.
    /// Также проверяется, что соответствующая ячейка в сетке не занята.
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            if (pos.x < 0 || pos.x + blockSize > Board.width)
                return false;
            if (pos.y < board.bottomOffset)
                return false;
            if (pos.y < board.bottomOffset + board.grid.GetLength(1))
            {
                int gridY = Mathf.FloorToInt(pos.y) - board.bottomOffset;
                if (board.grid[(int)pos.x, gridY] != null)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Фиксирует все тайлы фигуры в сетке.
    /// При добавлении индекс строки вычисляется как (worldY - board.bottomOffset).
    /// </summary>
    void AddToBoard()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y) - board.bottomOffset;
            if (x >= 0 && x < Board.width && y >= 0 && y < board.grid.GetLength(1))
                board.grid[x, y] = child;
        }
    }
}
