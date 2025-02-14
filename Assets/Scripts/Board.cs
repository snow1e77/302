using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  ласс дл€ управлени€ игровым полем.
/// –азмер пол€: 12 x 24.
/// ’ранит информацию о зан€тых клетках и выполн€ет проверку совпадений "3 в р€д".
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;
    public static int height = 24;
    // ћассив дл€ хранени€ ссылок на зан€тые клетки (кажда€ клетка содержит ссылку на блок)
    public Transform[,] grid = new Transform[width, height];

    /// <summary>
    /// ѕровер€ет, находитс€ ли позици€ внутри пол€.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        return ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y >= 0);
    }

    /// <summary>
    /// ќкругл€ет координаты позиции до целых чисел.
    /// </summary>
    public Vector2 Round(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// ƒобавл€ет блок в сетку в соответствии с его позицией.
    /// </summary>
    public void AddToGrid(Transform block)
    {
        Vector2 pos = Round(block.position);
        if ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y < height)
            grid[(int)pos.x, (int)pos.y] = block;
    }

    /// <summary>
    /// ѕроверка совпадений: ищем группы из 3 и более одинаковых блоков по горизонтали и вертикали.
    /// ≈сли така€ группа найдена Ц удал€ем соответствующие блоки.
    /// </summary>
    public void CheckMatches()
    {
        // —писок блоков, которые нужно удалить.
        List<Transform> tilesToRemove = new List<Transform>();

        // ѕроходим по всем клеткам пол€.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Transform tile = grid[x, y];
                if (tile != null)
                {
                    Color tileColor = tile.GetComponent<SpriteRenderer>().color;

                    // ѕроверка горизонтального совпадени€.
                    List<Transform> matchHorizontal = new List<Transform>();
                    matchHorizontal.Add(tile);
                    int xTemp = x + 1;
                    while (xTemp < width && grid[xTemp, y] != null &&
                           grid[xTemp, y].GetComponent<SpriteRenderer>().color == tileColor)
                    {
                        matchHorizontal.Add(grid[xTemp, y]);
                        xTemp++;
                    }
                    if (matchHorizontal.Count >= 3)
                    {
                        foreach (var t in matchHorizontal)
                        {
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                        }
                    }

                    // ѕроверка вертикального совпадени€.
                    List<Transform> matchVertical = new List<Transform>();
                    matchVertical.Add(tile);
                    int yTemp = y + 1;
                    while (yTemp < height && grid[x, yTemp] != null &&
                           grid[x, yTemp].GetComponent<SpriteRenderer>().color == tileColor)
                    {
                        matchVertical.Add(grid[x, yTemp]);
                        yTemp++;
                    }
                    if (matchVertical.Count >= 3)
                    {
                        foreach (var t in matchVertical)
                        {
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                        }
                    }
                }
            }
        }

        // ”дал€ем найденные совпадени€.
        foreach (Transform t in tilesToRemove)
        {
            Vector2 pos = Round(t.position);
            if ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y < height)
                grid[(int)pos.x, (int)pos.y] = null;
            Destroy(t.gameObject);
        }

        // ћожно запустить падение блоков, если вы хотите, чтобы над удалЄнными блоками опустились верхние.
        StartCoroutine(FillEmptySpaces());
    }

    /// <summary>
    ///  орутина, котора€ заставл€ет блоки выше пустых клеток падать вниз.
    /// </summary>
    IEnumerator FillEmptySpaces()
    {
        yield return new WaitForSeconds(0.1f);
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    for (int k = y + 1; k < height; k++)
                    {
                        if (grid[x, k] != null)
                        {
                            grid[x, y] = grid[x, k];
                            grid[x, k] = null;
                            // ѕеремещаем блок в новую позицию
                            grid[x, y].position = new Vector2(x, y);
                            break;
                        }
                    }
                }
            }
        }
    }
}
