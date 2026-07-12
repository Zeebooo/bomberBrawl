using Unity.Netcode;
using UnityEngine;

public abstract class AbilityBase : NetworkBehaviour
{
	private float coolDownDuration;
	private float cooldownTimer;
	private bool isCooldownActive = false;

	public float CooldownProgress => coolDownDuration > 0 ? cooldownTimer / coolDownDuration : 0f;
	public int CooldownTimeRemaining => Mathf.CeilToInt(cooldownTimer);
	public bool IsCooldownActive => isCooldownActive;

	public void setCoolDownDuration(float duration) => coolDownDuration = duration;

	protected virtual void Update()
	{
		if (!isCooldownActive) return;

		cooldownTimer -= Time.deltaTime;
		if (cooldownTimer <= 0f)
		{
			isCooldownActive = false;
		}
	}

	public void Activate()
	{
		if (isCooldownActive) return;
		isCooldownActive = true;
		cooldownTimer = coolDownDuration;
		ExecuteAbility();
	}

	protected abstract void ExecuteAbility();

	protected void ResetCooldown()
	{
		isCooldownActive = false;
		cooldownTimer = 0f;
	}
}
