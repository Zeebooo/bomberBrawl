using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
	public mapGenerator map;
	private HashSet<ulong> spawnedClients = new();
	private int spawnCount = 0;

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		StartCoroutine(SpawnExistingClients());
	}

	private IEnumerator SpawnExistingClients()
	{
		while (map.spawnPoints == null)
			yield return null;

		foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			SpawnPlayer(clientId);
	}

	void OnClientConnected(ulong clientId)
	{
		StartCoroutine(SpawnPlayerWhenReady(clientId));
	}

	private IEnumerator SpawnPlayerWhenReady(ulong clientId)
	{
		while (map.spawnPoints == null)
			yield return null;

		SpawnPlayer(clientId);
	}

	private void SpawnPlayer(ulong clientId)
	{
		if (!spawnedClients.Add(clientId)) return;
		PlayerData playerData = GameLobby.Instance.GetPlayerData(clientId);
		int prefabIndex = playerData.selectedCharacterIndex;
		int spawnIndex = spawnCount;
		spawnCount++;
		CharacterDataSO data = GameLobby.Instance.characters[prefabIndex];
		GameObject playerObj = Instantiate(data.prefab, map.spawnPoints[spawnIndex], Quaternion.identity);
		playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
		playerObj.GetComponent<characterScript>().ApplyCharacterData(data);
		playerObj.GetComponent<bombScript>().ApplyCharacterData(data);
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
	}
}
