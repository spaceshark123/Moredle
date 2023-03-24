using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulse : MonoBehaviour
{
    [Tooltip("The amount by which to pulse the object's scale.")]
    public float pulseStrength = 0.1f;

    [Tooltip("The frequency of the object's pulse.")]
    public float pulseFrequency = 1f;

    // The object's original scale, before pulsing.
    Vector3 originalScale;

    // Start is called before the first frame update.
    void Start()
    {
        // Save the object's original scale for later use.
        originalScale = transform.localScale;
    }

    // Update is called once per frame.
    void Update()
    {
        // Calculate the amount to offset the object's scale based on the sine of time multiplied by the pulse frequency.
        float offset = pulseStrength * Mathf.Sin(Time.time * pulseFrequency);

        // Set the object's scale to its original scale plus the calculated offset.
        transform.localScale = new Vector3(originalScale.x + offset, originalScale.y + offset, originalScale.z + offset);
    }
}
