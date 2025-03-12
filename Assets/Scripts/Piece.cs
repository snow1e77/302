using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет падающей фигурой (как в Тетрисе).
/// Фигура состоит из разноцветных тайлов – каждому тайлу присваивается свой случайный цвет из набора (независимо для I-фигуры).
/// Реализованы hard drop (по пробелу), wall kicks при повороте и теневой объект (ghost piece), который показывает конечное положение hard drop.
/// </summary>
public class Piece : MonoBehaviour
{
    public float fallTime = 1.0f;
    private float fallTimer = 0f;
    private bool _hasLanded = false;
    public float blockSize = 1f;
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    // Флаг для I-фигуры (для специальных wall kick offset'ов)
    public bool isIShape = false;

    private Board board;
    private GameObject ghostPiece;

    void Start()
    {
        board = FindObjectOfType<Board>();
        AssignTileColors();
        CreateGhostPiece();
    }

    /// <summary>
    /// Назначает каждому тайлу внутри фигуры свой случайный цвет.
    /// Если тайлов меньше или равно количеству доступных цветов, каждому присваивается уникальный цвет, иначе оставшиеся получают случайный.
    /// </summary>
    void AssignTileColors()
    {
        // Получаем список всех дочерних тайлов (исключая сам объект фигуры)
        Transform[] children = GetComponentsInChildren<Transform>();
        List<Transform> tiles = new List<Transform>();
        foreach (Transform t in children)
        {
            if (t != transform)
                tiles.Add(t);
        }

        int n = tiles.Count;
        List<Color> colorsToAssign = new List<Color>();

        // Если доступных цветов хватает для уникального назначения (длина массива >= число тайлов),
        // назначаем уникальные цвета
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
            // Если тайлов больше, чем цветов, для первых availableColors.Length тайлов назначаем уникальные цвета,
            // а для оставшихся – случайный цвет
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

        // Дополнительная проверка: если в последовательности получились 3 подряд одинаковых цвета,
        // меняем третий на другой, чтобы не было трёх подряд.
        for (int i = 2; i < colorsToAssign.Count; i++)
        {
            if (colorsToAssign[i].Equals(colorsToAssign[i - 1]) && colorsToAssign[i].Equals(colorsToAssign[i - 2]))
            {
                // Находим кандидата, отличный от предыдущего цвета
                foreach (Color candidate in availableColors)
                {
                    if (!candidate.Equals(colorsToAssign[i - 1]))
                    {
                        colorsToAssign[i] = candidate;
                        break;
                    }
                }
            }
        }

        // Сортируем тайлы по локальным координатам: сначала по Y, затем по X
        tiles.Sort((a, b) =>
        {
            if (a.localPosition.y != b.localPosition.y)
                return a.localPosition.y.CompareTo(b.localPosition.y);
            return a.localPosition.x.CompareTo(b.localPosition.x);
        });

        // Назначаем полученные цвета тайлам
        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer sr = tiles[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = colorsToAssign[i];
        }
    }


    /// <summary>
    /// Создает теневую фигуру (ghost piece), которая показывает конечное положение hard drop.
    /// Ghost piece – копия фигуры с уменьшенной прозрачностью.
    /// </summary>
    void CreateGhostPiece()
    {
        ghostPiece = Instantiate(gameObject, transform.position, transform.rotation);
        Destroy(ghostPiece.GetComponent<Piece>());
        SpriteRenderer[] srList = ghostPiece.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srList)
        {
            Color c = sr.color;
            c.a = 0.3f;
            sr.color = c;
        }
    }

    /// <summary>
    /// Обновляет позицию теневой фигуры, вычисляя hard drop положение.
    /// </summary>
    void UpdateGhost()
    {
        if (ghostPiece == null)
            return;
        Vector3 ghostPos = transform.position;
        while (IsValidPositionAt(ghostPos + Vector3.down))
        {
            ghostPos += Vector3.down;
        }
        ghostPos += Vector3.up;
        ghostPiece.transform.position = ghostPos;
        ghostPiece.transform.rotation = transform.rotation;
    }

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

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += Vector3.left;
            if (!IsValidPosition())
                transform.position += Vector3.right;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += Vector3.right;
            if (!IsValidPosition())
                transform.position += Vector3.left;
        }
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
        if (ghostPiece != null)
            Destroy(ghostPiece);
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
}
