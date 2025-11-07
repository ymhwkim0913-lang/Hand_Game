using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class CountDown : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI countdownText;

    [SerializeField] private GameObject ui_Panel;

    [SerializeField] private TextMeshProUGUI remainTimeText;

    [SerializeField] private CanvasGroup backGround;

    [SerializeField] private float countdownDuration = 1.0f;


    // Start is called before the first frame update
    void Start()
    {
        if (countdownText == null)
            return;

        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown() {

        countdownText.gameObject.SetActive(false);
        
        yield return new WaitForSeconds(2.5f);

        Intro_Animation();

        yield return new WaitForSeconds(4f);

        LeanTween.value(gameObject, 0f, 0.08f, 1f).
            setOnUpdate((float val) => {
                remainTimeText.alpha = val;
        });

        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        Animation();
        yield return new WaitForSeconds(countdownDuration);

        countdownText.text = "2";
        Animation();
        yield return new WaitForSeconds(countdownDuration);

        countdownText.text = "1";
        Animation();
        yield return new WaitForSeconds(countdownDuration);

        countdownText.gameObject.SetActive(false);

        InGameManager.Instance.GameStart();
    }

    private void Animation() {

        AudioManager.Instance.playSFX(0);
        countdownText.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

        LeanTween.scale(countdownText.gameObject, new Vector3(1f, 1f, 1f), 0.3f).
            setEase(LeanTweenType.easeOutBack);
    }

    private void Intro_Animation() {

        LeanTween.value(gameObject, 0f, 1f, 3f).
            setOnUpdate((float val) => {
        backGround.alpha = val;
    });

        ui_Panel.transform.localPosition = new Vector3(0f, 500f, 0f);

        LeanTween.moveLocalY(ui_Panel.gameObject, 0f, 3f).
            setEase(LeanTweenType.easeOutCirc);



    }

}
