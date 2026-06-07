using UnityEngine;

public class SpriteAnimationOnce : MonoBehaviour
{
    public SpriteRenderer targetRenderer;
    public Sprite[] frames;
    public float frameRate = 18f;

    private float timer;
    private int frameIndex;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        timer = 0f;
        frameIndex = 0;

        if (targetRenderer != null && frames != null && frames.Length > 0)
            targetRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (targetRenderer == null || frames == null || frames.Length == 0)
        {
            ObjectPool2D.Despawn(gameObject);
            return;
        }

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            frameIndex++;
        }

        if (frameIndex >= frames.Length)
        {
            ObjectPool2D.Despawn(gameObject);
            return;
        }

        targetRenderer.sprite = frames[frameIndex];
    }
}
