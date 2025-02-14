using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �������� ���� �������� �� ����� �����.
/// ������ ����� ����: ����������� ��������� (��� ��������) � ��������� �ϻ (�� 5 ������).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("������� �����")]
    // ������ �������� ����������� ��������� (��������, I, J, L, T, S, Z).
    public GameObject[] tetrominoPrefabs;
    // ������ ��������� �ϻ.
    public GameObject pentominoPrefab;

    /// <summary>
    /// ������� ��������� ������ � ������� ����� �������� ����.
    /// </summary>
    public void SpawnNextPiece()
    {
        // ����� ���������� ����� (��������� + ���������).
        int totalPieces = tetrominoPrefabs.Length + 1;
        int index = Random.Range(0, totalPieces);
        GameObject pieceToSpawn;
        if (index < tetrominoPrefabs.Length)
            pieceToSpawn = tetrominoPrefabs[index];
        else
            pieceToSpawn = pentominoPrefab;

        // ������� ������: � ������ �� �����������, �� ������� ������� ����.
        Vector3 spawnPosition = new Vector3(Board.width / 2, Board.height, 0);
        Instantiate(pieceToSpawn, spawnPosition, Quaternion.identity);
    }

    private void Start()
    {
        SpawnNextPiece();
    }
}
