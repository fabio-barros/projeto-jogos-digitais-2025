using UnityEngine;

public class CompanionShooter2D : MonoBehaviour
{
    public Transform target;
    public GameObject projectilePrefab;
    public LayerMask enemyLayers;
    public LayerMask obstacleLayers;
    public Vector3 followOffset = new Vector3(-1.2f, 1f, 0f);
    public float followSpeed = 7f;
    public float shootRange = 7f;
    public float fireCooldown = 0.75f;
    public float damageMultiplier = 0.1f;

    private float fireTimer;

    private void Start()
    {
        if (target == null)
        {
            PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
            if (player != null)
                target = player.transform;
        }
    }

    private void Update()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
            TryShoot();
    }

    private void TryShoot()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shootRange, enemyLayers);
        Damageable nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Damageable damageable = hits[i].GetComponent<Damageable>();
            if (damageable == null || damageable.IsDead)
                continue;

            if (!HasClearLineOfSight(hits[i].transform))
                continue;

            float distance = Vector2.SqrMagnitude(hits[i].transform.position - transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = damageable;
            }
        }

        if (nearest == null || projectilePrefab == null)
            return;

        Vector2 direction = nearest.transform.position - transform.position;
        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile != null)
        {
            projectile.damage *= damageMultiplier;
            projectile.SetDirection(direction);
        }

        fireTimer = fireCooldown;
    }

    private bool HasClearLineOfSight(Transform possibleTarget)
    {
        if (possibleTarget == null)
            return false;

        if (obstacleLayers.value == 0)
            return true;

        Vector2 origin = transform.position;
        Vector2 targetPoint = possibleTarget.position;
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleLayers);
        return hit.collider == null;
    }
}
