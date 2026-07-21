using Unity.Netcode;
using UnityEngine;

public class BuilderAbility : AbilityBase
{
	[SerializeField] private GameObject builderWallPrefab;
	[SerializeField] private RuntimeAnimatorController builderAnimatorController;
	private Rigidbody2D rb;
	private characterScript cs;
	private Animator _animator;
	private SpriteRenderer _spriteRenderer;
	private RuntimeAnimatorController originalAnimatorController;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		cs = GetComponent<characterScript>();
		_animator = GetComponent<Animator>();
		_spriteRenderer = GetComponent<SpriteRenderer>();
		originalAnimatorController = _animator.runtimeAnimatorController;
	}
	protected override void ExecuteAbility()
	{
		Vector2 movementDirection = cs.GetMovementDirection();
		Vector3 currentTileCenter = mapGenerator.Instance.GetTileCenter(transform.position);
		Vector3 targetTileCenter = currentTileCenter + (Vector3)movementDirection;

		if (mapGenerator.Instance.IsPositionOccupied(targetTileCenter) || mapGenerator.Instance.IsPositionOuterGrid(targetTileCenter))
		{
			ResetCooldown();
			return;
		}
		;
		NetworkSpawnWallServerRPC(targetTileCenter);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void NetworkSpawnWallServerRPC(Vector3 position)
	{
		SetBuildAnimationClientRpc(true);
		GameObject builderWall = Instantiate(builderWallPrefab, position, Quaternion.identity);
		builderWall.GetComponent<NetworkObject>().Spawn();
		PlayAbilitySFXClientRpc();
	}

	[ClientRpc]
	private void SetBuildAnimationClientRpc(bool isActive)
	{
		_animator.runtimeAnimatorController = isActive ? builderAnimatorController : originalAnimatorController;
		_spriteRenderer.flipX = isActive && transform.localScale.x > 0;
	}

	private void StopAnimation() // called from animation
	{
		SetBuildAnimationClientRpc(false);
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.builderAbilitySFX[0]);
	}
}
