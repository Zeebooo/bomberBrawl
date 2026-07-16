using UnityEngine;

public class HaldoHeadBobbing : MonoBehaviour
{
	[SerializeField] private float amplitude = 0.05f;
	[SerializeField] private float frequency = 5f;

	private Vector3 startLocalPos;

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
