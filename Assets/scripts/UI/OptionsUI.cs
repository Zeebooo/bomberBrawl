using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
	[SerializeField] private AudioMixer audioMixer;

	[Header("Music Volume")]
	[SerializeField] private Slider musicVolumeSlider;
	[SerializeField] private TextMeshProUGUI musicVolumeValueText;
	private float musicPreviewDuration = 2f;


	[Header("SFX Volume")]
	[SerializeField] private Slider sfxVolumeSlider;
	[SerializeField] private TextMeshProUGUI sfxVolumeValueText;

	[Header("ClickSFX Volume")]
	[SerializeField] private Slider clickSfxVolumeSlider;
	[SerializeField] private TextMeshProUGUI clickSfxVolumeValueText;

	[Header("Surrender")]
	[SerializeField] private Button surrenderButton;

	private const string MUSIC_VOL_KEY = "MusicVolume";
	private const string SFX_VOL_KEY = "SFXVolume";
	private const string CLICK_SFX_VOL_KEY = "ClickSFXVolume";
	private Coroutine musicPreviewCoroutine;
	public static OptionsUI Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		if (surrenderButton != null) surrenderButton.gameObject.SetActive(false);

		AddReleaseListener(musicVolumeSlider, () =>
		{
			if (GameStateHandler.Instance == null || !GameStateHandler.Instance.isGameInProgress())
			{
				if (musicPreviewCoroutine != null)
				{
					StopCoroutine(musicPreviewCoroutine);
				}
				musicPreviewCoroutine = StartCoroutine(PreviewMusic(musicPreviewDuration));
			}
		});
		AddReleaseListener(sfxVolumeSlider, () =>
		{
			SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.explosionsSFX[0]);
		});
		AddReleaseListener(clickSfxVolumeSlider, () =>
		{
			int roll = Random.Range(0, 2);
			if (roll == 0)
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			else
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
		});
	}

	private void Start()
	{
		musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1f);
		sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
		clickSfxVolumeSlider.value = PlayerPrefs.GetFloat(CLICK_SFX_VOL_KEY, 1f);

		if (GameStateHandler.Instance == null || !GameStateHandler.Instance.isGameInProgress())
		{
			ApplyMusicVolume(musicVolumeSlider.value);
			ApplySFXVolume(sfxVolumeSlider.value);
			ApplyClickSFXVolume(clickSfxVolumeSlider.value);
			gameObject.SetActive(false);
		}

		musicVolumeSlider.onValueChanged.AddListener(ApplyMusicVolume);
		sfxVolumeSlider.onValueChanged.AddListener(ApplySFXVolume);
		clickSfxVolumeSlider.onValueChanged.AddListener(ApplyClickSFXVolume);
	}

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	public void addSurrenderListener()
	{
		surrenderButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			changeActiveStatus();
			GameStateHandler.Instance.SurrenderServerRpc();
			GameLobby.Instance.LeaveLobby();
		});
		surrenderButton.gameObject.SetActive(true);
	}

	public void ApplyMusicVolume(float value)
	{
		audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
		musicVolumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
		PlayerPrefs.SetFloat(MUSIC_VOL_KEY, value);
	}

	public void tempApplyMusicVolume(float value)
	{
		audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
	}
	public void playerPrefsSetMusicVolume()
	{
		float value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, musicVolumeSlider.value);
		audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
	}

	public void ApplySFXVolume(float value)
	{
		audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
		sfxVolumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
		PlayerPrefs.SetFloat(SFX_VOL_KEY, value);
	}

	public void ApplyClickSFXVolume(float value)
	{
		audioMixer.SetFloat("ClickSFXVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
		clickSfxVolumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
		PlayerPrefs.SetFloat(CLICK_SFX_VOL_KEY, value);
	}

	private IEnumerator PreviewMusic(float duration)
	{
		SoundManager.Instance.PlayMusic(SoundManager.Instance.AudioRefs.backgroundMusic[0]);
		yield return new WaitForSeconds(duration);
		SoundManager.Instance.StopMusic();
		musicPreviewCoroutine = null;
	}

	private void AddReleaseListener(Slider slider, UnityAction callback)
	{
		if (!slider.gameObject.TryGetComponent(out EventTrigger trigger))
			trigger = slider.gameObject.AddComponent<EventTrigger>();
		var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
		entry.callback.AddListener(_ => callback());
		trigger.triggers.Add(entry);
	}

	public void changeActiveStatus()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}
}
