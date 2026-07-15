using Unity.Netcode;
using UnityEngine;

public abstract class AbilityBase : NetworkBehaviour
{
	private readonly NetworkVariable<float> coolDownDuration = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private readonly NetworkVariable<float> cooldownTimer = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private readonly NetworkVariable<bool> isCooldownActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public float CooldownProgress => coolDownDuration.Value > 0 ? cooldownTimer.Value / coolDownDuration.Value : 0f;
	public int CooldownTimeRemaining => Mathf.CeilToInt(cooldownTimer.Value);
	public bool IsCooldownActive => isCooldownActive.Value;

	public void setCoolDownDuration(float duration) => coolDownDuration.Value = duration;

	protected virtual void Update()
	{
		if (!IsOwner || !isCooldownActive.Value) return;

		cooldownTimer.Value -= Time.deltaTime;
		if (cooldownTimer.Value <= 0f)
		{
			isCooldownActive.Value = false;
		}
	}

	public void Activate()
	{
		if (isCooldownActive.Value) return;
		isCooldownActive.Value = true;
		cooldownTimer.Value = coolDownDuration.Value;
		ExecuteAbility();
	}

	protected abstract void ExecuteAbility();

	protected void ResetCooldown()
	{
		isCooldownActive.Value = false;
		cooldownTimer.Value = 0f;
	}
}
