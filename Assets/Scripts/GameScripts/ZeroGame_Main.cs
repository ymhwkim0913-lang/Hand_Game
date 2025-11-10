using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Unity UI(Text, Button 등)를 사용하기 위해 필요
using TMPro;

// C# 네트워킹(UDP)을 위해 필요한 using
using System; // 기본 C# 기능
using System.Text; // 문자열 인코딩(UTF8)을 위해 필요
using System.Net; // IP 주소(IPAddress, IPEndPoint)를 위해 필요
using System.Net.Sockets; // UDP 통신(UdpClient, SocketException)을 위해 필요
using System.Threading; // 별도 스레드(Thread)를 실행하기 위해 필요

public class ZeroGame_Main : MonoBehaviour
{
    //Inspector(인스펙터) 창에서 연결할 UI 요소들
    [Header("UI (미션 표시용)")]
    [SerializeField] private TextMeshProUGUI instructionText; // "공격! 목표: 3"과 같은 미션 텍스트 UI

    [Header("게임 상태 참조용")]
    [SerializeField] private TextMeshProUGUI nowGame_Text_Reference; // InGameManager가 표시하는 현재 게임 이름("제로게임") 텍스트

    //손 모양 상수 (플레이어는 0, 1, 2 중 하나를 내야함)
    private const int HAND_NONE = -1; // 플레이어가 손을 내지 않았거나 인식되지 않은 상태

    //제로게임 관련 변수
    private int playerCall;             // 목표 합계 숫자 (0~4)
    private int computerHand;           // 컴퓨터가 낸 숫자 (0~2)
    private bool isAttackMode = true;   // 공격(true) 또는 수비(false) 모드

    //내부 상태 변수
    private Coroutine zeroTimerCoroutine; // 현재 실행 중인 타이머 코루틴 (중지/관리를 위함)
    private bool isMissionActive = false; // 현재 미션이 진행 중인지 (중복 시작 방지용 플래그)

    //UDP 네트워크 수신 관련 변수
    private Thread receiveThread; // UDP 데이터를 수신할 별도 스레드
    private UdpClient client; // UDP 클라이언트 객체
    private int port = 12345; // Python에서 데이터를 보낼 포트 번호 (Python과 일치해야 함)
    private int lastPlayerHandVal = HAND_NONE; // 네트워크 스레드가 마지막으로 인식한 플레이어의 손 (0, 1, 2)

    //스레드 간 데이터 공유를 위한 큐(Queue)
    // 네트워크 스레드(ReceiveData)가 데이터를 넣고 -> 메인 스레드(Update/ProcessQueue)가 데이터를 빼감
    private Queue<int> handDataQueue = new Queue<int>();
    private readonly object queueLock = new object(); // 큐 접근 시 스레드 충돌을 방지하기 위한 잠금(lock) 객체


    void Start()
    {
        // 1. 필수 UI가 연결되었는지 확인
        if (nowGame_Text_Reference == null)
        {
            Debug.LogError("ZeroGame_Main: 'Now Game Text Reference' not assigned!");
        }

        // 2. 큐 초기화 (이전 데이터가 남아있을 경우 대비)
        lock (queueLock)
        {
            handDataQueue.Clear();
        }

        // 3. UDP 수신 스레드 생성 및 시작
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true; // 프로그램 종료 시 스레드도 함께 종료되도록 설정
        receiveThread.Start();
    }

    // 매 프레임마다 호출되는 Unity 메인 루프
    void Update()
    {
        // 1. 큐(Queue)에 쌓인 데이터(손 모양)가 있으면 메인 스레드에서 처리
        ProcessQueue();

        // 2. 참조할 UI 텍스트가 없으면 더 이상 진행하지 않음
        if (nowGame_Text_Reference == null) return;

        // 3. InGameManager가 표시하는 현재 게임 이름이 "ZeroGame"인지 확인
        // [참고] InGameManager의 gameList에 "ZeroGame"이라고 되어 있어야 합니다. "제로게임"이면 여기도 수정해야 합니다.
        if (nowGame_Text_Reference.text == "ZeroGame" || nowGame_Text_Reference.text == "제로게임")
        {
            // 4. "제로게임"인데, 아직 미션이 시작되지 않았다면 (최초 1회 실행)
            if (!isMissionActive)
            {
                isMissionActive = true; // 미션이 시작됨을 표시 (Update에서 중복 실행 방지)
                StartNewZeroMission();  // 새 제로게임 미션 및 타이머 시작
            }
        }
        else
        {
            // 5. "제로게임"이 아니라면 다른 미니게임으로 넘어감
            //    모든 미션 상태를 리셋하고, 혹시나 실행 중인 타이머가 있다면 중지
            isMissionActive = false;
            if (zeroTimerCoroutine != null)
            {
                StopCoroutine(zeroTimerCoroutine);
                zeroTimerCoroutine = null;
            }
        }
    }

    /// <summary>
    /// (네트워크 스레드) Python에서 보낸 데이터를 큐에 넣기
    /// </summary>
    private void ReceiveData()
    {
        client = new UdpClient(port); // 지정된 포트에서 수신 대기
        while (true) // 프로그램 실행 중 계속 반복
        {
            try
            {
                // 1. 데이터 수신 대기
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0); // 모든 IP로부터 데이터 수신
                byte[] data = client.Receive(ref anyIP); // 데이터가 올 때까지 여기서 대기

                // 2. 바이트 데이터를 문자열로 변환 (UTF-8)
                string text = Encoding.UTF8.GetString(data);

                // 3. 문자열을 정수(int)로 변환 (Python에서 "0", "1", "2"를 보냄)
                int detectedHandVal = int.Parse(text.Trim());

                // 4. 유효한 값(0, 1, 2)인지 확인
                if (detectedHandVal >= 0 && detectedHandVal <= 2)
                {
                    // 5. 이전에 인식된 손 모양과 값이 달라졌을 때만 처리 (성능 최적화)
                    if (detectedHandVal != lastPlayerHandVal)
                    {
                        // 6. '마지막 손 모양'을 업데이트 (타이머 종료 시 판정을 위함)
                        lastPlayerHandVal = detectedHandVal;

                        // 7. 현재 "제로게임" 미션이 활성화 상태일 때만 큐에 데이터를 추가
                        if (isMissionActive)
                        {
                            // 8. (중요) 큐에 접근할 때 lock을 걸어 메인 스레드와의 충돌 방지
                            lock (queueLock)
                            {
                                handDataQueue.Enqueue(detectedHandVal);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                // 스레드 종료 시 또는 데이터 변환 오류 시 예외 발생 가능
                Debug.LogWarning(err.ToString());
            }
        }
    }

    /// <summary>
    /// (메인 스레드) 큐에서 데이터를 꺼내 게임 로직 처리
    /// </summary>
    private void ProcessQueue()
    {
        // 큐에 데이터가 있는 동안 반복
        while (handDataQueue.Count > 0)
        {
            int handVal; // 꺼낸 데이터를 저장할 변수

            // (중요) 큐에 접근할 때 lock
            lock (queueLock)
            {
                handVal = handDataQueue.Dequeue(); // 큐에서 데이터 하나를 꺼냄
            }

            // 큐에서 꺼냈을 때도 여전히 미션이 활성 상태인지 재확인
            if (isMissionActive)
            {
                // 손 모양을 화면에 표시하는 함수 호출
                ProcessHandData(handVal);
            }
        }
    }

    /// <summary>
    /// (메인 스레드) 인식된 엄지 개수를 InGameManager에 전달 (화면 업데이트용)
    /// </summary>
    private void ProcessHandData(int playerVal) // playerVal은 0, 1, 2
    {
        // [핵심 매핑]
        // InGameManager의 handList 배열에서 제로게임 손 모양은 6, 7, 8번 인덱스에 있습니다.
        // ("zero_0", "zero_1", "zero_2")
        // 따라서 Python에서 받은 0, 1, 2 값에 6을 더해야 올바른 리소스 인덱스가 됩니다.
        int handResourceIndex = 6 + playerVal;

        // InGameManager의 싱글톤 인스턴스를 통해 플레이어 손 모양 파티클 변경 함수 호출
        InGameManager.Instance.playerHandChange(handResourceIndex);
    }

    /// <summary>
    /// 새 제로게임 미션 시작 (ZeroGame.cs 테스트 로직 기반)
    /// </summary>
    private void StartNewZeroMission()
    {
        // 1. 플레이어의 마지막 손 모양을 '없음'으로 초기화
        lastPlayerHandVal = HAND_NONE;

        // 2. [미션 생성] ZeroGame.cs와 동일하게 목표 숫자(Call), 컴퓨터 손, 모드 결정
        playerCall = UnityEngine.Random.Range(0, 5); // 0~4 중 랜덤 숫자 선택
        isAttackMode = (UnityEngine.Random.Range(0, 2) == 0); // 50% 확률로 공격/수비 모드 결정

        // 3. [컴퓨터 손 결정] ZeroGame.cs의 로직 (플레이어가 항상 이길 수 있는 경우의 수를 만듦)
        int maxPossibleComputerHand = playerCall;
        int finalMax = Mathf.Min(2, maxPossibleComputerHand); // 컴퓨터는 최대 2개만 낼 수 있으므로 2로 제한
        int minComputerHand = Mathf.Max(0, playerCall - 2); // 플레이어가 최대 2개 낼 것을 고려하여 최소치 설정
        computerHand = UnityEngine.Random.Range(minComputerHand, finalMax + 1); // 0~2 사이에서 랜덤 선택

        // 4. [UI 표시] 미션 텍스트 업데이트
        instructionText.text = GetMissionString();

        // 5. [화면 표시] 컴퓨터 손 모양을 InGameManager를 통해 화면에 표시 (인덱스 6, 7, 8 매핑)
        int oppoHandResourceIndex = 6 + computerHand;
        InGameManager.Instance.oppoentHandChange(oppoHandResourceIndex);

        // 6. [타이머 시작] 제한 시간 타이머(코루틴) 시작
        // [수정] InGameManager의 partTime을 사용하도록 변경
        zeroTimerCoroutine = StartCoroutine(ZeroMissionTimer());
    }

    /// <summary>
    /// [코루틴] 정해진 시간(InGameManager.Instance.partTime)이 지나면 "최종 판정" 수행
    /// </summary>
    private IEnumerator ZeroMissionTimer()
    {
        // 1. InGameManager의 공용 난이도 시간(partTime) 만큼 기다림
        yield return new WaitForSeconds(InGameManager.Instance.partTime);

        // --- (여기부터는 시간이 지난 후에 실행됨) ---

        // 2. [판정 시간] 타이머가 끝나는 '순간'에, 'lastPlayerHandVal'에 저장된 
        //    플레이어의 '마지막 엄지 개수'를 가져옴
        int finalPlayerHandCount = lastPlayerHandVal;

        // 3. [판정 수행] ZeroGame.cs 로직 기반으로 성공/실패 판정
        bool success = PerformZeroGameCheck(finalPlayerHandCount);

        // 4. [결과 전송] InGameManager에 성공 또는 실패를 알림
        if (success)
        {
            InGameManager.Instance.gameClear();  // 성공 시 (점수/시간/콤보 보상)
            // [삭제] 난이도 조절은 InGameManager가 담당하므로 DecreaseTimeLimit() 호출 삭제
        }
        else
        {
            InGameManager.Instance.gameFail();   // 실패 시 (시간 감소/콤보 리셋)
        }

        // 5. [미션 종료]
        isMissionActive = false; // 미션이 끝났음을 표시 (Update()에서 다음 미션이 시작됨)
        zeroTimerCoroutine = null; // 코루틴 핸들 초기화
    }

    /// <summary>
    /// 플레이어 엄지 개수(playerHandCount)가 미션을 성공했는지 여부(true/false)만 반환
    /// </summary>
    private bool PerformZeroGameCheck(int playerHandCount)
    {
        // 1. [실패 조건 1] 플레이어가 시간 내에 아무것도 내지 않은 경우 (HAND_NONE)
        if (playerHandCount == HAND_NONE) return false;

        // 2. 합계 계산 (플레이어 손 + 컴퓨터 손)
        int totalHand = playerHandCount + computerHand;

        bool isSuccess = false; // 성공 여부 플래그

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

    /// <summary>
    /// 애플리케이션 종료 시 호출 (리소스 정리)
    /// </summary>
    void OnApplicationQuit()
    {
        // 1. 네트워크 스레드가 살아있으면 강제 종료 (Abort)
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        // 2. UDP 클라이언트 닫기
        if (client != null)
        {
            client.Close();
        }
    }

    /// <summary>
    /// 현재 미션 상태에 맞는 텍스트를 생성하여 반환
    /// </summary>
    private string GetMissionString()
    {
        // 1. 모드 텍스트 설정
        string modeStr = isAttackMode ? "공격!" : "수비!";

        // 2. 목표 텍스트 설정
        string callStr = $"목표: {playerCall}";

        // (참고용) 컴퓨터가 낸 손을 표시하고 싶다면
        // string oppoStr = $"(컴퓨터 {computerHand}개)";

        // 3. UI에 표시될 최종 텍스트 조합
        return $"{modeStr} {callStr}";
    }
}