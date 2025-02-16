using UnityEngine;

/// <summary>
/// ��������� ������� �����.
/// ������ ���������� � ������������: X = Board.width/2 � Y = Board.height + board.bottomOffset,
/// �� ���� ��� ������� �������� ����.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("������� �����")]
    public GameObject[] tetrominoPrefabs;
    public GameObject pentominoPrefab;

    public void SpawnNextPiece()
    {
        int totalPieces = tetrominoPrefabs.Length + 1;
        int index = Random.Range(0, totalPieces);
        GameObject pieceToSpawn = (index < tetrominoPrefabs.Length) ? tetrominoPrefabs[index] : pentominoPrefab;
        Board board = FindObjectOfType<Board>();
        Vector3 spawnPosition = new Vector3(Board.width / 2f, Board.height + board.bottomOffset, 0);
        Instantiate(pieceToSpawn, spawnPosition, Quaternion.identity);
    }

    void Start()
    {
        SpawnNextPiece();
    }
}
