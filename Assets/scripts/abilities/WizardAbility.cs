using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WizardAbility : AbilityBase
{
	[SerializeField] private SpriteRenderer curseRingRenderer;
	private characterScript cs;
	private Animator _animator;
	private float curseRadius = 3.5f; // 3.5f
	private float movementspeedMultiplier = 0.8f;
	private float abilityDuration = 3f;

	private void Start()
	{
		cs = GetComponent<characterScript>();
		_animator = GetComponent<Animator>();

		if (curseRingRenderer != null)
		{
			float nativeRadius = curseRingRenderer.sprite.bounds.extents.x;
			float worldScale = curseRadius / nativeRadius;

			Vector3 parentScale = curseRingRenderer.transform.parent.lossyScale;
			curseRingRenderer.transform.localScale = new Vector3(
				worldScale / Mathf.Abs(parentScale.x),
				worldScale / Mathf.Abs(parentScale.y),
				1f
			);

			bool isPrimaryWizard = cs.GetAbility() is WizardAbility;
			curseRingRenderer.gameObject.SetActive(isPrimaryWizard && IsOwner);
		}
	}

	protected override void ExecuteAbility()
	{
		findCurseTargetsServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void findCurseTargetsServerRpc()
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, curseRadius, ~0);
		int characterHits = 0;

		foreach (Collider2D hit in hits)
		{
			characterScript otherCs = hit.GetComponentInParent<characterScript>();
			if (otherCs == null) continue;
			characterHits++;

			if (otherCs != cs)
			{
				float currentSpeed = otherCs.getMovementSpeed();
				StartCoroutine(applyCurse(currentSpeed, otherCs));
			}
		}
	}

	private IEnumerator applyCurse(float speedBefore, characterScript otherCs)
	{
		float speedReduction = speedBefore * movementspeedMultiplier;
		otherCs.reduceMovementSpeed(speedReduction);
		otherCs.changeCurseStatusRpc(true);
		yield return new WaitForSeconds(abilityDuration);
		otherCs.addMovementSpeed(speedReduction);
		otherCs.changeCurseStatusRpc(false);
	}

	public void setCurseRingVisibility(bool isVisible)
	{
		if (curseRingRenderer != null)
		{
			curseRingRenderer.gameObject.SetActive(isVisible);
		}
	}

	private void PlayAbilityNormalSFX()
	{
		SoundManager.Instance.PlaySFXNormalPitch(SoundManager.Instance.AudioRefs.monkAbilitySFX[0]);
	}
}
