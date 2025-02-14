using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер игры отвечает за спавн фигур.
/// Фигуры могут быть: стандартные тетромино (без квадрата) и пентомино «П» (из 5 блоков).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Префабы фигур")]
    // Массив префабов стандартных тетромино (например, I, J, L, T, S, Z).
    public GameObject[] tetrominoPrefabs;
    // Префаб пентомино «П».
    public GameObject pentominoPrefab;

    /// <summary>
    /// Спавнит следующую фигуру в верхней части игрового поля.
    /// </summary>
    public void SpawnNextPiece()
    {
        // Общее количество фигур (тетромино + пентомино).
        int totalPieces = tetrominoPrefabs.Length + 1;
        int index = Random.Range(0, totalPieces);
        GameObject pieceToSpawn;
        if (index < tetrominoPrefabs.Length)
            pieceToSpawn = tetrominoPrefabs[index];
        else
            pieceToSpawn = pentominoPrefab;

        // Позиция спавна: в центре по горизонтали, на верхней границе поля.
        Vector3 spawnPosition = new Vector3(Board.width / 2, Board.height, 0);
        Instantiate(pieceToSpawn, spawnPosition, Quaternion.identity);
    }

    private void Start()
    {
        SpawnNextPiece();
    }
}
