using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("������� �����")]
    public GameObject[] tetrominoPrefabs;
    public GameObject pentominoPrefab;

    // ��� ������ ����� ���������, ��� ���� ���������� � Y = bottomOffset,
    // ������� ������ ���������� �� ������ Board.height + bottomOffset.
    public void SpawnNextPiece()
    {
        int totalPieces = tetrominoPrefabs.Length + 1;
        int index = Random.Range(0, totalPieces);
        GameObject pieceToSpawn = (index < tetrominoPrefabs.Length) ? tetrominoPrefabs[index] : pentominoPrefab;
        Vector3 spawnPosition = new Vector3(Board.width / 2f, Board.height + FindObjectOfType<Board>().bottomOffset, 0);
        Instantiate(pieceToSpawn, spawnPosition, Quaternion.identity);
    }

    void Start()
    {
        SpawnNextPiece();
    }
}
