using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GeologistAbility : AbilityBase
{
	[SerializeField] private RuntimeAnimatorController geologistAnimatorController;
	[SerializeField] private Image abilityDurationCircle;
	private characterScript cs;
	private Animator _animator;
	private RuntimeAnimatorController originalAnimatorController;
	private float abilityDuration = 10f;
	private NetworkVariable<bool> isAbilityActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

	public override float EffectDuration => abilityDuration;

	private void Start()
	{
		cs = GetComponent<characterScript>();
		_animator = GetComponent<Animator>();
		originalAnimatorController = _animator.runtimeAnimatorController;
		abilityDurationCircle.gameObject.SetActive(false);
	}

	protected override void ExecuteAbility()
	{
		PlayAbilitySFXServerRpc();
		RunningCoroutine = StartCoroutine(ChannelingCoroutine());
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PlayAbilitySFXServerRpc()
	{
		PlayAbilitySFXClientRpc();
	}

	private IEnumerator ChannelingCoroutine()
	{
		isAbilityActive.Value = true;
		_animator.runtimeAnimatorController = geologistAnimatorController;
		abilityDurationCircle.gameObject.SetActive(true);

		float timer = 0f;
		while (timer < abilityDuration && isAbilityActive.Value)
		{
			timer += Time.deltaTime;
			abilityDurationCircle.fillAmount = 1f - (timer / abilityDuration);
			yield return null;
		}

		abilityDurationCircle.gameObject.SetActive(false);
		isAbilityActive.Value = false;
	}

	private void StopAnimation() // called from animation
	{
		_animator.runtimeAnimatorController = originalAnimatorController;
	}

	public bool IsAbilityActive()
	{
		return isAbilityActive.Value;
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void ConsumeDoublingRpc()
	{
		isAbilityActive.Value = false;
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.geologistAbilitySFX[0]);
	}
}
