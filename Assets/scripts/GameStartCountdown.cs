using UnityEngine;
using TMPro;
using System;

public class GameStartCountdown : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI countdownText;

	private void Start()
	{
		GameStateHandler.Instance.onStateChanged += HandleGameStateChanged;
		Hide();
	}

	private void HandleGameStateChanged(object sender, EventArgs e)
	{
		if(GameStateHandler.Instance.isCountDownToStart())
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	private void Show()
	{
		countdownText.gameObject.SetActive(true);
	}
	private void Hide()
	{
		countdownText.gameObject.SetActive(false);
	}

	private void Update()
	{
		countdownText.text = Mathf.Ceil(GameStateHandler.Instance.getCountDownToStartTimer()).ToString();
	}
}
