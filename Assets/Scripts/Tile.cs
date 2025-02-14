using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт для отдельного блока, назначающий ему случайный цвет.
/// </summary>
public class Tile : MonoBehaviour
{
    // Массив возможных цветов (назначается через инспектор)
    public Color[] possibleColors;

    private void Start()
    {
        if (possibleColors != null && possibleColors.Length > 0)
        {
            int index = Random.Range(0, possibleColors.Length);
            GetComponent<SpriteRenderer>().color = possibleColors[index];
        }
    }
}
