using UnityEngine;

public class AmbientLooper2D : MonoBehaviour
{
    public Vector2 velocity = new Vector2(-1f, 0f);
    public float travelDistance = 12f;
    public bool loop = true;
    public bool flipToVelocity = true;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
        ApplyFacing();
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (travelDistance <= 0f)
            return;

        if (Vector2.Distance(startPosition, transform.position) < travelDistance)
            return;

        if (loop)
        {
            transform.position = startPosition;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ApplyFacing()
    {
        if (!flipToVelocity || Mathf.Abs(velocity.x) < 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (velocity.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }
}
