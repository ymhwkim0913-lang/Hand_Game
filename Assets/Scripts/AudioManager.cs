using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private AudioSource as1;
    [SerializeField]
    private AudioSource as2;
    [SerializeField]
    private AudioSource sfxSource;
    [SerializeField]
    private List<AudioClip> audioClips = new List<AudioClip>();


    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void playSFX(int index) {
        // 유효한 인덱스인지 확인
        if (index >= 0 && index < audioClips.Count) {
            // 이미 재생 중인 오디오가 있으면 정지 (필요에 따라 주석 처리 가능)
            sfxSource.Stop();

            // 선택한 클립 할당 및 재생
            sfxSource.clip = audioClips[index];
            sfxSource.Play();
        }
        else {
            Debug.LogWarning($"유효하지 않은 오디오 인덱스입니다: {index}");
        }
    }

    public void audioPlay(AudioSource audio) {
        audio.Play();
    }

    public void gameStart() {
        LeanTween.value(gameObject, 1f, 0f, 0.7f).
            setOnUpdate((float val) => {
                as1.volume = val;
        });
        as2.Play();
    }

}
