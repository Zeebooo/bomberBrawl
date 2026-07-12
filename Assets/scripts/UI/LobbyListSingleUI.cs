using System.Collections;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListSingleUI : MonoBehaviour
{
	[SerializeField] private TMPro.TextMeshProUGUI lobbyNameText;
	[SerializeField] private TMPro.TextMeshProUGUI lobbyPlayersText;

	private Lobby lobby;

	public void Awake()
	{
		GetComponent<Button>().onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			foreach (var lobbyItem in FindObjectsByType<LobbyListSingleUI>())
			{
				StartCoroutine(lobbyItem.turnOffButtonForSeconds(2f));
			}
			GameLobby.Instance.JoinLobbyById(lobby.Id);
		});
	}

	private void Start()
	{
		GetComponent<Button>().interactable = true;
	}

	public void setLobby(Lobby lobby)
	{
		this.lobby = lobby;
		lobbyNameText.text = lobby.Name;
		int currentPlayers = lobby.MaxPlayers - lobby.AvailableSlots;
		lobbyPlayersText.text = $"{currentPlayers}/{lobby.MaxPlayers}";
	}

	public IEnumerator turnOffButtonForSeconds(float seconds)
	{
		GetComponent<Button>().interactable = false;
		yield return new WaitForSeconds(seconds);
		GetComponent<Button>().interactable = true;
	}
}
