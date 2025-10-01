using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RPS_Webcam_Controller : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image resultImage;
    [SerializeField] private Image playerChoiceImage;
    [SerializeField] private Image computerChoiceImage;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Game Sprites")]
    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Sprite paperSprite;
    [SerializeField] private Sprite scissorsSprite;
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private Sprite drawSprite;
    [SerializeField] private Sprite questionMarkSprite;

    // [수정] GameState의 Playing을 대문자로 통일했습니다.
    private enum GameState { Ready, Playing }
    private GameState currentState = GameState.Ready;

    private enum HandShape { None, Rock, Paper, Scissors }
    private HandShape playerHandShape = HandShape.None;
    private HandShape computerHandShape = HandShape.None;

    void Start()
    {
        ResetGame();
    }

    void Update()
    {
        if (currentState == GameState.Ready)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                playerHandShape = HandShape.Scissors;
                PlayRound();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                playerHandShape = HandShape.Rock;
                PlayRound();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                playerHandShape = HandShape.Paper;
                PlayRound();
            }
        }
    } // [수정] Update 함수는 여기서 끝납니다. 다른 함수들은 이 밖으로 나와야 합니다.

    private void PlayRound()
    {
        StartCoroutine(PlayRoundCoroutine());
    }

    private IEnumerator PlayRoundCoroutine()
    {
        // [수정] GameState.playing을 GameState.Playing으로 변경했습니다.
        currentState = GameState.Playing;
        instructionText.text = "";

        playerChoiceImage.sprite = ConvertHandShapeToSprite(playerHandShape);
        computerHandShape = (HandShape)Random.Range(1, 4);
        computerChoiceImage.sprite = ConvertHandShapeToSprite(computerHandShape);

        yield return new WaitForSeconds(1.0f);

        resultImage.gameObject.SetActive(true);
        if (playerHandShape == computerHandShape)
        {
            resultImage.sprite = drawSprite;
        }
        else if ((playerHandShape == HandShape.Rock && computerHandShape == HandShape.Scissors) ||
                 (playerHandShape == HandShape.Scissors && computerHandShape == HandShape.Paper) ||
                 (playerHandShape == HandShape.Paper && computerHandShape == HandShape.Rock))
        {
            resultImage.sprite = winSprite;
        }
        else
        {
            resultImage.sprite = loseSprite;
        }

        yield return new WaitForSeconds(2.0f);

        ResetGame();
    }

    // [수정] ResetGame 함수를 코루틴 밖으로 꺼내 독립적인 함수로 만들었습니다.
    private void ResetGame()
    {
        currentState = GameState.Ready;

        resultImage.gameObject.SetActive(false);
        playerChoiceImage.sprite = questionMarkSprite;
        computerChoiceImage.sprite = questionMarkSprite;

        instructionText.text = "키를 눌러주세요 (1:가위, 2:바위, 3:보)";
    }

    // [수정] 불필요한 함수 바깥의 코드를 모두 삭제했습니다.
    // [수정] OnDestroy와 같은 주석 처리된 함수들도 깔끔하게 정리했습니다.

    private Sprite ConvertHandShapeToSprite(HandShape shape)
    {
        switch (shape)
        {
            case HandShape.Rock: return rockSprite;
            case HandShape.Paper: return paperSprite;
            case HandShape.Scissors: return scissorsSprite;
            default: return questionMarkSprite;
        }
    }
}