using System.Collections.Generic;
using UnityEngine;

public class EnemyWaypointGraph2D : MonoBehaviour
{
    private static EnemyWaypointGraph2D activeGraph;

    private readonly List<EnemyWaypointNode2D> nodes = new List<EnemyWaypointNode2D>();
    private readonly List<EnemyWaypointNode2D> open = new List<EnemyWaypointNode2D>();
    private readonly HashSet<EnemyWaypointNode2D> closed = new HashSet<EnemyWaypointNode2D>();
    private readonly Dictionary<EnemyWaypointNode2D, EnemyWaypointNode2D> cameFrom = new Dictionary<EnemyWaypointNode2D, EnemyWaypointNode2D>();
    private readonly Dictionary<EnemyWaypointNode2D, float> gScore = new Dictionary<EnemyWaypointNode2D, float>();
    private readonly Dictionary<EnemyWaypointNode2D, float> fScore = new Dictionary<EnemyWaypointNode2D, float>();

    public static EnemyWaypointGraph2D Active
    {
        get
        {
            if (activeGraph == null)
                activeGraph = FindAnyObjectByType<EnemyWaypointGraph2D>();

            return activeGraph;
        }
    }

    private void Awake()
    {
        activeGraph = this;
        RefreshNodes();
    }

    public void RefreshNodes()
    {
        nodes.Clear();
        EnemyWaypointNode2D[] foundNodes = GetComponentsInChildren<EnemyWaypointNode2D>();
        for (int i = 0; i < foundNodes.Length; i++)
        {
            if (foundNodes[i] != null)
                nodes.Add(foundNodes[i]);
        }
    }

    public List<EnemyWaypointNode2D> FindPath(Vector2 startPosition, Vector2 targetPosition)
    {
        EnemyWaypointNode2D start = FindClosestNode(startPosition);
        EnemyWaypointNode2D goal = FindClosestNode(targetPosition);
        if (start == null || goal == null)
            return null;

        if (start == goal)
            return new List<EnemyWaypointNode2D> { goal };

        open.Clear();
        closed.Clear();
        cameFrom.Clear();
        gScore.Clear();
        fScore.Clear();

        open.Add(start);
        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        while (open.Count > 0)
        {
            EnemyWaypointNode2D current = GetLowestFScoreNode();
            if (current == goal)
                return ReconstructPath(current);

            open.Remove(current);
            closed.Add(current);

            EnemyWaypointNode2D[] neighbors = current.neighbors;
            if (neighbors == null)
                continue;

            for (int i = 0; i < neighbors.Length; i++)
            {
                EnemyWaypointNode2D neighbor = neighbors[i];
                if (neighbor == null || closed.Contains(neighbor))
                    continue;

                float tentativeScore = GetScore(gScore, current, float.MaxValue) + Vector2.Distance(current.Position, neighbor.Position);
                if (!open.Contains(neighbor))
                    open.Add(neighbor);
                else if (tentativeScore >= GetScore(gScore, neighbor, float.MaxValue))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeScore;
                fScore[neighbor] = tentativeScore + Heuristic(neighbor, goal);
            }
        }

        return null;
    }

    private EnemyWaypointNode2D FindClosestNode(Vector2 position)
    {
        EnemyWaypointNode2D closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            EnemyWaypointNode2D node = nodes[i];
            if (node == null)
                continue;

            float distance = Vector2.SqrMagnitude(node.Position - position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    private EnemyWaypointNode2D GetLowestFScoreNode()
    {
        EnemyWaypointNode2D best = open[0];
        float bestScore = GetScore(fScore, best, float.MaxValue);

        for (int i = 1; i < open.Count; i++)
        {
            float score = GetScore(fScore, open[i], float.MaxValue);
            if (score < bestScore)
            {
                best = open[i];
                bestScore = score;
            }
        }

        return best;
    }

    private List<EnemyWaypointNode2D> ReconstructPath(EnemyWaypointNode2D current)
    {
        List<EnemyWaypointNode2D> path = new List<EnemyWaypointNode2D> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    private float Heuristic(EnemyWaypointNode2D a, EnemyWaypointNode2D b)
    {
        return Mathf.Abs(a.Position.x - b.Position.x) + Mathf.Abs(a.Position.y - b.Position.y) * 1.5f;
    }

    private float GetScore(Dictionary<EnemyWaypointNode2D, float> scores, EnemyWaypointNode2D node, float fallback)
    {
        return scores.TryGetValue(node, out float score) ? score : fallback;
    }
}
