using UnityEngine;

[CreateAssetMenu(menuName = "Remnant Squad/Wave Definition", fileName = "WaveDefinition")]
public class WaveDefinition2D : ScriptableObject
{
    public string encounterLabel = "Camera Wave";
    public float initialDelay = 0.12f;
    public float spawnInterval = 0.3f;
    public bool triggerOnce = true;
    public WaveSpawnEntry2D[] spawns;
}

[System.Serializable]
public class WaveSpawnEntry2D
{
    public GameObject enemyPrefab;
    public Vector2 spawnPosition;
    public int startingDirection = -1;
}
