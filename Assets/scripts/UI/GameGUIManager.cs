using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameGUIManager : MonoBehaviour
{
	[SerializeField] private GameObject guiPanelPrefab;
	[SerializeField] private Transform guiContainer;
	private readonly WaitForSeconds pollInterval = new(0.1f);
	private bool spawned = false;

	void Start()
	{
		GameStateHandler.Instance.onStateChanged += HandleStateChanged;

		if (GameStateHandler.Instance.isCountDownToStart() || GameStateHandler.Instance.isGameInProgress())
			StartCoroutine(SpawnPanels());
	}

	void OnDestroy()
	{
		if (GameStateHandler.Instance != null)
			GameStateHandler.Instance.onStateChanged -= HandleStateChanged;
	}

	private void HandleStateChanged(object sender, System.EventArgs e)
	{
		if (!spawned && (GameStateHandler.Instance.isCountDownToStart() || GameStateHandler.Instance.isGameInProgress()))
		{
			StartCoroutine(SpawnPanels());
		}
		if (GameStateHandler.Instance.isResultScreen())
		{
			HidePanels();
		}
	}

	private IEnumerator SpawnPanels()
	{
		spawned = true;
		int expectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
		var characters = new List<characterScript>();

		while (characters.Count < expectedCount)
		{
			characters.Clear();
			characters.AddRange(FindObjectsByType<characterScript>());
			yield return pollInterval;
		}

		for (int i = 0; i < characters.Count; i++)
		{
			var character = characters[i];
			string playerName = GameLobby.Instance.GetNetworkPlayerName(character.OwnerClientId);
			if (string.IsNullOrEmpty(playerName))
				playerName = $"Player {character.OwnerClientId + 1}";

			GameObject panel = Instantiate(guiPanelPrefab, guiContainer);
			panel.GetComponent<guiHusScript>().Setup(character, playerName);
		}
	}

	private void HidePanels()
	{
		foreach (Transform child in guiContainer)
		{
			child.gameObject.SetActive(false);
		}
	}
}
