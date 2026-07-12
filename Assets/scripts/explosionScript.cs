using Unity.Netcode;
using UnityEngine;

public class explosionScript : NetworkBehaviour
{
	public int explosionDamage = 1;
	readonly NetworkVariable<bool> isStartExplosion = new(false);

	public override void OnNetworkSpawn()
	{
		if (isStartExplosion.Value)
		{
			SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.explosionsSFX[0]);
		}
		setDirection(GetComponent<Rigidbody2D>()?.linearVelocity ?? Vector2.zero);
	}

	public void removeExplosion()
	{
		if (IsServer)
		{
			GetComponent<NetworkObject>().Despawn();
		}
	}

	public void setDirection(Vector2 dir)
	{
		if (dir == Vector2.zero) return;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, angle);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.CompareTag("Player") && IsServer)
		{
			collision.gameObject.GetComponent<characterScript>()?.takeDamage(explosionDamage);
		}

		if (collision.gameObject.CompareTag("breakableWall"))
		{
			GameObject wall = collision.gameObject;
			wall.GetComponent<breakableWallScript>()?.breakWall(IsServer);
			mapGenerator.Instance.RemoveWallAtPosition(wall.transform.position);
		}
	}

	public void setIsStartExplosion()
	{
		isStartExplosion.Value = true;
	}
}
