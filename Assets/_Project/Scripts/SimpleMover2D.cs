using UnityEngine;

public class SimpleMover2D : MonoBehaviour
{
    public Vector2 localMoveOffset = new Vector2(3f, 0f);
    public float speed = 1f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (localMoveOffset.sqrMagnitude <= 0.01f || speed <= 0f)
            return;

        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        transform.position = Vector3.Lerp(startPosition, startPosition + (Vector3)localMoveOffset, t);
    }
}
