using Unity.Netcode;
using UnityEngine;

public class breakableWallScript : MonoBehaviour
{
	public GameObject wallBreakEffectPrefab;

	public void breakWall(bool isServer)
	{
		if (isServer)
		{
			ItemSpawningManager.Instance.spawnRandomPowerUp(transform.position);
			if (wallBreakEffectPrefab != null)
			{
				GameObject wallBreak = Instantiate(wallBreakEffectPrefab, transform.position, Quaternion.identity);
				if (wallBreak.TryGetComponent<NetworkObject>(out var wallBreakNetObj))
					wallBreakNetObj.Spawn();
			}
		}

		Destroy(gameObject);
	}
}
