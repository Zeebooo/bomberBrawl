using Unity.Netcode;
using UnityEngine;

public class breakableWallScript : MonoBehaviour
{
	private float itemSpawnChance = 0.6f;
	public GameObject wallBreakEffectPrefab;

	[Header("Common item settings")]
	public GameObject[] commonItemPrefabs;
	public GameObject[] rareItemPrefabs;
	public GameObject[] legendaryItemPrefabs;


	private float commonItemChance = 0.75f; // 0.75f

	private float rareItemChance = 0.2f; // 0.2f

	private float legendaryItemChance = 0.05f; // 0.05f

	public void breakWall(bool isServer)
	{
		float roll = Random.value;

		if (isServer && roll <= itemSpawnChance)
		{
			Vector3 spawnPos = new(
				Mathf.Floor(transform.position.x) + 0.5f,
				Mathf.Floor(transform.position.y) + 0.3f,
				0f
			);

			float spawnRoll = Random.value;
			GameObject prefab = null;

			if (spawnRoll <= commonItemChance)
			{
				prefab = commonItemPrefabs.Length > 0 ? commonItemPrefabs[Random.Range(0, commonItemPrefabs.Length)] : null;
			}
			else if (spawnRoll <= commonItemChance + rareItemChance)
			{
				prefab = rareItemPrefabs.Length > 0 ? rareItemPrefabs[Random.Range(0, rareItemPrefabs.Length)] : null;
			}
			else if (spawnRoll <= commonItemChance + rareItemChance + legendaryItemChance)
			{
				prefab = legendaryItemPrefabs.Length > 0 ? legendaryItemPrefabs[Random.Range(0, legendaryItemPrefabs.Length)] : null;
			}

			if (prefab != null)
			{
				GameObject item = Instantiate(prefab, spawnPos, Quaternion.identity);
				if (item.TryGetComponent<NetworkObject>(out var netObj))
				{
					netObj.Spawn();
				}
					
			}
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
