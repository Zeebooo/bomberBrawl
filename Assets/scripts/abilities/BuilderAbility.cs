using Unity.Netcode;
using UnityEngine;

public class BuilderAbility : AbilityBase
{
	[SerializeField] private GameObject builderWallPrefab;
	private Rigidbody2D rb;
	private characterScript cs;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		cs = GetComponent<characterScript>();
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
		NetworkSpawnWallServerRPC(targetTileCenter);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void NetworkSpawnWallServerRPC(Vector3 position)
	{
		GameObject builderWall = Instantiate(builderWallPrefab, position, Quaternion.identity);
		builderWall.GetComponent<NetworkObject>().Spawn();
		PlayAbilitySFXClientRpc();
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.builderAbilitySFX[0]);
	}
}
