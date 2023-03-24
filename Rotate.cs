using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Tooltip("The speed at which this object rotates around its z-axis.")]
    public float rotSpeed = 3f;

    // FixedUpdate is called at fixed intervals, which can help smooth out the rotation.
    void FixedUpdate()
    {
        // Rotate the object around its z-axis by the specified amount.
        transform.Rotate(0f, 0f, rotSpeed);
    }
}
