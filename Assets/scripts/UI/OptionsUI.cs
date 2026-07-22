using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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

	[Header("Fullscreen")]
	[SerializeField] private Toggle fullscreenToggle;

	[Header("Key bindings")]
	[SerializeField] private Button ButtonAbilityKeyBinding;
	[SerializeField] private TextMeshProUGUI TextAbilityBindingDescription;
	[SerializeField] private TextMeshProUGUI abilityTextChosenKeybind;

	[SerializeField] private Button RemoteExplosionBindingsButton;
	[SerializeField] private TextMeshProUGUI TextRemoteExplosionBindingDescription;
	[SerializeField] private TextMeshProUGUI RemoteExplosionTextChosenKeybind;

	[SerializeField] private Button dropBombExplosionBindingButton;
	[SerializeField] private TextMeshProUGUI TextDropBombExplosionBindingDescription;
	[SerializeField] private TextMeshProUGUI DropBombExplosionTextChosenKeybind;


	private string keyDescriptionHolder;
	private bool isWaitingForKeybinds = false;

	private const string MUSIC_VOL_KEY = "MusicVolume";
	private const string SFX_VOL_KEY = "SFXVolume";
	private const string CLICK_SFX_VOL_KEY = "ClickSFXVolume";
	private const string FULLSCREEN_KEY = "Fullscreen";
	private const string ABILITYBIND_KEY = "AbilityBind";
	private const string REMOTE_EXPLOSION_BIND_KEY = "RemoteExplosionBind";
	private const string DROP_BOMB_EXPLOSION_BIND_KEY = "DropBombExplosionBind";
	private Coroutine musicPreviewCoroutine;
	public static OptionsUI Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		if (surrenderButton != null) surrenderButton.gameObject.SetActive(false);
		if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);

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

		if (ButtonAbilityKeyBinding != null && TextAbilityBindingDescription != null)
		{
			ButtonAbilityKeyBinding.onClick.AddListener(() =>
			{
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
				prepareChangeKeybind(TextAbilityBindingDescription);
			});
		}

		if (RemoteExplosionBindingsButton != null && TextRemoteExplosionBindingDescription != null)
		{
			RemoteExplosionBindingsButton.onClick.AddListener(() =>
			{
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
				prepareChangeKeybind(TextRemoteExplosionBindingDescription);
			});
		}

		if (dropBombExplosionBindingButton != null && TextDropBombExplosionBindingDescription != null)
		{
			dropBombExplosionBindingButton.onClick.AddListener(() =>
			{
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
				prepareChangeKeybind(TextDropBombExplosionBindingDescription);
			});
		}
	}

	private void Start()
	{
		musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1f);
		sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
		clickSfxVolumeSlider.value = PlayerPrefs.GetFloat(CLICK_SFX_VOL_KEY, 1f);

		if (fullscreenToggle != null)
		{
			fullscreenToggle.isOn = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
			ApplyFullscreen(fullscreenToggle.isOn);
		}

		if (abilityTextChosenKeybind != null)
		{
			abilityTextChosenKeybind.text = PlayerPrefs.GetString(ABILITYBIND_KEY, "Q").ToUpper();
		}

		if (RemoteExplosionTextChosenKeybind != null)
		{
			RemoteExplosionTextChosenKeybind.text = PlayerPrefs.GetString(REMOTE_EXPLOSION_BIND_KEY, "Shift").ToUpper();
		}

		if (DropBombExplosionTextChosenKeybind != null)
		{
			DropBombExplosionTextChosenKeybind.text = PlayerPrefs.GetString(DROP_BOMB_EXPLOSION_BIND_KEY, "Space").ToUpper();
		}

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

	private void Update()
	{
		if (!isWaitingForKeybinds || Keyboard.current == null) return;
		foreach (KeyControl key in Keyboard.current.allKeys)
		{
			if (key.wasPressedThisFrame && key != Keyboard.current.escapeKey)
			{
				string keyName = key.displayName;
				if (keyName == "Left Shift")
					keyName = "Shift";
				else if (keyName == "Right Shift")
					keyName = "Shift";
				else if (keyName == "Left Ctrl")
					keyName = "Ctrl";
				else if (keyName == "Right Ctrl")
					keyName = "Ctrl";
				else if (keyName == "Left Alt")
					keyName = "Alt";
				else if (keyName == "Right Alt")
					keyName = "Alt";

				if (TextAbilityBindingDescription.text == "Press any key to bind...")
				{
					PlayerPrefs.SetString(ABILITYBIND_KEY, keyName);
					abilityTextChosenKeybind.text = keyName.ToUpper();
					TextAbilityBindingDescription.text = keyDescriptionHolder;
				}
				else if (TextRemoteExplosionBindingDescription.text == "Press any key to bind...")
				{
					PlayerPrefs.SetString(REMOTE_EXPLOSION_BIND_KEY, keyName);
					RemoteExplosionTextChosenKeybind.text = keyName.ToUpper();
					TextRemoteExplosionBindingDescription.text = keyDescriptionHolder;
				}
				else if (TextDropBombExplosionBindingDescription.text == "Press any key to bind...")
				{
					PlayerPrefs.SetString(DROP_BOMB_EXPLOSION_BIND_KEY, keyName);
					DropBombExplosionTextChosenKeybind.text = keyName.ToUpper();
					TextDropBombExplosionBindingDescription.text = keyDescriptionHolder;
				}

				isWaitingForKeybinds = false;
				break;
			}
		}
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

	private void ApplyFullscreen(bool value)
	{
		Screen.fullScreen = value;
		PlayerPrefs.SetInt(FULLSCREEN_KEY, value ? 1 : 0);
	}

	private void prepareChangeKeybind(TextMeshProUGUI descriptionText)
	{
		isWaitingForKeybinds = true;
		keyDescriptionHolder = descriptionText.text;
		descriptionText.text = "Press any key to bind...";
	}

	public void changeActiveStatus()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}
}
