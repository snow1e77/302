using UnityEngine;

/// <summary>
/// Управляет падающей фигурой, как в Тетрисе.
/// Когда фигура не может опуститься дальше, она фиксируется на поле,
/// вызывается проверка совпадений, и спавнится следующая фигура.
/// Все тайлы фигуры получают один случайный цвет (из набора 4 цветов).
/// Также реализован мгновенный hard drop по нажатию пробела.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f;
    private float fallTimer = 0f;

    // Размер одного тайла (если спрайт 256×256 с Pixels Per Unit = 256, то blockSize = 1)
    public float blockSize = 1f;

    // Массив доступных цветов (4 цвета)
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();
        // Генерируем один случайный цвет для всей фигуры
        Color pieceColor = availableColors[Random.Range(0, availableColors.Length)];
        foreach (Transform child in transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = pieceColor;
            }
        }
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
        // Hard drop по нажатию пробела
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
    /// Проверяет, что каждый тайл (считая, что его позиция – нижний левый угол) находится внутри поля.
    /// Допустимые X: от 0 до Board.width - blockSize.
    /// Допустимые Y: должны быть >= board.bottomOffset.
    /// Также проверяется, что соответствующая ячейка в сетке не занята.
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            // Проверка по горизонтали
            if (pos.x < 0 || pos.x + blockSize > Board.width)
                return false;
            // Проверка по вертикали
            if (pos.y < board.bottomOffset)
                return false;
            // Если тайл находится внутри всей сетки, проверяем занятость
            if (pos.y < board.bottomOffset + board.grid.GetLength(1))
            {
                int gridY = Mathf.RoundToInt(pos.y) - board.bottomOffset;
                if (board.grid[(int)pos.x, gridY] != null)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Фиксирует все тайлы фигуры в сетке.
    /// При добавлении индекс строки = worldY - board.bottomOffset.
    /// </summary>
    void AddToBoard()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y) - board.bottomOffset;
            if (x >= 0 && x < Board.width && y >= 0 && y < board.grid.GetLength(1))
                board.grid[x, y] = child;
        }
    }
}
