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

public class ChamChamCham_Webcam_Controller : MonoBehaviour
{
    // 🌟🌟🌟 [추가] 싱글톤 Instance 정의 (InGameManager에서 호출을 위해 필요) 🌟🌟🌟
    public static ChamChamCham_Webcam_Controller Instance { get; private set; }

    [Header("UI (미션 표시용)")]
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("게임 상태 참조용")]
    [SerializeField] private TextMeshProUGUI nowGame_Text_Reference;

    // InGameManager에서 사용하는 참참참 손 모양 값
    private const int HAND_LEFT = 3;      // 좌측 (Q 키)
    private const int HAND_MID = 4;       // 중앙 (W 키)
    private const int HAND_RIGHT = 5;     // 우측 (E 키)
    // HAND_NONE = -1 상수는 제거됨. (대신 0을 '손 없음'으로 간주)

    // --- 참참참 미션 관련 변수 ---
    // 참참참 미션: Follow(따라 하기) 또는 Avoid(피하기)
    private enum ChamMission { Follow, Avoid }
    private ChamMission currentMission;

    [Header("참참참 난이도 설정")]
    [SerializeField] private float cccTimeLimit = 3.0f;     // 현재 제한 시간 (시작 값)
    [SerializeField] private float cccMinTimeLimit = 0.5f;  // 최소 제한 시간 (여기까지 어려워짐)
    [SerializeField] private float cccTimeDecrement = 0.1f; // 성공 시 단축되는 시간

    private Coroutine cccTimerCoroutine; // 미션 타이머 코루틴
    private bool isMissionActive = false; // 현재 미션이 진행 중인지 (중복 판정 방지)


    // --- UDP 네트워크 수신 관련 변수 ---
    private Thread receiveThread;
    private UdpClient client;
    private int port = 12345; // Python과 동일한 포트 사용

    // '마지막으로 낸 손'을 저장하는 변수 (3, 4, 5 또는 0:손없음)
    private int lastPlayerHandVal = 0; // 0으로 초기화. (0은 유효한 손 모양 값이 아니므로 '손 없음' 상태로 사용)

    // 메인 스레드와 네트워크 스레드 간의 데이터 공유를 위한 큐(Queue)
    private Queue<int> handDataQueue = new Queue<int>();
    // 큐 접근을 동기화하기 위한 잠금(lock) 객체
    private readonly object queueLock = new object();

    void Awake()
    {
        // 🌟🌟🌟 싱글톤 초기화 🌟🌟🌟
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
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

            // 🌟🌟🌟 [추가됨] 키보드 입력 처리 🌟🌟🌟
            HandleKeyboardInput();
            // 🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟
        }
        else
        {
            // 4. "참참참"이 아니라면 모든 미션 상태를 리셋하고, 실행 중인 타이머가 있다면 중지
            isMissionActive = false;
            if (cccTimerCoroutine != null)
            {
                StopCoroutine(cccTimerCoroutine);
                cccTimerCoroutine = null;
            }
            // 🚨 [수정] lastPlayerHandVal을 0 (손 없음)으로 초기화
            lastPlayerHandVal = 0;
            // 🚨 [수정] InGameManager.playerHandChange(0) 호출 
            InGameManager.Instance.playerHandChange(0);
        }
    }

    // 🌟🌟🌟 [수정된 함수] 키보드 입력 처리 로직 (Q, W, E 키 사용) 🌟🌟🌟
    /// <summary>
    /// ⌨️ Q, W, E 키 입력을 처리하여 플레이어의 손을 변경합니다.
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 현재 미션이 활성화된 상태에서만 키 입력을 처리합니다.
        if (!isMissionActive) return;

        int newHandVal = 0; // 0으로 초기화

        if (Input.GetKeyDown(KeyCode.Q))
        {
            newHandVal = HAND_LEFT; // Q 키는 왼쪽 (3)
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            newHandVal = HAND_MID; // W 키는 중앙 (4)
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            newHandVal = HAND_RIGHT; // E 키는 오른쪽 (5)
        }

        // 유효한 키 입력 (3, 4, 5)이 들어왔고, 이전 손 모양과 다를 때만 처리
        if (newHandVal >= HAND_LEFT && newHandVal <= HAND_RIGHT && newHandVal != lastPlayerHandVal)
        {
            // 1. lastPlayerHandVal 업데이트
            lastPlayerHandVal = newHandVal;

            // 2. 즉시 게임 판정 로직 수행
            ProcessHandData(newHandVal);
        }
    }
    // 🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟

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

                Debug.Log($"[CCC_Webcam] Received text: '{text}'");
                int detectedHandVal = int.Parse(text.Trim());

                // 참참참은 3, 4, 5 값만 사용한다고 가정하고 범위를 벗어나면 무시
                if (detectedHandVal < HAND_LEFT || detectedHandVal > HAND_RIGHT) continue;


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
            // 🚨 [수정] ThreadAbortException을 명시적으로 처리
            catch (System.Threading.ThreadAbortException)
            {
                // 스레드 강제 종료는 정상적인 종료 절차이므로 아무것도 하지 않고 함수를 빠져나갑니다.
                return;
            }
            // 🚨 [수정] 경고 제거를 위해 변수 이름 제거
            catch (Exception)
            {
                // Debug.LogError(err.ToString()); 
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

        // InGameManager에서 partTime 값을 가져와 cccTimeLimit을 설정
        if (InGameManager.Instance != null)
        {
            cccTimeLimit = InGameManager.Instance.partTime;
        }

        // 1. 컴퓨터가 낼 손(opponentHand)과 미션(currentMission)을 랜덤으로 결정
        int opponentHand = UnityEngine.Random.Range(HAND_LEFT, HAND_RIGHT + 1); // 3, 4, 5 중 하나
        InGameManager.Instance.oppoentHandChange(opponentHand); // 컴퓨터 손 모양 화면에 표시

        currentMission = (ChamMission)UnityEngine.Random.Range(0, 2); // 0(Follow) 또는 1(Avoid)
        instructionText.text = GetMissionString(currentMission); // 미션 텍스트 표시

        // 2. [중요] 제한 시간 타이머(코루틴) 시작
        // cccTimeLimit (예: 3초) 후에 자동으로 '최종 판정'을 시작
        cccTimerCoroutine = StartCoroutine(CCCMissionTimer());
    }

    /// <summary>
    /// [코루틴] 정해진 시간(cccTimeLimit)이 지나면 "최종 판정" 수행
    /// </summary>
    private IEnumerator CCCMissionTimer()
    {
        // 1. 현재 설정된 제한 시간 만큼 기다림
        yield return new WaitForSeconds(cccTimeLimit);

        // 2. [판정 시간] 코드가 실행될 때 (시간이 지났을 때)
        // 'lastPlayerHandVal'에 저장된 플레이어의 '마지막 손 모양'을 가져옴
        int finalPlayerHand = lastPlayerHandVal;

        // finalPlayerHand가 3, 4, 5 중 하나인지 확인 (3 미만이면 실패)
        bool success = (finalPlayerHand >= HAND_LEFT) && PerformCCCCheck(finalPlayerHand);

        // 3. [성공] 미션을 성공했다면
        if (success)
        {
            // 리워드 및 콤보/점수 획득
            InGameManager.Instance.gameClear();
            // 난이도 상승 (제한 시간 0.1초 감소)
            DecreaseTimeLimit();
        }
        // 4. [실패] 미션을 실패했다면 (시간 초과, 손 모양 틀림, 손 모양 없음)
        else
        {
            // 벌칙 (시간 감소, 콤보 리셋)
            InGameManager.Instance.gameFail();
        }

        // 5. 미션 종료
        isMissionActive = false;
        cccTimerCoroutine = null;
    }

    /// <summary>
    /// 미션 성공 시 난이도 상승 (제한 시간 감소)
    /// </summary>
    private void DecreaseTimeLimit()
    {
        // 현재 제한 시간이 최소 시간(0.5초)보다 클 때만
        if (cccTimeLimit > cccMinTimeLimit)
        {
            cccTimeLimit -= cccTimeDecrement; // 0.1초 감소

            // 0.1초를 뺐는데 최소 시간보다 작아졌으면, 최소 시간으로 고정
            if (cccTimeLimit < cccMinTimeLimit)
            {
                cccTimeLimit = cccMinTimeLimit;
            }
        }
    }

    /// <summary>
    /// 현재 플레이어 손(playerVal)이 미션을 성공했는지 여부(true/false)만 반환
    /// </summary>
    private bool PerformCCCCheck(int playerVal)
    {
        // [중요] checkOppoentHand()는 판정하는 '순간'에 호출하여 컴퓨터 손을 가져옴
        int opponentVal = InGameManager.Instance.checkOppoentHand();

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

    /// <summary>
    /// 미션 텍스트를 "따라 해라!" 또는 "피해라!"만 반환
    /// </summary>
    private string GetMissionString(ChamMission mission)
    {
        if (mission == ChamMission.Follow)
        {
            return "따라 해라!";
        }
        else
        {
            return "피해라!";
        }
    }

    /// <summary>
    /// [InGameManager.cs]에서 참참참 게임의 승패 판정이 필요할 때 호출하는 함수입니다. (필수 추가)
    /// </summary>
    public void ChamChamChamClearOrFali()
    {
        // 현재 진행 중인 미션이 있을 경우에만 판정합니다.
        if (isMissionActive && cccTimerCoroutine != null)
        {
            // 1. 코루틴(시간 제한 타이머)을 강제로 멈춥니다.
            StopCoroutine(cccTimerCoroutine);
            cccTimerCoroutine = null;

            // 2. 최종 손 모양을 가져와 승패를 즉시 판정합니다.
            int finalPlayerHand = lastPlayerHandVal;

            // finalPlayerHand가 3, 4, 5 중 하나인지 확인
            // 플레이어가 손을 냈는지 확인하고, 미션 성공 여부 판정
            bool success = (finalPlayerHand >= HAND_LEFT) && PerformCCCCheck(finalPlayerHand);

            if (success)
            {
                InGameManager.Instance.gameClear();
                DecreaseTimeLimit(); // 성공 시 난이도 상승
            }
            else
            {
                InGameManager.Instance.gameFail();
            }
        }

        // 3. 미션 종료 상태로 설정합니다. (다음 Update()에서 새 미션이 시작되도록)
        isMissionActive = false;
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