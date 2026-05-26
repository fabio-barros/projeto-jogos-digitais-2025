using UnityEngine;
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int maxLives = 3;
    public float invulnerabilityTime = 0.7f;
    public float deathReloadDelay = 0.45f;
    public float respawnInvulnerabilityTime = 1.2f;

    private static int remainingLives = -1;

    private int currentHealth;
    private float invulnerabilityTimer;
    private float deathTimer;
    private bool isDead;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector3 respawnPosition;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public int CurrentLives { get { return Mathf.Max(remainingLives, 0); } }
    public int MaxLives { get { return maxLives; } }
    public bool IsInvulnerable { get { return invulnerabilityTimer > 0f; } }
    public bool IsDead { get { return isDead; } }
    public bool CanAct { get { return !isDead; } }
    public bool WasRecentlyHurt { get { return invulnerabilityTimer > invulnerabilityTime - 0.2f; } }

    private void Awake()
    {
        if (remainingLives <= 0 || remainingLives > maxLives)
            remainingLives = maxLives;

        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        respawnPosition = transform.position;
    }

    private void Update()
    {
        if (isDead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
                Respawn();

            return;
        }

        if (invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (spriteRenderer != null)
                spriteRenderer.enabled = Mathf.FloorToInt(invulnerabilityTimer * 12f) % 2 == 0;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (invulnerabilityTimer > 0) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        if (currentHealth <= 0)
        {
            remainingLives--;
            isDead = true;
            deathTimer = deathReloadDelay;
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Respawn()
    {
        if (remainingLives <= 0)
            remainingLives = maxLives;

        currentHealth = maxHealth;
        isDead = false;
        invulnerabilityTimer = respawnInvulnerabilityTime;
        transform.position = respawnPosition;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        SendMessage("OnPlayerRespawned", SendMessageOptions.DontRequireReceiver);
    }
}
