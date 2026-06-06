using UnityEngine;

public class EnemyProjectile2D : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 1;
    public float lifetime = 3f;
    public LayerMask hitLayers;
    public GameObject hitEffectPrefab;

    private Vector2 direction = Vector2.left;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude <= 0.01f) newDirection = Vector2.left;
        direction = newDirection.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        ApplyVelocity();
    }

    private void Start()
    {
        ApplyVelocity();
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (rb == null)
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }

    private void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }
}
