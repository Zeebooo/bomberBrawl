using System.Collections;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

public class GameLobby : NetworkBehaviour
{
	private const int maxPlayers = 4;
	private float heartbeatTimer;
	private float listLobbiesTimer;
	private float maxListLobbiesTime = 5f;
	private float maxHeartbeatTime = 15f;
	private string playerName;
	[SerializeField] public CharacterDataSO[] characters;

	private const string PLAYER_PREFS_MULTIPLAYER_NAME_KEY = "PlayerName";
	private const string KEY_RELAY_CODE = "RelayCode";

	private NetworkList<PlayerData> playerDataNetworkList;
	private Dictionary<ulong, string> cachedPlayerNames = new();

	private Lobby joinedLobby;
	private bool isLeavingLobby;
	public static GameLobby Instance { get; private set; }

	public event EventHandler<onLobbyListChangedEventArgs> onLobbyListChanged;
	public event EventHandler onPlayerDataNetworkListChanged;
	public class onLobbyListChangedEventArgs : EventArgs
	{
		public List<Lobby> lobbyList;
	}

	private void Awake()
	{
		playerDataNetworkList = new NetworkList<PlayerData>();

		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		playerName = PlayerPrefs.GetString(PLAYER_PREFS_MULTIPLAYER_NAME_KEY, "Player" + UnityEngine.Random.Range(0, 1000));

		DontDestroyOnLoad(gameObject);
		InitializeUnityAuthentication();
		playerDataNetworkList.OnListChanged += (NetworkListEvent<PlayerData> changeEvent) =>
		{
			onPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
		};
	}

	public string GetPlayerName()
	{
		return playerName;
	}

	public NetworkList<PlayerData> GetPlayerDataNetworkList() => playerDataNetworkList;
	public PlayerData GetPlayerData(ulong clientId)
	{
		foreach (var playerData in playerDataNetworkList)
		{
			if (playerData.clientId == clientId)
			{
				return playerData;
			}
		}
		return default;
	}
	public NetworkList<PlayerData> UpdatePlayerDataNetworkList(PlayerData playerData)
	{
		for (int i = 0; i < playerDataNetworkList.Count; i++)
		{
			if (playerDataNetworkList[i].clientId == playerData.clientId)
			{
				playerDataNetworkList.RemoveAt(i);
				playerDataNetworkList.Insert(i, playerData);
				return playerDataNetworkList;
			}
		}
		return playerDataNetworkList;
	}

	public void SetPlayerName(string newName)
	{
		this.playerName = newName;
		PlayerPrefs.SetString(PLAYER_PREFS_MULTIPLAYER_NAME_KEY, playerName);
	}

	private void Update()
	{
		HandleHeartBeat();
		HandlePeriodicListLobbies();
	}

	private void HandlePeriodicListLobbies()
	{
		if (joinedLobby != null || isLeavingLobby || !AuthenticationService.Instance.IsSignedIn || SceneManager.GetActiveScene().name != "LobbyScene") return;

		listLobbiesTimer -= Time.deltaTime;
		if (listLobbiesTimer <= 0f)
		{
			listLobbiesTimer = maxListLobbiesTime;
			listLobbies();
		}
	}

	private void HandleHeartBeat()
	{
		if (!isLobbyHost()) return;

		heartbeatTimer -= Time.deltaTime;
		if (heartbeatTimer <= 0f)
		{
			heartbeatTimer = maxHeartbeatTime;
			LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
		}
	}

	private bool isLobbyHost()
	{
		return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
	}

	private async void InitializeUnityAuthentication()
	{
		if (UnityServices.State != ServicesInitializationState.Initialized)
		{
			InitializationOptions options = new InitializationOptions();
			options.SetProfile(UnityEngine.Random.Range(0, int.MaxValue).ToString());
			await UnityServices.InitializeAsync(options);

			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}
	}

	private async void listLobbies()
	{
		try
		{
			QueryLobbiesOptions options = new QueryLobbiesOptions
			{
				Filters = new List<QueryFilter>
				{
					new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
				}
			};
			QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
			onLobbyListChanged?.Invoke(this, new onLobbyListChangedEventArgs { lobbyList = queryResponse.Results });
		}
		catch (Exception e)
		{
			Debug.LogError("Error listing lobbies: " + e.Message);
		}
	}

	private async Task<Allocation> AllocateRelay()
	{
		try
		{
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
			return allocation;
		}
		catch (Exception e)
		{
			Debug.LogError("Error allocating Relay: " + e.Message);
			return default;
		}
	}

	private async Task<string> GetRelayCode(Allocation allocation)
	{
		try
		{
			string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			return relayCode;
		}
		catch (Exception e)
		{
			Debug.LogError("Error getting Relay join code: " + e.Message);
			return default;
		}
	}

	private async Task<JoinAllocation> JoinRelay(string joinCode)
	{
		try
		{
			JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
			return joinAllocation;
		}
		catch (Exception e)
		{
			Debug.LogError("Error joining Relay: " + e.Message);
			return default;
		}
	}

	public async void CreateLobby(string lobbyName, bool isPrivate)
	{
		try
		{
			// Skapa alltid som privat tills relay-koden är klar
			joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, new CreateLobbyOptions { IsPrivate = true });

			Allocation allocation = await AllocateRelay();
			if (allocation == null)
			{
				Debug.LogError("Relay allocation failed — deleting lobby");
				await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
				joinedLobby = null;
				return;
			}

			string relayCode = await GetRelayCode(allocation);
			if (string.IsNullOrEmpty(relayCode))
			{
				Debug.LogError("Failed to get relay code — deleting lobby");
				await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
				joinedLobby = null;
				return;
			}

			// Sätt relay-koden och korrekt synlighet i samma anrop
			await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
			{
				IsPrivate = isPrivate,
				Data = new Dictionary<string, DataObject>
				{
					{ KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
				}
			});

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
				allocation.RelayServer.IpV4,
				(ushort)allocation.RelayServer.Port,
				allocation.AllocationIdBytes,
				allocation.Key,
				allocation.ConnectionData
			);

			NetworkManager.Singleton.OnClientConnectedCallback += NetWorkmanager_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
			NetworkManager.Singleton.StartHost();
			Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
		}
		catch (Exception e)
		{
			Debug.LogError("Error creating lobby: " + e.Message);
			if (joinedLobby != null)
			{
				try { await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id); } catch { }
				joinedLobby = null;
			}
		}
	}

	public void SetNetworkPlayerName(ulong clientId, string name)
	{
		cachedPlayerNames[clientId] = name;
		BroadcastPlayerNameClientRpc(clientId, new FixedString64Bytes(name));
	}

	[ClientRpc]
	private void BroadcastPlayerNameClientRpc(ulong clientId, FixedString64Bytes name, ClientRpcParams rpcParams = default)
	{
		cachedPlayerNames[clientId] = name.ToString();
	}

	public string GetNetworkPlayerName(ulong clientId)
	{
		return cachedPlayerNames.TryGetValue(clientId, out string name) ? name : "";
	}

	private void OnClientDisconnected(ulong clientId)
	{
		for (int i = 0; i < playerDataNetworkList.Count; i++)
		{
			if (playerDataNetworkList[i].clientId == clientId)
			{
				playerDataNetworkList.RemoveAt(i);
				break;
			}
		}
		cachedPlayerNames.Remove(clientId);
	}

	private void NetWorkmanager_OnClientConnectedCallback(ulong clientId)
	{
		playerDataNetworkList.Add(new PlayerData
		{
			clientId = clientId
		});

		var targetParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
		};
		foreach (var kvp in cachedPlayerNames)
			BroadcastPlayerNameClientRpc(kvp.Key, new FixedString64Bytes(kvp.Value), targetParams);
	}

	public async void QuickJoin()
	{
		try
		{
			joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
			if (joinedLobby.Data == null || !joinedLobby.Data.ContainsKey(KEY_RELAY_CODE))
			{
				Debug.LogError("Lobby has no relay code — aborting join");
				joinedLobby = null;
				return;
			}
			JoinAllocation joinAllocation = await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
			if (joinAllocation == null)
			{
				Debug.LogError("Failed to join relay — aborting join");
				joinedLobby = null;
				return;
			}
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData
			);
			NetworkManager.Singleton.StartClient();
		}
		catch (Exception e)
		{
			Debug.LogError("Error joining lobby: " + e.Message);
		}
	}

	public async void JoinLobbyByCode(string lobbyCode)
	{
		try
		{
			joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
			if (joinedLobby.Data == null || !joinedLobby.Data.ContainsKey(KEY_RELAY_CODE))
			{
				Debug.LogError("Lobby has no relay code — aborting join");
				joinedLobby = null;
				return;
			}
			JoinAllocation joinAllocation = await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
			if (joinAllocation == null)
			{
				Debug.LogError("Failed to join relay — aborting join");
				joinedLobby = null;
				return;
			}
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData
			);
			NetworkManager.Singleton.StartClient();
		}
		catch (Exception e)
		{
			Debug.LogError("Error joining lobby: " + e.Message);
		}
	}

	public async void JoinLobbyById(string lobbyId)
	{
		try
		{
			joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
			if (joinedLobby.Data == null || !joinedLobby.Data.ContainsKey(KEY_RELAY_CODE))
			{
				Debug.LogError("Lobby has no relay code — aborting join");
				joinedLobby = null;
				return;
			}
			JoinAllocation joinAllocation = await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
			if (joinAllocation == null)
			{
				Debug.LogError("Failed to join relay — aborting join");
				joinedLobby = null;
				return;
			}
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData
			);

			NetworkManager.Singleton.StartClient();
		}
		catch (Exception e)
		{
			Debug.LogError("Error joining lobby: " + e.Message);
		}
	}

	public async void LeaveLobby()
	{
		if (joinedLobby == null)
		{
			StartCoroutine(ShutdownAndLoad());
			return;
		}

		isLeavingLobby = true;

		try
		{
			if (isLobbyHost())
			{
				if (CharacterSelectReady.Instance != null && CharacterSelectReady.Instance.IsSpawned)
					CharacterSelectReady.Instance.KickAll();
				await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
			}
			else
			{
				await LobbyService.Instance.RemovePlayerAsync(
					joinedLobby.Id, AuthenticationService.Instance.PlayerId);
			}
		}
		catch (LobbyServiceException) { }

		joinedLobby = null;
		isLeavingLobby = false;
		StartCoroutine(ShutdownAndLoad());
	}

	public void HandleBeingKicked()
	{
		joinedLobby = null;
		StartCoroutine(ShutdownAndLoad());
	}

	private IEnumerator ShutdownAndLoad()
	{
		var nm = NetworkManager.Singleton;
		if (nm != null)
		{
			nm.Shutdown();
			Destroy(nm.gameObject);
		}
		yield return null;
		Loader.Load(Loader.Scene.LobbyScene);
	}

	public async void DeleteActiveLobby()
	{
		if (joinedLobby == null || !isLobbyHost()) return;
		try
		{
			await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Error deleting lobby: " + e.Message);
		}
		joinedLobby = null;
	}

	private async void OnApplicationQuit()
	{
		if (joinedLobby == null) return;
		try
		{
			if (isLobbyHost())
				await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
			else
				await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
		}
		catch (Exception) { }
	}

	public Lobby GetLobby()
	{
		return joinedLobby;
	}

	public int getMaxPlayers()
	{
		return maxPlayers;
	}

	public bool isPlayerIndexConnected(int playerIndex)
	{
		if (joinedLobby == null) return false;
		return playerIndex < joinedLobby.Players.Count;
	}

	public PlayerData getPlayerDataByIndex(int playerIndex)
	{
		return playerDataNetworkList[playerIndex];
	}
}
