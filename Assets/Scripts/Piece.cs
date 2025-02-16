using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// Фигура получает цвета для тайлов согласно настройке (для I-фигуры – единый цвет, для остальных – разноцветные).
/// Реализованы hard drop (по пробелу) и wall kicks при повороте. Если I-фигура не может повернуться, то она пробует сместиться на один блок вправо,
/// а затем, если необходимо, на один блок влево.
/// Поскольку pivot фигуры находится в центре, используется GetCellCoordinates для вычисления ячеек.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f;
    private float fallTimer = 0f;
    private bool _hasLanded = false;
    public float blockSize = 1f;
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Флаг для I-фигуры; установите true в инспекторе для I-фигуры
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
            // Для I-фигуры все тайлы получают один случайный цвет
            Color pieceColor = availableColors[Random.Range(0, availableColors.Length)];
            foreach (Transform child in transform)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = pieceColor;
            }
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
                    // Для I-фигуры используем стандартные смещения SRS
                    kicks = new Vector3[]
                    {
                new Vector3(0, 0, 0),
                new Vector3(-2, 0, 0),   // Отталкиваем на два блока влево
                new Vector3(1, 0, 0),
                new Vector3(-2, -1, 0),
                new Vector3(1, 2, 0)
                    };
                }
                else
                {
                    // Для остальных фигур добавляем вариант смещения на два блока влево
                    kicks = new Vector3[]
                    {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-2, 0, 0),   // Новый вариант: смещение на два блока влево
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

    void SnapPosition()
    {
        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), transform.position.z);
    }

    Vector2 GetCellCoordinates(Vector2 pos)
    {
        return new Vector2(Mathf.Floor(pos.x + 0.5f), Mathf.Floor(pos.y + 0.5f));
    }

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

    void AssignTileColors()
    {
        Transform[] children = GetComponentsInChildren<Transform>();
        List<Transform> tiles = new List<Transform>();
        foreach (Transform t in children)
        {
            if (t != transform)
                tiles.Add(t);
        }
        // Сортировка по локальным координатам (сначала по Y, затем по X)
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });

        // Если количество тайлов меньше или равно количеству доступных цветов, пытаемся назначить уникальные цвета
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
            // Если тайлов больше 4, назначаем уникальные для первых 4, а для остальных случайные
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

        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer sr = tiles[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = colorsToAssign[i];
        }
    }
}
