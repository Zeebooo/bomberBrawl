using Unity.Netcode;
using UnityEngine;

public abstract class AbilityBase : NetworkBehaviour
{
	private readonly NetworkVariable<float> coolDownDuration = new(0f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
	private readonly NetworkVariable<float> cooldownTimer = new(0f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
	private readonly NetworkVariable<bool> isCooldownActive = new(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

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

	public virtual void Activate()
	{
		if (isCooldownActive.Value) return;
		isCooldownActive.Value = true;
		cooldownTimer.Value = coolDownDuration.Value;
		ExecuteAbility();
	}

	protected abstract void ExecuteAbility();

	// Sant om senaste ExecuteAbility()-anropet misslyckades (dvs kallade ResetCooldown()). Används av Jester.
	public bool LastEffectFailed { get; private set; }

	// Kör effekten utan att röra den här instansens egen cooldown — används av Jester för att låna andra abilities.
	public void TriggerEffect()
	{
		LastEffectFailed = false;
		ExecuteAbility();
	}

	// Hur länge effekten visuellt pågår efter aktivering (0 = momentan). Används av Jester som fallback om RunningCoroutine inte sätts.
	public virtual float EffectDuration => 0f;

	// Sätts av abilities som startar en lokal coroutine i ExecuteAbility(), så Jester kan vänta in exakt när den är klar.
	public Coroutine RunningCoroutine { get; protected set; }

	protected void ResetCooldown()
	{
		isCooldownActive.Value = false;
		cooldownTimer.Value = 0f;
		LastEffectFailed = true;
	}
}
