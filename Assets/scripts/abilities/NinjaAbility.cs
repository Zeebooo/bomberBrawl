using Unity.Netcode;
using UnityEngine;

public class NinjaAbility : AbilityBase
{
	[SerializeField] private GameObject smokeCloudPrefab;
	private Rigidbody2D rb;
	private characterScript cs;
	private bool isTouchingSolidWall = false;
	private Vector2 wallDirection;
	private Vector3 wallCenter;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		cs = GetComponent<characterScript>();
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("solidWall"))
		{
			isTouchingSolidWall = true;
			wallCenter = collision.gameObject.transform.position;
			wallDirection = -collision.contacts[0].normal;
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("solidWall"))
		{
			isTouchingSolidWall = false;
		}
	}
	protected override void ExecuteAbility()
	{
		if (!isTouchingSolidWall)
		{
			ResetCooldown();
			return;
		}

		Vector2 teleportPos = (Vector2)wallCenter + wallDirection * 1f;

		if (!cs.IsWithinMap(teleportPos) || mapGenerator.Instance.IsPositionBreakableWall(teleportPos))
		{
			ResetCooldown();
			return;
		}

		Vector3 startPos = transform.position;
		SpawnSmokeCloudsServerRpc(startPos, teleportPos);
		transform.position = teleportPos;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SpawnSmokeCloudsServerRpc(Vector3 startPos, Vector3 endPos)
	{
		var smokeCloudStart = Instantiate(smokeCloudPrefab, startPos, Quaternion.identity);
		smokeCloudStart.GetComponent<NetworkObject>().Spawn();

		var smokeCloudEnd = Instantiate(smokeCloudPrefab, endPos, Quaternion.identity);
		smokeCloudEnd.GetComponent<NetworkObject>().Spawn();
		PlayAbilitySFXClientRpc();
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.ninjaAbilitySFX[0]);
	}
}
