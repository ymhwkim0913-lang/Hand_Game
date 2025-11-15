using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // InGameManager가 TMPro를 사용하므로 맞춰줍니다.

// C# 네트워킹(UDP)
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// InGameManager가 'ZeroGame_Main.Instance'로 호출하므로 클래스 이름이 일치해야 합니다.
public class ZeroGame_Main : MonoBehaviour
{
    // --- 싱글톤 인스턴스 ---
    // InGameManager가 'ZeroGame_Main.Instance.JudgeZeroGameResult()'로
    // 이 스크립트를 찾아 호출할 수 있도록 'public static Instance'를 만듭니다.
    public static ZeroGame_Main Instance { get; private set; }

    [Header("게임 상태 참조용")]
    // InGameManager의 'nowGame_Text' UI를 연결해야 합니다.
    [SerializeField] private TextMeshProUGUI nowGame_Text_Reference;

    // --- 손 모양 상수 ---
    private const int HAND_NONE = -1; // InGameManager의 기본값과 일치

    // --- 제로게임 미션 변수 ---
    private int playerCall;             // 목표 합계 숫자 (0~4)
    private int computerHand;           // 컴퓨터가 낸 숫자 (0~2)
    private bool isAttackMode = true;   // 공격(true) 또는 수비(false) 모드

    // --- 상태 변수 ---
    // 'volatile' 키워드는 여러 스레드가 'isMissionActive' 값을 공유할 때
    // 값이 즉시 갱신되도록 보장해줍니다. (네트워크 스레드와 메인 스레드)
    private volatile bool isMissionActive = false; // "제로게임" 미션이 설정되었는지 확인용

    // --- 1. 싱글톤 인스턴스 설정 ---
    void Awake()
    {
        // InGameManager가 'Instance'를 사용할 수 있도록 자신을 할당합니다.
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    // --- 2. 초기 설정 및 스레드 시작 ---
    void Start()
    {
        // 인스펙터 창에서 'nowGame_Text_Reference'가 연결되었는지 확인
        if (nowGame_Text_Reference == null)
        {
            Debug.LogError("ZeroGame_Main: 'Now Game Text Reference'가 연결되지 않았습니다!");
        }
    }

    // --- 3. "스위치" 작동 (메인 스레드) ---
    // 매 프레임마다 InGameManager의 텍스트를 감시
    void Update()
    {
        if (nowGame_Text_Reference == null) return;

        // InGameManager의 'gameList'에 적힌 문자열과 정확히 일치해야 합니다.
        // Trim()은 보이지 않는 공백을 제거하여 안정성을 높입니다.
        string currentGameName = nowGame_Text_Reference.text.Trim();

        if (currentGameName == "ZeroGame" || currentGameName == "제로게임")
        {
            // "제로게임"이 시작됐는데, 미션 설정이 아직 안됐다면
            if (!isMissionActive)
            {
                isMissionActive = true; // 스위치 ON
                StartNewZeroMission(); // 4. 미션 설정 시작
            }
        }
        else
        {
            // 다른 게임으로 전환 시 스위치 OFF
            isMissionActive = false;
        }
    }

    #region 제로게임 고유 로직

    // --- 4. 미션 설정 (RPS의 StartNewRPSMission과 동일한 역할) ---
    public void StartNewZeroMission()
    {
        // 1. 제로게임 미션 생성
        playerCall = UnityEngine.Random.Range(0, 5); // 0~4 중 랜덤 숫자 선택
        isAttackMode = (UnityEngine.Random.Range(0, 2) == 0); // 50% 확률로 공격/수비 모드 결정

        // 2. 컴퓨터 손 결정 (RPS의 opponentHand 결정과 같음)
        int maxPossibleComputerHand = playerCall;
        int finalMax = Mathf.Min(2, maxPossibleComputerHand);
        int minComputerHand = Mathf.Max(0, playerCall - 2);
        computerHand = UnityEngine.Random.Range(minComputerHand, finalMax + 1);

        // 3. 미션 텍스트를 InGameManager의 공용 변수에 "저장"
        // (CountDown 스크립트가 "3,2,1" 이후에 InGameManager.MissionCall()을 호출하여 표시할 것임)
        InGameManager.missionString = GetMissionString();

        // 4. 컴퓨터 손 모양 화면 표시 (인덱스 6, 7, 8 매핑 "번역")
        int oppoHandResourceIndex = 6 + computerHand;
        InGameManager.Instance.oppoentHandChange(oppoHandResourceIndex);
    }


    // --- 8. "심판" 함수 (InGameManager가 호출) ---
    // (RPS의 JudgeRPSResult와 동일한 역할)
    public void JudgeZeroGameResult()
    {
        // InGameManager가 타이머 종료 직후 이 함수를 호출함

        // "제로게임" 미션이 활성화 상태일 때(아직 판정 안 했을 때)만 판정
        if (!isMissionActive) return;

        // 1. 판정 시작 (중복 판정 방지를 위해 즉시 false로 바꿈)
        isMissionActive = false;

        // 2. InGameManager에 저장된 "최종 손 모양"을 가져옴
        // (ProcessHandData가 6,7,8로 저장했으므로 6,7,8 또는 -1이 반환됨)
        int finalPlayerHand = InGameManager.Instance.checkPlayerHand();

        // 3. [핵심] "역번역" 수행 (6,7,8 -> 0,1,2)
        // 판정 로직(PerformZeroGameCheck)은 0,1,2만 알아듣기 때문에 6을 빼줍니다.
        int finalPlayerHandCount = HAND_NONE; // 기본값 -1
        if (finalPlayerHand >= 6 && finalPlayerHand <= 8)
        {
            finalPlayerHandCount = finalPlayerHand - 6; // 6,7,8을 0,1,2로 변경
        }

        // 4. 승패 판정 (0,1,2 또는 -1 값으로 판정)
        bool success = PerformZeroGameCheck(finalPlayerHandCount);

        // 5. InGameManager(PD)에게 결과 보고
        if (success)
        {
            InGameManager.Instance.gameClear();
        }
        else
        {
            InGameManager.Instance.gameFail();
        }
    }

    // --- 9. 실제 판정 로직 (내부 함수) ---
    private bool PerformZeroGameCheck(int playerHandCount)
    {
        // 1. 플레이어가 시간 내에 아무것도 내지 않은 경우 (HAND_NONE)
        if (playerHandCount == HAND_NONE) return false;

        // 2. 합계 계산 (플레이어 손 + 컴퓨터 손)
        int totalHand = playerHandCount + computerHand;

        bool isSuccess = false;

        if (isAttackMode)
        {
            // 3. [공격 모드] 합계가 목표(Call)와 '같아야' 성공
            isSuccess = (totalHand == playerCall);
        }
        else
        {
            // 4. [수비 모드] 합계가 목표(Call)와 '달라야' 성공
            isSuccess = (totalHand != playerCall);
        }

        return isSuccess; // 판정 결과(true 또는 false) 반환
    }

    // --- 10. 미션 텍스트 생성 (RPS의 GetMissionString과 동일한 역할) ---
    private string GetMissionString()
    {
        string modeStr = isAttackMode ? $"총 {playerCall}개가 되라! " : $"총 {playerCall}개가 되면 안돼!";

        // 예: "공격! 목표: 3" 또는 "수비! 목표: 1"
        return modeStr;
    }

    #endregion
}