using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
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
