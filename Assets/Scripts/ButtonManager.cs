using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{

    GameObject nowPanel = null;

    public void gameStart_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    public void gameSetting_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    public void gameHowToPlay_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    public void back_Button() {
        if (nowPanel != null) { 
            LeanTween.moveLocalX(nowPanel, -1500f, 0.3f).setEaseOutCirc(); 
        }
    }

    public void GAMESTART(Animator ani) {
        ani.Rebind();
        // SceneManager.LoadScene("¥Ÿ¿Ω Ω≈");
    }

    public void gameEnd_Button() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}
