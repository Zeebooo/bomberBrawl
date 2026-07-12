using UnityEngine;
using TMPro;

public class TextPulsator : MonoBehaviour
{
    private TMP_Text tmpText;
    private Vector3 originalScale;

    [SerializeField] float pulseSpeed = 1f;
    [SerializeField] float maxScale = 1f;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Calculate a sine wave that goes from 0 to 1
        float scaleFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        
        // Map the 0-1 value to our desired scale range
        float currentScale = Mathf.Lerp(1f, maxScale, scaleFactor);

        // Apply the scale uniformly
        transform.localScale = originalScale * currentScale;
    }
}
