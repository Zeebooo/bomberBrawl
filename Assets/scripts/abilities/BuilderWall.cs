using UnityEngine;
using Unity.Netcode;

public class BuilderWall : NetworkBehaviour
{
	[SerializeField] private GameObject breakEffect;
	private float wallDuration = 3f;
	private float wallTimer = 0f;
	private bool hasTriggeredDestroy = false;

	public override void OnNetworkSpawn()
	{
		GetComponent<SpriteRenderer>().sortingOrder = -Mathf.FloorToInt(transform.position.y);
	}

	void Update()
	{
		if (hasTriggeredDestroy) return;

		wallTimer += Time.deltaTime;
		if (wallTimer >= wallDuration)
		{
			hasTriggeredDestroy = true;
			PlayAbilitySFXClientRpc();
			NetworkDestroyWallServerRPC();
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void NetworkDestroyWallServerRPC()
	{
		GameObject effect = Instantiate(breakEffect, transform.position, transform.rotation);
		effect.GetComponent<NetworkObject>().Spawn();
		Destroy(gameObject);
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.builderAbilitySFX[1]);
	}
}
