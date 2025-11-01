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

    void Awake() {
        Instance = this;
    }

    void Update() {

        isGame = true;

        if (Input.GetKeyDown(KeyCode.A)) oppoentHandChange(0);
        if (Input.GetKeyDown(KeyCode.S)) oppoentHandChange(1);
        if (Input.GetKeyDown(KeyCode.D)) oppoentHandChange(2);

        if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(0);
        if (Input.GetKeyDown(KeyCode.X)) playerHandChange(1);
        if (Input.GetKeyDown(KeyCode.C)) playerHandChange(2);

        if (Input.GetKeyDown(KeyCode.Space)) gameClear();

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

    // 게임을 랜덤으로 뽑음, 그 후에 화면에 있는 NEXT와 NOW 텍스트를 바꿈
    private void pickGame() {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];

        int randInt = Random.Range(0, 3);
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];
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
