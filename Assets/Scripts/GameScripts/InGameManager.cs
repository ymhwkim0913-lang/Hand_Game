using TMPro;
using UnityEngine;

public class InGameManager : MonoBehaviour
{
    #region 싱글톤 및 기타
    public static InGameManager Instance { get; private set; }
    public AnimationCurve scoreCurve;
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

    private int playerHand = 0;  // 플레이어 손
    private int oppoentHand = 0; // 상대 손

    // 최초 게임당 시간
    private const float MAXpartTime = 3.0f;

    // 각 게임당 시간
    public float partTime { get; private set; } = 3.0f;
    // 미니게임 남은 시간 (최초 3초부터 점점 줄어듦)
    private float minigameTime = 3.0f;
    // 성공한 게임 수
    private int clearedGame = 0;
    // 미션 텍스트
    public static string missionString = "가위바위보 시작!";
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

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        if (!isGame) return;

        timeTick();

        // 2. 키보드 테스트 입력
        if (Input.GetKeyDown(KeyCode.A)) oppoentHandChange(0);
        if (Input.GetKeyDown(KeyCode.S)) oppoentHandChange(1);
        if (Input.GetKeyDown(KeyCode.D)) oppoentHandChange(2);

        if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(0);
        if (Input.GetKeyDown(KeyCode.X)) playerHandChange(1);
        if (Input.GetKeyDown(KeyCode.C)) playerHandChange(2);

        if (Input.GetKeyDown(KeyCode.LeftAlt)) gameFail();
        if (Input.GetKeyDown(KeyCode.LeftControl)) gameClear();
    }

    private bool miniEnd = false;
    // 게임의 시간을 다루는 함수 (제한시간, 생존시간, 미니게임 시간 등)
    private void timeTick()
    {
        remainTime -= Time.deltaTime;               // 제한시간
        survivalTime += Time.deltaTime;             // 생존시간
        if (minigameTime >= 0.0f) minigameTime -= Time.deltaTime; // 미니게임 시간

        // minigameTime을 정규화 시킨 후, 화면 아래의 시간게이지를 감소시킴
        float normalized = minigameTime / partTime;
        normalized = Mathf.Clamp01(normalized);

        // 미니게임이 시작되면 화면 아래의 게이지가 줄어듦 (0이 되면 미니게임 성공여부 판정)
        timeGauge.sizeDelta = new Vector2(1920 * normalized, 30);




        ////////////////// ★★★★★ 이 아래 switch 부분을 작성해주세요 ★★★★★ //////////////////

        // 미니게임 제한시간이 끝났을 떄
        if (minigameTime <= 0.0f && miniEnd == false) {

            miniEnd = true;

            gameClear();

            // 현재 게임을 확인하고 각 게임 스크립트의 승리인지 패배인지를 여부를 불러옴
            // 0 : 가위바위보, 1 : 참참참, 2 : 제로게임
            switch (nowGame)
            {
                case 0:
                    RPS_Webcam_Controller.Instance.JudgeRPSResult();
                    break;                          // 가위바위보.cs에서 이걸 불러오기
                case 1:
                    missionString = "참참참 시작!";
                    // ChamChamChamClearOrFali();   -> 참참참 클리어 여부
                    break;                          // 참참참.cs에서 이걸 불러오기
                case 2:
                    missionString = "제로게임 시작!";
                    // ZeroGameClearOrFali();       -> 제로게임 클리어 여부
                    break;                          // 제로게임.cs에서 이걸 불러오기
                default:
                    break;
            }
        }

        // 게임 제한시간이 끝났을 때
        if (remainTime <= 0.0f)
        {
            remainTime = 0.0f;
            GameOver();
        }

        int minutes = (int)(survivalTime / 60);
        int seconds = (int)(survivalTime % 60);
        survivalTime_Text.text = $"{minutes}:{seconds:00}";

        if (remainTime > 10.0f) remainTime_Text.text = remainTime.ToString("F0");
        else remainTime_Text.text = remainTime.ToString("F1");
    }







    //////////////// PUBLIC 구간 ///////////////////
    //// 아래의 함수는 다른 스크립트에서 호출 가능 ////
    ////////////////////////////////////////////////


    // 미니게임 성공 시, 함수를 호출 (본인의 스크립트에서 미니게임 성공 조건을 달성했다면 이걸 호출하세요)
    public void gameClear()
    {
        // AudioManager.Instance.playSFX(); // 클리어 효과음 재생예정
        _Combo++;
        clearedGame++;
        remainTime += (5.0f - (survivalTime / 20.0f));
        addScore();
        pickGame();
    }

    // 미니게임 실패 시, 함수를 호출 (본인의 스크립트에서 미니게임이 실패했다면 이걸 호출하세요)
    public void gameFail()
    {
        // AudioManager.Instance.playSFX(); // 패배 효과음 재생예정
        _Combo = 0;
        remainTime -= 10.0f;
        pickGame();
    }

    // 매개변수 val 값으로 플레이어 손을 바꿈 (플레이어 손을 인식한 val값으로 바꾸세요)
    public void playerHandChange(int val)
    {
        if (playerHand == val) return;
        playerHand_effect.Clear();
        Texture2D loadTexture
            = Resources.Load<Texture2D>("Hand/" + handList[val]);

        var shapeModule_p = playerHand_effect.shape;
        playerHand = val;
        shapeModule_p.texture = loadTexture;
        playerHand_effect.Emit(15000);
    }

    // 매개변수 val 값으로 상대방의 손을 바꿈 (본인의 스크립트에서 상대의 손을 바꿨다면 이걸 호출하세요)
    public void oppoentHandChange(int val)
    {
        if (oppoentHand == val) return;
        oppoentHand_effect.Clear();
        Texture2D loadtexture_oppoent
            = Resources.Load<Texture2D>("Hand/" + handList[val]);

        var shapeModule_o = oppoentHand_effect.shape;
        oppoentHand = val;
        shapeModule_o.texture = loadtexture_oppoent;
        oppoentHand_effect.Emit(15000);
    }

    // 상대방의 손이 현재 무엇인지 확인함 (본인의 스크립트에서 상대의 손이 무엇인지 알고 싶다면 호출)
    public int checkOppoentHand()
    {
        return oppoentHand;
    }

    // 플레이어의 손이 현재 무엇인지 확인함 (본인의 스크립트에서 플레이어의 손이 무엇인지 알고 싶다면 호출)
    public int checkPlayerHand()
    {
        return playerHand;
    }

    // 미션 텍스트 출력 (이 함수를 사용하지 말고 IngameManager.Instance.missionString를 바꾸세요,
    // missionString을 바꾼다면 바뀐 텍스트가 미니게임이 바뀔 때, 자동으로 뜹니다)
    public void MissionCall()
    {

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


    //////////// 이 아래는 건들필요 없음 /////////////


    // 처음에 손이 안 보이는데, 보이게 만들기
    public void opening()
    {
        playerHand_effect.Play();
        oppoentHand_effect.Play();
    }


    // 게임 시작 시, 함수를 호출 (이미 사용됨, 재사용 ㄴ)
    public void GameStart()
    {
        AudioManager.Instance.playBGM(1);
        isGame = true;
        MissionCall();
    }




    //////////////// PRIVATE 구간 ///////////////////////////
    //// 아래의 함수는 여기에서만 사용됨 (다른 스크립트는 X) ////
    /////////////////////////////////////////////////////////

    // 제한시간이 종료되어 게임오버됨
    private void GameOver()
    {
        isGame = false;
        // AudioManager.Instance.playSFX(6); // 게임 실패 효과음
        // AudioManager.Instance.playBGM(2); // 게임 실패 브금 
        // 게임 패배 패널 띄우기
    }

    // 게임을 오래할 수록 점수가 증가됨
    // 계산 : 기본 1000점 x 콤보 수 x 생존 시간(최대 3배)
    private void addScore()
    {
        float multi = scoreCurve.Evaluate(survivalTime);
        _Score += (int)((1000 * _Combo) * (multi));
        score_Text.text = _Score.ToString("N0");
    }

    // 게임을 뽑고 텍스트를 바꿈
    private void pickGame() {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];

        int randInt = Random.Range(0, 3);
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];

        // 게임 중간에 3, 2, 1 카운트다운
        StartCoroutine(CountDown.Instance.CountDownStart(timeSet));

    }

    // 미니게임 시간을 partTime(미니게임 시간 설정값)으로 설정
    private void timeSet()
    {
        // partTime : 최초 3초로 시작하며, 0.5초까지 줄어드는 변수
        // 게임 10개 클리어 시, 미니게임 시간이 1초 감소
        partTime = MAXpartTime - (clearedGame / 10);
        partTime = Mathf.Clamp(partTime, 0.5f, 3.0f); // partTime을 0.5 ~ 3초로 제한
        minigameTime = partTime;
        miniEnd = false;
    }
}