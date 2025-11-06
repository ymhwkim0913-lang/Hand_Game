using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleGameManager : MonoBehaviour
{
    public GameObject introUI;

    // Start is called before the first frame update
    void Start() {
        introUI.SetActive(true);
        DontDestroyOnLoad(introUI);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
