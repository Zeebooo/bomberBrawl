using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour
{
	public static CharacterSelectReady Instance { get; private set; }

	public event EventHandler OnStateChanged;
	public event EventHandler OnPlayerReadyChanged;
	public event EventHandler OnCharacterChanged;

	private NetworkList<FixedString64Bytes> playerNames;
	private Dictionary<ulong, bool> playerReadyDictionary;

	private void Awake()
	{
		Instance = this;
		playerNames = new NetworkList<FixedString64Bytes>();
		playerReadyDictionary = new Dictionary<ulong, bool>();
	}

	public override void OnNetworkSpawn()
	{
		playerNames.OnListChanged += _ => OnStateChanged?.Invoke(this, EventArgs.Empty);

		if (IsServer)
		{
			playerNames.Add(new FixedString64Bytes(GameLobby.Instance.GetPlayerName()));
			GameLobby.Instance.SetNetworkPlayerName(NetworkManager.Singleton.LocalClientId, GameLobby.Instance.GetPlayerName());
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
		}
		else
		{
			SetPlayerNameServerRpc(new FixedString64Bytes(GameLobby.Instance.GetPlayerName()));
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetPlayerNameServerRpc(FixedString64Bytes name, ServerRpcParams rpcParams = default)
	{
		playerNames.Add(name);
		GameLobby.Instance.SetNetworkPlayerName(rpcParams.Receive.SenderClientId, name.ToString());
	}

	public void setPlayerReady()
	{
		setPlayerReadyServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void setPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
	{
		setPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);
		playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

		bool allClientsReady = true;
		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
			{
				allClientsReady = false;
				break;
			}
		}

		if (allClientsReady && NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
		{
			Loader.LoadNetwork(Loader.Scene.GameScene);
		}
	}

	[ClientRpc]
	private void setPlayerReadyClientRpc(ulong clientId)
	{
		playerReadyDictionary[clientId] = true;
		OnPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
	}

	public void setPlayerUnready()
	{
		setPlayerUnreadyServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void setPlayerUnreadyServerRpc(ServerRpcParams serverRpcParams = default)
	{
		setPlayerUnreadyClientRpc(serverRpcParams.Receive.SenderClientId);
		playerReadyDictionary.Remove(serverRpcParams.Receive.SenderClientId);
	}

	[ClientRpc]
	private void setPlayerUnreadyClientRpc(ulong clientId)
	{
		playerReadyDictionary.Remove(clientId);
		OnPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnClientDisconnected(ulong clientId)
	{
		playerReadyDictionary.Remove(clientId);
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer && NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
	}

	public bool isPlayerReady(ulong clientId)
	{
		return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
	}

	public int GetConnectedCount() => playerNames.Count;

	public string GetPlayerNameByClientId(ulong clientId)
	{
		var ids = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
		int index = ids.IndexOf(clientId);
		return index < playerNames.Count ? playerNames[index].ToString() : "";
	}

	public ulong GetClientIdByIndex(int index)
	{
		var ids = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
		return index < ids.Count ? ids[index] : 0;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void UpdateCharacterIndexServerRpc(int newIndex, ulong clientId)
	{
		var list = GameLobby.Instance.GetPlayerDataNetworkList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].clientId == clientId)
			{
				int clamped = newIndex;
				int total = GameLobby.Instance.characters.Length;
				if (clamped < 0) clamped = total - 1;
				else if (clamped >= total) clamped = 0;

				PlayerData updated = list[i];
				updated.selectedCharacterIndex = clamped;
				list[i] = updated;
				GameLobby.Instance.UpdatePlayerDataNetworkList(updated);

				NotifyCharacterChangedClientRpc();
				break;
			}
		}
	}

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
	private void NotifyCharacterChangedClientRpc()
	{
		OnCharacterChanged?.Invoke(this, EventArgs.Empty);
	}


	public void KickAll() => KickAllClientRpc();

	[ClientRpc]
	private void KickAllClientRpc()
	{
		if (IsHost) return;
		GameLobby.Instance.HandleBeingKicked();
	}
}
