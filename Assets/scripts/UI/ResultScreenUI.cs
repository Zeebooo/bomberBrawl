using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ResultScreenUI : MonoBehaviour
{
	[Header("Winning player")]
	[SerializeField] private TextMeshProUGUI winningPlayerText;
	[SerializeField] private RawImage winningPlayerImage;

	[Header("Loosing players")]
	[SerializeField] private GameObject loserSlotPrefab;
	[SerializeField] private Transform loserContainer;

	[Header("Other")]
	[SerializeField] private Button goBackToLobbyListButton;
	[SerializeField] private TextMeshProUGUI bigResultText;


	private static readonly Vector3 WinnerPreviewPos = new(-1000f, -1000f, 0f);
	private static readonly Vector3 LoserBasePreviewPos = new(1000f, -1000f, 0f);
	private const float LoserSpacing = 5f;

	private GameObject winnerInstance;
	private Camera winnerCamera;
	private RenderTexture winnerRT;

	private readonly List<GameObject> loserInstances = new();
	private readonly List<Camera> loserCameras = new();
	private readonly List<RenderTexture> loserRTs = new();

	private CanvasGroup canvasGroup;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0;
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
		goBackToLobbyListButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			SoundManager.Instance.StopMusic();
			OptionsUI.Instance.playerPrefsSetMusicVolume();
			GameLobby.Instance.LeaveLobby();
		});
		OptionsUI.Instance.tempApplyMusicVolume(0.3f);
	}

	private void Start()
	{
		GameStateHandler.Instance.onStateChanged += HandleStateChanged;
	}

	private void OnDestroy()
	{
		if (GameStateHandler.Instance != null)
			GameStateHandler.Instance.onStateChanged -= HandleStateChanged;

		CleanupInstance(ref winnerInstance, winnerCamera, ref winnerRT);

		for (int i = 0; i < loserInstances.Count; i++)
		{
			if (loserInstances[i] != null) Destroy(loserInstances[i]);
			if (loserCameras[i] != null) Destroy(loserCameras[i].gameObject);
			if (loserRTs[i] != null) loserRTs[i].Release();
		}
	}

	private void HandleStateChanged(object sender, EventArgs e)
	{
		if (GameStateHandler.Instance.isResultScreenState())
			StartCoroutine(ShowResultScreen());
	}

	private IEnumerator ShowResultScreen()
	{
		yield return null;

		characterScript winner = null;
		var losers = new List<characterScript>();

		foreach (var c in FindObjectsByType<characterScript>())
		{
			if (c.CurrentHealth.Value > 0)
				winner = c;
			else
				losers.Add(c);
		}

		// Vinnare
		if (winner != null)
		{
			int charIndex = GetCharIndex(winner.OwnerClientId);
			winnerCamera = CreatePreview(GetPortrait(charIndex), WinnerPreviewPos, winningPlayerImage, out winnerInstance, out winnerRT);

			string name = GetPlayerName(winner.OwnerClientId);
			if (winningPlayerText != null) winningPlayerText.text = name;
			if (bigResultText != null) bigResultText.text = $"{name} WINS";
		}
		else
		{
			if (winningPlayerText != null) winningPlayerText.gameObject.SetActive(false);
			if (winningPlayerImage != null) winningPlayerImage.gameObject.SetActive(false);
			if (bigResultText != null) bigResultText.text = "EVERYONE LOSES";
		}

		// Skapa slots först så layouten hinner beräkna RawImage-storlekar
		var loserImages = new List<RawImage>();
		float startX = LoserBasePreviewPos.x - (losers.Count - 1) * LoserSpacing * 0.5f;

		for (int i = 0; i < losers.Count; i++)
		{
			GameObject slot = Instantiate(loserSlotPrefab, loserContainer);
			if (slot.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI txt)
				txt.text = GetPlayerName(losers[i].OwnerClientId);
			loserImages.Add(slot.GetComponentInChildren<RawImage>());
		}

		// Vänta en frame så Unity räknar ut layouten innan vi läser RawImage-storlekarna
		yield return null;

		for (int i = 0; i < losers.Count; i++)
		{
			int charIndex = GetCharIndex(losers[i].OwnerClientId);
			Vector3 loserPos = new Vector3(startX + i * LoserSpacing, LoserBasePreviewPos.y, 0f);

			Camera cam = CreatePreview(GetPortrait(charIndex), loserPos, loserImages[i], out GameObject inst, out RenderTexture rt);

			if (inst.TryGetComponent<Animator>(out var anim))
			{
				anim.fireEvents = false;
				anim.SetTrigger(Animator.StringToHash("isDead"));
			}

			loserInstances.Add(inst);
			loserCameras.Add(cam);
			loserRTs.Add(rt);
		}

		canvasGroup.alpha = 1;
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.victorySoundSFX[0]);
	}

	private int GetCharIndex(ulong clientId)
	{
		int index = GameLobby.Instance.GetPlayerData(clientId).selectedCharacterIndex;
		return Mathf.Clamp(index, 0, GameLobby.Instance.characters.Length - 1);
	}

	private GameObject GetPortrait(int charIndex) => GameLobby.Instance.characters[charIndex].portrait;

	private string GetPlayerName(ulong clientId)
	{
		string name = GameLobby.Instance.GetNetworkPlayerName(clientId);
		return string.IsNullOrEmpty(name) ? $"Player {clientId + 1}" : name;
	}

	private Camera CreatePreview(GameObject prefab, Vector3 worldPos, RawImage display,
		out GameObject instance, out RenderTexture rt)
	{
		Vector2 size = display.rectTransform.rect.size;
		int w = size.x > 0 ? (int)size.x : 150;
		int h = size.y > 0 ? (int)size.y : 150;

		rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
		display.texture = rt;

		var camObj = new GameObject("ResultPreviewCam");
		var cam = camObj.AddComponent<Camera>();
		cam.orthographic = true;
		cam.orthographicSize = 0.8f;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = Color.clear;
		cam.targetTexture = rt;
		cam.cullingMask = ~0;
		cam.depth = -99;
		camObj.transform.position = new Vector3(worldPos.x, worldPos.y, -10f);

		instance = Instantiate(prefab, worldPos, Quaternion.identity);

		return cam;
	}

	private void CleanupInstance(ref GameObject instance, Camera cam, ref RenderTexture rt)
	{
		if (instance != null) { Destroy(instance); instance = null; }
		if (cam != null) Destroy(cam.gameObject);
		if (rt != null) { rt.Release(); rt = null; }
	}
}
