using UnityEngine;

public class PlayerShooter2D : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireCooldown = 0.18f;
    public float firePointDistance = 0.7f;

    private PlayerController2D controller;
    private float cooldownTimer;
    private float shootFeedbackTimer;

    public bool IsShooting { get { return shootFeedbackTimer > 0f; } }

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
    }

    private void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (shootFeedbackTimer > 0) shootFeedbackTimer -= Time.deltaTime;

        UpdateFirePointPosition();

        if (Input.GetKey(KeyCode.F) && cooldownTimer <= 0)
        {
            Shoot();
            cooldownTimer = fireCooldown;
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null || controller == null) return;

        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile != null) projectile.SetDirection(controller.AimDirection);

        shootFeedbackTimer = 0.12f;
    }

    private void UpdateFirePointPosition()
    {
        if (firePoint == null || controller == null) return;

        Vector2 aim = controller.AimDirection;
        firePoint.localPosition = new Vector3(aim.x * firePointDistance, 0.1f + aim.y * firePointDistance, 0f);
    }
}
