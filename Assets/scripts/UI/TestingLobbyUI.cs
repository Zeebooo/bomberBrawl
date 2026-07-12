using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;


public class TestingLobbyUI : MonoBehaviour
{
	[SerializeField] private Button createGameButton;
	[SerializeField] private Button joinGameButton;

	public void Awake()
	{
		createGameButton.onClick.AddListener(() =>
		{
			NetworkManager.Singleton.StartHost();
			Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
		});
		joinGameButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
	}
}
