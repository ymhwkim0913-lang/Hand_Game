using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    // 배경이 움직이는 속도. public으로 선언해서 유니티 에디터에서 값을 바꿀 수 있습니다.
    public float scrollSpeed = 30f;

    private RectTransform rectTransform; // 배경 그룹의 RectTransform
    private float imageWidth; // 이미지 하나의 폭

    void Start()
    {
        // 1. 이 스크립트가 붙어있는 오브젝트(ScrollingBackground)의 RectTransform을 가져옵니다.
        rectTransform = GetComponent<RectTransform>();

        // 2. 자식 오브젝트(BG_1)의 너비를 가져와서 imageWidth 변수에 저장합니다.
        //    정확한 계산을 위해 캔버스의 스케일 값도 곱해줍니다.
        imageWidth = transform.GetChild(0).GetComponent<RectTransform>().rect.width * transform.root.localScale.x;
    }

    void Update()
    {
        // 3. 매 프레임마다 왼쪽 방향(Vector2.left)으로 scrollSpeed 만큼 배경을 이동시킵니다.
        rectTransform.anchoredPosition += Vector2.left * scrollSpeed * Time.deltaTime;

        // 4. 만약 배경 그룹이 왼쪽으로 이미지 하나의 너비만큼(-imageWidth) 이동했다면,
        if (rectTransform.anchoredPosition.x <= -imageWidth)
        {
            // 5. 다시 원래 위치(0, 0)로 순간이동 시켜서 무한히 반복되는 것처럼 보이게 합니다.
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}