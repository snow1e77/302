using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// Когда фигура не может дальше опускаться, её тайлы фиксируются в сетке,
/// затем запускается проверка совпадений, гравитация и спавнится новая фигура.
/// Каждая фигура состоит из разноцветных тайлов – каждому тайлу присваивается свой случайный цвет из набора ровно из 4 цветов.
/// Реализованы hard drop (по пробелу) и wall kicks при повороте.
/// Поскольку pivot фигуры находится в центре, используется GetCellCoordinates для вычисления ячеек.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f; // Интервал автоматического падения
    private float fallTimer = 0f;
    private bool _hasLanded = false;  // Флаг, предотвращающий двойной спавн

    public float blockSize = 1f;  // Размер одного тайла (1×1)
    // Массив доступных цветов (ровно 4 цвета)
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Флаг для I-фигуры (специальные wall kick offset'ы), но теперь для разноцветности он не влияет на назначение цветов
    public bool isIShape = false;

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();
        // Всегда назначаем разноцветные тайлы для всей фигуры, даже для I-фигуры
        AssignTileColors();
    }

    /// <summary>
    /// Назначает каждому тайлу внутри фигуры свой случайный цвет.
    /// Алгоритм сортирует тайлы по локальным координатам и присваивает цвета из набора availableColors.
    /// Если тайлов больше, чем цветов, оставшиеся тайлы получают случайный цвет.
    /// </summary>
    void AssignTileColors()
    {
        // Получаем список дочерних тайлов (исключая корневой объект фигуры)
        Transform[] children = GetComponentsInChildren<Transform>();
        List<Transform> tiles = new List<Transform>();
        foreach (Transform t in children)
        {
            if (t != transform)
                tiles.Add(t);
        }
        // Сортируем тайлы по локальным координатам: сначала по Y, затем по X
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });

        List<Color> colorsToAssign = new List<Color>();
        if (tiles.Count <= availableColors.Length)
        {
            // Если тайлов меньше или равно 4, перемешиваем и присваиваем уникальные цвета
            List<Color> shuffled = new List<Color>(availableColors);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                Color temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }
            for (int i = 0; i < tiles.Count; i++)
                colorsToAssign.Add(shuffled[i]);
        }
        else
        {
            // Если тайлов больше, первые 4 получают уникальные цвета, а остальные – случайные
            List<Color> shuffled = new List<Color>(availableColors);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                Color temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }
            for (int i = 0; i < availableColors.Length; i++)
                colorsToAssign.Add(shuffled[i]);
            for (int i = availableColors.Length; i < tiles.Count; i++)
                colorsToAssign.Add(availableColors[Random.Range(0, availableColors.Length)]);
        }

        // Назначаем цвета тайлам
        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer sr = tiles[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = colorsToAssign[i];
        }
    }

    void Update()
    {
        if (_hasLanded)
            return;

        SnapPosition();

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
        // Поворот с wall kicks
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Vector3 originalPos = transform.position;
            Quaternion originalRot = transform.rotation;
            transform.Rotate(0, 0, -90);
            if (!IsValidPosition())
            {
                Vector3[] kicks;
                if (isIShape)
                {
                    kicks = new Vector3[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(-1, 0, 0),
                        new Vector3(2, 0, 0),
                        new Vector3(-2, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(0, -1, 0)
                    };
                }
                else
                {
                    kicks = new Vector3[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(-1, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(0, -1, 0)
                    };
                }
                bool validKick = false;
                foreach (Vector3 kick in kicks)
                {
                    transform.position = originalPos + kick;
                    if (IsValidPosition())
                    {
                        validKick = true;
                        break;
                    }
                }
                if (!validKick)
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
                SnapPosition();
            }
            transform.position += Vector3.up;
            SnapPosition();
            LandPiece();
        }
        // Автоматическое падение
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallTime)
        {
            transform.position += Vector3.down;
            SnapPosition();
            if (!IsValidPosition())
            {
                transform.position += Vector3.up;
                SnapPosition();
                LandPiece();
            }
            fallTimer = 0f;
        }
    }

    /// <summary>
    /// Округляет позицию фигуры до ближайших целых чисел, чтобы избежать дробных значений.
    /// </summary>
    void SnapPosition()
    {
        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), transform.position.z);
    }

    /// <summary>
    /// Получает координаты ячейки для позиции, учитывая, что pivot = (0.5, 0.5).
    /// Вычисляется как Floor(pos + 0.5).
    /// </summary>
    Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, что каждый тайл фигуры (его ячейка) находится внутри поля.
    /// Допустимые X: 0 ... Board.width - 1.
    /// Допустимые Y: >= board.bottomOffset.
    /// Также проверяется, что соответствующая ячейка в сетке не занята.
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
    /// Фиксирует фигуру – добавляет все тайлы в сетку, запускает проверку совпадений и спавн новой фигуры.
    /// Используется флаг _hasLanded для предотвращения двойного спавна.
    /// </summary>
    void LandPiece()
    {
        if (_hasLanded)
            return;
        _hasLanded = true;
        AddToBoard();
        board.CheckMatches();
        FindObjectOfType<GameManager>().SpawnNextPiece();
        enabled = false;
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
