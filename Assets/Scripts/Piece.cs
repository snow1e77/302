using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт отвечает за поведение падающей фигуры:
/// – перемещение влево/вправо;
/// – вращение;
/// – автоматическое падение;
/// – фиксацию фигуры и добавление её блоков в сетку поля.
/// После установки фигуры запускается проверка совпадений "3 в ряд".
/// </summary>
public class Piece : MonoBehaviour
{
    // Время между шагами падения фигуры.
    public float fallTime = 1.0f;
    private float fallTimer = 0;
    private Board board;

    private void Start()
    {
        board = FindObjectOfType<Board>();
    }

    private void Update()
    {
        // Обработка перемещения влево.
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += new Vector3(-1, 0, 0);
            if (!IsValidPosition())
                transform.position += new Vector3(1, 0, 0);
        }
        // Обработка перемещения вправо.
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += new Vector3(1, 0, 0);
            if (!IsValidPosition())
                transform.position += new Vector3(-1, 0, 0);
        }

        // Обработка вращения фигуры (на 90° против часовой стрелки).
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (!IsValidPosition())
                transform.Rotate(0, 0, 90);
        }

        // Логика автоматического падения фигуры.
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallTime)
        {
            transform.position += new Vector3(0, -1, 0);
            // Если после перемещения фигура оказалась в недопустимой позиции (например, достигла дна)
            if (!IsValidPosition())
            {
                // Возвращаем фигуру на предыдущую позицию.
                transform.position += new Vector3(0, 1, 0);
                // Фиксируем фигуру: добавляем каждый её блок в сетку поля.
                AddToBoard();
                // Запускаем проверку совпадений.
                board.CheckMatches();
                // Спавним следующую фигуру.
                FindObjectOfType<GameManager>().SpawnNextPiece();
                // Отключаем дальнейшее управление этой фигурой.
                enabled = false;
            }
            fallTimer = 0;
        }
    }

    /// <summary>
    /// Проверяет, что все блоки фигуры находятся в допустимых позициях (в пределах поля и не заняты).
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            // Проверка, что блок внутри поля.
            if (!board.IsInsideGrid(pos))
                return false;
            // Проверка, что клетка не занята другим блоком.
            if (pos.y < Board.height && board.grid[(int)pos.x, (int)pos.y] != null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Добавляет все блоки фигуры в сетку игрового поля.
    /// </summary>
    void AddToBoard()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            if (pos.y < Board.height)
                board.grid[(int)pos.x, (int)pos.y] = child;
        }
    }
}
