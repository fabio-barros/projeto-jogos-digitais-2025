using UnityEngine;

public class Pickup2D : MonoBehaviour
{
    public int healthReward;
    public int ammoReward;
    public int bombReward;
    public int scoreReward;
    public int defaultCoinScore = 100;
    public bool inferTypeFromName = true;
    public bool destroyOnPickup = true;

    private bool pickedUp;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;

        pickedUp = true;
        int finalHealthReward = ResolveHealthReward();
        int finalScoreReward = ResolveScoreReward();

        if (finalHealthReward > 0)
            health.Heal(finalHealthReward);

        PlayerShooter2D shooter = other.GetComponentInParent<PlayerShooter2D>();
        if (shooter != null && ammoReward > 0)
            shooter.RefillAmmo(ammoReward);

        PlayerBombThrower2D bombThrower = other.GetComponentInParent<PlayerBombThrower2D>();
        if (bombThrower != null && bombReward > 0)
            bombThrower.RefillBombs(bombReward);

        if (GameManager.Instance != null && finalScoreReward > 0)
        {
            if (IsNamedLike("coin"))
                GameManager.Instance.AddCoin(finalScoreReward);
            else
                GameManager.Instance.AddScore(finalScoreReward);
        }

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    private int ResolveHealthReward()
    {
        if (healthReward > 0)
            return healthReward;

        return inferTypeFromName && IsNamedLike("health") ? 1 : 0;
    }

    private int ResolveScoreReward()
    {
        if (scoreReward > 0)
            return scoreReward;

        return inferTypeFromName && IsNamedLike("coin") ? defaultCoinScore : 0;
    }

    private bool IsNamedLike(string token)
    {
        return name.ToLowerInvariant().Contains(token);
    }
}
