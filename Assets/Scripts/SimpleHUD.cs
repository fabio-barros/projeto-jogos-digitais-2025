using UnityEngine;
using UnityEngine.UI;

public class SimpleHUD : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Text healthText;
    public Text scoreText;

    private void Update()
    {
        if (playerHealth != null && healthText != null)
        {
            healthText.text = "HP: " + playerHealth.CurrentHealth + "/" + playerHealth.MaxHealth;
        }

        if (GameManager.Instance != null && scoreText != null)
        {
            scoreText.text = "Score: " + GameManager.Instance.Score;
        }
    }
}
