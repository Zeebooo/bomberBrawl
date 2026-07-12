using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
	[SerializeField] private Button mainMenuButton;
	[SerializeField] private Button createGameButton;
	[SerializeField] private Button quickJoinButton;
	[SerializeField] private Button joinCodeButton;
	[SerializeField] private TMP_InputField joinCodeInputField;
	[SerializeField] private TMP_InputField playerNameInputField;
	[SerializeField] private LobbyCreateUI lobbyCreateUI;
	[SerializeField] private Transform lobbyListContainer;
	[SerializeField] private Transform lobbyListTemplate;

	private void Awake()
	{
		mainMenuButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			Loader.Load(Loader.Scene.MainMenuScene);
		});
		createGameButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			lobbyCreateUI.Show();
		});
		quickJoinButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			GameLobby.Instance.QuickJoin();

		});
		joinCodeButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			GameLobby.Instance.JoinLobbyByCode(joinCodeInputField.text);
		});

		lobbyListTemplate.gameObject.SetActive(false);
	}

	private void Start()
	{
		playerNameInputField.text = GameLobby.Instance.GetPlayerName();
		playerNameInputField.onValueChanged.AddListener((newName) =>
		{
			GameLobby.Instance.SetPlayerName(newName);
		});

		GameLobby.Instance.onLobbyListChanged += GameLobby_onLobbyListChanged;
		updateLobbyList(new List<Lobby>());
	}

	private void GameLobby_onLobbyListChanged(object sender, GameLobby.onLobbyListChangedEventArgs e)
	{
		updateLobbyList(e.lobbyList);
	}

	private void updateLobbyList(List<Lobby> lobbyList)
	{
		foreach (Transform child in lobbyListContainer)
		{
			if (child == lobbyListTemplate) continue;
			Destroy(child.gameObject);
		}

		foreach (Lobby lobby in lobbyList)
		{
			Transform lobbyTransform = Instantiate(lobbyListTemplate, lobbyListContainer);
			lobbyTransform.gameObject.SetActive(true);
			lobbyTransform.GetComponent<LobbyListSingleUI>().setLobby(lobby);
		}
	}

	private void OnDestroy()
	{
		GameLobby.Instance.onLobbyListChanged -= GameLobby_onLobbyListChanged;
	}
}
