using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }

	[SerializeField] private AudioSource musicSource;
	[SerializeField] private AudioSource sfxSource;
	[SerializeField] private AudioSource clickSfxSource;
	[SerializeField] private AudioRefsSO audioRefs;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public void PlaySFX(AudioClip clip)
	{
		sfxSource.pitch = Random.Range(0.9f, 1.1f);
		sfxSource.PlayOneShot(clip);
	}

	public void PlayClickSFX(AudioClip clip)
	{
		clickSfxSource.pitch = Random.Range(0.9f, 1.1f);
		clickSfxSource.PlayOneShot(clip);
	}

	public void PlayMusic(AudioClip clip)
	{
		if (musicSource.clip == clip && musicSource.isPlaying) return;
		musicSource.clip = clip;
		musicSource.Play();
	}

	public void StopMusic() => musicSource.Stop();

	public AudioRefsSO AudioRefs => audioRefs;
}
