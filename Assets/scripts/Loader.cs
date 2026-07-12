using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
	public static int targetSceneIndex;

	public enum Scene
	{
		MainMenuScene,
		LobbyScene,
		CharacterSelectScene,
		GameScene,
	}

	public static void Load(Scene targetScene)
	{
		SceneManager.LoadScene(targetScene.ToString());
	}

	public static void LoadNetwork(Scene targetScene)
	{
		NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
	}
}
