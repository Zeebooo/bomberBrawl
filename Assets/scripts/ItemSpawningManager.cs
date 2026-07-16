using UnityEngine;
using Unity.Netcode;

public class ItemSpawningManager : MonoBehaviour
{
	[SerializeField] private GameObject[] commonItemPrefabs;
	[SerializeField] private GameObject[] rareItemPrefabs;
	[SerializeField] private GameObject[] legendaryItemPrefabs;

	private float itemSpawnChance = 0.6f;
	private float commonItemChance = 0.75f; // 0.75f
	private float rareItemChance = 0.2f; // 0.2f
	private float legendaryItemChance = 0.05f; // 0.05f

	public static ItemSpawningManager Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}

	public void spawnRandomPowerUp(Vector3 position)
	{
		float roll = Random.value;

		if (roll <= itemSpawnChance)
		{
			Vector3 spawnPos = new(
				Mathf.Floor(position.x) + 0.5f,
				Mathf.Floor(position.y) + 0.3f,
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
		}
	}
}
