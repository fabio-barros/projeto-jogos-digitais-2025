using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BombProjectile2D : MonoBehaviour
{
    public float fuseTime = 1.1f;
    public float explosionRadius = 1.8f;
    public int damage = 3;
    public LayerMask damageLayers;
    public GameObject explosionVisual;
    public Color visibleColor = new Color(0.05f, 1f, 0.95f, 1f);
    public Color trailColor = new Color(1f, 0.95f, 0.1f, 1f);
    public float trailTime = 0.45f;

    private static Sprite visibleBombSprite;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D bombCollider;
    private TrailRenderer trail;
    private bool exploded;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bombCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetVisibleBombSprite();
            spriteRenderer.color = visibleColor;
            spriteRenderer.sortingOrder = 7;
        }

        trail = GetComponent<TrailRenderer>();
        if (trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = 0.12f;
        trail.endWidth = 0.02f;
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trail.sortingOrder = 6;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            trail.material = new Material(shader);
    }

    private void Start()
    {
        Invoke(nameof(Explode), fuseTime);
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
        CancelInvoke();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Damageable damageable = hits[i].GetComponent<Damageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, hits[i].transform.position - transform.position);
        }

        if (explosionVisual != null)
        {
            GameObject visual = Instantiate(explosionVisual, transform.position, Quaternion.identity);
            Destroy(visual, 0.35f);
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (trail != null)
        {
            trail.Clear();
            trail.enabled = false;
        }

        if (bombCollider != null)
            bombCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    private static Sprite GetVisibleBombSprite()
    {
        if (visibleBombSprite != null)
            return visibleBombSprite;

        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        visibleBombSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 16f);
        return visibleBombSprite;
    }
}
