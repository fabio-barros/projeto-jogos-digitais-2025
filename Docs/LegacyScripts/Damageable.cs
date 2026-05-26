using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    public int maxHealth = 3;
    public int scoreValue = 100;
    public UnityEvent onDeath;

    private int _currentHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
