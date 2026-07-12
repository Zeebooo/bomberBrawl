using TMPro;
using UnityEngine;

public class WorldEventPopup : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI popUpText;
	[SerializeField] private TextMeshProUGUI timerText;

	private readonly float blinkDuration = 2f;
	private readonly float blinkInterval = 0.2f;
	private float blinkTimer;
	private float blinkElapsed;
	private bool isBlinking;
	private string currentEvent;

	private float remainingTime;

	public static WorldEventPopup Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		popUpText.enabled = false;
		timerText.enabled = false;
	}

	private void Update()
	{
		if (remainingTime >= 0f)
		{
			remainingTime -= Time.deltaTime;
			timerText.text = $"{currentEvent}: {Mathf.CeilToInt(remainingTime)}";
			if (remainingTime <= 0f)
				timerText.enabled = false;
		}

		if (!isBlinking) return;

		blinkElapsed += Time.deltaTime;
		blinkTimer += Time.deltaTime;

		if (blinkElapsed >= blinkDuration)
		{
			isBlinking = false;
			popUpText.enabled = false;
			return;
		}
		if (blinkTimer >= blinkInterval)
		{
			blinkTimer = 0f;
			popUpText.enabled = !popUpText.enabled;
		}
	}

	public void ShowEvent(float duration, string eventName)
	{
		remainingTime = duration;
		isBlinking = true;
		blinkElapsed = 0f;
		blinkTimer = 0f;
		popUpText.text = eventName;
		currentEvent = eventName;
		popUpText.enabled = true;
		timerText.enabled = true;
	}
}
