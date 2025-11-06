using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class InGameManager : MonoBehaviour
{

    #region 싱글톤 및 기타 (건들필요X)
    ///////
    public static InGameManager Instance { get; private set; }
    public AnimationCurve scoreCurve;
    ///////
    #endregion

    #region ★ 게임에 필요한 변수들

    //게임 목록
    private string[] gameList = new string[] { "가위바위보", "참참참", "제로게임" };
    // 손 사진의 목록
    private string[] handList = new string[] { "rock", "scissors", "paper",       // 바위가위보
                                             "ccc_left", "ccc_mid", "ccc_right",  // 참참참(좌중우)
                                             "zero_0", "zero_1", "zero_2"};      // 제로(0, 1, 2)

    // 게임 점수 및 콤보 수
    private int _Score = 0;
    private int _Combo = 0;
    // 게임 남은 시간
    private float remainTime = 60.0f;
    // 생존 시간
    private float survivalTime = 0.0f;

    private bool isGame = false;        // 게임 중인지
    private int nowGame = 0;    // 현재 게임 (0 : 가위바위보, 1 : 참참참, 2 : 제로게임)
    private int nextGame = 1;   // 다음 게임 (0 : 가위바위보, 1 : 참참참, 2 : 제로게임)

    private int playerHand = 0;
    private int oppoentHand = 0;

    #endregion

    #region ★ 하이라키창에서 선택할 것들 (건들 필요X)

    [SerializeField]
    private TextMeshProUGUI survivalTime_Text;
    [SerializeField]
    private TextMeshProUGUI remainTime_Text;
    [SerializeField]
    private TextMeshProUGUI nextGame_Text;
    [SerializeField]
    private TextMeshProUGUI nowGame_Text;
    [SerializeField]
    private TextMeshProUGUI score_Text;
    [SerializeField]
    private ParticleSystem playerHand_effect;
    [SerializeField]
    private ParticleSystem oppoentHand_effect;

    #endregion

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // [중요] 게임 시작 시 텍스트 초기화
        UpdateTimerText(remainTime);

        // ▼▼▼▼▼ [ 버그 수정! ] ▼▼▼▼▼
        // 게임을 시작 상태로 만듭니다. (이게 없어서 시간이 안 흘렀습니다)
        isGame = true;
        // ▲▲▲▲▲ [ 버그 수정! ] ▲▲▲▲▲
    }

    void Update()
    {

        // isGame이 false면 (게임 오버) 아무것도 하지 않음
        if (!isGame) return;

        // 1. 시간 흐름 및 게임 오버 체크
        timeTick();

        // 2. 키보드 테스트 입력
        if (Input.GetKeyDown(KeyCode.A)) oppoentHandChange(0);
        if (Input.GetKeyDown(KeyCode.S)) oppoentHandChange(1);
        if (Input.GetKeyDown(KeyCode.D)) oppoentHandChange(2);

        if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(0);
        if (Input.GetKeyDown(KeyCode.X)) playerHandChange(1);
        if (Input.GetKeyDown(KeyCode.C)) playerHandChange(2);

        if (Input.GetKeyDown(KeyCode.Space)) gameClear();
    }

    /// <summary>
    /// 플레이어의 손을 바꿉니다
    /// </summary>
    public void playerHandChange(int val)
    {

        if (playerHand == val) return;

        playerHand_effect.Clear();
        Texture2D loadtexture_player = Resources.Load<Texture2D>("Hand/" + handList[val]);

        var shapeModule_p = playerHand_effect.shape;
        playerHand = val;
        shapeModule_p.texture = loadtexture_player;

        playerHand_effect.Emit(15000);
    }

    /// <summary>
    /// 상대방의 손을 바꿉니다.
    /// </summary>
    public void oppoentHandChange(int val)
    {
        if (oppoentHand == val) return;
        oppoentHand_effect.Clear();

        Texture2D loadtexture_oppoent = Resources.Load<Texture2D>("Hand/" + handList[val]);

        var shapeModule_o = oppoentHand_effect.shape;
        oppoentHand = val;
        shapeModule_o.texture = loadtexture_oppoent;

        oppoentHand_effect.Emit(15000);
    }

    /// <summary>
    /// 상대방의 손을 확인합니다.
    /// </summary>
    public int checkOppoentHand()
    {
        return oppoentHand;
    }

    // 게임 성공 시
    public void gameClear()
    {

        Debug.Log("--- GAME CLEAR ---");

        // 1. 리워드 적용 (원래 로직)
        _Combo++;
        addScore();
        remainTime += (5.0f - (survivalTime / 30.0f));

        // 2. 텍스트 즉시 새로고침
        UpdateTimerText(remainTime);

        // 3. 다음 게임으로 넘김
        pickGame();
    }

    // 게임 실패 시
    public void gameFail()
    {

        Debug.Log("--- GAME FAIL ---");

        // 1. 벌칙 적용
        _Combo = 0;
        remainTime -= 10.0f;

        // 2. 텍스트 즉시 새로고침
        UpdateTimerText(remainTime);

        // 3. 다음 게임으로 넘김
        pickGame();
    }




    /////////////////////////////////////// 이 아래는 공개 함수가 아님 (건들필요X) ///////////////////////////////////////

    // 게임 오버
    private void endGame()
    {
        // [중요] isGame을 false로 바꿔서 Update()가 더 이상 timeTick()을 실행하지 못하게 함
        isGame = false;

        // [중요] 시간이 0 이하가 됐으므로 텍스트를 0으로 고정
        UpdateTimerText(0);
    }

    // 점수를 증가시킵니다
    private void addScore()
    {

        float multi = scoreCurve.Evaluate(survivalTime);
        _Score += (int)((1000 * _Combo) * (multi));      // 기본 1000점 x 콤보 수 x 생존 시간 보너스(최대 3배)
        score_Text.text = _Score.ToString("N0");

    }

    // 게임을 랜덤으로 뽑음
    private void pickGame()
    {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];

        int randInt = Random.Range(0, 3);
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];
    }


    // 제한시간 감소 및 텍스트 변경
    private void timeTick()
    {

        // 1. 시간 감소
        remainTime -= Time.deltaTime;
        survivalTime += Time.deltaTime;

        // 2. 시간이 0 이하로 내려갔는지 '스스로' 확인
        if (remainTime <= 0.0f)
        {
            endGame(); // 게임 오버 처리
            return;    // 텍스트 업데이트 등을 막고 즉시 종료
        }

        // 3. (시간이 0보다 클 때만) 생존 시간 텍스트 업데이트
        int minutes = (int)(survivalTime / 60);
        int seconds = (int)(survivalTime % 60);
        survivalTime_Text.text = $"{minutes}:{seconds:00}";

        // 4. (시간이 0보다 클 때만) 남은 시간 텍스트 업데이트
        UpdateTimerText(remainTime);
    }

    /// <summary>
    /// 남은 시간(remainTime) 텍스트를 즉시 업데이트합니다.
    /// </summary>
    private void UpdateTimerText(float time)
    {
        // 10초 초과면 소수점 없이
        if (time > 10.0f)
        {
            remainTime_Text.text = time.ToString("F0");
        }
        // 0~10초 사이면 소수점 1자리
        else if (time > 0.0f)
        {
            remainTime_Text.text = time.ToString("F1");
        }
        // 0 이하이면 "0.0"으로 고정
        else
        {
            remainTime_Text.text = "0.0";
        }
    }
}