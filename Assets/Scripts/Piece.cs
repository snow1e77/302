using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// При фиксации фигуры её тайлы добавляются в сетку, затем запускается проверка совпадений и гравитация,
/// после чего спавнится новая фигура.
/// Каждая фигура состоит из разноцветных тайлов – для обычных фигур каждому тайлу назначается свой цвет,
/// для I-фигуры все тайлы получают один случайный цвет (если нужно можно изменить логику).
/// Реализованы hard drop (по пробелу) и wall kicks при повороте.
/// Добавлена тенёвая фигура (ghost piece), которая показывает, куда опустится фигура при hard drop.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f; // Интервал автоматического падения
    private float fallTimer = 0f;
    private bool _hasLanded = false;

    public float blockSize = 1f;  // Размер одного тайла (1×1)
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Флаг для I-фигуры (специальные wall kick offset'ы)
    public bool isIShape = false;

    private Board board;
    private GameObject ghostPiece; // Теневой объект

    void Start()
    {
        board = FindObjectOfType<Board>();
        // Назначаем разноцветные тайлы для фигуры
        AssignTileColors();
        CreateGhostPiece();
    }

    /// <summary>
    /// Назначает каждому тайлу внутри фигуры свой случайный цвет.
    /// Если количество тайлов меньше или равно числу доступных цветов – каждому назначается уникальный цвет,
    /// иначе первые получают уникальные, а оставшиеся – случайный цвет.
    /// </summary>
    void AssignTileColors()
    {
        Transform[] children = GetComponentsInChildren<Transform>();
        List<Transform> tiles = new List<Transform>();
        foreach (Transform t in children)
        {
            if (t != transform)
                tiles.Add(t);
        }

        int n = tiles.Count;
        List<Color> colorsToAssign = new List<Color>();
        if (n <= availableColors.Length)
        {
            List<Color> shuffled = new List<Color>(availableColors);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                Color temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }
            for (int i = 0; i < n; i++)
                colorsToAssign.Add(shuffled[i]);
        }
        else
        {
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
            for (int i = availableColors.Length; i < n; i++)
                colorsToAssign.Add(availableColors[Random.Range(0, availableColors.Length)]);
        }

        // Назначаем цвета тайлам в порядке сортировки по локальной позиции (сначала по Y, затем по X)
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });

        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer sr = tiles[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = colorsToAssign[i];
        }
    }

    /// <summary>
    /// Создает теневую фигуру (ghost piece), которая копирует текущую фигуру,
    /// делает её прозрачной и обновляет ее позицию до конечного положения hard drop.
    /// </summary>
    void CreateGhostPiece()
    {
        ghostPiece = Instantiate(gameObject, transform.position, transform.rotation);
        // Удаляем компонент Piece из ghost
        Destroy(ghostPiece.GetComponent<Piece>());
        // Обходим все SpriteRenderer в ghost и уменьшаем альфа-канал
        SpriteRenderer[] srList = ghostPiece.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srList)
        {
            Color c = sr.color;
            c.a = 0.3f;
            sr.color = c;
        }
    }

    /// <summary>
    /// Обновляет позицию теневой фигуры, вычисляя hard drop позицию.
    /// </summary>
    void UpdateGhost()
    {
        if (ghostPiece == null)
            return;
        Vector3 ghostPos = transform.position;
        // Клонируем позицию и опускаем до тех пор, пока положение допустимо
        while (IsValidPositionAt(ghostPos + Vector3.down))
        {
            ghostPos += Vector3.down;
        }
        ghostPos += Vector3.up;
        ghostPiece.transform.position = ghostPos;
        ghostPiece.transform.rotation = transform.rotation;
    }

    /// <summary>
    /// Проверка позиции pos (без смещения объекта).
    /// </summary>
    bool IsValidPositionAt(Vector3 pos)
    {
        Vector3 original = transform.position;
        transform.position = pos;
        bool valid = IsValidPosition();
        transform.position = original;
        return valid;
    }

    void Update()
    {
        if (_hasLanded)
            return;

        SnapPosition();

        UpdateGhost();

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
                        new Vector3(0,0,0),
                        new Vector3(1,0,0),
                        new Vector3(-1,0,0),
                        new Vector3(2,0,0),
                        new Vector3(-2,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,-1,0)
                    };
                }
                else
                {
                    kicks = new Vector3[]
                    {
                        new Vector3(0,0,0),
                        new Vector3(1,0,0),
                        new Vector3(-1,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,-1,0)
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
    /// Округляет позицию фигуры до ближайших целых чисел, чтобы избежать дробных координат.
    /// </summary>
    void SnapPosition()
    {
        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), transform.position.z);
    }

    /// <summary>
    /// Получает координаты ячейки для позиции, учитывая, что pivot = (0.5,0.5).
    /// Вычисляется как Floor(pos + 0.5).
    /// </summary>
    Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

    /// <summary>
    /// Проверяет, что каждый тайл фигуры (его ячейка) находится внутри поля.
    /// Допустимые X: 0 ... Board.width - 1, Y: >= board.bottomOffset.
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
    /// Используется флаг _hasLanded, чтобы избежать двойного спавна.
    /// Также уничтожается теневой объект.
    /// </summary>
    void LandPiece()
    {
        if (_hasLanded)
            return;
        _hasLanded = true;
        AddToBoard();
        board.CheckMatches();
        if (ghostPiece != null)
            Destroy(ghostPiece);
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
