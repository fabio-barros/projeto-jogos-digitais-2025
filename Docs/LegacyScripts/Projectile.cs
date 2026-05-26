using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 14f;
    public int damage = 1;
    public float lifetime = 2.5f;
    public LayerMask hitLayers;

    private Vector2 _direction = Vector2.right;

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;

        if (_direction.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Destroy(gameObject);
        }
    }
}
