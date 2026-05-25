using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerabilityTime = 0.7f;

    private int _currentHealth;
    private float _invulnerabilityTimer;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    private void Update()
    {
        if (_invulnerabilityTimer > 0)
        {
            _invulnerabilityTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int amount)
    {
        if (_invulnerabilityTimer > 0) return;

        _currentHealth -= amount;
        _invulnerabilityTimer = invulnerabilityTime;

        if (_currentHealth <= 0)
        {
            RestartLevel();
        }
    }

    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
