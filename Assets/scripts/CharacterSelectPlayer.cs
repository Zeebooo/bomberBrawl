using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectPlayer : MonoBehaviour
{
	[SerializeField] private int playerIndex;
	[SerializeField] private GameObject readyIndicator;
	[SerializeField] private TextMeshProUGUI playerNameText;

	private EventHandler onPlayerDataChanged;
	private EventHandler onStateChanged;
	private EventHandler onPlayerReadyChanged;

	private void Start()
	{
		/*onPlayerDataChanged = (sender, args) => updatePlayer();
		onStateChanged = (sender, args) => updatePlayer();
		onPlayerReadyChanged = (sender, args) => updatePlayer();*/
		GameLobby.Instance.onPlayerDataNetworkListChanged += onPlayerDataChanged;
		CharacterSelectReady.Instance.OnStateChanged += onStateChanged;
		CharacterSelectReady.Instance.OnPlayerReadyChanged += onPlayerReadyChanged;
		//updatePlayer();
	}

	private void OnDestroy()
	{
		if (GameLobby.Instance != null)
			GameLobby.Instance.onPlayerDataNetworkListChanged -= onPlayerDataChanged;
		if (CharacterSelectReady.Instance != null)
		{
			CharacterSelectReady.Instance.OnStateChanged -= onStateChanged;
			CharacterSelectReady.Instance.OnPlayerReadyChanged -= onPlayerReadyChanged;
		}
	}

	private void updatePlayer()
	{
		ulong clientId = CharacterSelectReady.Instance.GetClientIdByIndex(playerIndex);
		if (playerIndex < CharacterSelectReady.Instance.GetConnectedCount() && clientId != NetworkManager.Singleton.LocalClientId)
		{
			Show();
			if (playerNameText != null)
			{
				//playerNameText.text = CharacterSelectReady.Instance.GetPlayerNameByIndex(playerIndex);
			}

			readyIndicator.SetActive(CharacterSelectReady.Instance.isPlayerReady(clientId));
		}
		else
		{
			Hide();
		}
	}

	private void Show()
	{
		gameObject.SetActive(true);
	}

	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
