using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    public float rotationSpeed = 30f;

    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}