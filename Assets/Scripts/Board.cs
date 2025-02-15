using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт для управления игровым полем.
/// Нижняя граница (дно) поднята на bottomOffset юнитов.
/// Допустимые мировые Y-координаты для блоков начинаются с bottomOffset.
/// При фиксации блоков индекс строки = (worldY - bottomOffset).
/// </summary>
public class Board : MonoBehaviour
{
    public static int width = 12;    // Количество ячеек по X
    public static int height = 24;   // Количество ячеек по Y

    // Поднимаем дно поля на 1 юнит – допустимые Y-координаты начинаются с 1
    public int bottomOffset = 1;

    // Сетка игрового поля. Индекс по Y вычисляется как (worldY - bottomOffset)
    public Transform[,] grid = new Transform[width, height];

    /// <summary>
    /// Проверяет, находится ли мировая позиция pos внутри поля.
    /// Допустимо, если x от 0 до width-1, а y ≥ bottomOffset.
    /// </summary>
    public bool IsInsideGrid(Vector2 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        return (x >= 0 && x < width && y >= bottomOffset);
    }

    /// <summary>
    /// Округляет позицию до целых значений.
    /// </summary>
    public Vector2 Round(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    /// <summary>
    /// Фиксирует блок, переводя мировую координату в индекс строки: gridY = worldY - bottomOffset.
    /// </summary>
    public void AddToGrid(Transform block)
    {
        Vector2 pos = Round(block.position);
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y) - bottomOffset; // например, если pos.y = 1 и bottomOffset = 1, то y = 0
        if (x >= 0 && x < width && y >= 0 && y < height)
            grid[x, y] = block;
    }

    /// <summary>
    /// Простейшая проверка совпадений по горизонтали и вертикали.
    /// Если найдено 3 и более смежных блоков одинакового цвета, они удаляются.
    /// (Этот метод можно доработать под конкретную логику игры.)
    /// </summary>
    public void CheckMatches()
    {
        List<Transform> tilesToRemove = new List<Transform>();

        // Пробегаем по всей сетке
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Transform tile = grid[x, y];
                if (tile != null)
                {
                    Color tileColor = tile.GetComponent<SpriteRenderer>().color;

                    // Горизонтальная проверка
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
                        foreach (Transform t in matchHorizontal)
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                    }

                    // Вертикальная проверка
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
                        foreach (Transform t in matchVertical)
                            if (!tilesToRemove.Contains(t))
                                tilesToRemove.Add(t);
                    }
                }
            }
        }

        // Удаляем совпавшие блоки
        foreach (Transform t in tilesToRemove)
        {
            Vector2 pos = Round(t.position);
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y) - bottomOffset;
            if (x >= 0 && x < width && y >= 0 && y < height)
                grid[x, y] = null;
            Destroy(t.gameObject);
        }

        // Отключаем опускание блоков – блоки остаются на своих местах (в воздухе)
        // Если хотите потом вернуть падение, можно вызвать FillEmptySpaces()
        // StartCoroutine(FillEmptySpaces());
    }

    /// <summary>
    /// Отключённый метод опускания блоков.
    /// Если оставить пустым, блоки не будут перемещаться вниз для заполнения пустот.
    /// </summary>
    IEnumerator FillEmptySpaces()
    {
        yield return null;
        // Метод не опускает блоки – оставляем его пустым
    }
}
