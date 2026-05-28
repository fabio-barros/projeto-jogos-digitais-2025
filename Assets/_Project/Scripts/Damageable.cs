using UnityEngine;
using UnityEngine.Events;
using System;

public class Damageable : MonoBehaviour
{
    public int maxHealth = 3;
    public int scoreValue = 100;
    public float deathDelay = 0.2f;
    public float hitFlashDuration = 0.08f;
    public float knockbackForce = 2.5f;
    public GameObject deathEffectPrefab;
    public UnityEvent onDeath;

    private int currentHealth;
    private bool isDead;
    private float hitFlashTimer;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Rigidbody2D rb;

    public event Action Died;
    public int CurrentHealth { get { return currentHealth; } }
    public bool IsDead { get { return isDead; } }

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    private void Update()
    {
        if (hitFlashTimer <= 0f)
            return;

        hitFlashTimer -= Time.deltaTime;
        if (hitFlashTimer <= 0f)
            RestoreColors();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;
        Flash();
        ApplyKnockback(hitDirection);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        Died?.Invoke();
        onDeath?.Invoke();

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Destroy(gameObject, deathDelay);
    }

    private void Flash()
    {
        if (spriteRenderers == null)
            return;

        hitFlashTimer = hitFlashDuration;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = Color.white;
        }
    }

    private void RestoreColors()
    {
        if (spriteRenderers == null || originalColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private void ApplyKnockback(Vector2 hitDirection)
    {
        if (rb == null || knockbackForce <= 0f || hitDirection.sqrMagnitude <= 0.01f)
            return;

        Vector2 force = hitDirection.normalized * knockbackForce;
        force.y = Mathf.Max(force.y, 0.6f);
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}
