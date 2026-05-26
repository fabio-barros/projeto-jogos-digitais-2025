using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    public int maxHealth = 3;
    public int scoreValue = 100;
    public UnityEvent onDeath;

    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
