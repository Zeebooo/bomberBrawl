using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class WorldEventHandler : NetworkBehaviour
{
	private bool testing = true;
	public static WorldEventHandler Instance { get; private set; }
	private float eventInterval;
	private float eventTimer;
	private readonly WaitForSeconds switchShowDelay = new(0.2f);
	[SerializeField] private GameObject poofEffectPrefab;
	[SerializeField] private GameObject globalLight;

	private void Awake()
	{
		Instance = this;
		eventInterval = Random.Range(20f, 40f);
		eventTimer = eventInterval;
	}

	void Update()
	{
		if (!IsServer) return;
		if (GameStateHandler.Instance == null || !GameStateHandler.Instance.isGameInProgress()) return;

		eventTimer -= Time.deltaTime;
		if (testing && Keyboard.current.tKey.wasPressedThisFrame)
		{
			eventTimer = 0f;
			TriggerSwitchRpc(10f, "Switch");
		}
		else if (eventTimer <= 0f)
		{
			eventInterval = Random.Range(20f, 30f);
			eventTimer = eventInterval;
			TriggerRandomEvent();
		}
	}

	private void TriggerRandomEvent()
	{
		string[] worldEvents = ItempickupScript.getWorldEvents();
		int randomIndex = Random.Range(0, worldEvents.Length);

		switch (worldEvents[randomIndex])
		{
			case "MixUp":
				TriggerMixupRpc(10f, "MixUp");
				break;
			case "Switch":
				TriggerSwitchRpc(0f, "Switch");
				break;
			case "Darkness":
				TriggerDarknessRpc(10f, "Darkness");
				break;
		}
	}

	// MIXUP

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void TriggerMixupRpc(float duration, string eventName)
	{
		StartCoroutine(MixupCoroutine(duration));
		ShowPopupRpc(duration, eventName);
	}

	public IEnumerator MixupCoroutine(float duration)
	{
		foreach (var character in FindObjectsByType<characterScript>())
		{
			character.InvertedControlsActive.Value = true;
		}
		yield return new WaitForSeconds(duration);
		foreach (var character in FindObjectsByType<characterScript>())
		{
			character.InvertedControlsActive.Value = false;
		}
	}

	// DARKNESS

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	public void TriggerDarknessRpc(float duration, string eventName)
	{
		StartCoroutine(DarknessCoroutine(duration));
		ShowPopupRpc(duration, eventName);
	}
	public IEnumerator DarknessCoroutine(float duration)
	{
		globalLight.GetComponent<Light2D>().intensity = 0;
		foreach (var character in FindObjectsByType<characterScript>())
		{
			if (character.IsOwner)
			{
				var torch = character.torchPrefab;
				torch.SetActive(true);
				torch.GetComponent<Light2D>().intensity = 5;
			}
		}
		yield return new WaitForSeconds(duration);
		globalLight.GetComponent<Light2D>().intensity = 1;
		foreach (var character in FindObjectsByType<characterScript>())
		{
			if (character.IsOwner)
			{
				var torch = character.torchPrefab;
				torch.SetActive(false);
				torch.GetComponent<Light2D>().intensity = 0;
			}
		}
	}

	// SLIPPERY

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void TriggerSlipperyRpc(float duration, string eventName)
	{
		StartCoroutine(SlipperyCoroutine(duration));
		ShowPopupRpc(duration, eventName);
	}

	private IEnumerator SlipperyCoroutine(float duration)
	{
		foreach (var character in FindObjectsByType<characterScript>())
		{
			character.SlipperyActive.Value = true;
		}
		yield return new WaitForSeconds(duration);
		foreach (var character in FindObjectsByType<characterScript>())
		{
			character.SlipperyActive.Value = false;
		}
	}

	// SWITCH

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void TriggerSwitchRpc(float duration, string eventName)
	{
		characterScript[] allCharacters = FindObjectsByType<characterScript>();
		System.Collections.Generic.List<characterScript> aliveCharactersList = new();
		foreach (var c in allCharacters)
		{
			if (c.CurrentHealth.Value > 0)
			{
				aliveCharactersList.Add(c);
			}
		}
		characterScript[] characters = aliveCharactersList.ToArray();
		int n = characters.Length;
		if (n < 2)
		{
			return;
		}

		Vector3[] positions = new Vector3[n];
		for (int i = 0; i < n; i++)
		{
			positions[i] = characters[i].transform.position;
		}

		int[] order = new int[n];
		for (int i = 0; i < n; i++)
		{
			order[i] = i;
		}

		for (int i = n - 1; i > 0; i--)
		{
			int j = Random.Range(0, i);
			(order[i], order[j]) = (order[j], order[i]);
		}

		ulong[] clientIds = new ulong[n];
		Vector3[] newPositions = new Vector3[n];
		for (int i = 0; i < n; i++)
		{
			clientIds[i] = characters[i].OwnerClientId;
			newPositions[i] = SnapToTileCenter(positions[order[i]]);
		}

		SetCharactersVisibleRpc(false);
		if (poofEffectPrefab != null)
		{
			for (int i = 0; i < n; i++)
			{
				GameObject poof = Instantiate(poofEffectPrefab, positions[i], Quaternion.identity);
				if (poof.TryGetComponent<NetworkObject>(out var poofNetObj))
				{
					poofNetObj.Spawn();
				}
			}
		}
		SetInterpolationRpc(false);

		ApplySwitchPositionsRpc(clientIds, newPositions);

		StartCoroutine(ShowAfterDelay());
		ShowPopupRpc(duration, eventName);
	}

	// Positionerna är redan beräknade en gång på servern — varje klient flyttar bara sin egen (ägar-auktoritativa) karaktär.
	[Rpc(SendTo.Everyone)]
	private void ApplySwitchPositionsRpc(ulong[] clientIds, Vector3[] newPositions)
	{
		foreach (var character in FindObjectsByType<characterScript>())
		{
			if (!character.IsOwner) continue;

			for (int i = 0; i < clientIds.Length; i++)
			{
				if (character.OwnerClientId == clientIds[i])
				{
					character.transform.position = newPositions[i];
					break;
				}
			}
		}
	}

	// HELPER METHODS

	private Vector3 SnapToTileCenter(Vector3 position)
	{
		float x = Mathf.Floor(position.x) + 0.5f;
		float y = Mathf.Floor(position.y) + 0.65f;
		return new Vector3(x, y, position.z);
	}

	private IEnumerator ShowAfterDelay()
	{
		yield return switchShowDelay;
		SetInterpolationRpc(true);
		SetCharactersVisibleRpc(true);
	}

	[Rpc(SendTo.Everyone)]
	private void SetInterpolationRpc(bool interpolate)
	{
		foreach (var character in FindObjectsByType<characterScript>())
		{
			if (character.TryGetComponent(out NetworkTransform nt))
			{
				nt.Interpolate = interpolate;
			}
		}
	}

	[Rpc(SendTo.Everyone)]
	private void SetCharactersVisibleRpc(bool visible)
	{
		foreach (var character in FindObjectsByType<characterScript>())
		{
			character.switchPreparation(visible);
			SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.swooshSFX[0]);
		}
	}

	[Rpc(SendTo.Everyone)]
	private void ShowPopupRpc(float duration, string eventName)
	{
		if (WorldEventPopup.Instance != null)
		{
			WorldEventPopup.Instance.ShowEvent(duration, eventName);
		}
	}
}
