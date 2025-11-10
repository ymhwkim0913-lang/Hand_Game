using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour //참참참
{
    public enum Direction {
        Left,
        Right
    }

    [Header("UI 요소")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreText;
    public Image computerChoiceImage;

    [Header("게임 오브젝트")]
    public RectTransform turretHead;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform canvasTransform;
    public GameObject gameOverPanel;
    public GameObject startMenuPanel;
    public GameObject gameUIPanel;

    [Header("스프라이트 이미지")]
    public Sprite leftArrowSprite;
    public Sprite rightArrowSprite;

    private int score = 0;
    private bool isRoundRunning = false;

    void Start()
    {
        startMenuPanel.SetActive(true);
        gameUIPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!gameUIPanel.activeSelf || isRoundRunning) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine(PlayRound(Direction.Left));
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(PlayRound(Direction.Right));
        }
    }

    IEnumerator PlayRound(Direction playerChoice)
    {
        isRoundRunning = true;

        int computerChoiceInt = Random.Range(0, 2);
        Direction computerChoice = (Direction)computerChoiceInt;

        if (computerChoice == Direction.Left)
        {
            computerChoiceImage.sprite = leftArrowSprite;
            turretHead.rotation = Quaternion.Euler(0, 0, 70);
        }
        else
        {
            computerChoiceImage.sprite = rightArrowSprite;
            turretHead.rotation = Quaternion.Euler(0, 0, -70);
        }

        if (playerChoice == computerChoice)
        {
            resultText.text = "결과: 패배!";
            yield return new WaitForSeconds(0.5f);
            turretHead.rotation = Quaternion.Euler(0, 0, 0);
            yield return new WaitForSeconds(0.5f);
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation, canvasTransform);

            yield return new WaitForSeconds(1f);
            gameOverPanel.SetActive(true);
        }
        else
        {
            resultText.text = "결과: 승리!";
            score++;
            scoreText.text = "Score: " + score;

            yield return new WaitForSeconds(2f);

            turretHead.rotation = Quaternion.Euler(0, 0, 0);
            resultText.text = "결과";
            isRoundRunning = false;
        }
    }

    public void StartGame()
    {
        startMenuPanel.SetActive(false);
        gameUIPanel.SetActive(true);

        score = 0;
        scoreText.text = "Score: " + score;
        resultText.text = "결과";
        isRoundRunning = false;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}