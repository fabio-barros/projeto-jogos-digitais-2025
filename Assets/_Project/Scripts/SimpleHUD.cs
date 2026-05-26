using UnityEngine;
using UnityEngine.UI;

public class SimpleHUD : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public PlayerShooter2D playerShooter;
    public PlayerBombThrower2D playerBombThrower;
    public Text healthText;
    public Text livesText;
    public Text ammoText;
    public Text bombText;
    public Text scoreText;

    private void Update()
    {
        if (playerHealth != null && healthText != null)
            healthText.text = "HP: " + playerHealth.CurrentHealth + "/" + playerHealth.MaxHealth;

        if (playerHealth != null && livesText != null)
            livesText.text = "Lives: " + playerHealth.CurrentLives + "/" + playerHealth.MaxLives;

        if (GameManager.Instance != null && scoreText != null)
            scoreText.text = "Score: " + GameManager.Instance.Score;

        if (playerBombThrower != null && bombText != null)
            bombText.text = "Bombs: " + playerBombThrower.CurrentBombs + "/" + playerBombThrower.MaxBombs;

        if (playerShooter != null && ammoText != null)
            ammoText.text = playerShooter.IsReloading
                ? "Ammo: Reload"
                : "Ammo: " + playerShooter.CurrentAmmo + "/" + playerShooter.MaxAmmo;
    }
}
