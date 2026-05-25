using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireCooldown = 0.18f;

    private PlayerController2D _controller;
    private float _cooldownTimer;

    private void Awake()
    {
        _controller = GetComponent<PlayerController2D>();
    }

    private void Update()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.F) && _cooldownTimer <= 0)
        {
            Shoot();
            _cooldownTimer = fireCooldown;
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetDirection(new Vector2(_controller.FacingDirection, 0));
        }
    }
}
