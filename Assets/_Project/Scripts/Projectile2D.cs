using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 2.5f;
    public LayerMask hitLayers;
    public GameObject hitEffectPrefab;

    private Vector2 direction = Vector2.right;
    private Rigidbody2D rb;
    private float lifetimeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

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
        ApplyVelocity();
    }

    private void OnEnable()
    {
        lifetimeTimer = lifetime;
        ApplyVelocity();
    }

    private void Update()
    {
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Despawn();
            return;
        }

        if (rb == null)
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, direction);
            SpawnHitEffect();
            Despawn();
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            SpawnHitEffect();
            Despawn();
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            ObjectPool2D.Spawn(hitEffectPrefab, transform.position, Quaternion.identity);
    }

    private void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    private void Despawn()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        ObjectPool2D.Despawn(gameObject);
    }
}
