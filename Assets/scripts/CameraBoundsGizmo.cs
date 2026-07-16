using UnityEngine;

[ExecuteAlways]
public class CameraBoundsGizmo : MonoBehaviour
{
	private float targetAspect = 21f / 9f;

	private static readonly (float orthoSize, Vector3 center, Color color)[] CameraConfigs =
	{
		(6.2f, new Vector3(4f, 4.5f, 0f), Color.cyan),   // 2 spelare
		(7f, new Vector3(4f, 5.75f, 0f), Color.yellow),  // 3-4 spelare
	};

	// Matchar exakt mapGenerator.cs: width2Players/height2Players och width3orMorePlayers/height3orMorePlayers
	private static readonly (int width, int height, Color color)[] MazeConfigs =
	{
		(11, 9, Color.green),  // 2 spelare
		(13, 11, Color.red),   // 3-4 spelare
	};

	private void OnDrawGizmos()
	{
		foreach (var (orthoSize, center, color) in CameraConfigs)
		{
			float height = orthoSize * 2f;
			float width = height * targetAspect;

			Gizmos.color = color;
			Gizmos.DrawWireCube(center, new Vector3(width, height, 0f));
		}

		foreach (var (mazeWidth, mazeHeight, color) in MazeConfigs)
		{
			// Samma formel som GenerateMap() använder för att placera väggtiles (loop från -1 till width/height)
			Vector3 min = new(-1f + 0.5f, -1f + 0.65f, 0f);
			Vector3 max = new(mazeWidth + 0.5f, mazeHeight + 0.65f, 0f);
			Vector3 center = (min + max) / 2f;
			Vector3 size = max - min;

			Gizmos.color = color;
			Gizmos.DrawWireCube(center, size);
		}
	}
}
