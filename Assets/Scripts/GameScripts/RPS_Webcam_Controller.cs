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
    [Header("UI (미션 표시용)")]
    [SerializeField] private TextMeshProUGUI instructionText;

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

    [Header("가위바위보 난이도 설정")]
    [SerializeField] private float rpsTimeLimit = 30.0f; // 현재 제한 시간 (시작 값)
    [SerializeField] private float rpsMinTimeLimit = 0.5f; // 최소 제한 시간 (여기까지 어려워짐)
    [SerializeField] private float rpsTimeDecrement = 0.1f; // 성공 시 단축되는 시간

    private Coroutine rpsTimerCoroutine; // 미션 타이머 코루틴
    private bool isMissionActive = false; // 현재 미션이 진행 중인지 (중복 판정 방지)


    // --- UDP 네트워크 수신 관련 변수 ---
    private Thread receiveThread;
    private UdpClient client;
    private int port = 12345;

    // ▼▼▼ [로직 변경] '마지막으로 낸 손'을 저장하는 변수로 사용 ▼▼▼
    private int lastPlayerHandVal = HAND_NONE;
    // ▲▲▲ [로직 변경] ▲▲▲

    // 메인 스레드와 네트워크 스레드 간의 데이터 공유를 위한 큐(Queue)
    private Queue<int> handDataQueue = new Queue<int>();
    // 큐 접근을 동기화하기 위한 잠금(lock) 객체
    private readonly object queueLock = new object();


    void Start()
    {
        if (nowGame_Text_Reference == null)
        {
            Debug.LogError("RPS_Webcam_Controller: 'Now Game Text Reference'가 연결되지 않았습니다!");
        }

        lock (queueLock)
        {
            handDataQueue.Clear();
        }

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();


    }

    void Update()
    {
        // 1. 큐에 쌓인 데이터(손 모양)가 있으면 메인 스레드에서 처리
        ProcessQueue();

        if (nowGame_Text_Reference == null) return;

        // 2. 현재 게임이 "가위바위보"인지 확인
        if (nowGame_Text_Reference.text == "가위바위보")
        {
            // 3. "가위바위보" 게임인데, 아직 미션이 시작되지 않았다면 (최초 1회 실행)
            if (!isMissionActive)
            {
                isMissionActive = true; // 미션이 시작됨을 표시 (중복 시작 방지)
                StartNewRPSMission();   // 새 미션 및 타이머 시작
            }
        }
        else
        {
            // 4. "가위바위보"가 아니라면 (예: 참참참)
            // 모든 미션 상태를 리셋하고, 혹시나 실행 중인 타이머가 있다면 중지
            isMissionActive = false;
            if (rpsTimerCoroutine != null)
            {
                StopCoroutine(rpsTimerCoroutine);
                rpsTimerCoroutine = null;
            }
        }
    }

    /// <summary>
    /// (네트워크 스레드) Python에서 보낸 데이터를 큐에 넣기
    /// </summary>
    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                Debug.Log($"[RPS_Webcam] Received text: '{text}'");
                int detectedHandVal = int.Parse(text.Trim());

                // ▼▼▼ [로직 변경] 큐에 넣기 전에 lastPlayerHandVal을 업데이트 ▼▼▼
                // (큐가 처리되기 전에 타이머가 끝나도 마지막 손 모양을 기억하기 위함)

                // 지금 손 모양이 이전과 다를 때만 업데이트
                if (detectedHandVal != lastPlayerHandVal)
                {
                    lastPlayerHandVal = detectedHandVal;

                    if (isMissionActive)
                    {
                        lock (queueLock)
                        {
                            handDataQueue.Enqueue(detectedHandVal);
                        }
                    }
                }
                // ▲▲▲ [로직 변경] ▲▲▲
            }
            catch (Exception err)
            {
                Debug.LogError(err.ToString());
            }
        }
    }

    /// <summary>
    /// (메인 스레드) 큐에서 데이터를 꺼내 게임 로직 처리
    /// </summary>
    private void ProcessQueue()
    {
        while (handDataQueue.Count > 0)
        {
            int handVal;
            lock (queueLock)
            {
                handVal = handDataQueue.Dequeue();
            }

            // 큐에서 꺼냈을 때도 여전히 미션이 활성 상태인지 재확인
            if (isMissionActive)
            {
                ProcessHandData(handVal);
            }
        }
    }

    // ▼▼▼▼▼ [로직 대폭 수정] ▼▼▼▼▼

    /// <summary>
    /// (메인 스레드) 인식된 손 모양을 InGameManager에 전달 (화면 업데이트용)
    /// [승패 판정 로직 삭제됨]
    /// </summary>
    private void ProcessHandData(int playerVal)
    {
        // 1. 플레이어 손 모양을 화면에 즉시 표시 (요구사항: 플레이어가 바꿀 수 있게)
        InGameManager.Instance.playerHandChange(playerVal);

        // 2. (중요) 여기서 더 이상 승패 판정을 하지 않습니다.
        //    판정은 RPSMissionTimer 코루틴이 타이머 종료 시 1회만 수행합니다.
    }

    /// <summary>
    /// 새 가위바위보 미션 시작 (컴퓨터 손 표시, 타이머 시작)
    /// </summary>
    private void StartNewRPSMission()
    {
        lastPlayerHandVal = HAND_NONE; // 플레이어의 마지막 손 모양을 '없음'으로 초기화

        // 1. 컴퓨터가 낼 손(opponentHand)과 미션(currentMission)을 랜덤으로 결정
        int opponentHand = UnityEngine.Random.Range(0, 3);
        InGameManager.Instance.oppoentHandChange(opponentHand); // 컴퓨터 손 모양 화면에 표시

        currentMission = (Mission)UnityEngine.Random.Range(0, 3);
        instructionText.text = GetMissionString(currentMission, opponentHand); // 미션 텍스트 표시

        // 2. [중요] 제한 시간 타이머(코루틴) 시작
        // rpsTimeLimit (예: 3초) 후에 자동으로 '판정'을 시작
        rpsTimerCoroutine = StartCoroutine(RPSMissionTimer());
    }

    /// <summary>
    /// [코루틴] 정해진 시간(rpsTimeLimit)이 지나면 "최종 판정" 수행
    /// </summary>
    private IEnumerator RPSMissionTimer()
    {
        // 1. 현재 설정된 제한 시간 (예: 3초) 만큼 기다림
        yield return new WaitForSeconds(rpsTimeLimit);

        // 2. [판정 시간] 이 코드가 실행될 때 (3초가 지났을 때)
        //    'lastPlayerHandVal'에 저장된 플레이어의 '마지막 손 모양'을 가져옴
        int finalPlayerHand = lastPlayerHandVal;

        bool success = PerformRPSCheck(finalPlayerHand);

        // 3. [성공] 미션을 성공했다면
        if (success)
        {
            // 리워드 (시간 증가, 점수/콤보 획득)
            InGameManager.Instance.gameClear();  
            // 난이도 상승 (제한 시간 0.1초 감소)
            DecreaseTimeLimit();
        }
        // 4. [실패] 미션을 실패했다면 (시간이 초과됐거나, 손 모양이 틀렸거나)
        else
        {
            // 벌칙 (시간 감소, 콤보 리셋)
            InGameManager.Instance.gameFail();
        }

        // 5. 미션 종료
        isMissionActive = false;
        rpsTimerCoroutine = null;
    }

    /// <summary>
    /// 미션 성공 시 난이도 상승 (제한 시간 감소)
    /// </summary>
    private void DecreaseTimeLimit()
    {
        // 현재 제한 시간이 최소 시간(0.5초)보다 클 때만
        if (rpsTimeLimit > rpsMinTimeLimit)
        {
            rpsTimeLimit -= rpsTimeDecrement; // 0.1초 감소

            // 0.1초를 뺐는데 최소 시간보다 작아졌으면, 최소 시간으로 고정
            if (rpsTimeLimit < rpsMinTimeLimit)
            {
                rpsTimeLimit = rpsMinTimeLimit;
            }
        }
    }

    /// <summary>
    /// 현재 플레이어 손(playerVal)이 미션을 성공했는지 여부(true/false)만 반환
    /// </summary>
    private bool PerformRPSCheck(int playerVal)
    {
        // [중요] checkOppoentHand()는 판정하는 '순간'에 호출해야 함
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

    // ▲▲▲▲▲ [로직 대폭 수정] ▲▲▲▲▲


    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        if (client != null)
            client.Close();
    }

    private string ConvertValToHandName(int val)
    {
        switch (val)
        {
            case HAND_ROCK: return "바위";
            case HAND_PAPER: return "보";
            case HAND_SCISSORS: return "가위";
            default: return "??";
        }
    }

    /// <summary>
    /// 미션 텍스트를 "이겨라!", "비겨라!", "져라!"만 반환하도록 수정
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