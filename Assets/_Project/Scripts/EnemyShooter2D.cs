using UnityEngine;

public class EnemyShooter2D : MonoBehaviour
{
    public GameObject enemyProjectilePrefab;
    public Transform firePoint;
    public float range = 8f;
    public float fireCooldown = 1.5f;

    private Transform player;
    private float cooldownTimer;
    private float shootingFeedbackTimer;

    public bool IsShooting { get { return shootingFeedbackTimer > 0f; } }

    private void Start()
    {
        PlayerController2D foundPlayer = FindAnyObjectByType<PlayerController2D>();
        if (foundPlayer != null) player = foundPlayer.transform;
    }

    private void Update()
    {
        if (player == null || enemyProjectilePrefab == null || firePoint == null) return;

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (shootingFeedbackTimer > 0) shootingFeedbackTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= range && cooldownTimer <= 0)
        {
            GameObject projectileObject = Instantiate(enemyProjectilePrefab, firePoint.position, Quaternion.identity);
            EnemyProjectile2D projectile = projectileObject.GetComponent<EnemyProjectile2D>();
            if (projectile != null)
                projectile.SetDirection((player.position - firePoint.position).normalized);

            cooldownTimer = fireCooldown;
            shootingFeedbackTimer = 0.2f;
        }
    }
}
