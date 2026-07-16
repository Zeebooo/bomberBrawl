using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class guiHusScript : MonoBehaviour
{
	[SerializeField] private Image guiBackground;
	[SerializeField] private TextMeshProUGUI playerNameText;
	[SerializeField] private CharacterDataSO defaultData;

	private TextMeshProUGUI movementSpeedText;
	private TextMeshProUGUI bombAmountText;
	private TextMeshProUGUI radiusText;
	private Image[] hearts;
	private Image remoteIcon;
	private Image penetratingIcon;
	[SerializeField] private Image abilityIcon;
	private Sprite[] activeHealthSprites;
	private characterScript character;
	private bombScript bomb;
	private AbilityBase ability;
	private CanvasGroup canvasGroup;
	[SerializeField] private Image abilityCooldownImage;
	[SerializeField] private TextMeshProUGUI abilityCooldownText;

	void Awake()
	{
		if (!Application.isPlaying) return;

		var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
		movementSpeedText = texts[0];
		bombAmountText = texts[1];
		radiusText = texts[2];

		hearts = new Image[]
		{
			transform.Find("heart1").GetComponent<Image>(),
			transform.Find("heart2").GetComponent<Image>(),
			transform.Find("heart3").GetComponent<Image>(),
			transform.Find("heart4").GetComponent<Image>()
		};
		remoteIcon = transform.Find("remote").GetComponent<Image>();
		penetratingIcon = transform.Find("penetrating").GetComponent<Image>();

		remoteIcon.gameObject.SetActive(false);
		penetratingIcon.gameObject.SetActive(false);

		if (abilityCooldownImage != null) abilityCooldownImage.fillAmount = 0f;
		if (abilityCooldownText != null) abilityCooldownText.gameObject.SetActive(false);

		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0;
	}

	public void Setup(characterScript c, string playerName)
	{
		character = c;
		bomb = c.GetComponent<bombScript>();

		int charIndex = Mathf.Clamp(
			GameLobby.Instance.GetPlayerData(c.OwnerClientId).selectedCharacterIndex,
			0, GameLobby.Instance.characters.Length - 1
		);
		activeHealthSprites = GameLobby.Instance.characters[charIndex].GUI;
		abilityIcon.sprite = GameLobby.Instance.characters[charIndex].abilityIcon;

		ability = c.GetAbility();
		if (playerNameText != null) playerNameText.text = playerName;
		canvasGroup.alpha = 1;
	}

	void Update()
	{
		if (character == null || bomb == null) return;

		UpdateHearts(character.CurrentHealth.Value);
		UpdateMoveSpeed(character.MoveSpeed.Value);
		UpdateBombAmount(bomb.MaxBombs.Value);
		UpdateRadius(bomb.ExplosionRadius.Value);
		remoteIcon.gameObject.SetActive(bomb.RemoteBomb.Value);
		penetratingIcon.gameObject.SetActive(bomb.PenetrateWalls.Value);
		UpdateAbilityCooldown();
	}

	void UpdateAbilityCooldown()
	{
		if (ability == null || abilityCooldownImage == null || abilityCooldownText == null) return;

		if (ability is JesterAbility jester && jester.CurrentIcon != null)
			abilityIcon.sprite = jester.CurrentIcon;

		if (!character.IsOwner)
		{
			abilityCooldownImage.fillAmount = 0f;
			abilityCooldownText.gameObject.SetActive(false);
			return;
		}

		abilityCooldownImage.fillAmount = ability.CooldownProgress;
		abilityCooldownText.gameObject.SetActive(ability.IsCooldownActive);
		if (ability.IsCooldownActive)
			abilityCooldownText.text = ability.CooldownTimeRemaining.ToString();
	}

	void UpdateHearts(int health)
	{
		for (int i = 0; i < hearts.Length; i++)
			hearts[i].enabled = i < health;

		if (guiBackground != null && activeHealthSprites != null && activeHealthSprites.Length > 0)
		{
			int index = Mathf.Clamp(health - 1, 0, activeHealthSprites.Length - 1);
			guiBackground.sprite = activeHealthSprites[index];
		}
	}

	void UpdateMoveSpeed(float speed)
	{
		movementSpeedText.text = $"{CharacterSelectUI.FormatPercent(speed / defaultData.moveSpeed)}";
	}

	void UpdateBombAmount(int amount) => bombAmountText.text = $"{amount}";
	void UpdateRadius(float radius) => radiusText.text = $"{CharacterSelectUI.FormatPercent(radius / defaultData.explosionRadius)}";
}
