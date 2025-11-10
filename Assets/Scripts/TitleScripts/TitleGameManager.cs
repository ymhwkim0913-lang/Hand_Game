using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleGameManager : MonoBehaviour
{
    public GameObject introUI;

    // 게임Scene이 이동할 때, 애니메이션이 출력되게 바꿈
    void Start() {
        introUI.SetActive(true);
        DontDestroyOnLoad(introUI);
    }

}
