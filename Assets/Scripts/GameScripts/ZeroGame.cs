/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//C# 네트워킹(UDP)을 위해 필요한 기능
using System; // 기본 C# 기능
using System.Text; // 문자열 인코딩(UTF8)을 위해 필요
using System.Net; // IP 주소(IPAddress, IPEndPoint)를 위해 필요
using System.Net.Sockets; // UDP 통신(UdpClient, SocketException)을 위해 필요
using System.Threading; // 별도 스레드(Thread)를 실행하기 위해 필요

public class ZeroGame : MonoBehaviour
{
    // --- 손 모양 상수 (플레이어는 0, 1, 2 중 하나를 냄) ---
    private const int HAND_NONE = -1;

    private bool isAttackMode = true;       //현재 모드를 기억할 스위치
    private int playerCall;             // 게임이 랜덤으로 부른 숫자
    private int computerHand;           // 컴퓨터가 랜덤으로 낸 숫자 (0~2)
    private int playerHand;             // 플레이어가 낸 숫자

    void Start()
    {
        retryButton.gameObject.SetActive(false);
        backgroundPanel.color = baseColor;
        StartNewRound();
        UpdateScore();
    }

    void Update()
    {
        if (gameOver) return;

        int handInput = -1;
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) handInput = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) handInput = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) handInput = 2;

        if (handInput != -1)
        {
            Debug.Log($"[DEV] Keyboard Input: {handInput}");
            PlayerHandNumber(handInput);
        }
    }

    // 새로운 라운드를 시작하는 함수
    void StartNewRound()
    {
        if (gameOver) return;

        CancelInvoke();

        backgroundPanel.color = baseColor;

        playerCall = Random.Range(0, 5);    // 0~4 중 랜덤 숫자 선택

        // 풀 수 없는 문제 방지 로직
        int maxPossibleComputerHand = playerCall;
        int finalMax = Mathf.Min(2, maxPossibleComputerHand);
        int minComputerHand = Mathf.Max(0, playerCall - 2);
        computerHand = Random.Range(minComputerHand, finalMax + 1);

        // 공격/수비 모드 랜덤 결정
        isAttackMode = (Random.Range(0, 2) == 0);


        callText.text = $"Game Call: {playerCall}";
        computerHandText.text = $"Computer: {computerHand}";
        resultText.text = "Waiting...";

        if (isAttackMode)
        {
            modeText.text = "Attack!"; //숫자를 맞혀라!
        }
        else
        {
            modeText.text = "Defense!"; //숫자를 피해라!
        }

        Invoke("TimeOut", roundTime); // 제한 시간이 지나면 TimeOut() 실행
    }

    // 플레이어가 숫자를 입력했을 때 실행되는 함수
    public void PlayerHandNumber(int number)
    {
        if (gameOver) return;
        if (number < 0 || number > 2) return;

        playerHand = number;
        CancelInvoke("TimeOut");

        int totalHand = playerHand + computerHand;

        bool isSuccess = false; // 성공 여부를 저장할 변수

        if (isAttackMode)
        {
            //공격 모드 합계가 Call과 같아야 성공
            isSuccess = (totalHand == playerCall);
        }
        else
        {
            //수비 모드 합계가 Call과 달라야 성공
            isSuccess = (totalHand != playerCall);
        }

        if (isSuccess) // 정답인 경우 (공격 성공 또는 수비 성공)
        {
            backgroundPanel.color = successColor;
            resultText.text = "Success!";

            score++;
            UpdateScore();
            roundTime = Mathf.Max(1f, roundTime - 0.2f);
            Invoke("StartNewRound", 1.5f);
        }
        else // 틀린 경우
        {
            backgroundPanel.color = failColor;

            string failMsg = isAttackMode ? "실패! (못 맞힘)" : "실패! (피해야 함)";
            resultText.text = $"{failMsg} / ({computerHand} + {playerHand} = {totalHand})";

            GameOver();
        }
    }

    // 제한 시간이 초과되었을 때 실행되는 함수
    void TimeOut()
    {
        backgroundPanel.color = failColor;
        resultText.text = "Time Out!";
        GameOver();
    }

    // 게임 종료 처리 함수
    void GameOver()
    {
        gameOver = true;
        resultText.text = "Game Over";
        callText.text = "";
        computerHandText.text = "";
        modeText.text = "";
        retryButton.gameObject.SetActive(true);
    }

    // 점수 UI를 갱신하는 함수
    void UpdateScore()
    {
        scoreText.text = $"Score: {score}";
    }

    // 다시하기 버튼을 눌렀을 때 실행되는 함수
    public void RetryGame()
    {
        CancelInvoke();

        score = 0;
        roundTime = 3f; // 난이도 초기화
        gameOver = false;
        UpdateScore();

        retryButton.gameObject.SetActive(false);
        StartNewRound();
    }
}
*/