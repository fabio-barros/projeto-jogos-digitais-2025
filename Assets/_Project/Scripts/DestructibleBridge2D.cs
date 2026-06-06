using UnityEngine;

public class DestructibleBridge2D : MonoBehaviour
{
    public int hitPoints = 3;
    public float blastReachPadding = 0.8f;
    public GameObject intactRoot;
    public GameObject destroyedRoot;

    private Collider2D[] bridgeColliders;
    private bool destroyed;

    private void Awake()
    {
        bridgeColliders = GetComponentsInChildren<Collider2D>();
        if (destroyedRoot != null)
            destroyedRoot.SetActive(false);
    }

    private void OnEnable()
    {
        BombProjectile2D.Exploded += OnBombExploded;
    }

    private void OnDisable()
    {
        BombProjectile2D.Exploded -= OnBombExploded;
    }

    private void OnBombExploded(Vector3 explosionPosition, float radius, int damage)
    {
        if (destroyed)
            return;

        float distance = Vector2.Distance(transform.position, explosionPosition);
        float bridgeHalfWidth = transform.localScale.x * 0.5f;
        if (distance > radius + bridgeHalfWidth + blastReachPadding)
            return;

        hitPoints -= Mathf.Max(1, damage);
        if (hitPoints <= 0)
            BreakBridge();
    }

    private void BreakBridge()
    {
        destroyed = true;

        for (int i = 0; i < bridgeColliders.Length; i++)
        {
            if (bridgeColliders[i] != null)
                bridgeColliders[i].enabled = false;
        }

        if (intactRoot != null)
            intactRoot.SetActive(false);

        if (destroyedRoot != null)
            destroyedRoot.SetActive(true);
    }
}
