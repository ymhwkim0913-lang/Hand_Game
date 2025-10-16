using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ZeroGame : MonoBehaviour
{
    public TextMeshProUGUI callText;      // ������ �θ� ���ڸ� ǥ���ϴ� �ؽ�Ʈ
    public TextMeshProUGUI resultText;    // ���(����/����/���ӿ���)�� ǥ���ϴ� �ؽ�Ʈ
    public TextMeshProUGUI scoreText;     // ������ ǥ���ϴ� �ؽ�Ʈ
    public Button retryButton;            // �ٽ��ϱ� ��ư
    public Image backgroundPanel;         // ��� ���� �����ϴ� �г�

    private int playerCall;               // ������ �������� �θ� ����
    private int playerHand;               // �÷��̾ �� ����
    private int score = 0;                // ����
    private float roundTime = 3f;         // ���� �ð� (�� ����)
    private bool gameOver = false;        // ���� ���� ����
    private Color baseColor = new Color32(46, 46, 46, 255);      // �⺻ ���� (ȸ��)
    private Color successColor = new Color32(76, 175, 80, 255);  // ���� �� ����
    private Color failColor = new Color32(244, 67, 54, 255);     // ���� �� ����

    void Start()
    {
        retryButton.gameObject.SetActive(false); // ó������ �ٽ��ϱ� ��ư ����
        backgroundPanel.color = baseColor;       // ����� �⺻������ ����
        StartNewRound();                         // ù ���� ����
        UpdateScore();                           // ���� �ʱ�ȭ ǥ��
    }

    // ���ο� ���带 �����ϴ� �Լ�
    void StartNewRound()
    {
        if (gameOver) return; // ������ ���� ���¸� �������� ����

        CancelInvoke(); // ������ ����� Invoke �Լ� ���

        backgroundPanel.color = baseColor; // ���� ������ �� ������ �⺻������ �ʱ�ȭ

        playerCall = Random.Range(0, 3);   // 0~2 �� ���� ���� ����
        callText.text = $"���� ��: {playerCall}"; // ���� ���ڸ� ȭ�鿡 ǥ��
        resultText.text = "��� �����...";       // ��� �ؽ�Ʈ �ʱ�ȭ

        Invoke("TimeOut", roundTime); // ���� �ð��� ������ TimeOut() ����
    }

    // �÷��̾ ���ڸ� �Է����� �� ����Ǵ� �Լ�
    public void PlayerHandNumber(int number)
    {
        if (gameOver) return; // ���� ���� ���¸� ����

        playerHand = number;  // �Է��� ���ڸ� ����
        CancelInvoke("TimeOut"); // ���� �ð� Ÿ�̸� ���

        if (playerHand == playerCall) // ������ ���
        {
            backgroundPanel.color = successColor; // ����� ���� ������ ����
            resultText.text = "����!";
            score++;                              // ���� 1 ����
            UpdateScore();                        // ���� UI ����

            roundTime = Mathf.Max(0.5f, roundTime - 0.2f); // ���� �ð��� ���� ���� (�ּ� 0.5��)

            Invoke("StartNewRound", 1.5f); // ��� �� �� ���� ����
        }
        else // Ʋ�� ���
        {
            backgroundPanel.color = failColor; // ����� ���� ������ ����
            resultText.text = "����!";
            GameOver();                        // ���� ���� ó��
        }
    }

    // ���� �ð��� �ʰ��Ǿ��� �� ����Ǵ� �Լ�
    void TimeOut()
    {
        backgroundPanel.color = failColor; // �ð� �ʰ� �� ���� ���� ǥ��
        GameOver();                        // ���� ���� ó��
    }

    // ���� ���� ó�� �Լ�
    void GameOver()
    {
        gameOver = true;                   // ���� ���¸� ����� ����
        resultText.text = "���� ����";     // ��� �ؽ�Ʈ ǥ��
        callText.text = "";                 // �� ���� �ʱ�ȭ
        retryButton.gameObject.SetActive(true); // �ٽ��ϱ� ��ư ǥ��
    }

    // ���� UI�� �����ϴ� �Լ�
    void UpdateScore()
    {
        scoreText.text = $"����: {score}";
    }

    // �ٽ��ϱ� ��ư�� ������ �� ����Ǵ� �Լ�
    public void RetryGame()
    {
        CancelInvoke(); // ����� �Լ� ���

        score = 0;      // ���� �ʱ�ȭ
        roundTime = 3f; // ���� �ð� �ʱ�ȭ
        gameOver = false; // ���� ���¸� �ٽ� ���� �������� ����
        UpdateScore();    // ���� UI ����

        retryButton.gameObject.SetActive(false); // �ٽ��ϱ� ��ư ����
        StartNewRound();                         // �� ���� ����
    }
}
