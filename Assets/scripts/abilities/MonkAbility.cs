using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MonkAbility : AbilityBase
{
	private characterScript cs;
	private Animator _animator;
	private float channelingDuration = 3f;
	private int healAmount = 1;

	public override float EffectDuration => channelingDuration;

	private void Start()
	{
		cs = GetComponent<characterScript>();
		_animator = GetComponent<Animator>();
	}

	protected override void ExecuteAbility()
	{
		RunningCoroutine = StartCoroutine(ChannelingCoroutine());
	}

	private IEnumerator ChannelingCoroutine()
	{
		if (cs.getCurrentHealth() >= cs.getMaxHealth())
		{
			ResetCooldown();
			yield break;;
		}
		float channelingTimer = 0f;
		_animator.SetBool("isChanneling", true);
		do
		{
			channelingTimer += Time.deltaTime;
			yield return null;
		} while (channelingTimer < channelingDuration && cs.GetMovementDirection() == Vector2.zero);
		_animator.SetBool("isChanneling", false);

		if (channelingTimer >= channelingDuration)
		{
			cs.addHealth(healAmount);
		}
		else
		{
			ResetCooldown();
		}
	}

	private void PlayAbilityLowSFX() // called from animation
	{
		SoundManager.Instance.PlaySFXLowPitch(SoundManager.Instance.AudioRefs.monkAbilitySFX[0]);
	}

	private void PlayAbilityNormalSFX() // called from animation
	{
		SoundManager.Instance.PlaySFXNormalPitch(SoundManager.Instance.AudioRefs.monkAbilitySFX[0]);
	}
	private void PlayAbilitySFXHighPitch() // called from animation
	{
		SoundManager.Instance.PlaySFXHighPitch(SoundManager.Instance.AudioRefs.monkAbilitySFX[0]);
	}
}
