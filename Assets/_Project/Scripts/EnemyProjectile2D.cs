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
    private float lifetimeTimer;

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

    public void ApplyVariant(float speedOverride, int damageOverride, RuntimeAnimatorController animationController, Vector2 scaleOverride, Color tint)
    {
        if (speedOverride > 0f)
            speed = speedOverride;

        if (damageOverride > 0)
            damage = damageOverride;

        if (scaleOverride.x > 0f && scaleOverride.y > 0f)
            transform.localScale = new Vector3(scaleOverride.x, scaleOverride.y, transform.localScale.z);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = tint;

        if (animationController != null)
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();

            animator.runtimeAnimatorController = animationController;
        }
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
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            PlayerController2D controller = collision.GetComponent<PlayerController2D>();
            if (controller != null && controller.IsCrouching && Mathf.Abs(direction.y) < 0.2f)
                return;

            playerHealth.TakeDamage(damage);
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
