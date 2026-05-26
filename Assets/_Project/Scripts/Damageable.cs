using UnityEngine;
using UnityEngine.Events;
using System;

public class Damageable : MonoBehaviour
{
    public int maxHealth = 3;
    public int scoreValue = 100;
    public float deathDelay = 0.2f;
    public UnityEvent onDeath;

    private int currentHealth;
    private bool isDead;

    public event Action Died;
    public int CurrentHealth { get { return currentHealth; } }
    public bool IsDead { get { return isDead; } }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        Died?.Invoke();
        onDeath?.Invoke();

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Destroy(gameObject, deathDelay);
    }
}
