using UnityEngine;

public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f;
    private float fallTimer = 0f;

    // Размер одного блока (при Pixels Per Unit = 256 для 256×256 спрайта – blockSize = 1)
    public float blockSize = 1f;

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();
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

    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            if (pos.x < 0 || pos.x + blockSize > Board.width)
                return false;
            if (pos.y < board.bottomOffset)
                return false;
            if (pos.y < Board.height + board.bottomOffset)
            {
                int gridY = Mathf.RoundToInt(pos.y) - board.bottomOffset;
                if (board.grid[(int)pos.x, gridY] != null)
                    return false;
            }
        }
        return true;
    }

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
