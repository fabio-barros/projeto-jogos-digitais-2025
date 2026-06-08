using UnityEngine;

public class EnemyShooter2D : MonoBehaviour
{
    public GameObject enemyProjectilePrefab;
    public Transform firePoint;
    public float range = 8f;
    public float fireCooldown = 1.5f;
    public float horizontalShotHeight = 0.25f;
    public float verticalShotOffset = 0.25f;
    public bool cardinalAimOnly = true;
    public bool allowVerticalShots;
    public int burstCount = 1;
    public float burstSpacing = 0.16f;
    public float projectileScaleMultiplier = 1f;
    public LayerMask obstacleLayers;

    private Transform player;
    private float cooldownTimer;
    private float shootingFeedbackTimer;
    private int burstShotsRemaining;
    private float burstTimer;
    private Vector2 burstDirection = Vector2.left;
    private Damageable damageable;

    public bool IsShooting { get { return shootingFeedbackTimer > 0f; } }

    private void Start()
    {
        damageable = GetComponent<Damageable>();
        PlayerController2D foundPlayer = FindAnyObjectByType<PlayerController2D>();
        if (foundPlayer != null) player = foundPlayer.transform;
    }

    private void Update()
    {
        if (damageable != null && damageable.IsDead)
            return;

        if (player == null || enemyProjectilePrefab == null || firePoint == null) return;

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (shootingFeedbackTimer > 0) shootingFeedbackTimer -= Time.deltaTime;

        if (burstShotsRemaining > 0)
        {
            burstTimer -= Time.deltaTime;
            if (burstTimer <= 0f)
                FireBurstShot();

            return;
        }

        Vector2 aimDirection = GetAimDirection(player.position - transform.position);
        UpdateFirePoint(aimDirection);

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= range && cooldownTimer <= 0 && HasClearLineOfSight(player))
        {
            burstDirection = aimDirection;
            burstShotsRemaining = Mathf.Max(1, burstCount);
            burstTimer = 0f;
            cooldownTimer = fireCooldown;
        }
    }

    private void FireBurstShot()
    {
        UpdateFirePoint(burstDirection);

        GameObject projectileObject = ObjectPool2D.Spawn(enemyProjectilePrefab, firePoint.position, Quaternion.identity);
        projectileObject.transform.localScale = enemyProjectilePrefab.transform.localScale * projectileScaleMultiplier;

        EnemyProjectile2D projectile = projectileObject.GetComponent<EnemyProjectile2D>();
        if (projectile != null)
            projectile.SetDirection(burstDirection);

        burstShotsRemaining--;
        burstTimer = burstSpacing;
        shootingFeedbackTimer = 0.2f;
    }

    private Vector2 GetAimDirection(Vector2 toPlayer)
    {
        if (!cardinalAimOnly)
            return toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.left;

        if (!allowVerticalShots)
            return toPlayer.x >= 0f ? Vector2.right : Vector2.left;

        if (Mathf.Abs(toPlayer.y) > Mathf.Abs(toPlayer.x) * 1.15f)
            return toPlayer.y > 0f ? Vector2.up : Vector2.down;

        return toPlayer.x >= 0f ? Vector2.right : Vector2.left;
    }

    private void UpdateFirePoint(Vector2 direction)
    {
        if (firePoint == null)
            return;

        if (Mathf.Abs(direction.x) > 0.5f)
            firePoint.position = transform.position + new Vector3(direction.x * 0.62f, horizontalShotHeight, 0f);
        else
            firePoint.position = transform.position + new Vector3(0f, direction.y > 0f ? verticalShotOffset : -verticalShotOffset, 0f);
    }

    public bool HasClearLineOfSight(Transform target)
    {
        if (target == null)
            return false;

        if (obstacleLayers.value == 0)
            return true;

        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 targetPoint = target.position;
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleLayers);
        return hit.collider == null;
    }

    private void OnDrawGizmos()
    {
        if (!GameplayDebugOverlay2D.DrawLineOfSight || player == null)
            return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Gizmos.color = HasClearLineOfSight(player) ? Color.green : Color.red;
        Gizmos.DrawLine(origin, player.position);
    }
}
