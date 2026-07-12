using UnityEngine;

public class WaitingForPlayersUI : MonoBehaviour
{
	private void Start()
	{
		GameStateHandler.Instance.onLocalPlayerReadyChanged += HandleLocalPlayerReadyChanged;
		GameStateHandler.Instance.onStateChanged += HandleStateChanged;
		Hide();
	}

	private void HandleLocalPlayerReadyChanged(object sender, System.EventArgs e)
	{
		if (GameStateHandler.Instance.isLocalPlayerReadyToStart())
		{
			Show();
		}
	}
	private void HandleStateChanged(object sender, System.EventArgs e)
	{
		if (GameStateHandler.Instance.isCountDownToStart())
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
