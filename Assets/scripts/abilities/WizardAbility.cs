using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WizardAbility : AbilityBase
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
	}

	private void PlayAbilityNormalSFX() // called from animation
	{
		SoundManager.Instance.PlaySFXNormalPitch(SoundManager.Instance.AudioRefs.monkAbilitySFX[0]);
	}
}
