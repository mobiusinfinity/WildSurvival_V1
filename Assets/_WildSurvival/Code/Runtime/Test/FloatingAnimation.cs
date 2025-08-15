using UnityEngine;

/// <summary>
/// Simple floating and rotation animation for test objects
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    [Header("Floating")]
    public bool enableFloating = true;
    public float amplitude = 0.5f;
    public float frequency = 1f;

    [Header("Rotation")]
    public bool enableRotation = true;
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Pulsing")]
    public bool enablePulsing = false;
    public float pulseScale = 0.1f;
    public float pulseSpeed = 2f;

    private float startY;
    private Vector3 originalScale;

    void Start()
    {
        startY = transform.position.y;
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Floating
        if (enableFloating)
        {
            Vector3 pos = transform.position;
            pos.y = startY + Mathf.Sin(Time.time * frequency) * amplitude;
            transform.position = pos;
        }

        // Rotation
        if (enableRotation)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }

        // Pulsing
        if (enablePulsing)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = originalScale * pulse;
        }
    }

    public void SetFloatParameters(float newAmplitude, float newFrequency)
    {
        amplitude = newAmplitude;
        frequency = newFrequency;
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
}