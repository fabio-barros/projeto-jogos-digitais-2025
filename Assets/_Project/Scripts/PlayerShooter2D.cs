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

    public bool IsShooting { get { return shootFeedbackTimer > 0f; } }
    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }
    public bool IsReloading { get { return reloadTimer > 0f; } }

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

        if (RemnantInput.ShootHeld() && cooldownTimer <= 0 && currentAmmo > 0 && !IsReloading)
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

        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile != null) projectile.SetDirection(controller.AimDirection);

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
        firePoint.localPosition = new Vector3(aim.x * firePointDistance, 0.1f + aim.y * firePointDistance, 0f);
    }
}
