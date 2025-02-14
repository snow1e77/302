using UnityEngine;

public class CameraFitBoard : MonoBehaviour
{
    // ������� ������� �������� ���� (� �������� Unity)
    // ��������, ���� ���� 12 ������ �� ������ � 24 �� ������:
    public float boardWidth = 12f;
    public float boardHeight = 24f;

    void Start()
    {
        // �������� ��������� ������, �� ������� ����� ���� ������
        Camera cam = GetComponent<Camera>();

        // ������������ ����������� ������ ������ (������/������)
        float screenRatio = (float)Screen.width / Screen.height;

        // ������������ ����������� ������ �������� ����
        float targetRatio = boardWidth / boardHeight;

        /*  
            � ������������� ������ �������� orthographicSize ���������� �������� ������
            ������� ������� � ������� ��������. �� ���� ������� ������ = orthographicSize * 2.
            
            ����� ������� ������ = orthographicSize * 2 * Camera.aspect (��� Camera.aspect � ����������� ������).
            
            ���� ������ � ����� ������� ���� ��������� ���������� � ������.
            ���������� ����������� ������ ������ (screenRatio) � ������������ ������ ���� (targetRatio):
        */

        // ���� ����� ���� ��� ����� ���������� ����������� ����:
        if (screenRatio >= targetRatio)
        {
            // ����� ������ �������� �������������� ��������.
            // ������������� orthographicSize ���, ����� ������ �������� ���� (boardHeight) ��������� �����������.
            // ��� ��� ������� ������ = orthographicSize * 2, ��:
            cam.orthographicSize = boardHeight / 2f;
        }
        else
        {
            // ���� ����� ���, �� ������ ���������� �������������� ��������.
            // �� �����, ��� ������� ������ = orthographicSize * 2 * screenRatio.
            // ����� ��� ������ ��������� ������ �������� ����, ������:
            // boardWidth = orthographicSize * 2 * screenRatio  =>  orthographicSize = boardWidth / (2 * screenRatio)
            cam.orthographicSize = boardWidth / (2f * screenRatio);
        }

        // ������������� ������� ������ ���, ����� � ����� �������� � ������� �������� ����.
        // ���� ������� ���� ���������� �� (0,0) � ����� ������� boardWidth x boardHeight, 
        // �� ��� ����� ��������� � ����� (boardWidth/2, boardHeight/2).
        // ��� Z ��������� ���������� (������ ������ ������������� � ������������� Z, ����� �������� �� �����).
        transform.position = new Vector3(boardWidth / 2f, boardHeight / 2f, transform.position.z);
    }
}
