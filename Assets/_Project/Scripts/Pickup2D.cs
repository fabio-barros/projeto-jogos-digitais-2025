using UnityEngine;

public class Pickup2D : MonoBehaviour
{
    public int healthReward;
    public int ammoReward;
    public int bombReward;
    public int scoreReward;
    public bool destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null)
            return;

        if (healthReward > 0)
            health.Heal(healthReward);

        PlayerShooter2D shooter = other.GetComponent<PlayerShooter2D>();
        if (shooter != null && ammoReward > 0)
            shooter.RefillAmmo(ammoReward);

        PlayerBombThrower2D bombThrower = other.GetComponent<PlayerBombThrower2D>();
        if (bombThrower != null && bombReward > 0)
            bombThrower.RefillBombs(bombReward);

        if (GameManager.Instance != null && scoreReward > 0)
            GameManager.Instance.AddScore(scoreReward);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
