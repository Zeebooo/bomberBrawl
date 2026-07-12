using Unity.Netcode;
using UnityEngine;

public class NinjaAbility : AbilityBase
{
	[SerializeField] private GameObject smokeCloudPrefab;
	private Rigidbody2D rb;
	private characterScript cs;
	private bool isTouchingWall = false;
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
			isTouchingWall = true;
			wallCenter = collision.gameObject.transform.position;
			wallDirection = -collision.contacts[0].normal;
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("solidWall"))
		{
			isTouchingWall = false;
		}
	}
	protected override void ExecuteAbility()
	{
		if (!isTouchingWall) return;

		Vector2 teleportPos = (Vector2)wallCenter + wallDirection * 1f;

		if (!cs.IsWithinMap(teleportPos) || mapGenerator.Instance.IsPositionBreakableWall(teleportPos))
		{
			ResetCooldown();
			return;
		}

		var smokeCloudStart = Instantiate(smokeCloudPrefab, transform.position, Quaternion.identity);
		smokeCloudStart.GetComponent<NetworkObject>().Spawn();

		var smokeCloudEnd = Instantiate(smokeCloudPrefab, teleportPos, Quaternion.identity);
		smokeCloudEnd.GetComponent<NetworkObject>().Spawn();
		transform.position = teleportPos;
		PlayAbilitySFXClientRpc();
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.ninjaAbilitySFX[0]);
	}
}
