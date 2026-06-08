using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyAmbushTrigger2D : MonoBehaviour
{
    public EnemyAmbushReveal2D[] ambushEnemies;
    public bool triggerOnce = true;
    public bool destroyAfterTrigger;

    private bool triggered;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.5f, 3f);
        box.offset = new Vector2(0f, 0.8f);
    }

    private void Awake()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && triggered)
            return;

        if (other.GetComponentInParent<PlayerController2D>() == null)
            return;

        Trigger();
    }

    public void Trigger()
    {
        triggered = true;

        if (ambushEnemies == null || ambushEnemies.Length == 0)
            ambushEnemies = GetComponentsInParent<EnemyAmbushReveal2D>();

        for (int i = 0; i < ambushEnemies.Length; i++)
        {
            if (ambushEnemies[i] != null)
                ambushEnemies[i].Reveal();
        }

        if (destroyAfterTrigger)
            Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Gizmos.color = triggered ? Color.gray : new Color(1f, 0.65f, 0.1f, 0.8f);
        Vector3 center = transform.TransformPoint(box.offset);
        Vector3 size = new Vector3(box.size.x * transform.lossyScale.x, box.size.y * transform.lossyScale.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
