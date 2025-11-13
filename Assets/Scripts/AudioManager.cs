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
    private AudioSource bgmSource;
    [SerializeField]
    private AudioSource sfxSource;
    [SerializeField]
    private List<AudioClip> sfxClips = new List<AudioClip>();
    [SerializeField]
    private List<AudioClip> bgmClips = new List<AudioClip>();

    private void Awake() {
        Instance = this;
        playBGM(0);
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// 매개변수 index에 해당하는 효과음을 재생
    /// </summary>
    /// <param name="index"> 
    /// <list type="bullet">
    /// <item><description>0 : 인트로          </description></item>
    /// <item><description>1 : 초반 카운트     </description></item>
    /// <item><description>2 : 게임 중 카운트 3</description></item>
    /// <item><description>3 : 게임 중 카운트 2</description></item>
    /// <item><description>4 : 게임 중 카운트 1</description></item>
    /// <item><description>5 : 미션 텍스트     </description></item>
    /// </list>
    /// </param>
    public void playSFX(int index) {
        if (index >= 0 && index < sfxClips.Count) {
            AudioClip clip = sfxClips[index];

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

    /// <summary>
    /// 매개변수 <paramref name="index"/> 에 해당하는 배경음악(BGM)을 재생
    /// </summary>
    /// <param name="index">
    /// <list type="bullet">
    /// <item><description>0 : 메인 테마</description></item>
    /// <item><description>1 : 인게임</description></item>
    /// </list>
    /// </param>
    public void playBGM(int index) {
        if (index >= 0 && index < bgmClips.Count) {
            AudioClip clip = bgmClips[index];

            // 기존 BGM 중지 후 새 BGM 교체
            bgmSource.Stop();
            bgmSource.volume = 1f;
            bgmSource.clip = clip;
            bgmSource.Play();
        }
        else {
            Debug.LogWarning($"유효하지 않은 BGM 인덱스입니다: {index}");
        }
    }

    // 소리 재생
    public void audioPlay(AudioSource audio) {
        audio.Play();
    }

    // 게임 시작 시, BGM을 확 끄지 않고 점점 줄이기
    public void gameStart() {

        // 현재 재생 중인 BGM 볼륨을 1초 동안 0으로 줄이기
        LeanTween.value(gameObject, bgmSource.volume, 0f, 0.7f)
            .setOnUpdate((float val) => {
                bgmSource.volume = val;
            })
            .setOnComplete(() => {
                // 기존 BGM 정지
                bgmSource.Stop();

                // 새로운 BGM 재생
                playSFX(0);
            });
    }
}
