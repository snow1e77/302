using UnityEngine;

/// <summary>
/// Скрипт для управления падающей фигурой. Фигура падает, как в тетрисе, и при фиксации вызывается 
/// метод AddToBoard(), после чего запускается проверка совпадений. При старте каждому блоку фигуры 
/// присваивается один случайный цвет.
/// </summary>
public class Piece : MonoBehaviour
{
    // Интервал падения фигуры (в секундах)
    public float fallTime = 1.0f;
    private float fallTimer = 0f;

    // Размер одного блока (если используется спрайт 256×256 с Pixels Per Unit = 256, то blockSize = 1)
    public float blockSize = 1f;

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();

        // Генерируем случайный цвет для всей фигуры
        Color pieceColor = new Color(Random.value, Random.value, Random.value);

        // Применяем случайный цвет ко всем дочерним блокам фигуры
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

        // Автоматическое падение фигуры
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallTime)
        {
            transform.position += Vector3.down;
            if (!IsValidPosition())
            {
                // Если позиция недопустима, отменяем последнее движение вверх
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
    /// Проверяет, что каждая точка (нижний левый угол каждого блока) находится внутри поля.
    /// Допустимые X: от 0 до Board.width - blockSize.
    /// Допустимые Y: должны быть >= board.bottomOffset.
    /// Также проверяется, что соответствующая ячейка сетки не занята.
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            // Проверка по горизонтали
            if (pos.x < 0 || pos.x + blockSize > Board.width)
                return false;
            // Проверка по вертикали: нижняя граница блока должна быть не ниже board.bottomOffset
            if (pos.y < board.bottomOffset)
                return false;
            // Если блок находится в видимой части поля, проверяем, что ячейка не занята
            if (pos.y < Board.height + board.bottomOffset)
            {
                int gridY = Mathf.RoundToInt(pos.y) - board.bottomOffset;
                if (board.grid[(int)pos.x, gridY] != null)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Фиксирует все дочерние блоки фигуры в сетке.
    /// При добавлении индекс строки вычисляется как (worldY - board.bottomOffset).
    /// </summary>
    void AddToBoard()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y) - board.bottomOffset;
            if (y < Board.height)
                board.grid[x, y] = child;
        }
    }
}
