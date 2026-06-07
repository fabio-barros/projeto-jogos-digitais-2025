using UnityEngine;

public class EnemyWaypointNode2D : MonoBehaviour
{
    public EnemyWaypointNode2D[] neighbors;
    public bool jumpHint;
    public bool dropHint;

    public Vector2 Position { get { return transform.position; } }

    private void OnDrawGizmos()
    {
        Gizmos.color = jumpHint ? Color.cyan : dropHint ? Color.magenta : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        if (neighbors == null)
            return;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.55f);
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null)
                Gizmos.DrawLine(transform.position, neighbors[i].transform.position);
        }
    }
}
