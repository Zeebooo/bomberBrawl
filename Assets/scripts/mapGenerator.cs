using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class mapGenerator : NetworkBehaviour
{
	[Header("Tilemaps")]
	public Tilemap groundTilemap;

	[Header("Tiles")]
	public TileBase groundTile;

	[Header("Prefabs")]
	[SerializeField] private GameObject solidWallPrefab;
	[SerializeField] private GameObject solidWallTopBottomPrefab;
	[SerializeField] private GameObject solidWallLeftRightPrefab;
	[SerializeField] private GameObject breakableWallPrefab;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private GameObject mapDecorations;

	[Header("Settings")]
	private int width2Players = 11;
	private int height2Players = 9;

	private int width3orMorePlayers = 13;
	private int height3orMorePlayers = 11;
	private int width;
	private int height;
	private int playerCount;

	private List<Vector3> breakableWallPositions = new();

	private float breakableWallChance = 0.8f;

	public Vector3[] spawnPoints { get; private set; }

	readonly NetworkVariable<int> mapSeed = new(0);
	public static mapGenerator Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}
	public override void OnNetworkSpawn()
	{
		mapSeed.OnValueChanged += (_, seed) => GenerateMap(seed);

		if (IsServer)
		{
			mapSeed.Value = Random.Range(1, int.MaxValue);
		}
		else if (mapSeed.Value != 0)
		{
			// Klient som joinar efter att seed redan är satt
			GenerateMap(mapSeed.Value);
		}
	}

	void GenerateMap(int seed)
	{
		Random.InitState(seed);

		playerCount = NetworkManager.Singleton.ConnectedClients.Count;
		if (playerCount <= 2)
		{
			width = width2Players;
			height = height2Players;
			mainCamera.transform.position = new Vector3(4f, 4.5f, -10f);
			mainCamera.orthographicSize = 6.2f;
			mapDecorations.SetActive(true);
		}
		else
		{
			width = width3orMorePlayers;
			height = height3orMorePlayers;
			mainCamera.transform.position = new Vector3(4f, 5.75f, -10f);
			mainCamera.orthographicSize = 7f;
			mapDecorations.SetActive(false);
		}

		spawnPoints = new Vector3[]
		{
			new(0.5f, height - 0.35f), // top left
			new(width - 0.5f, 0.65f), // bottom right
			new(width - 0.5f, height - 0.35f), // top right
			new(0.5f, 0.65f), // bottom left
		};

		for (int x = -1; x <= width; x++)
		{
			for (int y = -1; y <= height; y++)
			{
				bool isLeftRightEdge = x == -1 || x == width;
				bool isTopBottomEdge = y == -1 || y == height;
				bool isInnerGrid = x % 2 != 0 && y % 2 != 0;

				// Vänster och höger ytterkanter
				if (isLeftRightEdge)
				{
					GameObject wall = Instantiate(solidWallLeftRightPrefab, new Vector3(x + 0.5f, y + 0.65f, 0), Quaternion.identity);
					wall.GetComponent<SpriteRenderer>().sortingOrder = -y;
				}
				// Övre och undre ytterkanter
				else if (isTopBottomEdge)
				{
					GameObject wall = Instantiate(solidWallTopBottomPrefab, new Vector3(x + 0.5f, y + 0.65f, 0), Quaternion.identity);
					wall.GetComponent<SpriteRenderer>().sortingOrder = -y;
				}
				// Inre rutnätsväggar
				else if (isInnerGrid)
				{
					GameObject wall = Instantiate(solidWallPrefab, new Vector3(x + 0.5f, y + 0.65f, 0), Quaternion.identity);
					wall.GetComponent<SpriteRenderer>().sortingOrder = -y;
				}
				else
				{
					Vector3Int pos = new(x, y, 0);
					groundTilemap.SetTile(pos, groundTile);
					bool isSafe = IsSpawnProtected(x, y);

					if (!isSafe && Random.value <= breakableWallChance)
					{
						GameObject wall = Instantiate(breakableWallPrefab, new Vector3(x + 0.5f, y + 0.65f, 0), Quaternion.identity);
						breakableWallPositions.Add(wall.transform.position);
						wall.GetComponent<SpriteRenderer>().sortingOrder = -y;
					}
				}
			}
		}
	}

	bool IsSpawnProtected(int x, int y)
	{
		if (playerCount <= 2)
		{
			return (x <= 2 && y >= height - 2) || // uppe vänster
			(x >= width - 2 && y <= 2);  // nere höger
		}
		else if (playerCount == 3)
		{
			return (x <= 2 && y >= height - 2) ||       // uppe vänster
			(x >= width - 2 && y <= 2) ||             // nere höger
			(x >= width - 2 && y >= height - 2);      // uppe höger
		}
		else if (playerCount == 4)
		{
			return (x <= 2 && y >= height - 2) ||       // uppe vänster
			(x >= width - 2 && y <= 2) ||             // nere höger
			(x >= width - 2 && y >= height - 2) ||     // uppe höger
			(x <= 2 && y <= 2);                        // nere vänster
		}
		return false;
	}

	public float getMapHeight()
	{
		return height;
	}

	public float getMapWidth()
	{
		return width;
	}

	public bool IsPositionBreakableWall(Vector3 position)
	{
		foreach (Vector3 wallPosition in breakableWallPositions)
		{
			if (Vector3.Distance(wallPosition, position) < 0.1f)
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveWallAtPosition(Vector3 position)
	{
		for (int i = 0; i < breakableWallPositions.Count; i++)
		{
			if (Vector3.Distance(breakableWallPositions[i], position) < 0.1f)
			{
				breakableWallPositions.RemoveAt(i);
				return;
			}
		}
	}
}
