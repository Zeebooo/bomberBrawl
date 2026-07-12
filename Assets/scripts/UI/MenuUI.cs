using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class MenuUI : MonoBehaviour
{
	[Header("Buttons")]
	[SerializeField] private Button playButton;
	[SerializeField] private Button infoButton;
	[SerializeField] private Button optionsButton;
	[SerializeField] private Button goBackButton;
	[SerializeField] private Button quitButton;

	[Header("Panels")]
	[SerializeField] private GameObject infoPanel;
	[SerializeField] private GameObject optionsPanel;

	public static MenuUI Instance { get; private set; }

	private void Awake()
	{
		playButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			Loader.Load(Loader.Scene.LobbyScene);
		});
		infoButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			infoPanel.SetActive(true);
			goBackButton.gameObject.SetActive(true);
		});
		optionsButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			optionsPanel.SetActive(true);
			goBackButton.gameObject.SetActive(true);
		});
		goBackButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			infoPanel.SetActive(false);
			optionsPanel.SetActive(false);
			goBackButton.gameObject.SetActive(false);
		});
		quitButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			Application.Quit();
		});
		Instance = this;
	}

	private void Start()
	{
		OptionsUI.Instance.ApplyMusicVolume(PlayerPrefs.GetFloat("MusicVolume"));
		OptionsUI.Instance.ApplySFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
		OptionsUI.Instance.ApplyClickSFXVolume(PlayerPrefs.GetFloat("ClickSFXVolume"));
		infoPanel.SetActive(false);
		//optionsPanel.SetActive(false);
		goBackButton.gameObject.SetActive(false);
	}
}
