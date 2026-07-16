using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HaldoAbility : AbilityBase
{
	private characterScript cs;
	private Animator _animator;
	private float abilityDuration = 3f;
	private float movementSpeedMultiplier = 2f;
	private RuntimeAnimatorController originalAnimatorController;
	[SerializeField] private RuntimeAnimatorController whirlwindAnimatorController;
	[SerializeField] private GameObject bobbingHeadPrefab;

	private void Start()
	{
		cs = GetComponent<characterScript>();
		_animator = GetComponent<Animator>();
		originalAnimatorController = _animator.runtimeAnimatorController;
		bobbingHeadPrefab.SetActive(false);
	}

	protected override void ExecuteAbility()
	{
		WhirlwindServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void WhirlwindServerRpc()
	{
		StartCoroutine(WhirlwindRoutine());
		PlayAbilitySFXClientRpc();
	}

	private IEnumerator WhirlwindRoutine()
	{
		float bonus = cs.getMovementSpeed() * (movementSpeedMultiplier - 1f);
		cs.addMovementSpeed(bonus);
		SetWhirlwindVisualsClientRpc(true);

		yield return new WaitForSeconds(abilityDuration);

		cs.addMovementSpeed(-bonus);
		SetWhirlwindVisualsClientRpc(false);
	}

	[ClientRpc]
	private void SetWhirlwindVisualsClientRpc(bool isActive)
	{
		_animator.runtimeAnimatorController = isActive ? whirlwindAnimatorController : originalAnimatorController;
		bobbingHeadPrefab.SetActive(isActive);
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.haldoAbilitySFX[0]);
	}
}
