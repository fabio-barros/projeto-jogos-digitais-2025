using UnityEngine;

public class GameplayDebugOverlay2D : MonoBehaviour
{
    public static bool DrawEnemyAI { get; private set; }
    public static bool DrawLineOfSight { get; private set; }
    public static bool DrawSpawnTriggers { get; private set; }

    public bool showOverlay;
    public KeyCode toggleOverlayKey = KeyCode.F1;
    public KeyCode toggleEnemyAIKey = KeyCode.F2;
    public KeyCode toggleLineOfSightKey = KeyCode.F3;
    public KeyCode toggleSpawnsKey = KeyCode.F4;

    private GUIStyle panelStyle;
    private GUIStyle textStyle;

    private void Update()
    {
        if (Input.GetKeyDown(toggleOverlayKey))
            showOverlay = !showOverlay;

        if (Input.GetKeyDown(toggleEnemyAIKey))
            DrawEnemyAI = !DrawEnemyAI;

        if (Input.GetKeyDown(toggleLineOfSightKey))
            DrawLineOfSight = !DrawLineOfSight;

        if (Input.GetKeyDown(toggleSpawnsKey))
            DrawSpawnTriggers = !DrawSpawnTriggers;
    }

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        EnsureStyles();

        GUILayout.BeginArea(new Rect(14f, 130f, 380f, 260f), panelStyle);
        GUILayout.Label("REMNANT DEBUG", textStyle);
        GUILayout.Label("F1 Overlay  F2 AI  F3 LOS  F4 Spawns", textStyle);
        GUILayout.Space(6f);

        EnemyPatrol2D[] enemies = FindObjectsByType<EnemyPatrol2D>(FindObjectsSortMode.None);
        EnemyWaveTrigger2D[] waves = FindObjectsByType<EnemyWaveTrigger2D>(FindObjectsSortMode.None);
        Projectile2D[] playerProjectiles = FindObjectsByType<Projectile2D>(FindObjectsSortMode.None);
        EnemyProjectile2D[] enemyProjectiles = FindObjectsByType<EnemyProjectile2D>(FindObjectsSortMode.None);

        GUILayout.Label("Enemies active: " + enemies.Length, textStyle);
        GUILayout.Label("Wave triggers: " + waves.Length, textStyle);
        GUILayout.Label("Player bullets: " + playerProjectiles.Length + " | Enemy bullets: " + enemyProjectiles.Length, textStyle);

        ObjectPool2D pool = ObjectPool2D.Instance;
        GUILayout.Label("Pool active: " + pool.TotalActiveCount + " | inactive: " + pool.TotalInactiveCount, textStyle);

        if (Camera.main != null)
        {
            float cameraRightEdge = Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect;
            GUILayout.Label("Camera X: " + Camera.main.transform.position.x.ToString("0.0") + " | right edge: " + cameraRightEdge.ToString("0.0"), textStyle);
        }

        GUILayout.Space(6f);
        GUILayout.Label("AI: " + OnOff(DrawEnemyAI) + " | LOS: " + OnOff(DrawLineOfSight) + " | Spawns: " + OnOff(DrawSpawnTriggers), textStyle);
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = Texture2D.grayTexture;
        panelStyle.padding = new RectOffset(10, 10, 10, 10);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.white;
        textStyle.fontSize = 14;
    }

    private string OnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
