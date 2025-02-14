using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����� ��� ���������� ������� �����.
/// ������ ����: 12 x 24.
/// ������ ���������� � ������� ������� � ��������� �������� ���������� "3 � ���".
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;
    public static int height = 24;
    // ������ ��� �������� ������ �� ������� ������ (������ ������ �������� ������ �� ����)
    public Transform[,] grid = new Transform[width, height];

    /// <summary>
    /// ���������, ��������� �� ������� ������ ����.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        return ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y >= 0);
    }

    /// <summary>
    /// ��������� ���������� ������� �� ����� �����.
    /// </summary>
    public Vector2 Round(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// ��������� ���� � ����� � ������������ � ��� ��������.
    /// </summary>
    public void AddToGrid(Transform block)
    {
        Vector2 pos = Round(block.position);
        if ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y < height)
            grid[(int)pos.x, (int)pos.y] = block;
    }

    /// <summary>
    /// �������� ����������: ���� ������ �� 3 � ����� ���������� ������ �� ����������� � ���������.
    /// ���� ����� ������ ������� � ������� ��������������� �����.
    /// </summary>
    public void CheckMatches()
    {
        // ������ ������, ������� ����� �������.
        List<Transform> tilesToRemove = new List<Transform>();

        // �������� �� ���� ������� ����.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Transform tile = grid[x, y];
                if (tile != null)
                {
                    Color tileColor = tile.GetComponent<SpriteRenderer>().color;

                    // �������� ��������������� ����������.
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

                    // �������� ������������� ����������.
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

        // ������� ��������� ����������.
        foreach (Transform t in tilesToRemove)
        {
            Vector2 pos = Round(t.position);
            if ((int)pos.x >= 0 && (int)pos.x < width && (int)pos.y < height)
                grid[(int)pos.x, (int)pos.y] = null;
            Destroy(t.gameObject);
        }

        // ����� ��������� ������� ������, ���� �� ������, ����� ��� ��������� ������� ���������� �������.
        StartCoroutine(FillEmptySpaces());
    }

    /// <summary>
    /// ��������, ������� ���������� ����� ���� ������ ������ ������ ����.
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
                            // ���������� ���� � ����� �������
                            grid[x, y].position = new Vector2(x, y);
                            break;
                        }
                    }
                }
            }
        }
    }
}
