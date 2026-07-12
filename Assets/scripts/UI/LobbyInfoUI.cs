using TMPro;
using UnityEngine;

public class LobbyInfoUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI lobbyNameText;
	[SerializeField] private TextMeshProUGUI lobbyCodeText;

	private void Start()
	{
		var lobby = GameLobby.Instance.GetLobby();
		if (lobby == null) return;

		lobbyNameText.text = lobby.Name;

		if (!string.IsNullOrEmpty(lobby.LobbyCode))
		{
			lobbyCodeText.gameObject.SetActive(true);
			lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
		}
		else
		{
			lobbyCodeText.gameObject.SetActive(false);
		}
	}
}
