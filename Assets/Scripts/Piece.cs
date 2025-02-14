using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ �������� �� ��������� �������� ������:
/// � ����������� �����/������;
/// � ��������;
/// � �������������� �������;
/// � �������� ������ � ���������� � ������ � ����� ����.
/// ����� ��������� ������ ����������� �������� ���������� "3 � ���".
/// </summary>
public class Piece : MonoBehaviour
{
    // ����� ����� ������ ������� ������.
    public float fallTime = 1.0f;
    private float fallTimer = 0;
    private Board board;

    private void Start()
    {
        board = FindObjectOfType<Board>();
    }

    private void Update()
    {
        // ��������� ����������� �����.
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += new Vector3(-1, 0, 0);
            if (!IsValidPosition())
                transform.position += new Vector3(1, 0, 0);
        }
        // ��������� ����������� ������.
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += new Vector3(1, 0, 0);
            if (!IsValidPosition())
                transform.position += new Vector3(-1, 0, 0);
        }

        // ��������� �������� ������ (�� 90� ������ ������� �������).
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (!IsValidPosition())
                transform.Rotate(0, 0, 90);
        }

        // ������ ��������������� ������� ������.
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallTime)
        {
            transform.position += new Vector3(0, -1, 0);
            // ���� ����� ����������� ������ ��������� � ������������ ������� (��������, �������� ���)
            if (!IsValidPosition())
            {
                // ���������� ������ �� ���������� �������.
                transform.position += new Vector3(0, 1, 0);
                // ��������� ������: ��������� ������ � ���� � ����� ����.
                AddToBoard();
                // ��������� �������� ����������.
                board.CheckMatches();
                // ������� ��������� ������.
                FindObjectOfType<GameManager>().SpawnNextPiece();
                // ��������� ���������� ���������� ���� �������.
                enabled = false;
            }
            fallTimer = 0;
        }
    }

    /// <summary>
    /// ���������, ��� ��� ����� ������ ��������� � ���������� �������� (� �������� ���� � �� ������).
    /// </summary>
    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = board.Round(child.position);
            // ��������, ��� ���� ������ ����.
            if (!board.IsInsideGrid(pos))
                return false;
            // ��������, ��� ������ �� ������ ������ ������.
            if (pos.y < Board.height && board.grid[(int)pos.x, (int)pos.y] != null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// ��������� ��� ����� ������ � ����� �������� ����.
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
