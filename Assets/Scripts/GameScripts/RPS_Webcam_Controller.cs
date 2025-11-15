using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// --- C# 네트워킹(UDP)을 위해 필요한 using ---
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class RPS_Webcam_Controller : MonoBehaviour
{
    // ▼▼▼ [추가] InGameManager가 이 스크립트를 찾을 수 있도록 싱글톤 인스턴스 추가 ▼▼▼
    public static RPS_Webcam_Controller Instance { get; private set; }
    // ▲▲▲ [추가] ▲▲▲

    [Header("게임 상태 참조용")]
    [SerializeField] private TextMeshProUGUI nowGame_Text_Reference;

    // --- InGameManager의 손 모양 값 ---
    private const int HAND_ROCK = 0;
    private const int HAND_SCISSORS = 1;
    private const int HAND_PAPER = 2;
    private const int HAND_NONE = -1;

    // --- 미션 관련 변수 ---
    private enum Mission { Tie, Win, Lose }
    private Mission currentMission;

    // [삭제] InGameManager가 타이머를 제어하므로 이 스크립트의 타이머는 필요 없음
    // [Header("가위바위보 난이도 설정")]
    // [SerializeField] private float rpsTimeLimit = 3.0f;
    // [삭제] private Coroutine rpsTimerCoroutine; 

    private bool isMissionActive = false; // "가위바위보" 미션이 설정되었는지 확인용

    // ▼▼▼ [추가] 싱글톤 인스턴스 설정 ▼▼▼
    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }
    // ▲▲▲ [추가] ▲▲▲

    void Start()
    {
        if (nowGame_Text_Reference == null)
        {
            Debug.LogError("RPS_Webcam_Controller: 'Now Game Text Reference'가 연결되지 않았습니다!");
        }
    }

    void Update()
    {

        if (nowGame_Text_Reference == null) return;

        // 2. 현재 게임이 "가위바위보"인지 확인
        if (nowGame_Text_Reference.text == "가위바위보")
        {
            // 3. "가위바위보" 게임인데, 아직 미션이 설정되지 않았다면 (최초 1회 실행)
            if (!isMissionActive)
            {
                isMissionActive = true;
                StartNewRPSMission(); // 미션 설정 (타이머 X)
            }
        }
        else
        {
            // 4. "가위바위보"가 아니라면 (예: 참참참)
            isMissionActive = false;
        }
    }

  
    /// <summary>
    /// 새 가위바위보 미션 "설정" (타이머 시작 X)
    /// </summary>
    public void StartNewRPSMission()
    {
        // 1. 상대방 손 랜덤 설정
        int opponentHand = UnityEngine.Random.Range(0, 3);
        InGameManager.Instance.oppoentHandChange(opponentHand);

        // 2. 미션 랜덤 설정
        currentMission = (Mission)UnityEngine.Random.Range(0, 3);

        // 3. InGameManager가 사용할 미션 텍스트를 static 변수에 저장
        InGameManager.missionString = GetMissionString(currentMission, opponentHand);

    }


    // ▼▼▼ [수정됨] 중복 호출 방지 로직 추가 ▼▼▼
    /// <summary>
    /// InGameManager의 타이머가 0이 됐을 때 호출될 판정 함수
    /// </summary>
    public void JudgeRPSResult()
    {
        // [중요!] "가위바위보" 미션이 활성화된 상태일 때(아직 판정 안 했을 때)만 판정
        if (!isMissionActive) return;

        // 1. 판정 시작! (중복 판정 방지를 위해 즉시 false로 바꿈)
        isMissionActive = false;

        // 2. 타이머가 끝난 시점의 플레이어 마지막 손 모양을 가져옴
        int finalPlayerHand = InGameManager.Instance.checkPlayerHand(); // InGameManager의 변수를 직접 체크
        // int finalPlayerHand = lastPlayerHandVal; // (이전 방식)

        // 3. 승패 판정
        bool success = PerformRPSCheck(finalPlayerHand);

        // 4. InGameManager의 함수 호출
        if (success)
        {
            InGameManager.Instance.gameClear();
        }
        else
        {
            InGameManager.Instance.gameFail();
        }

        // 5. isMissionActive는 InGameManager의 pickGame()이 호출된 후,
        //    Update()에서 "가위바위보"가 다시 걸리면 true로 바뀔 것임.
    }
    // ▲▲▲ [수정됨] ▲▲▲

    /// <summary>
    /// 현재 플레이어 손(playerVal)이 미션을 성공했는지 여부(true/false)만 반환
    /// </summary>
    private bool PerformRPSCheck(int playerVal)
    {
        int opponentVal = InGameManager.Instance.checkOppoentHand();
        bool success = false;
        switch (currentMission)
        {
            case Mission.Tie:
                success = (playerVal == opponentVal);
                break;
            case Mission.Win:
                success = (playerVal == HAND_ROCK && opponentVal == HAND_SCISSORS) ||
                            (playerVal == HAND_SCISSORS && opponentVal == HAND_PAPER) ||
                            (playerVal == HAND_PAPER && opponentVal == HAND_ROCK);
                break;
            case Mission.Lose:
                success = (playerVal == HAND_ROCK && opponentVal == HAND_PAPER) ||
                            (playerVal == HAND_SCISSORS && opponentVal == HAND_ROCK) ||
                            (playerVal == HAND_PAPER && opponentVal == HAND_SCISSORS);
                break;
        }
        return success;
    }


    /// <summary>
    /// 미션 텍스트를 "이겨라!", "비겨라!", "져라!"만 반환
    /// </summary>
    private string GetMissionString(Mission mission, int oppoHand)
    {
        string missionText = "";
        switch (mission)
        {
            case Mission.Tie: missionText = "비겨라!"; break;
            case Mission.Win: missionText = "이겨라!"; break;
            case Mission.Lose: missionText = "져라!"; break;
        }
        return missionText;
    }
}