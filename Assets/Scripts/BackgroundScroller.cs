using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    // ����� �����̴� �ӵ�. public���� �����ؼ� ����Ƽ �����Ϳ��� ���� �ٲ� �� �ֽ��ϴ�.
    public float scrollSpeed = 30f;

    private RectTransform rectTransform; // ��� �׷��� RectTransform
    private float imageWidth; // �̹��� �ϳ��� ��

    void Start()
    {
        // 1. �� ��ũ��Ʈ�� �پ��ִ� ������Ʈ(ScrollingBackground)�� RectTransform�� �����ɴϴ�.
        rectTransform = GetComponent<RectTransform>();

        // 2. �ڽ� ������Ʈ(BG_1)�� �ʺ� �����ͼ� imageWidth ������ �����մϴ�.
        //    ��Ȯ�� ����� ���� ĵ������ ������ ���� �����ݴϴ�.
        imageWidth = transform.GetChild(0).GetComponent<RectTransform>().rect.width * transform.root.localScale.x;
    }

    void Update()
    {
        // 3. �� �����Ӹ��� ���� ����(Vector2.left)���� scrollSpeed ��ŭ ����� �̵���ŵ�ϴ�.
        rectTransform.anchoredPosition += Vector2.left * scrollSpeed * Time.deltaTime;

        // 4. ���� ��� �׷��� �������� �̹��� �ϳ��� �ʺ�ŭ(-imageWidth) �̵��ߴٸ�,
        if (rectTransform.anchoredPosition.x <= -imageWidth)
        {
            // 5. �ٽ� ���� ��ġ(0, 0)�� �����̵� ���Ѽ� ������ �ݺ��Ǵ� ��ó�� ���̰� �մϴ�.
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}