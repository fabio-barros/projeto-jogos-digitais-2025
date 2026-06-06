using UnityEngine;

public class EnemyWaveTrigger2D : MonoBehaviour
{
    public GameObject[] spawnObjects;
    public Transform[] spawnPoints;
    public float initialDelay = 0.15f;
    public float spawnInterval = 0.35f;
    public bool triggerOnce = true;
    public bool startActive;
    public bool triggerWhenCameraReachesX;
    public float cameraXTrigger;
    public bool despawnTriggerAfterUse = true;

    private bool triggered;
    private bool spawning;
    private int nextSpawnIndex;
    private float timer;

    private void Awake()
    {
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
            spawnObject.transform.position = spawnPoints[nextSpawnIndex].position;

        spawnObject.SetActive(true);
        StaggerAnimation(spawnObject, nextSpawnIndex);
        nextSpawnIndex++;
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

    private void OnDrawGizmosSelected()
    {
        if (!triggerWhenCameraReachesX)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(cameraXTrigger, -30f, 0f), new Vector3(cameraXTrigger, 12f, 0f));
    }
}
