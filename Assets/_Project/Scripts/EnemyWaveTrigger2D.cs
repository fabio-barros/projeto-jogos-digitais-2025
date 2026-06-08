using UnityEngine;

public class EnemyWaveTrigger2D : MonoBehaviour
{
    public WaveDefinition2D waveDefinition;
    public bool applyWaveDefinitionOnAwake;
    public GameObject[] spawnObjects;
    public Transform[] spawnPoints;
    public float initialDelay = 0.15f;
    public float spawnInterval = 0.35f;
    public bool triggerOnce = true;
    public bool startActive;
    public bool triggerWhenCameraReachesX;
    public float cameraXTrigger;
    public bool despawnTriggerAfterUse = true;
    public bool resolveBlockedSpawns = true;
    public LayerMask spawnBlockLayers;
    public Vector2 spawnClearance = new Vector2(0.9f, 1.2f);
    public float groundSnapDistance = 4f;

    private bool triggered;
    private bool spawning;
    private int nextSpawnIndex;
    private float timer;

    public bool HasTriggered { get { return triggered; } }
    public bool IsSpawning { get { return spawning; } }
    public int NextSpawnIndex { get { return nextSpawnIndex; } }

    private void Awake()
    {
        if (applyWaveDefinitionOnAwake && waveDefinition != null)
            ApplyWaveDefinition();

        if (spawnObjects != null)
        {
            for (int i = 0; i < spawnObjects.Length; i++)
            {
                if (spawnObjects[i] != null)
                    spawnObjects[i].SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (startActive)
            TriggerWave();
    }

    private void Update()
    {
        if (!triggered && triggerWhenCameraReachesX && Camera.main != null)
        {
            float cameraRightEdge = Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect;
            if (cameraRightEdge >= cameraXTrigger)
                TriggerWave();
        }

        if (!spawning || spawnObjects == null || spawnObjects.Length == 0)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        SpawnNext();
        timer = spawnInterval;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController2D>() == null)
            return;

        TriggerWave();
    }

    private void TriggerWave()
    {
        if (triggerOnce && triggered)
            return;

        triggered = true;
        spawning = true;
        nextSpawnIndex = 0;
        timer = initialDelay;
    }

    private void ApplyWaveDefinition()
    {
        initialDelay = waveDefinition.initialDelay;
        spawnInterval = waveDefinition.spawnInterval;
        triggerOnce = waveDefinition.triggerOnce;

        if (waveDefinition.spawns == null)
            return;

        spawnObjects = new GameObject[waveDefinition.spawns.Length];
        spawnPoints = new Transform[waveDefinition.spawns.Length];

        for (int i = 0; i < waveDefinition.spawns.Length; i++)
        {
            WaveSpawnEntry2D entry = waveDefinition.spawns[i];
            if (entry == null || entry.enemyPrefab == null)
                continue;

            GameObject spawnedEnemy = Instantiate(entry.enemyPrefab, entry.spawnPosition, Quaternion.identity);
            spawnedEnemy.name = waveDefinition.encounterLabel + "_Spawn_" + i.ToString("00");
            SetPatrolDirection(spawnedEnemy, entry.startingDirection);
            spawnObjects[i] = spawnedEnemy;

            GameObject point = new GameObject("SpawnPoint_" + i.ToString("00"));
            point.transform.SetParent(transform);
            point.transform.position = entry.spawnPosition;
            spawnPoints[i] = point.transform;
        }
    }

    private void SpawnNext()
    {
        while (nextSpawnIndex < spawnObjects.Length && spawnObjects[nextSpawnIndex] == null)
            nextSpawnIndex++;

        if (nextSpawnIndex >= spawnObjects.Length)
        {
            spawning = false;
            return;
        }

        GameObject spawnObject = spawnObjects[nextSpawnIndex];
        if (spawnPoints != null && nextSpawnIndex < spawnPoints.Length && spawnPoints[nextSpawnIndex] != null)
            spawnObject.transform.position = ResolveSpawnPosition(ResolveOffCameraSpawnPosition(spawnPoints[nextSpawnIndex].position));

        spawnObject.SetActive(true);
        StaggerAnimation(spawnObject, nextSpawnIndex);
        nextSpawnIndex++;
    }

    private Vector3 ResolveOffCameraSpawnPosition(Vector3 requestedPosition)
    {
        if (!triggerWhenCameraReachesX || Camera.main == null)
            return requestedPosition;

        Camera camera = Camera.main;
        float halfWidth = camera.orthographicSize * camera.aspect;
        float leftEdge = camera.transform.position.x - halfWidth;
        float rightEdge = camera.transform.position.x + halfWidth;
        float margin = Mathf.Max(1.4f, spawnClearance.x * 1.5f);

        if (requestedPosition.x <= leftEdge || requestedPosition.x >= rightEdge)
            return requestedPosition;

        float cameraCenter = camera.transform.position.x;
        float offscreenX = requestedPosition.x < cameraCenter ? leftEdge - margin : rightEdge + margin;
        return new Vector3(offscreenX, requestedPosition.y, requestedPosition.z);
    }

    private Vector3 ResolveSpawnPosition(Vector3 requestedPosition)
    {
        if (!resolveBlockedSpawns || spawnBlockLayers.value == 0)
            return requestedPosition;

        Vector2[] offsets = new Vector2[]
        {
            Vector2.zero,
            new Vector2(-0.9f, 0f),
            new Vector2(0.9f, 0f),
            new Vector2(-1.8f, 0f),
            new Vector2(1.8f, 0f),
            new Vector2(0f, 0.9f),
            new Vector2(-0.9f, 0.9f),
            new Vector2(0.9f, 0.9f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 candidate = (Vector2)requestedPosition + offsets[i];
            candidate = SnapToGround(candidate);

            if (!Physics2D.OverlapBox(candidate, spawnClearance, 0f, spawnBlockLayers))
                return new Vector3(candidate.x, candidate.y, requestedPosition.z);
        }

        return requestedPosition;
    }

    private Vector2 SnapToGround(Vector2 position)
    {
        RaycastHit2D hit = Physics2D.Raycast(position + Vector2.up * 0.6f, Vector2.down, groundSnapDistance, spawnBlockLayers);
        if (hit.collider == null || hit.collider.isTrigger)
            return position;

        return new Vector2(position.x, hit.point.y + spawnClearance.y * 0.5f + 0.08f);
    }

    private void StaggerAnimation(GameObject spawnObject, int spawnIndex)
    {
        Animator animator = spawnObject.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float offset = (spawnIndex % 6) * 0.13f;
        animator.Play(state.shortNameHash, 0, offset);
        animator.Update(0f);
    }

    private void SetPatrolDirection(GameObject enemy, int startDirection)
    {
        int direction = startDirection >= 0 ? 1 : -1;
        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol != null)
            patrol.startingDirection = direction;

        Vector3 scale = enemy.transform.localScale;
        enemy.transform.localScale = new Vector3(Mathf.Abs(scale.x) * direction, scale.y, scale.z);
    }

    private void OnDrawGizmos()
    {
        if (!GameplayDebugOverlay2D.DrawSpawnTriggers)
            return;

        DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        Color triggerColor = triggered ? Color.gray : Color.yellow;
        Gizmos.color = triggerColor;
        Gizmos.DrawWireCube(transform.position, GetTriggerSize());

        if (triggerWhenCameraReachesX)
            Gizmos.DrawLine(new Vector3(cameraXTrigger, -30f, 0f), new Vector3(cameraXTrigger, 12f, 0f));

        if (spawnPoints == null)
            return;

        Gizmos.color = triggered ? Color.cyan : Color.magenta;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                continue;

            Gizmos.DrawWireSphere(spawnPoints[i].position, 0.35f);
            Gizmos.DrawLine(transform.position, spawnPoints[i].position);
        }
    }

    private Vector3 GetTriggerSize()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        return box != null ? box.size : Vector3.one;
    }
}
