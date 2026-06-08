using UnityEngine;

public class PlayerShooter2D : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireCooldown = 0.18f;
    public float firePointDistance = 0.7f;
    public int maxAmmo = 30;
    public float reloadTime = 1.1f;

    private PlayerController2D controller;
    private PlayerHealth playerHealth;
    private int currentAmmo;
    private float cooldownTimer;
    private float shootFeedbackTimer;
    private float reloadTimer;
    private Vector2 currentShootDirection = Vector2.right;

    public bool IsShooting { get { return shootFeedbackTimer > 0f; } }
    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }
    public bool IsReloading { get { return reloadTimer > 0f; } }
    public Vector2 CurrentShootDirection { get { return currentShootDirection; } }

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        playerHealth = GetComponent<PlayerHealth>();
        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (shootFeedbackTimer > 0) shootFeedbackTimer -= Time.deltaTime;

        if (playerHealth != null && !playerHealth.CanAct) return;

        bool shootHeld = RemnantInput.ShootHeld();
        if (shootHeld && controller != null)
        {
            currentShootDirection = GetShootDirection();

            if (Mathf.Abs(currentShootDirection.x) > 0.15f)
                controller.FaceDirection(currentShootDirection.x >= 0f ? 1 : -1);
        }
        else if (controller != null)
        {
            currentShootDirection = controller.AimDirection;
        }

        UpdateFirePointPosition();

        if (reloadTimer > 0f)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
                currentAmmo = maxAmmo;
        }

        if (RemnantInput.ReloadDown() && currentAmmo < maxAmmo && !IsReloading)
            StartReload();

        if (currentAmmo <= 0 && !IsReloading)
            StartReload();

        if (shootHeld && cooldownTimer <= 0 && currentAmmo > 0 && !IsReloading)
        {
            Shoot();
            cooldownTimer = fireCooldown;
        }
    }

    public void RefillAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null || controller == null) return;

        GameObject projectileObject = ObjectPool2D.Spawn(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile != null) projectile.SetDirection(currentShootDirection);

        currentAmmo--;
        shootFeedbackTimer = 0.12f;
    }

    private void StartReload()
    {
        reloadTimer = reloadTime;
    }

    private void UpdateFirePointPosition()
    {
        if (firePoint == null || controller == null) return;

        Vector2 aim = controller.AimDirection;
        if (RemnantInput.ShootHeld())
            aim = currentShootDirection;

        float baseHeight = RemnantInput.CrouchHeld() && controller.IsGrounded ? -0.15f : 0.1f;
        float horizontalOffset = Mathf.Abs(aim.x) > 0.15f ? Mathf.Sign(aim.x) * controller.FacingDirection * firePointDistance : 0f;
        firePoint.localPosition = new Vector3(horizontalOffset, baseHeight + aim.y * firePointDistance, 0f);
    }

    private Vector2 GetShootDirection()
    {
        if (controller == null)
            return Vector2.right;

        if (RemnantInput.CrouchHeld() && controller.IsGrounded)
        {
            if (Mathf.Abs(controller.AimDirection.x) > 0.2f)
                return new Vector2(controller.AimDirection.x > 0f ? 1f : -1f, 0f);

            return new Vector2(controller.FacingDirection, 0f);
        }

        if (controller.AimDirection.sqrMagnitude > 0.01f)
            return controller.AimDirection.normalized;

        return new Vector2(controller.FacingDirection, 0f);
    }
}
