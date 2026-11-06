using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 150f;

    void Update()
    {
        // 1. Get input from W/S (Vertical) and A/D (Horizontal)
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // 2. Move the player forward/backward based on the direction they are currently facing
        transform.Translate(Vector3.forward * verticalInput * moveSpeed * Time.deltaTime);

        // 3. Rotate the player left/right around the Y axis
        transform.Rotate(Vector3.up * horizontalInput * rotationSpeed * Time.deltaTime);
    }
}
