using UnityEngine;

public class itemBobbing : MonoBehaviour
{
    private float amplitude = 0.1f;
    private float frequency = 3f;

    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        float scaleFactor = 1f + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localScale = startScale * scaleFactor;
    }
}
