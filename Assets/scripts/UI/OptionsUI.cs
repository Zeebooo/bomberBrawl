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

	[SerializeField] private Button resetKeybindsButton;

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

		if (resetKeybindsButton != null)
		{
			resetKeybindsButton.onClick.AddListener(() =>
			{
				SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
				ResetKeybindsToDefault();
			});
		}
	}

	private void Start()
	{
		musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.5f);
		sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.5f);
		clickSfxVolumeSlider.value = PlayerPrefs.GetFloat(CLICK_SFX_VOL_KEY, 0.5f);

		if (fullscreenToggle != null)
		{
			fullscreenToggle.isOn = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
			ApplyFullscreen(fullscreenToggle.isOn);
		}

		foreach (var slot in GetKeybindSlots())
		{
			if (slot.chosenText != null)
				slot.chosenText.text = DisplayTextForSavedKey(PlayerPrefs.GetString(slot.prefsKey, slot.defaultKey));
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
				// key.name är den interna, ospråkberoende identifieraren (t.ex. "space", "leftShift") och
				// används för sparning/uppslagning. key.displayName är lokaliserad av OS:et och används bara för visning.
				string savedKeyName = key.name;
				string displayText;
				if (savedKeyName == "leftShift" || savedKeyName == "rightShift")
				{
					savedKeyName = "shift";
					displayText = "SHIFT";
				}
				else if (savedKeyName == "leftCtrl" || savedKeyName == "rightCtrl")
				{
					savedKeyName = "ctrl";
					displayText = "CTRL";
				}
				else if (savedKeyName == "leftAlt" || savedKeyName == "rightAlt")
				{
					savedKeyName = "alt";
					displayText = "ALT";
				}
				else
				{
					displayText = key.displayName.ToUpper();
				}

				if (TextAbilityBindingDescription.text == "Press any key to bind...")
				{
					ClearCollisions(ABILITYBIND_KEY, savedKeyName);
					PlayerPrefs.SetString(ABILITYBIND_KEY, savedKeyName);
					abilityTextChosenKeybind.text = displayText;
					TextAbilityBindingDescription.text = keyDescriptionHolder;
				}
				else if (TextRemoteExplosionBindingDescription.text == "Press any key to bind...")
				{
					ClearCollisions(REMOTE_EXPLOSION_BIND_KEY, savedKeyName);
					PlayerPrefs.SetString(REMOTE_EXPLOSION_BIND_KEY, savedKeyName);
					RemoteExplosionTextChosenKeybind.text = displayText;
					TextRemoteExplosionBindingDescription.text = keyDescriptionHolder;
				}
				else if (TextDropBombExplosionBindingDescription.text == "Press any key to bind...")
				{
					ClearCollisions(DROP_BOMB_EXPLOSION_BIND_KEY, savedKeyName);
					PlayerPrefs.SetString(DROP_BOMB_EXPLOSION_BIND_KEY, savedKeyName);
					DropBombExplosionTextChosenKeybind.text = displayText;
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

	private struct KeybindSlot
	{
		public string prefsKey;
		public string defaultKey;
		public TextMeshProUGUI chosenText;
	}

	private KeybindSlot[] GetKeybindSlots()
	{
		return new[]
		{
			new KeybindSlot { prefsKey = ABILITYBIND_KEY, defaultKey = "Q", chosenText = abilityTextChosenKeybind },
			new KeybindSlot { prefsKey = REMOTE_EXPLOSION_BIND_KEY, defaultKey = "Shift", chosenText = RemoteExplosionTextChosenKeybind },
			new KeybindSlot { prefsKey = DROP_BOMB_EXPLOSION_BIND_KEY, defaultKey = "Space", chosenText = DropBombExplosionTextChosenKeybind },
		};
	}

	private string DisplayTextForSavedKey(string savedKey)
	{
		return string.IsNullOrEmpty(savedKey) ? "NONE" : savedKey.ToUpper();
	}

	// Om den nybundna tangenten redan används av en annan bindning, unbinda den andra helt istället för att låta två actions dela samma tangent.
	private void ClearCollisions(string prefsKeyBeingSet, string newKeyValue)
	{
		foreach (var slot in GetKeybindSlots())
		{
			if (slot.prefsKey == prefsKeyBeingSet) continue;

			string existing = PlayerPrefs.GetString(slot.prefsKey, slot.defaultKey);
			if (existing == newKeyValue)
			{
				PlayerPrefs.SetString(slot.prefsKey, "");
				if (slot.chosenText != null) slot.chosenText.text = "NONE";
			}
		}
	}

	public void ResetKeybindsToDefault()
	{
		foreach (var slot in GetKeybindSlots())
		{
			PlayerPrefs.SetString(slot.prefsKey, slot.defaultKey);
			if (slot.chosenText != null) slot.chosenText.text = slot.defaultKey.ToUpper();
		}
	}

	public void changeActiveStatus()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}
}
