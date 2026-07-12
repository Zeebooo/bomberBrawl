using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyCreateUI : MonoBehaviour
{
	[SerializeField] private Button goBackButton;
	[SerializeField] private Button createPublicButton;
	[SerializeField] private Button createPrivateButton;
	[SerializeField] private TMP_InputField lobbyNameInput;

	private void Awake()
	{
		createPublicButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);

			if (!string.IsNullOrWhiteSpace(lobbyNameInput.text))
			{
				StartCoroutine(deactivateButtonsTemporarily(3f));
				GameLobby.Instance.CreateLobby(lobbyNameInput.text, false);

			}
		});
		createPrivateButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.positiveClickSFX[0]);
			if (!string.IsNullOrWhiteSpace(lobbyNameInput.text))
			{
				StartCoroutine(deactivateButtonsTemporarily(3f));
				GameLobby.Instance.CreateLobby(lobbyNameInput.text, true);

			}
		});
		goBackButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayClickSFX(SoundManager.Instance.AudioRefs.negativeClickSFX[0]);
			Hide();
		});
	}

	private void deactivateButtons()
	{
		createPublicButton.interactable = false;
		createPrivateButton.interactable = false;
		goBackButton.interactable = false;
		lobbyNameInput.interactable = false;
	}

	private IEnumerator deactivateButtonsTemporarily(float seconds)
	{
		deactivateButtons();
		yield return new WaitForSeconds(seconds);
		activateButtons();
	}

	private void activateButtons()
	{
		createPublicButton.interactable = true;
		createPrivateButton.interactable = true;
		goBackButton.interactable = true;
		lobbyNameInput.interactable = true;
	}

	private void Start()
	{
		Hide();
	}

	public void Show()
	{
		activateButtons();
		gameObject.SetActive(true);
	}
	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
