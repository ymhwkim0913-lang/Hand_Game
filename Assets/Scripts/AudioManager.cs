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
        if (index >= 0 && index < audioClips.Count) {
            AudioClip clip = audioClips[index];

            // 새로운 오디오 소스 객체 생성
            GameObject sfxObject = new GameObject("SFX_" + clip.name);
            sfxObject.transform.parent = this.transform; // AudioManager 하위로 넣기

            AudioSource newSource = sfxObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            newSource.volume = sfxSource.volume;  // 기존 sfxSource의 설정을 복사
            newSource.pitch = sfxSource.pitch;
            newSource.spatialBlend = sfxSource.spatialBlend;
            newSource.Play();

            // 재생이 끝나면 자동으로 삭제
            Destroy(sfxObject, clip.length);
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
