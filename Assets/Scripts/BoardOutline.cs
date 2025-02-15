using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BoardOutline : MonoBehaviour
{
    [Header("Board Settings")]
    // ������� �������� ���� (��� ����� ��������)
    public float boardWidth = 12f;
    public float boardHeight = 24f;

    [Header("Line Settings")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.red;

    // �������� �������: -0.5 ����� ����� � 0.5 ����� �����
    public Vector2 offset = new Vector2(-0.5f, 0.5f);

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 5; // 4 ���� + ������� � ������ �����

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // ���������� ����������� ������ ��� ��������
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // ��������� ������� ����� � ������ �������� offset:
        Vector3 bottomLeft = new Vector3(0 + offset.x, 0 + offset.y, 0);
        Vector3 bottomRight = new Vector3(boardWidth + offset.x, 0 + offset.y, 0);
        Vector3 topRight = new Vector3(boardWidth + offset.x, boardHeight + offset.y, 0);
        Vector3 topLeft = new Vector3(0 + offset.x, boardHeight + offset.y, 0);

        // ������ ������� LineRenderer ��� ��������� ��������� �����
        lineRenderer.SetPosition(0, bottomLeft);
        lineRenderer.SetPosition(1, bottomRight);
        lineRenderer.SetPosition(2, topRight);
        lineRenderer.SetPosition(3, topLeft);
        lineRenderer.SetPosition(4, bottomLeft);
    }
}
