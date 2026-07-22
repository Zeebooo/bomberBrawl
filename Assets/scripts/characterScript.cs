using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.Universal;

public class characterScript : NetworkBehaviour
{
	[Header("Player stats")]
	readonly NetworkVariable<int> maxHealth = new(3);
	readonly NetworkVariable<int> currentHealth = new(3);
	readonly NetworkVariable<float> moveSpeed = new(5f);
	public float invincibilityDuration = 1.5f;
	private float invincibilityTimer = 0f;
	readonly NetworkVariable<bool> shieldActive = new(false);
	readonly NetworkVariable<bool> godMode = new(false);
	private Vector2 movementDirection;

	[Header("World event settings")]
	readonly NetworkVariable<bool> invertedControlsActive = new(false);
	public float invertedControlsDuration = 0f;
	readonly NetworkVariable<bool> slipperyActive = new(false);

	public NetworkVariable<int> CurrentHealth => currentHealth;
	public NetworkVariable<float> MoveSpeed => moveSpeed;
	public NetworkVariable<bool> GodMode => godMode;
	public NetworkVariable<bool> InvertedControlsActive => invertedControlsActive;
	public NetworkVariable<bool> SlipperyActive => slipperyActive;

	[Header("References")]
	private Rigidbody2D rb;
	private Collider2D col;
	private bombScript bomb;
	public GameObject bombPrefab;
	public GameObject shieldPrefab;
	public GameObject torchPrefab;
	private AbilityBase ability;
	[SerializeField] private AbilityBase primaryAbility;
	private Animator _animator;
	[SerializeField] private RuntimeAnimatorController godModeController;
	private RuntimeAnimatorController defaultController;
	[SerializeField] private GameObject shadowPrefab;
	[SerializeField] private GameObject activeShadowPrefab;
	[SerializeField] private GameObject curseSymbolPrefab;

	static readonly int runUpHash = Animator.StringToHash("isRunningUp");
	static readonly int runDownHash = Animator.StringToHash("isRunningDown");
	static readonly int runHorizontalHash = Animator.StringToHash("isRunningHorizontal");
	static readonly int isDead = Animator.StringToHash("isDead");

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		_animator = GetComponent<Animator>();
		col = GetComponent<Collider2D>();
		bomb = GetComponent<bombScript>();
		defaultController = _animator.runtimeAnimatorController;
		shieldPrefab.SetActive(false);
		torchPrefab.SetActive(false);
		ability = primaryAbility != null ? primaryAbility : GetComponent<AbilityBase>();
		curseSymbolPrefab.SetActive(false);

		if (IsOwner)
		{
			shadowPrefab.SetActive(false);
			activeShadowPrefab.SetActive(true);
		}
		else
		{
			shadowPrefab.SetActive(true);
			activeShadowPrefab.SetActive(false);
		}
	}

	void Awake()
	{
		ability = primaryAbility != null ? primaryAbility : GetComponent<AbilityBase>();
	}

	public override void OnNetworkSpawn()
	{
		shieldActive.OnValueChanged += (_, newValue) => shieldPrefab.SetActive(newValue);
		currentHealth.OnValueChanged += (_, newValue) =>
		{
			if (newValue <= 0)
				Die();
		};
		godMode.OnValueChanged += (_, newValue) =>
		{
			_animator.runtimeAnimatorController = newValue && godModeController != null
				? godModeController
				: defaultController;
		};
	}

	void FixedUpdate()
	{
		if (IsServer)
		{
			if (invincibilityTimer > 0f)
			{
				invincibilityTimer -= Time.deltaTime;
			}
		}

		if (IsOwner)
		{
			moveCharacter();
			if (mapGenerator.Instance != null && !IsWithinMap(transform.position))
			{
				transform.position = ClampToMap(transform.position);
				rb.linearVelocity = Vector2.zero;
			}

			var kb = Keyboard.current;
			if (kb == null) return;

		}
	}

	void Update()
	{
		if (!IsOwner) return;
		pauseGame();

		var kb = Keyboard.current;
		if (kb == null || ability == null) return;

		string abilityKeyName = PlayerPrefs.GetString("AbilityBind", "Q");
		if (!string.IsNullOrEmpty(abilityKeyName) && kb[abilityKeyName] is ButtonControl abilityKey && abilityKey.wasPressedThisFrame)
		{
			ability.Activate();
		}
	}

	public Vector2 GetMovementDirection()
	{
		return movementDirection;
	}

	void moveCharacter()
	{
		if (GameStateHandler.Instance == null || !GameStateHandler.Instance.isGameInProgress()) return;

		var kb = Keyboard.current;
		if (kb == null) return;
		resetAnimation();

		Vector2 dir = Vector2.zero;
		movementDirection = Vector2.zero;
		Vector3 leftScaleCharacter = new Vector3(-4, 4, 4);
		Vector3 rightScaleCharacter = new Vector3(4, 4, 4);

		Vector3 torchLeftScale = new Vector3(-torchPrefab.transform.localScale.x, torchPrefab.transform.localScale.y, 1);
		Vector3 torchRightScale = new Vector3(torchPrefab.transform.localScale.x, torchPrefab.transform.localScale.y, 1);

		if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
		{
			if (invertedControlsActive.Value)
			{
				_animator.SetBool(runDownHash, true);
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				dir.y -= 1f;
				movementDirection = Vector2.down;
			}
			else
			{
				_animator.SetBool(runUpHash, true);
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				dir.y += 1f;
				movementDirection = Vector2.up;
			}
		}
		if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
		{
			if (invertedControlsActive.Value)
			{
				_animator.SetBool(runUpHash, true);
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				dir.y += 1f;
				movementDirection = Vector2.up;
			}
			else
			{
				_animator.SetBool(runDownHash, true);
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				dir.y -= 1f;
				movementDirection = Vector2.down;
			}
		}
		if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
		{
			_animator.SetBool(runHorizontalHash, true);
			if (invertedControlsActive.Value)
			{
				transform.localScale = leftScaleCharacter;
				torchPrefab.transform.localScale = torchRightScale;
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				dir.x -= 1f;
				movementDirection = Vector2.left;
			}
			else
			{
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				transform.localScale = rightScaleCharacter;
				torchPrefab.transform.localScale = torchLeftScale;
				dir.x += 1f;
				movementDirection = Vector2.right;
			}
		}
		if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
		{
			_animator.SetBool(runHorizontalHash, true);
			if (invertedControlsActive.Value)
			{
				transform.localScale = rightScaleCharacter;
				torchPrefab.transform.localScale = torchLeftScale;
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				dir.x += 1f;
				movementDirection = Vector2.right;
			}
			else
			{
				shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
				torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 0;
				torchPrefab.transform.localScale = torchRightScale;
				transform.localScale = leftScaleCharacter;
				dir.x -= 1f;
				movementDirection = Vector2.left;
			}
		}

		Vector2 targetVelocity = dir.normalized * moveSpeed.Value;
		if (slipperyActive.Value)
			rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, 1.2f * Time.fixedDeltaTime);
		else
			rb.linearVelocity = targetVelocity;
	}

	void pauseGame()
	{
		if (Keyboard.current == null) return;
		if (Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			OptionsUI.Instance.changeActiveStatus();
		}
	}

	void Die()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.deathSoundSFX[0]);
		_animator.SetTrigger(isDead);
		rb.linearVelocity = Vector2.zero;
		enabled = false;
		col.enabled = false;
		bomb.enabled = false;
		resetAnimation();
	}

	public void forceKill()
	{
		currentHealth.Value = 0;
	}


	public void switchPreparation(bool visible)
	{
		if (currentHealth.Value <= 0) return;

		foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
		{
			sr.enabled = visible;
		}
		col.enabled = visible;
	}

	public void resetAnimation()
	{
		_animator.SetBool(runHorizontalHash, false);
		_animator.SetBool(runUpHash, false);
		_animator.SetBool(runDownHash, false);
		shieldPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
		torchPrefab.GetComponent<SpriteRenderer>().sortingOrder = 2;
	}

	public bool IsWithinMap(Vector3 pos)
	{
		float mapWidth = mapGenerator.Instance.getMapWidth();
		float mapHeight = mapGenerator.Instance.getMapHeight();

		return pos.x >= 0f && pos.x <= mapWidth &&
			   pos.y >= 0f && pos.y <= mapHeight;
	}

	private Vector3 ClampToMap(Vector3 position)
	{
		float x = Mathf.Clamp(position.x, 1f, mapGenerator.Instance.getMapWidth() - 1f);
		float y = Mathf.Clamp(position.y, 1f, mapGenerator.Instance.getMapHeight() - 1f);
		return new Vector3(x, y, position.z);
	}

	public void ApplyCharacterData(CharacterDataSO data)
	{
		maxHealth.Value = data.maxHealth;
		currentHealth.Value = data.maxHealth;
		moveSpeed.Value = data.moveSpeed;
		if (ability != null) ability.setCoolDownDuration(data.abilityCooldown);
	}

	public float getInvertedControlsDuration() => invertedControlsDuration;

	// Publika wrappers så att övriga scripts (t.ex. ItempickupScript) inte behöver ändras
	public void takeDamage(int damage) => takeDamageRpc(damage);
	public void addHealth(int amount) => addHealthRpc(amount);
	public void activateShield() => activateShieldRpc();
	public void activateGodMode(float duration) => activateGodModeRpc(duration);
	[Rpc(SendTo.Everyone)]
	public void changeCurseStatusRpc(bool isActive) => curseSymbolPrefab.SetActive(isActive);
	public void deactivateGodMode() => deactivateGodModeRpc();
	public void increaseMovementSpeed(float multiplier) => moveSpeed.Value *= multiplier;
	public void addMovementSpeed(float amount) => moveSpeed.Value += amount;
	public void reduceMovementSpeed(float amound) => moveSpeed.Value -= amound;
	public float getMovementSpeed() => moveSpeed.Value;
	public int getCurrentHealth() => currentHealth.Value;
	public int getMaxHealth() => maxHealth.Value;
	public AbilityBase GetAbility() => ability;

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void takeDamageRpc(int damage)
	{
		if (invincibilityTimer > 0f || godMode.Value) return;

		if (shieldActive.Value)
		{
			shieldActive.Value = false;
			invincibilityTimer = invincibilityDuration;
			return;
		}

		currentHealth.Value -= damage;
		invincibilityTimer = invincibilityDuration;

	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void addHealthRpc(int amount)
	{
		if (currentHealth.Value < maxHealth.Value)
		{
			if (currentHealth.Value + amount > maxHealth.Value)
			{
				currentHealth.Value = maxHealth.Value;
			}
			else
			{
				currentHealth.Value += amount;
			}
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void activateShieldRpc()
	{
		shieldActive.Value = true;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void activateGodModeRpc(float duration)
	{
		godMode.Value = true;
		if (TryGetComponent(out bombScript bombScript))
		{
			bombScript.godModeTimer = duration;
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void deactivateGodModeRpc()
	{
		godMode.Value = false;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void setResultScreenRpc() // Kallas från animation
	{
		if (GameStateHandler.Instance.isGameOverState())
			GameStateHandler.Instance.setState("resultScreen");
	}
}
