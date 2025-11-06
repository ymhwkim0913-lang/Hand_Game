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
    private string[] handList = new string[] { "rock", "scissors", "paper",          // 바위가위보
                                               "ccc_left", "ccc_mid", "ccc_right",  // 참참참(좌중우)
                                               "zero_0", "zero_1", "zero_2"};       // 제로(0, 1, 2)

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

    // 플레이어, 상대방의 손을 int(정수)로 저장합니다.
    // {0:바위} {1:가위} {2:보}                      -> 가위바위보 변수
    // {3:참참참-좌측} {4:참참참-중앙} {5:참참참-우측} -> 참참참 변수
    // {6:제로 0개} {7:제로 1개} {8:제로 2개}         -> 제로게임 변수
    private int playerHand = 0;
    private int oppoentHand = 0;
    private bool isFollowMission = true; // 참참참 미션 (true:따라하기, false:피하기)

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

    #endregion

    void Awake() {
        Instance = this;
    }
    void Start()
    {
        pickGame(); // 게임 시작 시 1회 호출하여 NOW GAME의 상대방 손을 설정합니다.
    }

    void Update() {

        isGame = true;

        if (isGame) {
            if (remainTime > 0.0) {
                timeTick();
            } else endGame();
        }
    }

    /// <summary>
    /// 플레이어의 손을 바꿉니다
    /// 
    /// [예문] playerHandChange(2) => 플레이어 손을 paper(보)로 바꿈
    /// 
    /// </summary>
    /// <param name="val">바꿀 값</param>
    public void playerHandChange(int val) {

        if (playerHand == val) return;

        playerHand_effect.Clear();
        Texture2D loadtexture_player = Resources.Load<Texture2D>("Hand/" + handList[val]);
        var shapeModule_p = playerHand_effect.shape;
        playerHand = val;
        shapeModule_p.texture = loadtexture_player;
        playerHand_effect.Emit(15000);

        // ★ 현재 게임이 "참참참"일 때만 승패를 판정합니다.
        if (nowGame == 1)
        {
            // 1. 상대방의 손이 무엇인지 가져옵니다. (3:좌, 4:중, 5:우)
            int oppoHand = checkOppoentHand();

            // 2. "val" 값이 플레이어의 손입니다. (3:좌, 4:중, 5:우)
            // 3. 1단계에서 저장해둔 미션(isFollowMission)을 확인합니다.

            if (isFollowMission == true) // "따라하기" 미션일 때
            {
                if (val == oppoHand) gameClear(); // (성공) 따라했으면 승리!
                else gameFail();                 // (실패) 못 따라했으면 패배!
            }
            else // "피하기" 미션일 때
            {
                if (val != oppoHand) gameClear(); // (성공) 피했으면 승리!
                else gameFail();                 // (실패) 못 피했으면 패배!
            }
        }
    }

    /// <summary>
    /// 상대방의 손을 바꿉니다.
    /// 
    /// [예문] oppoentHandChange(2) => 상대방 손을 paper(보)로 바꿈
    /// 
    /// </summary>
    /// <param name="val">바꿀 값</param>
    public void oppoentHandChange(int val) {
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
    /// 이 코드를 이용하여 상대방의 손 모양을 확인하고 조건문(if)을 작성하세요.
    /// 
    /// [예문] if (checkOppoentHand() == 1)  => 상대의 손이 가위(1)일 때의 조건문
    /// 
    /// </summary>
    /// <returns></returns>
    public int checkOppoentHand() {
        return oppoentHand;
    }

    // 게임 성공 시 (게임 진행 30초마다 증가되는 시간 1초씩 감소) [예] 60초 진행 중 -> 3초만 증가
    // 만약, 게임이 성공 했다면 이 함수를 호출하세요.
    public void gameClear() {
        pickGame();
        _Combo++;
        addScore();
        remainTime += (5.0f - (survivalTime / 30.0f));
    }

    // 게임 실패 시, 게임 시작 -10초 및 콤보 깨짐
    // 만약, 게임에 실패했다면 이 함수를 호출하세요.
    public void gameFail() {
        pickGame();
        _Combo = 0;
        remainTime -= 10.0f;
    }




 /////////////////////////////////////// 이 아래는 공개 함수가 아님 (건들필요X) ///////////////////////////////////////

    // 게임 오버
    private void endGame() {
        isGame = false;
    }

    // 점수를 증가시킵니다
    private void addScore() {

        float multi = scoreCurve.Evaluate(survivalTime);
        _Score += (int)((1000 * _Combo) * (multi));     // 기본 1000점 x 콤보 수 x 생존 시간 보너스(최대 3배)
        score_Text.text = _Score.ToString("N0");

    }
    private void PrepareNextChamChamChamRound()
    {
        // 1. 미션을 랜덤으로 정합니다.
        isFollowMission = (Random.Range(0, 2) == 0);
        if (isFollowMission)
        {
            missionText.text = "따라 해라!";
            missionText.color = Color.yellow;
        }
        else
        {
            missionText.text = "피해라!";
            missionText.color = Color.cyan;
        }

        // 2. 상대방 방향을 랜덤으로 정합니다. (3:좌, 4:중, 5:우)
        int oppoDir = Random.Range(3, 6);

        // 3. 상대방 손을 미리 바꿉니다.
        oppoentHandChange(oppoDir);
    }

    // 게임을 랜덤으로 뽑음, 그 후에 화면에 있는 NEXT와 NOW 텍스트를 바꿈
    private void pickGame() {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];

        int randInt = Random.Range(0, 3);
        
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];
        if (nowGame == 1) // 참참참일 때만 다음 라운드를 준비합니다.
        {
            PrepareNextChamChamChamRound();
        }
        else // 가위바위보(0) 또는 제로게임(2)일 때 (참참참 외의 게임)
        {
            missionText.text = "";

            // 다른 게임의 상대방 손 움직임을 위한 로직 (최소한의 동작)
            if (nowGame == 0) // 가위바위보
            {
                oppoentHandChange(Random.Range(0, 3));
            }
            else if (nowGame == 2) // 제로게임
            {
                oppoentHandChange(Random.Range(6, 9));
            }
        }
    }

    // 제한시간 감소 및 텍스트 변경
    private void timeTick() {

        remainTime -= Time.deltaTime;
        survivalTime += Time.deltaTime;

        int minutes = (int)(survivalTime / 60);
        int seconds = (int)(survivalTime % 60);

        if (remainTime > 10.0f) remainTime_Text.text = remainTime.ToString("F0");
        else remainTime_Text.text = remainTime.ToString("F1");
        survivalTime_Text.text = $"{minutes}:{seconds:00}";
    }

}
