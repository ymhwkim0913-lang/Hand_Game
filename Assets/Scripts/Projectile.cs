// --- Unity의 기본 기능을 사용하기 위한 선언 ---
using UnityEngine;

// --- 포탄(Projectile) 오브젝트에 부착되어 스스로 움직이게 만드는 스크립트 ---
public class Projectile : MonoBehaviour
{
    // public: 유니티 인스펙터 창에서 값을 직접 수정할 수 있게 해줍니다.
    public float speed = 10f; // 포탄이 날아가는 속도

    // --- 이 스크립트(가 부착된 오브젝트)가 생성될 때 단 한 번 호출되는 함수 ---
    void Start()
    {
        // Destroy(파괴할 대상, 몇 초 뒤에);
        // 이 코드는 화면 밖으로 날아간 포탄이 계속 쌓여서 게임이 느려지는 것을 방지합니다.
        Destroy(gameObject, 3f);
    }

    // --- 매 프레임마다 계속해서 호출되는 함수 ---
    void Update()
    {
        // transform.Translate(방향 * 속도 * Time.deltaTime);
        // 위 공식은 '특정 방향으로 부드럽게 이동'시키는 가장 기본적인 방법입니다.

        // - Vector2.down: 이 오브젝트의 '아래쪽' 방향 (우리가 이미지를 뒤집었기 때문에 down이 앞쪽)
        // - speed: 우리가 설정한 속도
        // - Time.deltaTime: 이전 프레임과 현재 프레임 사이의 시간 간격.
        //                  이 값을 곱해주면 컴퓨터 성능과 상관없이 모든 컴퓨터에서 동일한 속도로 움직이게 됩니다. (매우 중요!)
        transform.Translate(Vector2.down * speed * Time.deltaTime);
    }
}