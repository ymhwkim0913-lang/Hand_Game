using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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

    // 최초 게임당 시간
    private const float MAXpartTime = 3.0f;

    // 각 게임당 시간
    public float partTime { get; private set; } = 3.0f;

    private float minigameTime = 3.0f;
    // 성공한 게임 수
    private int clearedGame = 0;

    public static string missionString;
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
    [SerializeField]
    private TextMeshProUGUI missionText;
    [SerializeField]
    private RectTransform timeGauge;

    #endregion

    void Awake() {
        Instance = this;
    }
    void Start() {
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
    /// 미니게임의 제한시간을 get합니다.
    /// </summary>
    /// <returns> 미니게임의 제한 시간 </returns>

    /// <summary>
    /// 미션이 애니메이션으로 나타납니다.
    /// </summary>
    public void MissionCall() {

        missionText.text = missionString;

        missionText.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

        LeanTween.value(gameObject, 0f, 1f, 0.2f).
        setOnUpdate((float val) => {
                missionText.alpha = val;
            });

        LeanTween.scale(missionText.gameObject, new Vector3(1f, 1f, 1f), 0.1f).
            setEase(LeanTweenType.easeOutBack);

        LeanTween.value(gameObject, 2.5f, 0f, 1f).
        setOnUpdate((float val) => {
            missionText.alpha = val;
        });

    }

    /// <summary>
    /// 게임 시작 전
    /// </summary>
    public void opening() {
        playerHand_effect.Play();
        oppoentHand_effect.Play();
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

        //// ★ 현재 게임이 "참참참"일 때만 승패를 판정합니다.
        //if (nowGame == 1)
        //{
        //    // 1. 상대방의 손이 무엇인지 가져옵니다. (3:좌, 4:중, 5:우)
        //    int oppoHand = checkOppoentHand();
        //
        //    // 2. "val" 값이 플레이어의 손입니다. (3:좌, 4:중, 5:우)
        //    // 3. 1단계에서 저장해둔 미션(isFollowMission)을 확인합니다.
        //
        //    if (isFollowMission == true) // "따라하기" 미션일 때
        //    {
        //        if (val == oppoHand) gameClear(); // (성공) 따라했으면 승리!
        //        else gameFail();                 // (실패) 못 따라했으면 패배!
        //    }
        //    else // "피하기" 미션일 때
        //    {
        //        if (val != oppoHand) gameClear(); // (성공) 피했으면 승리!
        //        else gameFail();                 // (실패) 못 피했으면 패배!
        //    }
        //}
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

    public void GameStart() {

        // 브금 재생
        AudioManager.Instance.playSFX(1);
        isGame = true;
        MissionCall();
    }

    // 게임 성공 시
    public void gameClear()
    {
        _Combo++;
        clearedGame++;
        addScore();
        remainTime += (5.0f - (survivalTime / 20.0f));

        pickGame();
    }

    // 게임 실패 시
    public void gameFail()
    {
        // 1. 벌칙 적용
        _Combo = 0;
        remainTime -= 10.0f;

        // 3. 다음 게임으로 넘김
        pickGame();
    }

    // 게임 오버
    private void GameOver()
    {
        // [중요] isGame을 false로 바꿔서 Update()가 더 이상 timeTick()을 실행하지 못하게 함
        isGame = false;

    }

    // 점수를 증가시킵니다
    private void addScore()
    {

        float multi = scoreCurve.Evaluate(survivalTime);
        _Score += (int)((1000 * _Combo) * (multi));      // 기본 1000점 x 콤보 수 x 생존 시간 보너스(최대 3배)
        score_Text.text = _Score.ToString("N0");

    }

    private void pickGame()
    {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];

        int randInt = Random.Range(0, 3);
        
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];

        StartCoroutine(CountDown.Instance.CountDownStart(timeSet));
    }

    private void timeSet() {
        minigameTime = partTime;
    }

    // 제한시간 감소 및 텍스트 변경
    private void timeTick()
    {
        // 1. 시간 감소
        remainTime -= Time.deltaTime;
        survivalTime += Time.deltaTime;

        if (minigameTime >= 0.0f) minigameTime -= Time.deltaTime;
        partTime = MAXpartTime - (clearedGame / 10);
        partTime = Mathf.Clamp(partTime, 0.5f, 3.0f);
        float normalized = minigameTime / partTime;
        normalized = Mathf.Clamp01(normalized);
        timeGauge.sizeDelta = new Vector2(1920 * normalized, 30);

        // 2. 시간이 0 이하로 내려갔는지 '스스로' 확인
        if (remainTime <= 0.0f)
        {
            remainTime = 0.0f;
            GameOver(); // 게임 오버 처리
        }

        // 3. (시간이 0보다 클 때만) 생존 시간 & 남은 시간 텍스트 업데이트
        int minutes = (int)(survivalTime / 60);
        int seconds = (int)(survivalTime % 60);
        survivalTime_Text.text = $"{minutes}:{seconds:00}";

        if (remainTime > 10.0f) remainTime_Text.text = remainTime.ToString("F0");
        else remainTime_Text.text = remainTime.ToString("F1");
    }

}