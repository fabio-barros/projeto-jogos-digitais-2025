using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int maxLives = 3;
    public float invulnerabilityTime = 0.7f;
    public float deathReloadDelay = 1.35f;
    public float respawnInvulnerabilityTime = 1.2f;
    public float fallDeathY = -18f;

    private static int remainingLives = -1;

    private int currentHealth;
    private float invulnerabilityTimer;
    private float deathTimer;
    private bool isDead;
    private bool isGameOver;
    private SpriteRenderer[] spriteRenderers;
    private bool[] rendererInitialEnabled;
    private Color[] rendererInitialColors;
    private Rigidbody2D rb;
    private Vector3 respawnPosition;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public int CurrentLives { get { return Mathf.Max(remainingLives, 0); } }
    public int MaxLives { get { return maxLives; } }
    public bool IsInvulnerable { get { return invulnerabilityTimer > 0f; } }
    public bool IsDead { get { return isDead; } }
    public bool IsGameOver { get { return isGameOver; } }
    public bool CanAct { get { return !isDead && !isGameOver; } }
    public bool WasRecentlyHurt { get { return invulnerabilityTimer > invulnerabilityTime - 0.2f; } }

    private void Awake()
    {
        if (remainingLives <= 0 || remainingLives > maxLives)
            remainingLives = maxLives;

        currentHealth = maxHealth;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        rendererInitialEnabled = new bool[spriteRenderers.Length];
        rendererInitialColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            rendererInitialEnabled[i] = spriteRenderers[i].enabled;
            rendererInitialColors[i] = spriteRenderers[i].color;
        }

        rb = GetComponent<Rigidbody2D>();
        respawnPosition = transform.position;
    }

    private void Update()
    {
        if (isGameOver)
        {
            if (RemnantInput.RestartDown())
                RestartScene();

            return;
        }

        if (!isDead && transform.position.y <= fallDeathY)
            Die();

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
            SetVisualsVisible(Mathf.FloorToInt(invulnerabilityTimer * 12f) % 2 == 0);
        }
        else
        {
            SetVisualsVisible(true);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (invulnerabilityTimer > 0) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void SetRespawnPoint(Vector3 position)
    {
        respawnPosition = position;
    }

    private void Respawn()
    {
        if (remainingLives <= 0)
        {
            GameOver();
            return;
        }

        currentHealth = maxHealth;
        isDead = false;
        invulnerabilityTimer = respawnInvulnerabilityTime;
        transform.position = respawnPosition;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetVisualsVisible(true);

        SendMessage("OnPlayerRespawned", SendMessageOptions.DontRequireReceiver);
    }

    private void Die()
    {
        if (isDead || isGameOver) return;

        remainingLives--;
        currentHealth = 0;
        isDead = true;
        deathTimer = deathReloadDelay;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void GameOver()
    {
        isGameOver = true;
        isDead = true;
        currentHealth = 0;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetVisualsVisible(true);
    }

    private void RestartScene()
    {
        remainingLives = maxLives;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private void SetVisualsVisible(bool visible)
    {
        if (spriteRenderers == null || rendererInitialEnabled == null || rendererInitialColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null || !rendererInitialEnabled[i])
                continue;

            Color color = rendererInitialColors[i];
            color.a = visible ? rendererInitialColors[i].a : 0f;
            spriteRenderers[i].color = color;
        }
    }
}
