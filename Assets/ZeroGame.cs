using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ZeroGame : MonoBehaviour
{
    public TextMeshProUGUI callText;      // 게임이 부른 숫자를 표시하는 텍스트
    public TextMeshProUGUI resultText;    // 결과(성공/실패/게임오버)를 표시하는 텍스트
    public TextMeshProUGUI scoreText;     // 점수를 표시하는 텍스트
    public Button retryButton;            // 다시하기 버튼
    public Image backgroundPanel;         // 배경 색을 제어하는 패널

    private int playerCall;               // 게임이 랜덤으로 부른 숫자
    private int playerHand;               // 플레이어가 낸 숫자
    private int score = 0;                // 점수
    private float roundTime = 3f;         // 제한 시간 (초 단위)
    private bool gameOver = false;        // 게임 종료 여부
    private Color baseColor = new Color32(46, 46, 46, 255);      // 기본 배경색 (회색)
    private Color successColor = new Color32(76, 175, 80, 255);  // 성공 시 색상
    private Color failColor = new Color32(244, 67, 54, 255);     // 실패 시 색상

    void Start()
    {
        retryButton.gameObject.SetActive(false); // 처음에는 다시하기 버튼 숨김
        backgroundPanel.color = baseColor;       // 배경을 기본색으로 설정
        StartNewRound();                         // 첫 라운드 시작
        UpdateScore();                           // 점수 초기화 표시
    }

    // 새로운 라운드를 시작하는 함수
    void StartNewRound()
    {
        if (gameOver) return; // 게임이 끝난 상태면 실행하지 않음

        CancelInvoke(); // 이전에 예약된 Invoke 함수 취소

        backgroundPanel.color = baseColor; // 라운드 시작할 때 배경색을 기본색으로 초기화

        playerCall = Random.Range(0, 3);   // 0~2 중 랜덤 숫자 선택
        callText.text = $"게임 콜: {playerCall}"; // 랜덤 숫자를 화면에 표시
        resultText.text = "결과 대기중...";       // 결과 텍스트 초기화

        Invoke("TimeOut", roundTime); // 제한 시간이 지나면 TimeOut() 실행
    }

    // 플레이어가 숫자를 입력했을 때 실행되는 함수
    public void PlayerHandNumber(int number)
    {
        if (gameOver) return; // 게임 종료 상태면 무시

        playerHand = number;  // 입력한 숫자를 저장
        CancelInvoke("TimeOut"); // 제한 시간 타이머 취소

        if (playerHand == playerCall) // 정답인 경우
        {
            backgroundPanel.color = successColor; // 배경을 성공 색으로 변경
            resultText.text = "성공!";
            score++;                              // 점수 1 증가
            UpdateScore();                        // 점수 UI 갱신

            roundTime = Mathf.Max(0.5f, roundTime - 0.2f); // 제한 시간을 점점 줄임 (최소 0.5초)

            Invoke("StartNewRound", 1.5f); // 잠시 후 새 라운드 시작
        }
        else // 틀린 경우
        {
            backgroundPanel.color = failColor; // 배경을 실패 색으로 변경
            resultText.text = "실패!";
            GameOver();                        // 게임 종료 처리
        }
    }

    // 제한 시간이 초과되었을 때 실행되는 함수
    void TimeOut()
    {
        backgroundPanel.color = failColor; // 시간 초과 시 실패 색상 표시
        GameOver();                        // 게임 종료 처리
    }

    // 게임 종료 처리 함수
    void GameOver()
    {
        gameOver = true;                   // 게임 상태를 종료로 설정
        resultText.text = "게임 오버";     // 결과 텍스트 표시
        callText.text = "";                 // 콜 숫자 초기화
        retryButton.gameObject.SetActive(true); // 다시하기 버튼 표시
    }

    // 점수 UI를 갱신하는 함수
    void UpdateScore()
    {
        scoreText.text = $"점수: {score}";
    }

    // 다시하기 버튼을 눌렀을 때 실행되는 함수
    public void RetryGame()
    {
        CancelInvoke(); // 예약된 함수 취소

        score = 0;      // 점수 초기화
        roundTime = 3f; // 제한 시간 초기화
        gameOver = false; // 게임 상태를 다시 진행 가능으로 변경
        UpdateScore();    // 점수 UI 갱신

        retryButton.gameObject.SetActive(false); // 다시하기 버튼 숨김
        StartNewRound();                         // 새 라운드 시작
    }
}
