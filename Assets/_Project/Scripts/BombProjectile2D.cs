using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BombProjectile2D : MonoBehaviour
{
    public float fuseTime = 1.1f;
    public float explosionRadius = 1.8f;
    public int damage = 3;
    public LayerMask damageLayers;
    public GameObject explosionVisual;

    private bool exploded;

    private void Start()
    {
        Destroy(gameObject, fuseTime);
    }

    private void OnDestroy()
    {
        Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.sqrMagnitude > 8f)
            Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Damageable damageable = hits[i].GetComponent<Damageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
        }

        if (explosionVisual != null)
        {
            GameObject visual = Instantiate(explosionVisual, transform.position, Quaternion.identity);
            Destroy(visual, 0.35f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
