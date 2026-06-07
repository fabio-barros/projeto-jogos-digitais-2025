using UnityEngine;

[CreateAssetMenu(menuName = "Remnant Squad/Enemy Definition", fileName = "EnemyDefinition")]
public class EnemyDefinition2D : ScriptableObject
{
    public string displayName = "Keth Grunt";
    public int maxHealth = 2;
    public int scoreValue = 120;
    public float moveSpeed = 2f;
    public float detectionRange = 28f;
    public float preferredShootDistance = 5.5f;
    public float closePressureDistance = 2.5f;
    public float jumpForce = 7.4f;
    public float fireCooldown = 1.4f;
    public int burstCount = 1;
    public bool canShoot = true;
    public bool canJumpObstacles = true;

    public void ApplyTo(GameObject enemy)
    {
        if (enemy == null)
            return;

        Damageable damageable = enemy.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.maxHealth = maxHealth;
            damageable.scoreValue = scoreValue;
            damageable.ResetHealthToMax();
        }

        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol != null)
        {
            patrol.moveSpeed = moveSpeed;
            patrol.detectionRange = detectionRange;
            patrol.preferredShootDistance = preferredShootDistance;
            patrol.closePressureDistance = closePressureDistance;
            patrol.jumpForce = jumpForce;
            patrol.canJumpObstacles = canJumpObstacles;
        }

        EnemyShooter2D shooter = enemy.GetComponent<EnemyShooter2D>();
        if (shooter != null)
        {
            shooter.enabled = canShoot;
            shooter.fireCooldown = fireCooldown;
            shooter.burstCount = burstCount;
        }
    }
}
