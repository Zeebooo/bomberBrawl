using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
	[Header("Buttons")]
	[SerializeField] private Button readyButton;
	[SerializeField] private Button unReadyButton;
	[SerializeField] private Button goBackButton;
	[SerializeField] private Button cycleUpButton;
	[SerializeField] private Button cycleDownButton;

	[Header("Character display")]
	[SerializeField] private RawImage portraitImage;
	[SerializeField] private TextMeshProUGUI characterNameText;
	[SerializeField] private TextMeshProUGUI statsText;
	[SerializeField] private TextMeshProUGUI abilityText;

	[Header("Other")]
	[SerializeField] private TextMeshProUGUI lobbyNameText;
	[SerializeField] private TextMeshProUGUI lobbyCodeText;
	[SerializeField] private TextMeshProUGUI playerNameText;
	[SerializeField] private Transform opponentStatusContainer;
	[SerializeField] private Transform opponentStatusTemplate;
	[SerializeField] private CharacterDataSO defaultData;

	private static readonly Vector3 PreviewPos = new(-300f, -100f, 0f);
	private GameObject previewInstance;
	private Camera previewCamera;
	private RenderTexture previewRT;

	private EventHandler onStateChanged;

	private void Awake()
	{
		readyButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			StartCoroutine(WaitThenSetPlayerReady());
		});
		unReadyButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			StartCoroutine(WaitThenSetPlayerUnready());
		});
		goBackButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			GameLobby.Instance.LeaveLobby();
		});
		cycleUpButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			CharacterSelectReady.Instance.UpdateCharacterIndexServerRpc(GetLocalPlayerData().selectedCharacterIndex + 1, NetworkManager.Singleton.LocalClientId);
		});
		cycleDownButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			CharacterSelectReady.Instance.UpdateCharacterIndexServerRpc(GetLocalPlayerData().selectedCharacterIndex - 1, NetworkManager.Singleton.LocalClientId);
		});
	}

	private IEnumerator WaitThenSetPlayerReady()
	{
		yield return new WaitForSeconds(SoundManager.Instance.AudioRefs.positiveClickSFX[0].length);
		CharacterSelectReady.Instance.setPlayerReady();
	}

	private IEnumerator WaitThenSetPlayerUnready()
	{
		yield return new WaitForSeconds(SoundManager.Instance.AudioRefs.positiveClickSFX[0].length);
		CharacterSelectReady.Instance.setPlayerUnready();
	}

	private void Start()
	{
		Lobby lobby = GameLobby.Instance.GetLobby();
		if (lobby != null)
		{
			lobbyNameText.text = "Lobby name: " + lobby.Name;
			lobbyCodeText.text = string.IsNullOrEmpty(lobby.LobbyCode) ? "" : "Lobby code: " + lobby.LobbyCode;
		}
		opponentStatusTemplate.gameObject.SetActive(false);

		GameLobby.Instance.onPlayerDataNetworkListChanged += HandlePlayerDataChanged;
		CharacterSelectReady.Instance.OnStateChanged += onStateChanged;
		CharacterSelectReady.Instance.OnPlayerReadyChanged += HandleReadyChanged;
		CharacterSelectReady.Instance.OnCharacterChanged += HandleCharacterChanged;

		playerNameText.text = GameLobby.Instance.GetPlayerName();

		StartCoroutine(SetupPreviewNextFrame());
		UpdateReadyButtons();
		updateOpponentList(new List<ulong>());
	}

	private void HandlePlayerDataChanged(object sender, EventArgs e)
	{
		updateOpponentList(new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds));
		UpdateCharacterDisplay();
	}

	private IEnumerator SetupPreviewNextFrame()
	{
		yield return null;
		UpdateCharacterDisplay();
	}

	private void OnDestroy()
	{
		if (CharacterSelectReady.Instance != null)
		{
			CharacterSelectReady.Instance.OnStateChanged -= onStateChanged;
			CharacterSelectReady.Instance.OnPlayerReadyChanged -= HandleReadyChanged;
			CharacterSelectReady.Instance.OnCharacterChanged -= HandleCharacterChanged;
		}
		if (GameLobby.Instance != null)
			GameLobby.Instance.onPlayerDataNetworkListChanged -= HandlePlayerDataChanged;
		CleanupPreview();
	}

	private void UpdateCharacterDisplay()
	{
		PlayerData localData = GetLocalPlayerData();
		if (GameLobby.Instance.characters == null || GameLobby.Instance.characters.Length == 0) return;

		int index = Mathf.Clamp(localData.selectedCharacterIndex, 0, GameLobby.Instance.characters.Length - 1);
		CharacterDataSO characterData = GameLobby.Instance.characters[index];

		string moveSpeedString = FormatPercent(characterData.moveSpeed / defaultData.moveSpeed);
		string radiusString = FormatPercent(characterData.explosionRadius / defaultData.explosionRadius);

		if (characterNameText != null) characterNameText.text = characterData.characterName;
		if (statsText != null) statsText.text = $"HP: {characterData.maxHealth}\nMovement Speed: {moveSpeedString}\nMax Bombs: {characterData.maxBombs}\nExplosion Radius: {radiusString}";
		if (abilityText != null) abilityText.text = $"{characterData.abilityName}\n{characterData.abilityDescription}";

		RefreshPreview(characterData.prefab);
	}

	private void RefreshPreview(GameObject prefab)
	{
		CleanupPreview();

		Vector2 size = portraitImage.rectTransform.rect.size;
		int w = size.x > 0 ? (int)size.x : 150;
		int h = size.y > 0 ? (int)size.y : 200;

		previewRT = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
		portraitImage.texture = previewRT;

		var camObj = new GameObject("CharSelectPreviewCam");
		previewCamera = camObj.AddComponent<Camera>();
		previewCamera.orthographic = true;
		previewCamera.orthographicSize = 0.8f;
		previewCamera.clearFlags = CameraClearFlags.SolidColor;
		previewCamera.backgroundColor = Color.clear;
		previewCamera.targetTexture = previewRT;
		previewCamera.cullingMask = ~0;
		previewCamera.depth = -99;
		camObj.transform.position = new Vector3(PreviewPos.x, PreviewPos.y, -10f);

		previewInstance = Instantiate(prefab, PreviewPos, Quaternion.identity);
		if (previewInstance.TryGetComponent<Animator>(out var anim))
			anim.fireEvents = false;
	}

	private void CleanupPreview()
	{
		if (previewInstance != null) { Destroy(previewInstance); previewInstance = null; }
		if (previewCamera != null) { Destroy(previewCamera.gameObject); previewCamera = null; }
		if (previewRT != null) { previewRT.Release(); previewRT = null; }
	}

	private PlayerData GetLocalPlayerData()
	{
		ulong localId = NetworkManager.Singleton.LocalClientId;
		var list = GameLobby.Instance.GetPlayerDataNetworkList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].clientId == localId)
				return list[i];
		}
		return default;
	}

	public static string FormatPercent(float ratio)
	{
		int percent = Mathf.RoundToInt(ratio * 100f);
		if (percent > 100) return $"<color=green>+{percent - 100}%</color>";
		if (percent < 100) return $"<color=red>-{100 - percent}%</color>";
		return "Normal";
	}

	private void HandleReadyChanged(object sender, EventArgs e)
	{
		UpdateReadyButtons();
		foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (clientId == NetworkManager.Singleton.LocalClientId) continue;
			UpdateReadyIndicator(clientId, CharacterSelectReady.Instance.isPlayerReady(clientId));
		}
	}
	private void HandleCharacterChanged(object sender, EventArgs e) => UpdateCharacterDisplay();

	private void UpdateReadyButtons()
	{
		ulong localId = NetworkManager.Singleton.LocalClientId;
		bool localReady = CharacterSelectReady.Instance.isPlayerReady(localId);
		readyButton.gameObject.SetActive(!localReady);
		unReadyButton.gameObject.SetActive(localReady);
	}
	private void UpdateReadyIndicator(ulong clientId, bool isReady)
	{
		foreach (Transform child in opponentStatusContainer)
		{
			if (child == opponentStatusTemplate) continue;
			TextMeshProUGUI nameText = child.GetComponentInChildren<TextMeshProUGUI>();
			if (nameText != null && nameText.text == CharacterSelectReady.Instance.GetPlayerNameByClientId(clientId))
			{
				child.GetComponentInChildren<Image>(true).gameObject.SetActive(isReady);
				break;
			}
		}
	}

	private void updateOpponentList(List<ulong> lobbyList)
	{
		foreach (Transform child in opponentStatusContainer)
		{
			if (child == opponentStatusTemplate) continue;
			Destroy(child.gameObject);
		}	
		foreach (ulong clientId in lobbyList)
		{
			if (clientId == NetworkManager.Singleton.LocalClientId) continue;
			GameObject statusObj = Instantiate(opponentStatusTemplate.gameObject, opponentStatusContainer);
			TextMeshProUGUI nameText = statusObj.GetComponentInChildren<TextMeshProUGUI>();
			if (nameText != null)
			{
				nameText.text = CharacterSelectReady.Instance.GetPlayerNameByClientId(clientId);
			}
			statusObj.GetComponentInChildren<Image>(true).gameObject.SetActive(CharacterSelectReady.Instance.isPlayerReady(clientId));
			statusObj.SetActive(true);
		}
	}
}
