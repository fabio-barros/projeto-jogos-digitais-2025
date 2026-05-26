using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerabilityTime = 0.7f;

    private int currentHealth;
    private float invulnerabilityTimer;
    private SpriteRenderer spriteRenderer;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
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
        if (invulnerabilityTimer > 0) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        if (currentHealth <= 0)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}
