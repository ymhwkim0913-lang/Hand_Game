// --- C# 네트워킹(UDP)을 위해 필요한 using ---
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChamChamCham_Webcam_Controller : MonoBehaviour
{
    // 🌟🌟🌟 [추가] 싱글톤 Instance 정의 (InGameManager에서 호출을 위해 필요) 🌟🌟🌟
    public static ChamChamCham_Webcam_Controller Instance { get; private set; }

    [Header("게임 상태 참조용")]
    [SerializeField] private TextMeshProUGUI nowGame_Text_Reference;

    // InGameManager에서 사용하는 참참참 손 모양 값
    private const int HAND_LEFT = 3;      // 좌측 (Q 키)
    private const int HAND_MID = 4;       // 중앙 (W 키)
    private const int HAND_RIGHT = 5;     // 우측 (E 키)

    // --- 참참참 미션 관련 변수 ---
    // 참참참 미션: Follow(따라 하기) 또는 Avoid(피하기)
    private enum ChamMission { Follow, Avoid }
    private ChamMission currentMission;

    private bool isMissionActive = false; // 현재 미션이 진행 중인지 (중복 판정 방지)

    // --- UDP 네트워크 수신 관련 변수 ---
    private Thread receiveThread;
    private UdpClient client;
    private int port = 12346; // Python과 동일한 포트 사용

    // '마지막으로 낸 손'을 저장하는 변수 (3, 4, 5 또는 0:손없음)
    private int lastPlayerHandVal = 0; // 0으로 초기화. (0은 유효한 손 모양 값이 아니므로 '손 없음' 상태로 사용)

    // 메인 스레드와 네트워크 스레드 간의 데이터 공유를 위한 큐(Queue)
    private Queue<int> handDataQueue = new Queue<int>();
    // 큐 접근을 동기화하기 위한 잠금(lock) 객체
    private readonly object queueLock = new object();

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (nowGame_Text_Reference == null)
        {
            Debug.LogError("ChamChamCham_Webcam_Controller: 'Now Game Text Reference'가 연결되지 않았습니다!");
        }

        lock (queueLock)
        {
            handDataQueue.Clear();
        }

        // UDP 수신 스레드 시작
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // 1. 큐에 쌓인 데이터(손 모양)가 있으면 메인 스레드에서 처리
        ProcessQueue();

        if (nowGame_Text_Reference == null) return;

        // 2. 현재 게임이 "참참참"인지 확인 (InGameManager의 gameList[1] 참조)
        if (nowGame_Text_Reference.text == "참참참")
        {
            // 3. "참참참" 게임인데, 아직 미션이 시작되지 않았다면 (최초 1회 실행)
            if (!isMissionActive)
            {
                isMissionActive = true; // 미션이 시작됨을 표시 (중복 시작 방지)
                StartNewCCCMission();    // 새 미션 및 타이머 시작
            }
        }
        else
        {
            isMissionActive = false;
        }
    }

    // --- UDP 수신 로직 (RPS 코드와 동일) ---
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
                int detectedHandVal = int.Parse(text.Trim());

                // 지금 손 모양이 이전과 다를 때만 업데이트 (중복 데이터 처리 최소화)
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
            }
            catch (Exception err) {
                Debug.LogError(err.ToString());
            }
        }
    }

    // --- 큐 처리 로직 (RPS 코드와 동일) ---
    private void ProcessQueue()
    {
        while (handDataQueue.Count > 0)
        {
            int handVal;
            lock (queueLock)
            {
                handVal = handDataQueue.Dequeue();
            }

            if (isMissionActive)
            {
                ProcessHandData(handVal);
            }
        }
    }

    // --- 참참참 로직 (수정된 부분) ---

    /// <summary>
    /// (메인 스레드) 인식된 손 모양을 InGameManager에 전달 (화면 업데이트용)
    /// </summary>
    private void ProcessHandData(int playerVal)
    {
        // 1. 플레이어 손 모양을 화면에 즉시 표시
        InGameManager.Instance.playerHandChange(playerVal);
    }

    /// <summary>
    /// 새 참참참 미션 시작 (컴퓨터 손 표시, 미션 텍스트, 타이머 시작)
    /// </summary>
    private void StartNewCCCMission()
    {
        // lastPlayerHandVal을 0 (손 없음)으로 초기화
        lastPlayerHandVal = 0;

        // 1. 컴퓨터가 낼 손(opponentHand)과 미션(currentMission)을 랜덤으로 결정
        int opponentHand = UnityEngine.Random.Range(HAND_LEFT, HAND_RIGHT + 1); // 3, 4, 5 중 하나
        InGameManager.Instance.oppoentHandChange(opponentHand); // 컴퓨터 손 모양 화면에 표시

        currentMission = (ChamMission)UnityEngine.Random.Range(0, 2); // 0(Follow) 또는 1(Avoid)
        // 3. InGameManager가 사용할 미션 텍스트를 static 변수에 저장
        InGameManager.missionString = GetMissionString(currentMission, opponentHand);
    }

    private string GetMissionString(ChamMission mission, int oppoHand) {
        string missionText = "";
        switch (mission) {
            case ChamMission.Follow: missionText = "따라해라!"; break;
            case ChamMission.Avoid: missionText = "피해라!"; break;
        }
        return missionText;
    }

    /// <summary>
    /// [코루틴] 정해진 시간(cccTimeLimit)이 지나면 "최종 판정" 수행
    /// </summary>
    public void JubgeCCCResult()
    {
        if (!isMissionActive) return;

        // 1. 판정 시작! (중복 판정 방지를 위해 즉시 false로 바꿈)
        isMissionActive = false;

        int finalPlayerHand = InGameManager.Instance.checkPlayerHand();

        // finalPlayerHand가 3, 4, 5 중 하나인지 확인 (3 미만이면 실패)
        bool success = (finalPlayerHand >= HAND_LEFT) && PerformCCCCheck(finalPlayerHand);

        // 3. [성공] 미션을 성공했다면
        if (success)
        {
            // 리워드 및 콤보/점수 획득
            InGameManager.Instance.gameClear();
        }
        // 4. [실패] 미션을 실패했다면 (시간 초과, 손 모양 틀림, 손 모양 없음)
        else
        {
            // 벌칙 (시간 감소, 콤보 리셋)
            InGameManager.Instance.gameFail();
        }
    }

    /// <summary>
    /// 현재 플레이어 손(playerVal)이 미션을 성공했는지 여부(true/false)만 반환
    /// </summary>
    private bool PerformCCCCheck(int playerVal)
    {
        // [중요] checkOppoentHand()는 판정하는 '순간'에 호출하여 컴퓨터 손을 가져옴
        // 이 숫자는 반전이 되어야함 (게임에서는 실제로 반전돼서 보이기 때문)
        int opponentVal = (8 - InGameManager.Instance.checkOppoentHand());

        if (currentMission == ChamMission.Follow) // 따라 하기 미션
        {
            // 플레이어 손과 컴퓨터 손이 같으면 성공
            return playerVal == opponentVal;
        }
        else // Avoid (피하기) 미션
        {
            // 플레이어 손과 컴퓨터 손이 다르면 성공
            return playerVal != opponentVal;
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        if (client != null)
            client.Close();
    }
}