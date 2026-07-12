using UnityEngine;

public class torchBobbing : MonoBehaviour
{
	private Vector3 startLocalPos;
	private float amplitude = 0.002f;
	private float frequency = 10f;

	void Start()
	{
		startLocalPos = transform.localPosition;
	}

	void Update()
	{
		float bob = Mathf.Sin(Time.time * frequency) * amplitude;
		transform.localPosition = new Vector3(startLocalPos.x, startLocalPos.y + bob, startLocalPos.z);
	}
}

