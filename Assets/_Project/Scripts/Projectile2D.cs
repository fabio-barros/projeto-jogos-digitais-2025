using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 14f;
    public int damage = 1;
    public float lifetime = 2.5f;
    public LayerMask hitLayers;

    private Vector2 direction = Vector2.right;

    public void SetDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude <= 0.01f) newDirection = Vector2.right;
        direction = newDirection.normalized;

        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
            Destroy(gameObject);
    }
}
