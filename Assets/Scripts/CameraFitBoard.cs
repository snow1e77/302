using UnityEngine;

public class CameraFitBoard : MonoBehaviour
{
    // «адайте размеры игрового пол€ (в единицах Unity)
    // Ќапример, если поле 12 единиц по ширине и 24 по высоте:
    public float boardWidth = 12f;
    public float boardHeight = 24f;

    void Start()
    {
        // ѕолучаем компонент камеры, на котором висит этот скрипт
        Camera cam = GetComponent<Camera>();

        // –ассчитываем соотношение сторон экрана (ширина/высота)
        float screenRatio = (float)Screen.width / Screen.height;

        // –ассчитываем соотношение сторон игрового пол€
        float targetRatio = boardWidth / boardHeight;

        /*  
            ¬ ортогональной камере параметр orthographicSize определ€ет половину высоты
            видимой области в мировых единицах. “о есть видима€ высота = orthographicSize * 2.
            
            “акже видима€ ширина = orthographicSize * 2 * Camera.aspect (где Camera.aspect Ч соотношение сторон).
            
            Ќаша задача Ч чтобы игровое поле полностью помещалось в камере.
            —равниваем соотношение сторон экрана (screenRatio) с соотношением сторон пол€ (targetRatio):
        */

        // ≈сли экран шире или равен требуемому соотношению пол€:
        if (screenRatio >= targetRatio)
        {
            // “огда высота €вл€етс€ ограничивающим фактором.
            // ”станавливаем orthographicSize так, чтобы высота игрового пол€ (boardHeight) полностью поместилась.
            // “ак как видима€ высота = orthographicSize * 2, то:
            cam.orthographicSize = boardHeight / 2f;
        }
        else
        {
            // ≈сли экран уже, то ширина становитс€ ограничивающим фактором.
            // ћы знаем, что видима€ ширина = orthographicSize * 2 * screenRatio.
            // „тобы эта ширина равн€лась ширине игрового пол€, решаем:
            // boardWidth = orthographicSize * 2 * screenRatio  =>  orthographicSize = boardWidth / (2 * screenRatio)
            cam.orthographicSize = boardWidth / (2f * screenRatio);
        }

        // ”станавливаем позицию камеры так, чтобы еЄ центр совпадал с центром игрового пол€.
        // ≈сли игровое поле начинаетс€ от (0,0) и имеет размеры boardWidth x boardHeight, 
        // то его центр находитс€ в точке (boardWidth/2, boardHeight/2).
        // ќсь Z оставл€ем неизменной (обычно камера располагаетс€ с отрицательным Z, чтобы смотреть на сцену).
        transform.position = new Vector3(boardWidth / 2f, boardHeight / 2f, transform.position.z);
    }
}
