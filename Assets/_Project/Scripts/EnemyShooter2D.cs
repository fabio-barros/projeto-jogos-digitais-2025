using UnityEngine;

public class EnemyShooter2D : MonoBehaviour
{
    public enum RunNGunShotVariant
    {
        DefaultRifle,
        MultiRifle,
        Blade,
        Fireball,
        Energy,
        BigShot
    }

    private enum RunNGunShootState
    {
        Init,
        Active,
        Waiting,
        Computing
    }

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
    public bool applyRunNGunShotVariant = true;
    public RunNGunShotVariant shotVariant = RunNGunShotVariant.DefaultRifle;
    public bool useRunNGunShootCycle = true;
    public float initTime = 0f;
    public float activeShootTime = 0.9f;
    public float waitShootTime = 0.5f;
    public float maxAngleWithLeftShot = 60f;
    public float maxAngleWithUpShot = 30f;
    public float maxAngleWithRightShot = 60f;
    public bool requireSameLevelToShoot = true;
    public float sameLevelVerticalTolerance = 0.85f;
    public bool requirePlayerInFrontToShoot = true;

    private Transform player;
    private float cooldownTimer;
    private float shootingFeedbackTimer;
    private int burstShotsRemaining;
    private float burstTimer;
    private Vector2 burstDirection = Vector2.left;
    private Damageable damageable;
    private RunNGunShootState runNGunState = RunNGunShootState.Init;
    private float runNGunStateTimer;
    private float runNGunShotTimer;
    private Vector2 runNGunLineOfShot = Vector2.left;
    private float projectileSpeedOverride;
    private int projectileDamageOverride;
    private Vector2 projectileScaleOverride;
    private RuntimeAnimatorController projectileAnimationController;
    private Color projectileTint = Color.white;

    public bool IsShooting { get { return shootingFeedbackTimer > 0f; } }
    public bool IsShootingUp { get; private set; }
    public bool IsWaiting { get; private set; }
    public bool IsWaitingUp { get; private set; }

    private void Start()
    {
        damageable = GetComponent<Damageable>();
        ApplyRunNGunShotVariant();

        PlayerController2D foundPlayer = FindAnyObjectByType<PlayerController2D>();
        if (foundPlayer != null) player = foundPlayer.transform;
    }

    public void ApplyRunNGunShotVariant()
    {
        if (!applyRunNGunShotVariant)
            return;

        useRunNGunShootCycle = true;
        allowVerticalShots = false;
        cardinalAimOnly = true;
        projectileTint = Color.white;

        switch (shotVariant)
        {
            case RunNGunShotVariant.MultiRifle:
                ConfigureShotVariant(0.18f, 0.9f, 0.9f, 3, 0.09f, 7.5f, 1, new Vector2(0.44f, 0.22f), "Animations/Bullets/DefaultBulletCont", Color.white);
                break;

            case RunNGunShotVariant.Blade:
                ConfigureShotVariant(0.32f, 0.6f, 0.95f, 1, 0.12f, 6f, 1, new Vector2(0.5f, 0.28f), "Animations/Bullets/BladeBulletCont", new Color(0.8f, 0.95f, 1f, 1f));
                break;

            case RunNGunShotVariant.Fireball:
                ConfigureShotVariant(0.1f, 0.12f, 0.95f, 1, 0.12f, 5.6f, 1, new Vector2(0.46f, 0.46f), "Animations/Bullets/FireballBulletCont", new Color(1f, 0.72f, 0.42f, 1f));
                break;

            case RunNGunShotVariant.Energy:
                ConfigureShotVariant(0.22f, 0.55f, 0.75f, 1, 0.12f, 8.5f, 1, new Vector2(0.46f, 0.28f), "Animations/Bullets/EnergyBulletCont", new Color(0.6f, 0.95f, 1f, 1f));
                break;

            case RunNGunShotVariant.BigShot:
                ConfigureShotVariant(0.45f, 0.7f, 1.05f, 1, 0.12f, 5.2f, 2, new Vector2(0.72f, 0.5f), "Animations/Bullets/BigBulletCont", new Color(1f, 0.9f, 0.55f, 1f));
                break;

            default:
                ConfigureShotVariant(0.22f, 0.5f, 1f, 1, 0.16f, 7f, 1, new Vector2(0.44f, 0.22f), "Animations/Bullets/DefaultBulletCont", Color.white);
                break;
        }
    }

    private void ConfigureShotVariant(float cooldown, float activeTime, float waitTime, int shotsPerBurst, float spacing, float projectileSpeed, int projectileDamage, Vector2 projectileScale, string controllerResourcePath, Color tint)
    {
        fireCooldown = cooldown;
        activeShootTime = activeTime;
        waitShootTime = waitTime;
        burstCount = Mathf.Max(1, shotsPerBurst);
        burstSpacing = spacing;
        projectileSpeedOverride = projectileSpeed;
        projectileDamageOverride = projectileDamage;
        projectileScaleOverride = projectileScale;
        projectileTint = tint;
        projectileAnimationController = Resources.Load<RuntimeAnimatorController>(controllerResourcePath);
    }

    private void Update()
    {
        if (damageable != null && damageable.IsDead)
            return;

        if (player == null || enemyProjectilePrefab == null || firePoint == null) return;

        if (useRunNGunShootCycle)
        {
            UpdateRunNGunShootCycle();
            return;
        }

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

        if (CanShootAtPlayer() && cooldownTimer <= 0)
        {
            burstDirection = aimDirection;
            burstShotsRemaining = Mathf.Max(1, burstCount);
            burstTimer = 0f;
            cooldownTimer = fireCooldown;
        }
    }

    private void UpdateRunNGunShootCycle()
    {
        if (shootingFeedbackTimer > 0f)
            shootingFeedbackTimer -= Time.deltaTime;

        runNGunStateTimer += Time.deltaTime;
        runNGunShotTimer -= Time.deltaTime;

        ResetRunNGunAnimationFlags();

        bool canSeePlayer = CanShootAtPlayer();

        if (!canSeePlayer)
        {
            if (runNGunState != RunNGunShootState.Init)
                ChangeRunNGunState(RunNGunShootState.Computing);

            return;
        }

        switch (runNGunState)
        {
            case RunNGunShootState.Init:
                if (runNGunStateTimer >= initTime)
                    ChangeRunNGunState(RunNGunShootState.Computing);
                break;

            case RunNGunShootState.Computing:
                ComputeRunNGunLineOfShot(player.position - transform.position);
                ChangeRunNGunState(RunNGunShootState.Active);
                break;

            case RunNGunShootState.Active:
                SetRunNGunShootingFlags(runNGunLineOfShot);
                UpdateFirePoint(runNGunLineOfShot);

                if (canSeePlayer && runNGunShotTimer <= 0f)
                {
                    FireRunNGunShot();
                    runNGunShotTimer = fireCooldown;
                }

                if (runNGunStateTimer >= activeShootTime)
                    ChangeRunNGunState(RunNGunShootState.Waiting);
                break;

            case RunNGunShootState.Waiting:
                SetRunNGunWaitingFlags(runNGunLineOfShot);

                if (runNGunStateTimer >= waitShootTime)
                    ChangeRunNGunState(RunNGunShootState.Computing);
                break;
        }
    }

    private void ChangeRunNGunState(RunNGunShootState nextState)
    {
        runNGunState = nextState;
        runNGunStateTimer = 0f;

        if (nextState == RunNGunShootState.Active)
            runNGunShotTimer = 0f;
    }

    private void ComputeRunNGunLineOfShot(Vector2 toPlayer)
    {
        if (toPlayer.sqrMagnitude <= 0.01f)
            return;

        float angleWithLeft = Vector2.Angle(toPlayer, Vector2.left);
        float angleWithUp = Vector2.Angle(toPlayer, Vector2.up);
        float angleWithRight = Vector2.Angle(toPlayer, Vector2.right);

        if (angleWithLeft < maxAngleWithLeftShot)
            runNGunLineOfShot = Vector2.left;
        else if (angleWithRight < maxAngleWithRightShot)
            runNGunLineOfShot = Vector2.right;
        else if (allowVerticalShots && angleWithUp < maxAngleWithUpShot)
            runNGunLineOfShot = Vector2.up;
        else
            runNGunLineOfShot = toPlayer.x >= 0f ? Vector2.right : Vector2.left;

        FaceRunNGunShotDirection(runNGunLineOfShot);
        UpdateFirePoint(runNGunLineOfShot);
    }

    private bool CanShootAtPlayer()
    {
        if (player == null)
            return false;

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > range)
            return false;

        if (requireSameLevelToShoot && Mathf.Abs(toPlayer.y) > sameLevelVerticalTolerance)
            return false;

        if (requirePlayerInFrontToShoot && Mathf.Abs(toPlayer.x) > 0.05f && Mathf.Sign(toPlayer.x) != Mathf.Sign(transform.localScale.x))
            return false;

        return HasClearLineOfSight(player);
    }

    private void FaceRunNGunShotDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.5f)
            return;

        Vector3 scale = transform.localScale;
        float sign = direction.x > 0f ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * sign, scale.y, scale.z);
    }

    private void FireRunNGunShot()
    {
        int shots = Mathf.Max(1, burstCount);
        for (int i = 0; i < shots; i++)
        {
            Vector2 offset = GetBurstOffset(i, shots, runNGunLineOfShot);
            GameObject projectileObject = ObjectPool2D.Spawn(enemyProjectilePrefab, firePoint.position + (Vector3)offset, Quaternion.identity);
            projectileObject.transform.localScale = enemyProjectilePrefab.transform.localScale * projectileScaleMultiplier;

            EnemyProjectile2D projectile = projectileObject.GetComponent<EnemyProjectile2D>();
            if (projectile != null)
            {
                ApplyProjectileVariant(projectile);
                projectile.SetDirection(runNGunLineOfShot);
            }
        }

        shootingFeedbackTimer = 0.2f;
    }

    private Vector2 GetBurstOffset(int shotIndex, int totalShots, Vector2 direction)
    {
        if (totalShots <= 1)
            return Vector2.zero;

        float spacing = 0.12f;
        float centeredIndex = shotIndex - (totalShots - 1) * 0.5f;
        if (Mathf.Abs(direction.x) > 0.5f)
            return Vector2.up * centeredIndex * spacing;

        return Vector2.right * centeredIndex * spacing;
    }

    private void ResetRunNGunAnimationFlags()
    {
        IsShootingUp = false;
        IsWaiting = false;
        IsWaitingUp = false;
    }

    private void SetRunNGunShootingFlags(Vector2 direction)
    {
        bool shootingUp = direction == Vector2.up;
        IsShootingUp = shootingUp;
        shootingFeedbackTimer = shootingUp ? 0f : Mathf.Max(shootingFeedbackTimer, 0.05f);
    }

    private void SetRunNGunWaitingFlags(Vector2 direction)
    {
        bool waitingUp = direction == Vector2.up;
        IsWaitingUp = waitingUp;
        IsWaiting = !waitingUp;
    }

    private void FireBurstShot()
    {
        UpdateFirePoint(burstDirection);

        GameObject projectileObject = ObjectPool2D.Spawn(enemyProjectilePrefab, firePoint.position, Quaternion.identity);
        projectileObject.transform.localScale = enemyProjectilePrefab.transform.localScale * projectileScaleMultiplier;

        EnemyProjectile2D projectile = projectileObject.GetComponent<EnemyProjectile2D>();
        if (projectile != null)
        {
            ApplyProjectileVariant(projectile);
            projectile.SetDirection(burstDirection);
        }

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

    private void ApplyProjectileVariant(EnemyProjectile2D projectile)
    {
        if (projectile == null)
            return;

        Vector2 finalScale = projectileScaleOverride;
        if (finalScale.x > 0f && finalScale.y > 0f && projectileScaleMultiplier > 0f)
            finalScale *= projectileScaleMultiplier;

        projectile.ApplyVariant(projectileSpeedOverride, projectileDamageOverride, projectileAnimationController, finalScale, projectileTint);
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
