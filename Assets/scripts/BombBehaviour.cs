using Unity.Netcode;
using UnityEngine;

public class BombBehaviour : NetworkBehaviour
{
	private float pushSpeed = 4f;

	private bool playerLeftHitbox = false;

	private void OnTriggerExit2D(Collider2D other)
	{
		if (!playerLeftHitbox && other.TryGetComponent<characterScript>(out _))
		{
			playerLeftHitbox = true;
			GetComponent<Collider2D>().isTrigger = false;
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (!collision.gameObject.TryGetComponent<characterScript>(out var character)) return;
		if (!character.IsOwner) return; // bara den ägande klienten skickar push-RPC

		Vector2 dir = (transform.position - collision.transform.position).normalized;

		if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
		{
			dir = new Vector2(Mathf.Sign(dir.x), 0f);
		}
		else
		{
			dir = new Vector2(0f, Mathf.Sign(dir.y));
		}

		PushBombServerRpc(dir);
	}

	[Rpc(SendTo.Everyone)]
	private void PushBombServerRpc(Vector2 direction)
	{
		GetComponent<Rigidbody2D>().linearVelocity = direction * pushSpeed;
	}
}
