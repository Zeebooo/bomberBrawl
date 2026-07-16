using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;

public class GameStateHandler : NetworkBehaviour
{
	public static GameStateHandler Instance { get; private set; }
	public event EventHandler onStateChanged;
	public event EventHandler onTutorialInteraction;
	public event EventHandler onLocalPlayerReadyChanged;
	private enum State
	{
		waitingToStart,
		countDownToStart,
		gameInProgress,
		gameOver,
		resultScreen
	}

	private NetworkVariable<State> state = new NetworkVariable<State>(State.waitingToStart);
	private bool isLocalPlayerReady = false;
	private bool enterWasPressed = false;
	private NetworkVariable<float> countDownToStartTimer = new NetworkVariable<float>(3f);
	private bool isGameOver = false;
	private bool isAnimationsReset = false;
	private Dictionary<ulong, bool> playerReadyDictionary;

	void Awake()
	{
		Instance = this;
		playerReadyDictionary = new Dictionary<ulong, bool>();
	}

	private void Start()
	{
		GameStateHandler.Instance.onTutorialInteraction += HandleTutorialInteraction;
	}

	public override void OnNetworkSpawn()
	{
		state.OnValueChanged += stateValueChanged;

		if (IsServer)
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
	}

	private void OnClientDisconnected(ulong clientId)
	{
		if (state.Value == State.gameInProgress || state.Value == State.countDownToStart)
		{
			foreach (var character in FindObjectsByType<characterScript>())
			{
				if (character.OwnerClientId == clientId)
				{
					character.forceKill();
					break;
				}
			}
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer && NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
	}

	private void stateValueChanged(State previousValue, State newValue)
	{
		onStateChanged?.Invoke(this, EventArgs.Empty);
		if (newValue == State.gameInProgress)
		{
			OptionsUI.Instance.addSurrenderListener();
		}
	}

	private void HandleTutorialInteraction(object sender, EventArgs e)
	{
		if (state.Value == State.waitingToStart)
		{
			isLocalPlayerReady = true;
			onLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
			setPlayerReadyServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void setPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
	{
		playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

		bool allClientsReady = true;
		foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
			{
				allClientsReady = false;
				break;
			}
		}

		if (allClientsReady)
		{
			state.Value = State.countDownToStart;
		}
	}

	void Update()
	{
		bool spaceDown = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
		if (spaceDown && !enterWasPressed && state.Value == State.waitingToStart)
		{
			onTutorialInteraction?.Invoke(this, EventArgs.Empty);
		}
		enterWasPressed = spaceDown;

		if (!IsServer) return;

		switch (state.Value)
		{
			case State.waitingToStart:
				break;
			case State.countDownToStart:
				countDownToStartTimer.Value -= Time.deltaTime;
				if (countDownToStartTimer.Value <= 0f)
				{
					GameLobby.Instance.DeleteActiveLobby();
					state.Value = State.gameInProgress;
				}
				break;
			case State.gameInProgress:
				int aliveCount = 0;
				int deadCount = 0;
				foreach (var character in FindObjectsByType<characterScript>())
				{
					if (character.CurrentHealth.Value > 0) aliveCount++;
					else deadCount++;
				}
				
				if ((deadCount > 0 && aliveCount <= 1) || NetworkManager.Singleton.ConnectedClients.Count <= 1)
				{
					state.Value = State.gameOver;
				}
				break;
			case State.gameOver:
				if (!isAnimationsReset)
				{
					foreach (var character in FindObjectsByType<characterScript>())
					{
						character.resetAnimation();
					}
					isAnimationsReset = true;
				}
				break;
			case State.resultScreen:
				break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SurrenderServerRpc(ServerRpcParams rpcParams = default)
	{
		ulong loserId = rpcParams.Receive.SenderClientId;
		foreach (var character in FindObjectsByType<characterScript>())
		{
			if (character.OwnerClientId == loserId)
			{
				character.forceKill();
				break;
			}
		}
	}

	public bool isGameInProgress() => state.Value == State.gameInProgress;
	public bool isGameOverState() => state.Value == State.gameOver;
	public bool isWaitingToStart() => state.Value == State.waitingToStart;
	public bool isResultScreen() => state.Value == State.resultScreen;

	public bool isCountDownToStart()
	{
		return state.Value == State.countDownToStart;
	}

	public bool isLocalPlayerReadyToStart()
	{
		return isLocalPlayerReady;
	}

	public float getCountDownToStartTimer()
	{
		return countDownToStartTimer.Value;
	}

	public bool isResultScreenState()
	{
		return state.Value == State.resultScreen;
	}

	public void setState(string newState)
	{
		switch (newState)
		{
			case "waitingToStart":
				state.Value = State.waitingToStart;
				break;
			case "countDownToStart":
				state.Value = State.countDownToStart;
				break;
			case "gameInProgress":
				state.Value = State.gameInProgress;
				break;
			case "gameOver":
				state.Value = State.gameOver;
				break;
			case "resultScreen":
				state.Value = State.resultScreen;
				break;
		}
	}
}
