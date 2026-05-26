#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RemnantSquadAlphaBuilder
{
    private const string BasePath = "Assets/_Project";
    private const string GeneratedPath = BasePath + "/Generated";
    private const string PrefabsPath = BasePath + "/Prefabs";
    private const string ScenesPath = BasePath + "/Scenes";

    [MenuItem("Remnant Squad/Generate Alpha Scene")]
    public static void GenerateAlphaScene()
    {
        GenerateAlphaScene(showDialog: true);
    }

    public static void GenerateAlphaSceneBatch()
    {
        GenerateAlphaScene(showDialog: false);
    }

    private static void GenerateAlphaScene(bool showDialog)
    {
        EnsureFolders();
        EnsureLayers();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_01_Alpha";

        Sprite playerSprite = CreateColorSprite("Sprite_Player_Volkov", new Color(0.1f, 0.7f, 1f, 1f));
        Sprite enemySprite = CreateColorSprite("Sprite_Keth_Grunt", new Color(0.9f, 0.2f, 0.25f, 1f));
        Sprite bruteSprite = CreateColorSprite("Sprite_Keth_Brute", new Color(0.6f, 0.1f, 0.8f, 1f));
        Sprite groundSprite = CreateColorSprite("Sprite_Ground", new Color(0.3f, 0.3f, 0.3f, 1f));
        Sprite projectileSprite = CreateColorSprite("Sprite_Projectile", new Color(1f, 0.95f, 0.2f, 1f));
        Sprite enemyProjectileSprite = CreateColorSprite("Sprite_Enemy_Projectile", new Color(1f, 0.2f, 0.1f, 1f));
        Sprite powSprite = CreateColorSprite("Sprite_POW", new Color(1f, 0.85f, 0.15f, 1f));
        Sprite endSprite = CreateColorSprite("Sprite_EndTrigger", new Color(0.1f, 1f, 0.3f, 0.55f));

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        CreateCamera();

        GameObject playerProjectilePrefab = CreatePlayerProjectilePrefab(projectileSprite);
        GameObject enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyProjectileSprite);

        CreateGround("Ground_Main", groundSprite, new Vector2(0f, -4f), new Vector2(24f, 1f));
        CreateGround("Platform_01", groundSprite, new Vector2(-2f, -2f), new Vector2(3f, 0.45f));
        CreateGround("Platform_02", groundSprite, new Vector2(3f, -0.8f), new Vector2(3.2f, 0.45f));
        CreateGround("Platform_03", groundSprite, new Vector2(7.2f, -2.1f), new Vector2(3.5f, 0.45f));
        CreateGround("Border_Left", groundSprite, new Vector2(-11.8f, -2.4f), new Vector2(0.4f, 3f));
        CreateGround("Border_Right", groundSprite, new Vector2(11.8f, -2.4f), new Vector2(0.4f, 3f));

        GameObject player = CreatePlayer(playerSprite, playerProjectilePrefab);

        CreateEnemy("Keth_Grunt_01", enemySprite, enemyProjectilePrefab, new Vector2(-0.5f, -3.35f), 2, 100, false);
        CreateEnemy("Keth_Grunt_02", enemySprite, enemyProjectilePrefab, new Vector2(3.2f, -3.35f), 2, 100, true);
        CreateEnemy("Keth_Brute_Alpha", bruteSprite, enemyProjectilePrefab, new Vector2(8.2f, -3.2f), 6, 300, true, new Vector2(1.2f, 1.35f));

        GameObject completePanel = CreateHUD(player.GetComponent<PlayerHealth>());
        CreatePOW(powSprite, new Vector2(5.5f, -3.3f));
        CreateEndTrigger(endSprite, new Vector2(10.6f, -3.25f), completePanel);

        Camera.main.GetComponent<CameraFollow2D>().target = player.transform;

        string scenePath = ScenesPath + "/Level_01_Alpha.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        const string successMessage = "Level_01_Alpha was generated successfully.\n\nOpen Assets/_Project/Scenes/Level_01_Alpha and press Play.\n\nControls:\nA/D = Move\nSpace/W = Jump\nF = Shoot\nLeft Shift = Dash\nE = Rescue POW";

        if (showDialog)
            EditorUtility.DisplayDialog("Remnant Squad Alpha Created", successMessage, "OK");
        else
            Debug.Log(successMessage);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "_Project");
        CreateFolderIfMissing(BasePath, "Generated");
        CreateFolderIfMissing(BasePath, "Prefabs");
        CreateFolderIfMissing(BasePath, "Scenes");
    }

    private static void CreateFolderIfMissing(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureLayers()
    {
        EnsureLayer("Ground");
        EnsureLayer("Player");
        EnsureLayer("Enemy");
        EnsureLayer("Projectile");
        EnsureLayer("POW");
    }

    private static void EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning("No empty Unity layer slot found for: " + layerName);
    }

    private static Sprite CreateColorSprite(string name, Color color)
    {
        string texturePath = GeneratedPath + "/" + name + ".png";

        if (!File.Exists(texturePath))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(texturePath);

        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static GameObject CreateBoxObject(string name, Sprite sprite, Vector2 position, Vector2 scale, string layerName)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) obj.layer = layer;

        return obj;
    }

    private static void CreateGround(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject ground = CreateBoxObject(name, sprite, position, scale, "Ground");
        ground.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    private static GameObject CreatePlayerProjectilePrefab(Sprite sprite)
    {
        GameObject projectile = CreateBoxObject("PlayerProjectile", sprite, Vector2.zero, new Vector2(0.35f, 0.18f), "Projectile");
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;

        Projectile2D script = projectile.AddComponent<Projectile2D>();
        script.speed = 15f;
        script.damage = 1;
        script.lifetime = 2.5f;
        script.hitLayers = LayerMask.GetMask("Ground");

        string path = PrefabsPath + "/PlayerProjectile.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);
        return prefab;
    }

    private static GameObject CreateEnemyProjectilePrefab(Sprite sprite)
    {
        GameObject projectile = CreateBoxObject("EnemyProjectile", sprite, Vector2.zero, new Vector2(0.28f, 0.28f), "Projectile");
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;

        EnemyProjectile2D script = projectile.AddComponent<EnemyProjectile2D>();
        script.speed = 7f;
        script.damage = 1;
        script.lifetime = 3f;
        script.hitLayers = LayerMask.GetMask("Ground");

        string path = PrefabsPath + "/EnemyProjectile.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);
        return prefab;
    }

    private static GameObject CreatePlayer(Sprite sprite, GameObject projectilePrefab)
    {
        GameObject player = CreateBoxObject("Player_Volkov", sprite, new Vector2(-8.5f, -3.25f), new Vector2(0.85f, 1.25f), "Player");

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        player.AddComponent<BoxCollider2D>().size = Vector2.one;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.58f, 0f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0.65f, 0.1f, 0f);

        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.groundCheck = groundCheck.transform;
        controller.groundLayer = LayerMask.GetMask("Ground");

        PlayerShooter2D shooter = player.AddComponent<PlayerShooter2D>();
        shooter.projectilePrefab = projectilePrefab;
        shooter.firePoint = firePoint.transform;

        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.maxHealth = 3;

        return player;
    }

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, Vector2 position, int health, int score, bool canShoot)
    {
        return CreateEnemy(name, sprite, enemyProjectilePrefab, position, health, score, canShoot, new Vector2(0.85f, 1.1f));
    }

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, Vector2 position, int health, int score, bool canShoot, Vector2 scale)
    {
        GameObject enemy = CreateBoxObject(name, sprite, position, scale, "Enemy");

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        enemy.AddComponent<BoxCollider2D>().size = Vector2.one;

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = health;
        damageable.scoreValue = score;

        enemy.AddComponent<DamageOnContact>().damage = 1;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(enemy.transform);
        groundCheck.transform.localPosition = new Vector3(-0.45f, -0.58f, 0f);

        GameObject wallCheck = new GameObject("WallCheck");
        wallCheck.transform.SetParent(enemy.transform);
        wallCheck.transform.localPosition = new Vector3(-0.55f, 0f, 0f);

        EnemyPatrol2D patrol = enemy.AddComponent<EnemyPatrol2D>();
        patrol.moveSpeed = scale.x > 1f ? 1.1f : 1.8f;
        patrol.groundCheck = groundCheck.transform;
        patrol.wallCheck = wallCheck.transform;
        patrol.groundLayer = LayerMask.GetMask("Ground");

        if (canShoot)
        {
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(enemy.transform);
            firePoint.transform.localPosition = new Vector3(-0.6f, 0.15f, 0f);

            EnemyShooter2D shooter = enemy.AddComponent<EnemyShooter2D>();
            shooter.enemyProjectilePrefab = enemyProjectilePrefab;
            shooter.firePoint = firePoint.transform;
            shooter.range = 8f;
            shooter.fireCooldown = scale.x > 1f ? 2.2f : 1.7f;
        }

        return enemy;
    }

    private static void CreatePOW(Sprite sprite, Vector2 position)
    {
        GameObject pow = CreateBoxObject("POW_Rescue_Objective", sprite, position, new Vector2(0.75f, 1f), "POW");
        pow.AddComponent<BoxCollider2D>().isTrigger = true;

        GameObject prompt = CreateWorldText("POW_Prompt_Text", "Press E to rescue", new Vector3(0f, 1f, 0f), 0.25f);
        prompt.transform.SetParent(pow.transform);
        prompt.transform.localPosition = new Vector3(0f, 1f, 0f);

        GameObject rescued = CreateWorldText("POW_Rescued_Text", "+500 POW rescued!", new Vector3(0f, 1.25f, 0f), 0.25f);
        rescued.transform.SetParent(pow.transform);
        rescued.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        POWRescue rescue = pow.AddComponent<POWRescue>();
        rescue.scoreReward = 500;
        rescue.healReward = 1;
        rescue.rescuePrompt = prompt;
        rescue.rescuedVisual = rescued;

        prompt.SetActive(false);
        rescued.SetActive(false);
    }

    private static GameObject CreateWorldText(string name, string text, Vector3 position, float size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.position = position;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = size;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 10;

        return textObject;
    }

    private static GameObject CreateHUD(PlayerHealth playerHealth)
    {
        GameObject canvasObject = new GameObject("Canvas_HUD");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text healthText = CreateUIText("HealthText", canvasObject.transform, "HP: 3/3", new Vector2(20f, -20f), TextAnchor.UpperLeft, 28, font);
        Text scoreText = CreateUIText("ScoreText", canvasObject.transform, "Score: 0", new Vector2(-20f, -20f), TextAnchor.UpperRight, 28, font);
        CreateUIText("ControlsText", canvasObject.transform, "A/D Move  |  Space/W Jump  |  F Shoot  |  Shift Dash  |  E Rescue POW", new Vector2(0f, 20f), TextAnchor.LowerCenter, 22, font);

        GameObject completePanel = new GameObject("AlphaCompletePanel");
        completePanel.transform.SetParent(canvasObject.transform);

        RectTransform panelRect = completePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image image = completePanel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        Text completeText = CreateUIText("AlphaCompleteText", completePanel.transform, "ALPHA COMPLETE\nMechanics Validated", Vector2.zero, TextAnchor.MiddleCenter, 44, font);
        RectTransform completeRect = completeText.GetComponent<RectTransform>();
        completeRect.anchorMin = Vector2.zero;
        completeRect.anchorMax = Vector2.one;
        completeRect.offsetMin = Vector2.zero;
        completeRect.offsetMax = Vector2.zero;

        completePanel.SetActive(false);

        SimpleHUD hud = canvasObject.AddComponent<SimpleHUD>();
        hud.playerHealth = playerHealth;
        hud.healthText = healthText;
        hud.scoreText = scoreText;

        return completePanel;
    }

    private static Text CreateUIText(string name, Transform parent, string text, Vector2 anchoredPosition, TextAnchor alignment, int fontSize, Font font)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);

        RectTransform rect = textObject.AddComponent<RectTransform>();

        if (alignment == TextAnchor.UpperLeft)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(500f, 80f);
        }
        else if (alignment == TextAnchor.UpperRight)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(500f, 80f);
        }
        else if (alignment == TextAnchor.LowerCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(1000f, 60f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 250f);
        }

        rect.anchoredPosition = anchoredPosition;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = font;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;

        return uiText;
    }

    private static void CreateEndTrigger(Sprite sprite, Vector2 position, GameObject completePanel)
    {
        GameObject endTrigger = CreateBoxObject("End_Level_Trigger", sprite, position, new Vector2(0.8f, 1.6f), "POW");
        endTrigger.AddComponent<BoxCollider2D>().isTrigger = true;

        EndLevelTrigger trigger = endTrigger.AddComponent<EndLevelTrigger>();
        trigger.alphaCompletePanel = completePanel;

        CreateWorldText("End_Label", "END", new Vector3(position.x, position.y + 1.2f, 0f), 0.3f);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.13f, 1f);

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-6f, -1.1f, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.offset = new Vector3(2f, 1.2f, -10f);
        follow.smoothSpeed = 8f;
    }
}
#endif
