using UnityEngine;

public class TutorialUI : MonoBehaviour
{
	private void Start()
	{
		GameStateHandler.Instance.onLocalPlayerReadyChanged += HandleLocalPlayerReadyChanged;
		SoundManager.Instance.PlayMusic(SoundManager.Instance.AudioRefs.backgroundMusic[0]);
		Show();
	}

	private void HandleLocalPlayerReadyChanged(object sender, System.EventArgs e)
	{
		if (GameStateHandler.Instance.isLocalPlayerReadyToStart())
		{
			Hide();
		}
	}

	private void Show()
	{
		gameObject.SetActive(true);
	}
	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
