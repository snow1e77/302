using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// Когда фигура не может дальше опускаться, её тайлы фиксируются в сетке,
/// запускается проверка совпадений, гравитация и спавнится новая фигура.
/// Для обычных фигур каждый тайл получает свой случайный цвет из набора 4 цветов,
/// а для I-фигуры все тайлы получают один случайный цвет.
/// Реализованы hard drop (по пробелу) и wall kicks при повороте.
/// Wall kick для I-фигуры: если после поворота у правой границы нет места,
/// фигура пробует сместиться на два блока влево.
/// Поскольку pivot фигуры находится в центре, для вычисления ячеек используется GetCellCoordinates.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f;
    private float fallTimer = 0f;
    private bool _hasLanded = false;

    public float blockSize = 1f;
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Флаг, указывающий, что фигура имеет форму I. Для I-фигуры isIShape = true (задаётся в инспекторе).
    public bool isIShape = false;

    private Board board;

    void Start()
    {
        board = FindObjectOfType<Board>();
        if (!isIShape)
        {
            AssignTileColors();
        }
        else
        {
            Color pieceColor = availableColors[Random.Range(0, availableColors.Length)];
            foreach (Transform child in transform)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = pieceColor;
            }
        }
    }

    /// <summary>
    /// Назначает каждому тайлу внутри фигуры свой случайный цвет.
    /// Если тайлов меньше или равно 4, все цвета уникальны; если больше, для оставшихся выбирается случайный цвет.
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
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });
        List<Color> colorsToAssign = new List<Color>();
        if (tiles.Count <= availableColors.Length)
        {
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
            for (int i = 0; i < availableColors.Length; i++)
                colorsToAssign.Add(availableColors[i]);
            for (int i = availableColors.Length; i < tiles.Count; i++)
                colorsToAssign.Add(availableColors[Random.Range(0, availableColors.Length)]);
        }
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
                    // Для I-фигуры: если у правой границы нет места, пробуем смещение на два блока влево
                    kicks = new Vector3[]
                    {
                        new Vector3(0,0,0),
                        new Vector3(1,0,0),
                        new Vector3(-2,0,0)  // смещение на 2 блока влево
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
    /// Приводит позицию фигуры к целым числам, чтобы избежать дробных значений.
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
    /// Допустимые X: от 0 до Board.width - 1; Y: >= board.bottomOffset.
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
    /// Индекс строки = (cell.y - board.bottomOffset).
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
