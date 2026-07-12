using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;


[RequireComponent(typeof(characterScript))]
public class bombScript : NetworkBehaviour
{
	[Header("Objects")]
	public GameObject[] explosionPrefabs;
	public GameObject[] explosionStartPrefabs;
	public GameObject bombPrefab;
	public Tilemap groundTilemap;

	private GameObject explosionPrefab;
	private GameObject explosionStartPrefab;

	public NetworkVariable<int> MaxBombs => maxBombs;
	public NetworkVariable<float> ExplosionRadius => explosionRadius;
	public NetworkVariable<bool> RemoteBomb => remoteBomb;
	public NetworkVariable<bool> PenetrateWalls => penetrateWalls;

	[Header("Bomb settings")]
	public float fuseTimer = 3f;
	readonly NetworkVariable<int> maxBombs = new(1);
	readonly NetworkVariable<int> currentBombs = new(1);
	readonly NetworkVariable<float> explosionRadius = new(1.28f);
	readonly NetworkVariable<bool> remoteBomb = new(false);
	readonly NetworkVariable<bool> penetrateWalls = new(false);
	private bool remoteTrigger = false;
	private bool spaceWasPressed = false;
	private bool shiftWasPressed = false;

	[Header("Godmode settings")]
	public float godModeTimer = 0f;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			currentBombs.Value = maxBombs.Value;
		}

		int index = (int)Mathf.Min(OwnerClientId, (ulong)(explosionPrefabs.Length - 1));
		explosionPrefab = explosionPrefabs[index];
		explosionStartPrefab = explosionStartPrefabs[index];

		var kb = Keyboard.current;
		if (kb != null)
		{
			spaceWasPressed = kb.spaceKey.isPressed;
			shiftWasPressed = kb.leftShiftKey.isPressed;
		}
	}
	
	void Update()
	{
		if (IsServer && godModeTimer > 0f)
		{
			godModeTimer -= Time.deltaTime;
			if (godModeTimer <= 0f)
				GetComponent<characterScript>().deactivateGodMode();
		}

		if (!IsOwner) return;
		if (!GameStateHandler.Instance.isGameInProgress()) return;
		var kb = Keyboard.current;
		if (kb == null) return;

		bool spaceDown = kb.spaceKey.isPressed;
		bool shiftDown = kb.leftShiftKey.isPressed;

		if (spaceDown && !spaceWasPressed && currentBombs.Value > 0)
		{
			DropBombServerRpc(transform.position);
		}
		if (remoteBomb.Value && shiftDown && !shiftWasPressed)
		{
			DetonateServerRpc();
		}

		spaceWasPressed = spaceDown;
		shiftWasPressed = shiftDown;
	}

	[ServerRpc(RequireOwnership = false)]
	void DropBombServerRpc(Vector3 ownerPosition)
	{
		if (currentBombs.Value <= 0 && godModeTimer <= 0f) return;
		if (godModeTimer <= 0f) currentBombs.Value--;
		{
			StartCoroutine(dropBomb(ownerPosition));
		}
	}

	[ServerRpc(RequireOwnership = false)]
	void DetonateServerRpc()
	{
		remoteTrigger = true;
	}

	private IEnumerator dropBomb(Vector3 spawnPosition)
	{
		Vector3 tileCenter = new(
			Mathf.Floor(spawnPosition.x) + 0.5f,
			Mathf.Floor(spawnPosition.y) + 0.5f,
			0f
		);

		GameObject bomb = Instantiate(bombPrefab, tileCenter, Quaternion.identity);
		NetworkObject bombNetObj = bomb.GetComponent<NetworkObject>();
		bombNetObj.Spawn();

		remoteTrigger = false;
		float timer = 0f;
		while (timer < fuseTimer && !remoteTrigger)
		{
			timer += Time.deltaTime;
			yield return null;
		}

		if (bomb != null)
		{
			Vector3 bombPosition = bomb.transform.position;
			bombNetObj.Despawn();

			SpawnExplosion(bombPosition, Vector2.zero, 0, explosionStartPrefab);
			SpawnExplosion(bombPosition, Vector2.right, -90, explosionPrefab);
			SpawnExplosion(bombPosition, Vector2.up, 0, explosionPrefab);
			SpawnExplosion(bombPosition, Vector2.down, 180, explosionPrefab);
			SpawnExplosion(bombPosition, Vector2.left, 90, explosionPrefab);

			if (currentBombs.Value < maxBombs.Value)
			{
				currentBombs.Value++;
			}
		}
	}

	public void SpawnExplosion(Vector3 origin, Vector2 direction, int rotation, GameObject explosionPref)
	{
		RaycastHit2D hit = Physics2D.Raycast(origin, direction, explosionRadius.Value, ~LayerMask.GetMask("Ignore Raycast", "Bomb"));
		float hitDistance = (hit.collider != null) ? hit.distance : explosionRadius.Value;
		float spriteSize = explosionPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size.y;

		GameObject explosion = null;

		if (explosionPref == explosionStartPrefab)
		{
			explosion = Instantiate(explosionPref, origin, Quaternion.Euler(0, 0, 0));
			explosion.GetComponent<explosionScript>().setIsStartExplosion();
		}
		else if (penetrateWalls.Value)
		{
			explosion = Instantiate(explosionPref, origin, Quaternion.Euler(0, 0, rotation));
			explosion.transform.localScale = new Vector3(explosionPref.transform.localScale.x, explosionRadius.Value, 1f);
		}
		else if (hit.collider == null || hit.collider.CompareTag("solidWall") || hit.collider.CompareTag("breakableWall") || hit.collider.CompareTag("builderWall"))
		{
			explosion = Instantiate(explosionPref, origin, Quaternion.Euler(0, 0, rotation));
			explosion.transform.localScale = new Vector3(explosionPref.transform.localScale.x, hitDistance / spriteSize, 1f);
		}

		if (explosion != null)
		{
			explosion.GetComponent<NetworkObject>().Spawn();
		}
	}


	public void ApplyCharacterData(CharacterDataSO data)
	{
		maxBombs.Value = data.maxBombs;
		currentBombs.Value = data.maxBombs;
		explosionRadius.Value = data.explosionRadius;
	}

	public void addBomb()
	{
		currentBombs.Value++;
		maxBombs.Value++;
	}
	public void increaseExplosionRadius(float multiplier) => explosionRadius.Value *= multiplier;
	public void setRemoteBomb() => remoteBomb.Value = true;
	public void setPenetrateWalls() => penetrateWalls.Value = true;
}
