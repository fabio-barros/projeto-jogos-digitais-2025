#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class RunNGunLevel1RemnantConverter
{
    private const string SourceScenePath = "Assets/Scenes/RunNGun_Level1_Original.unity";
    private const string ConvertedScenePath = "Assets/Scenes/RunNGun_Level1_NeraGameplay.unity";
    private const string MirrorScenePath = "Assets/_Project/Scenes/RunNGun_Level1_NeraGameplay.unity";
    private const string AutoConvertFlagPath = "Assets/_Project/Generated/RunNGunRemnant/AutoConvert.flag";
    private const string NeraPath = "Assets/_Project/Art/Nera/sprites";
    private const string GeneratedPath = "Assets/_Project/Generated/RunNGunRemnant";
    private const string PrefabsPath = "Assets/_Project/Prefabs";
    private const string AnimationsPath = "Assets/_Project/Animations";

    [InitializeOnLoadMethod]
    private static void AutoConvertIfRequested()
    {
        if (!File.Exists(AutoConvertFlagPath))
            return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoConvertFlagPath))
                return;

            File.Delete(AutoConvertFlagPath);
            Convert(false);
        };
    }

    [MenuItem("Remnant Squad/Convert RunNGun Level 1 To Nera Gameplay")]
    public static void ConvertMenu()
    {
        Convert(false);
    }

    public static void ConvertBatch()
    {
        Convert(true);
    }

    private static void Convert(bool batch)
    {
        EnsureFolders();
        EnsureLayers();

        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        Vector3 playerStart = FindOriginalPlayerPosition(new Vector3(-9f, -2.1f, 0f));
        RemoveOriginalRuntimeObjects();
        ConfigureTilemaps();

        GameObject playerProjectile = LoadPrefab("PlayerProjectile_Relay") ?? LoadPrefab("PlayerProjectile");
        GameObject bomb = LoadPrefab("Bomb_Relay") ?? LoadPrefab("PlayerBomb");
        GameObject enemyProjectile = LoadPrefab("EnemyProjectile_Relay") ?? LoadPrefab("EnemyProjectile_Subway");
        GameObject enemyDeath = LoadPrefab("EnemyDeathEffect") ?? LoadPrefab("EnemyDeath_Relay") ?? LoadPrefab("EnemyDeath_Subway");
        GameObject gruntPrefab = LoadPrefab("RelayGruntPrefab");
        GameObject brutePrefab = LoadPrefab("RelayBrutePrefab");

        GameObject player = CreateNeraPlayer(playerStart, playerProjectile, bomb);
        ConfigureCamera(player.transform);
        CreateRuntimePools(playerProjectile, bomb, enemyProjectile, enemyDeath);
        GameObject completePanel = CreateHud(player.GetComponent<PlayerHealth>());
        ConvertFinishFlag(completePanel);
        ConvertPickups();
        ConvertEnemies(gruntPrefab, brutePrefab);
        CreateRunNGunEnemyWaves(gruntPrefab, brutePrefab);
        CreateGameManager();
        CreateCheckpoint(playerStart + Vector3.right * 1.5f);
        CreateWaypointGraph();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ConvertedScenePath);
        File.Copy(ConvertedScenePath, MirrorScenePath, true);
        AssetDatabase.ImportAsset(MirrorScenePath, ImportAssetOptions.ForceUpdate);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ConvertedScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("RunNGun Level 1 converted to Remnant/Nera gameplay: " + ConvertedScenePath);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "_Project");
        CreateFolderIfMissing("Assets/_Project", "Generated");
        CreateFolderIfMissing("Assets/_Project/Generated", "RunNGunRemnant");
        CreateFolderIfMissing("Assets/_Project/Generated/RunNGunRemnant", "Sprites");
        CreateFolderIfMissing("Assets/_Project", "Scenes");
    }

    private static void CreateFolderIfMissing(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureLayers()
    {
        EnsureLayer("Ground");
        EnsureLayer("Player");
        EnsureLayer("Enemy");
        EnsureLayer("Projectile");
        EnsureLayer("Pickup");
        EnsureLayer("POW");

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
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
    }

    private static Vector3 FindOriginalPlayerPosition(Vector3 fallback)
    {
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (behaviour != null && behaviour.GetType().Name == "PlayerBehaviour")
                return behaviour.transform.position;
        }

        GameObject named = GameObject.Find("Player");
        return named != null ? named.transform.position : fallback;
    }

    private static void RemoveOriginalRuntimeObjects()
    {
        HashSet<GameObject> rootsToRemove = new HashSet<GameObject>();
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName == "PlayerBehaviour" || typeName == "AudioManager" || typeName == "HUDUI" ||
                typeName == "HealthUI" || typeName == "ScoreUI" || typeName == "WeaponUpgradeUI" ||
                typeName == "GameOverUI" || typeName == "WinUI" || typeName == "PauseMenuUI")
            {
                rootsToRemove.Add(behaviour.transform.root.gameObject);
            }
        }

        AddIfFound(rootsToRemove, "Canvas");
        AddIfFound(rootsToRemove, "EventSystem");

        foreach (GameObject root in rootsToRemove)
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddIfFound(HashSet<GameObject> objects, string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null)
            objects.Add(found);
    }

    private static void ConfigureTilemaps()
    {
        ConfigureGroundTilemap("Tilemap Collidable", false);
        ConfigureGroundTilemap("Tilemap One-Way Platforms", true);
    }

    private static void ConfigureGroundTilemap(string name, bool oneWay)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
            return;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            obj.layer = groundLayer;

        SnapTilemapTransformToWholeCells(obj.transform);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D tilemapCollider = obj.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
            tilemapCollider = obj.AddComponent<TilemapCollider2D>();

        CompositeCollider2D composite = obj.GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = obj.AddComponent<CompositeCollider2D>();

#pragma warning disable 0618
        tilemapCollider.usedByComposite = true;
#pragma warning restore 0618

        if (oneWay)
        {
            tilemapCollider.usedByEffector = true;
            tilemapCollider.extrusionFactor = 0.03f;

            PlatformEffector2D effector = obj.GetComponent<PlatformEffector2D>();
            if (effector == null)
                effector = obj.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 175f;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.edgeRadius = 0.02f;
            composite.usedByEffector = true;

            OneWayTilemapSupport2D support = obj.GetComponent<OneWayTilemapSupport2D>();
            if (support == null)
                support = obj.AddComponent<OneWayTilemapSupport2D>();
            support.colliderHeight = 0.18f;
            support.surfaceArc = 175f;
        }
    }

    private static void SnapTilemapTransformToWholeCells(Transform tilemapTransform)
    {
        if (tilemapTransform == null)
            return;

        Vector3 position = tilemapTransform.localPosition;
        tilemapTransform.localPosition = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), position.z);
        tilemapTransform.localScale = Vector3.one;
    }

    private static GameObject CreateNeraPlayer(Vector3 position, GameObject projectilePrefab, GameObject bombPrefab)
    {
        Sprite playerSprite = LoadSprite(NeraPath + "/legs/idle/leg_idle.png") ??
                              LoadSprite(NeraPath + "/whole body/idle alt/idle_alt1.png") ??
                              LoadSprite(NeraPath + "/whole body/crouch idle/crouch_idle0.png") ??
                              CreateColorSprite("NeraFallback", new Color(0.1f, 0.7f, 1f, 1f));

        GameObject player = new GameObject("Player_Nera_Remnant");
        player.transform.position = position;
        player.transform.localScale = Vector3.one;
        player.layer = LayerMask.NameToLayer("Player");

        SpriteRenderer rootRenderer = player.AddComponent<SpriteRenderer>();
        rootRenderer.sprite = playerSprite;
        rootRenderer.sortingOrder = 40;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.55f, 1.05f);
        collider.offset = new Vector2(0f, -0.16f);

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, -0.72f, 0f);

        GameObject legsObject = new GameObject("Legs");
        legsObject.transform.SetParent(player.transform);
        legsObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        SpriteRenderer legsRenderer = legsObject.AddComponent<SpriteRenderer>();
        legsRenderer.sprite = playerSprite;
        legsRenderer.sortingOrder = 42;

        GameObject torsoObject = new GameObject("Torso");
        torsoObject.transform.SetParent(player.transform);
        torsoObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
        torsoRenderer.sprite = LoadSprite(NeraPath + "/torso/idle/torso_idle0.png");
        torsoRenderer.sortingOrder = 43;

        if (torsoRenderer.sprite != null)
            rootRenderer.enabled = false;

        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.moveSpeed = 2.55f;
        controller.crouchMoveMultiplier = 0.45f;
        controller.crouchColliderOffset = new Vector2(0f, -0.445f);
        controller.keepFeetPlantedWhenCrouching = true;
        controller.jumpForce = 10.5f;
        controller.minimumCharacterScale = 1f;
        controller.groundCheckRadius = 0.2f;
        controller.groundCheck = groundCheck;
        controller.groundLayer = LayerMask.GetMask("Ground");

        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.maxHealth = 3;
        health.maxLives = 3;
        health.SetRespawnPoint(position);

        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(player.transform);
        firePoint.localPosition = new Vector3(0.6f, 0.08f, 0f);
        PlayerShooter2D shooter = player.AddComponent<PlayerShooter2D>();
        shooter.projectilePrefab = projectilePrefab;
        shooter.firePoint = firePoint;
        shooter.maxAmmo = 40;

        Transform throwPoint = new GameObject("ThrowPoint").transform;
        throwPoint.SetParent(player.transform);
        throwPoint.localPosition = new Vector3(0.52f, 0.22f, 0f);
        PlayerBombThrower2D bombs = player.AddComponent<PlayerBombThrower2D>();
        bombs.bombPrefab = bombPrefab;
        bombs.throwPoint = throwPoint;
        bombs.maxBombs = 3;

        PlayerMeleeAttack2D melee = player.AddComponent<PlayerMeleeAttack2D>();
        melee.enemyLayers = LayerMask.GetMask("Enemy");

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationsPath + "/PlayerVolkov.controller");
        player.AddComponent<PlayerAnimationDriver>();

        NeraPlayerVisual neraVisual = player.AddComponent<NeraPlayerVisual>();
        neraVisual.legsRenderer = legsRenderer;
        neraVisual.torsoRenderer = torsoRenderer;
        neraVisual.torsoOffset = new Vector3(0f, -0.24f, 0f);
        neraVisual.runShootTorsoOffset = new Vector3(0f, -0.24f, 0f);
        neraVisual.crouchMoveFrameRate = 6f;
        neraVisual.meleeFrameRate = 22f;
        neraVisual.legsIdle = LoadSprites(NeraPath + "/legs/idle/leg_idle.png");
        neraVisual.legsRun = LoadSpriteSequence(NeraPath + "/legs/run", "leg_run", 0, 7);
        neraVisual.legsJump = LoadSpriteSequence(NeraPath + "/legs/jump", "leg_jump", 1, 4);
        neraVisual.legsHurt = LoadSpriteSequence(NeraPath + "/legs/hurt ground", "hurt_ground", 1, 4);
        neraVisual.legsDeath = LoadSpriteSequence(NeraPath + "/legs/death ground", "death", 1, 12);
        neraVisual.torsoIdle = LoadSpriteSequence(NeraPath + "/torso/idle", "torso_idle", 0, 3);
        neraVisual.torsoRun = LoadSpriteSequence(NeraPath + "/torso/run", "torso_run", 0, 7);
        neraVisual.torsoLookUp = LoadSpriteSequence(NeraPath + "/torso/look_up", "loop_up", 0, 3);
        neraVisual.torsoShoot = LoadSpriteSequence(NeraPath + "/torso/shoot", "shoot", 0, 3);
        neraVisual.torsoShootUp = LoadSpriteSequence(NeraPath + "/torso/shoot_up", "shoot_up", 0, 3);
        neraVisual.wholeIdleAlt = LoadSpriteSequence(NeraPath + "/whole body/idle alt", "idle_alt", 1, 4);
        neraVisual.wholeCrouchIdle = LoadSpriteSequence(NeraPath + "/whole body/crouch idle", "crouch_idle", 0, 3);
        neraVisual.wholeCrouchMove = LoadSpriteSequence(NeraPath + "/whole body/crouch move", "crouch_move", 0, 5);
        neraVisual.wholeCrouchShoot = LoadSpriteSequence(NeraPath + "/whole body/crouch shoot", "crouch_shoot", 1, 4);
        neraVisual.wholeHurt = LoadSpriteSequence(NeraPath + "/whole body/hurt ground", "hurt_ground", 1, 4);
        neraVisual.wholeDeath = LoadSpriteSequence(NeraPath + "/whole body/death ground", "death", 1, 12);
        neraVisual.wholeMelee = LoadSpriteSequence(NeraPath + "/whole body/melee knife", "melee_knife", 1, 11);

        return player;
    }

    private static void ConfigureCamera(Transform player)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;

        foreach (MonoBehaviour behaviour in camera.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == "CameraFollow")
                UnityEngine.Object.DestroyImmediate(behaviour);
        }

        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        if (follow == null)
            follow = camera.gameObject.AddComponent<CameraFollow2D>();
        follow.target = player;
        follow.offset = new Vector3(3f, 1f, -4f);
        follow.smoothSpeed = 9f;
        follow.useBounds = false;
    }

    private static void CreateRuntimePools(params GameObject[] prefabs)
    {
        GameObject existing = GameObject.Find("Runtime_Pools");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        GameObject pools = new GameObject("Runtime_Pools");
        ObjectPool2D pool = pools.AddComponent<ObjectPool2D>();
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
                pool.Prewarm(prefab, 24);
        }
        pools.AddComponent<PhysicsLayerSetup2D>();
    }

    private static GameObject CreateHud(PlayerHealth playerHealth)
    {
        GameObject canvasObject = new GameObject("Canvas_HUD_Remnant");
        canvasObject.transform.localScale = Vector3.one;
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObject.AddComponent<GraphicRaycaster>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text scoreText = CreateUIText("ScoreText", canvasObject.transform, "Score: 0", new Vector2(24f, -18f), TextAnchor.UpperLeft, 22, font);
        Text healthText = CreateUIText("HealthText", canvasObject.transform, "HP", new Vector2(24f, -52f), TextAnchor.UpperLeft, 17, font);
        Text livesText = CreateUIText("LivesText", canvasObject.transform, "Lives", new Vector2(24f, -78f), TextAnchor.UpperLeft, 17, font);
        Text ammoText = CreateUIText("AmmoText", canvasObject.transform, "Ammo", new Vector2(220f, -52f), TextAnchor.UpperLeft, 17, font);
        Text bombText = CreateUIText("BombText", canvasObject.transform, "Bombs", new Vector2(220f, -78f), TextAnchor.UpperLeft, 17, font);
        Text gameOverText = CreateUIText("GameOverText", canvasObject.transform, "GAME OVER\nPress Enter or Start", Vector2.zero, TextAnchor.MiddleCenter, 40, font);
        gameOverText.gameObject.SetActive(false);

        GameObject completePanel = new GameObject("LevelCompletePanel");
        completePanel.transform.SetParent(canvasObject.transform);
        RectTransform completeRect = completePanel.AddComponent<RectTransform>();
        completeRect.anchorMin = Vector2.zero;
        completeRect.anchorMax = Vector2.one;
        completeRect.offsetMin = Vector2.zero;
        completeRect.offsetMax = Vector2.zero;
        completePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        Text completeText = CreateUIText("CompleteText", completePanel.transform, "LEVEL COMPLETE", Vector2.zero, TextAnchor.MiddleCenter, 42, font);
        completeText.rectTransform.anchorMin = Vector2.zero;
        completeText.rectTransform.anchorMax = Vector2.one;
        completeText.rectTransform.offsetMin = Vector2.zero;
        completeText.rectTransform.offsetMax = Vector2.zero;
        completePanel.SetActive(false);

        SimpleHUD hud = canvasObject.AddComponent<SimpleHUD>();
        hud.playerHealth = playerHealth;
        hud.playerShooter = playerHealth.GetComponent<PlayerShooter2D>();
        hud.playerBombThrower = playerHealth.GetComponent<PlayerBombThrower2D>();
        hud.healthText = healthText;
        hud.livesText = livesText;
        hud.ammoText = ammoText;
        hud.bombText = bombText;
        hud.scoreText = scoreText;
        hud.gameOverText = gameOverText;
        hud.lifeHeartSprite = LoadSprite("Assets/_Project/External/RunNGunOriginal/Resources/Sprite Sheets/heart pixel art 32x32.png") ??
                              LoadSprite("Assets/_Project/External/RunNGunReference/SpriteSheets/heart pixel art 32x32.png");
        return completePanel;
    }

    private static Text CreateUIText(string name, Transform parent, string text, Vector2 anchoredPosition, TextAnchor alignment, int fontSize, Font font)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = alignment == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = alignment == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = alignment == TextAnchor.MiddleCenter ? new Vector2(760f, 220f) : new Vector2(360f, 36f);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = font;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        return uiText;
    }

    private static void ConvertFinishFlag(GameObject completePanel)
    {
        GameObject finish = GameObject.Find("FinishFlag");
        if (finish == null)
            return;

        foreach (MonoBehaviour behaviour in finish.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == "LevelLoader")
                UnityEngine.Object.DestroyImmediate(behaviour);
        }

        Collider2D collider = finish.GetComponent<Collider2D>();
        if (collider == null)
            collider = finish.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        EndLevelTrigger trigger = finish.GetComponent<EndLevelTrigger>();
        if (trigger == null)
            trigger = finish.AddComponent<EndLevelTrigger>();
        trigger.alphaCompletePanel = completePanel;
        finish.layer = LayerMask.NameToLayer("POW");
    }

    private static void ConvertPickups()
    {
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName != "CoinBehaviour")
                continue;

            GameObject obj = behaviour.gameObject;
            UnityEngine.Object.DestroyImmediate(behaviour);
            ConfigurePickup(obj, 0, 0, 0, 100);
        }

        GameObject pickups = GameObject.Find("Pickups");
        if (pickups == null)
            return;

        foreach (Transform child in pickups.GetComponentsInChildren<Transform>(true))
        {
            if (child == pickups.transform)
                continue;

            string lowerName = child.name.ToLowerInvariant();
            if (lowerName.Contains("health"))
                ConfigurePickup(child.gameObject, 1, 0, 0, 150);
            else if (lowerName.Contains("ammo"))
                ConfigurePickup(child.gameObject, 0, 20, 0, 100);
        }
    }

    private static void ConfigurePickup(GameObject obj, int health, int ammo, int bombs, int score)
    {
        obj.layer = LayerMask.NameToLayer("Pickup");
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider == null)
            collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        Pickup2D pickup = obj.GetComponent<Pickup2D>();
        if (pickup == null)
            pickup = obj.AddComponent<Pickup2D>();
        pickup.healthReward = health;
        pickup.ammoReward = ammo;
        pickup.bombReward = bombs;
        pickup.scoreReward = score;
    }

    private static void ConvertEnemies(GameObject gruntPrefab, GameObject brutePrefab)
    {
        GameObject enemiesRoot = GameObject.Find("Enemies") ?? new GameObject("Enemies_Remnant");
        List<Transform> originals = new List<Transform>();

        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (behaviour != null && behaviour.GetType().Name == "EnemyBehaviour")
                originals.Add(behaviour.transform.root);
        }

        HashSet<Transform> unique = new HashSet<Transform>(originals);
        int index = 0;
        foreach (Transform original in unique)
        {
            if (original == null)
                continue;

            GameObject prefab = original.name.ToLowerInvariant().Contains("rpg") || index % 9 == 8 ? brutePrefab : gruntPrefab;
            if (prefab != null)
            {
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                enemy.name = "RemnantEnemy_" + index.ToString("00");
                enemy.transform.SetParent(enemiesRoot.transform);
                enemy.transform.position = original.position;
                EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
                if (patrol != null)
                    patrol.startingDirection = index % 2 == 0 ? -1 : 1;
            }

            UnityEngine.Object.DestroyImmediate(original.gameObject);
            index++;
        }
    }

    private static void CreateRunNGunEnemyWaves(GameObject gruntPrefab, GameObject brutePrefab)
    {
        if (gruntPrefab == null)
            return;

        GameObject existing = GameObject.Find("Enemies_Remnant_Waves");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        GameObject wavesRoot = new GameObject("Enemies_Remnant_Waves");
        WavePlan[] plans =
        {
            new WavePlan("W01_FirstContact", 9f, 0.15f, 0.32f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(15.5f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(18.2f, 8.0f), -1),
            }),
            new WavePlan("W02_CrossfireFrontBack", 22f, 0.05f, 0.28f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(31.5f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(34.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(18.0f, 8.0f), 1),
                new SpawnPlan(gruntPrefab, new Vector2(16.2f, 8.0f), 1),
            }),
            new WavePlan("W03_PlatformAmbush", 38f, 0.1f, 0.3f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(43.5f, 9.4f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(47.0f, 9.4f), -1),
                new SpawnPlan(brutePrefab ?? gruntPrefab, new Vector2(51.5f, 8.0f), -1),
            }),
            new WavePlan("W04_BackPressure", 56f, 0.05f, 0.26f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(51.5f, 8.0f), 1),
                new SpawnPlan(gruntPrefab, new Vector2(49.2f, 8.0f), 1),
                new SpawnPlan(gruntPrefab, new Vector2(65.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(68.0f, 8.0f), -1),
            }),
            new WavePlan("W05_CorridorRush", 72f, 0.05f, 0.24f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(79.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(82.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(84.5f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(69.0f, 8.0f), 1),
            }),
            new WavePlan("W06_FinalClamp", 92f, 0.05f, 0.22f, new SpawnPlan[]
            {
                new SpawnPlan(gruntPrefab, new Vector2(98.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(101.0f, 8.0f), -1),
                new SpawnPlan(brutePrefab ?? gruntPrefab, new Vector2(105.0f, 8.0f), -1),
                new SpawnPlan(gruntPrefab, new Vector2(88.5f, 8.0f), 1),
                new SpawnPlan(gruntPrefab, new Vector2(86.5f, 8.0f), 1),
            }),
        };

        for (int i = 0; i < plans.Length; i++)
            CreateEnemyWave(wavesRoot.transform, plans[i]);
    }

    private static void CreateEnemyWave(Transform root, WavePlan plan)
    {
        GameObject triggerObject = new GameObject(plan.name + "_Trigger");
        triggerObject.transform.SetParent(root);
        triggerObject.transform.position = new Vector3(plan.cameraXTrigger, 6.25f, 0f);
        triggerObject.layer = LayerMask.NameToLayer("Pickup");

        BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.25f, 12f);

        EnemyWaveTrigger2D trigger = triggerObject.AddComponent<EnemyWaveTrigger2D>();
        trigger.initialDelay = plan.initialDelay;
        trigger.spawnInterval = plan.spawnInterval;
        trigger.triggerOnce = true;
        trigger.triggerWhenCameraReachesX = true;
        trigger.cameraXTrigger = plan.cameraXTrigger;
        trigger.spawnBlockLayers = LayerMask.GetMask("Ground");
        trigger.spawnClearance = new Vector2(0.8f, 1.15f);
        trigger.groundSnapDistance = 6f;

        trigger.spawnObjects = new GameObject[plan.spawns.Length];
        trigger.spawnPoints = new Transform[plan.spawns.Length];

        for (int i = 0; i < plan.spawns.Length; i++)
        {
            SpawnPlan spawn = plan.spawns[i];
            GameObject enemy = InstantiateEnemyForWave(spawn, plan.name, i, root);
            GameObject point = new GameObject(plan.name + "_SpawnPoint_" + i.ToString("00"));
            point.transform.SetParent(triggerObject.transform);
            point.transform.position = spawn.position;

            trigger.spawnObjects[i] = enemy;
            trigger.spawnPoints[i] = point.transform;
        }
    }

    private static GameObject InstantiateEnemyForWave(SpawnPlan spawn, string waveName, int index, Transform root)
    {
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(spawn.prefab);
        enemy.name = waveName + "_Enemy_" + index.ToString("00");
        enemy.transform.SetParent(root);
        enemy.transform.position = spawn.position;
        enemy.layer = LayerMask.NameToLayer("Enemy");

        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol != null)
        {
            patrol.startingDirection = spawn.startingDirection;
            patrol.groundLayer = LayerMask.GetMask("Ground");
            patrol.obstacleLayer = LayerMask.GetMask("Ground");
            patrol.enemyLayer = LayerMask.GetMask("Enemy");
        }

        EnemyShooter2D shooter = enemy.GetComponent<EnemyShooter2D>();
        if (shooter != null)
        {
            bool brute = spawn.prefab != null && spawn.prefab.name.ToLowerInvariant().Contains("brute");
            shooter.obstacleLayers = LayerMask.GetMask("Ground");
            shooter.allowVerticalShots = true;
            shooter.useRunNGunShootCycle = true;
            shooter.fireCooldown = brute ? 0.28f : 0.22f;
            shooter.activeShootTime = brute ? 0.9f : 0.5f;
            shooter.waitShootTime = brute ? 0.5f : 1f;
            shooter.horizontalShotHeight = 0.3f;
            shooter.verticalShotOffset = 0.78f;
        }

        Vector3 scale = enemy.transform.localScale;
        float enemyScaleMultiplier = spawn.prefab != null && spawn.prefab.name.ToLowerInvariant().Contains("brute") ? 1.12f : 1.28f;
        enemy.transform.localScale = new Vector3(Mathf.Abs(scale.x) * enemyScaleMultiplier * (spawn.startingDirection >= 0 ? 1f : -1f), scale.y * enemyScaleMultiplier, scale.z);
        enemy.SetActive(false);
        return enemy;
    }

    private struct WavePlan
    {
        public string name;
        public float cameraXTrigger;
        public float initialDelay;
        public float spawnInterval;
        public SpawnPlan[] spawns;

        public WavePlan(string name, float cameraXTrigger, float initialDelay, float spawnInterval, SpawnPlan[] spawns)
        {
            this.name = name;
            this.cameraXTrigger = cameraXTrigger;
            this.initialDelay = initialDelay;
            this.spawnInterval = spawnInterval;
            this.spawns = spawns;
        }
    }

    private struct SpawnPlan
    {
        public GameObject prefab;
        public Vector2 position;
        public int startingDirection;

        public SpawnPlan(GameObject prefab, Vector2 position, int startingDirection)
        {
            this.prefab = prefab;
            this.position = position;
            this.startingDirection = startingDirection;
        }
    }

    private static void CreateGameManager()
    {
        if (UnityEngine.Object.FindAnyObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    private static void CreateCheckpoint(Vector3 position)
    {
        GameObject checkpoint = new GameObject("Checkpoint_Start_Remnant");
        checkpoint.transform.position = position;
        checkpoint.layer = LayerMask.NameToLayer("Pickup");
        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, 1.4f);
        checkpoint.AddComponent<Checkpoint2D>().scoreReward = 0;
    }

    private static void CreateWaypointGraph()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EnemyWaypointGraph2D>() != null)
            return;

        GameObject graphObject = new GameObject("Enemy_WaypointGraph_RunNGunLevel1");
        graphObject.AddComponent<EnemyWaypointGraph2D>();
        Vector2[] positions =
        {
            new Vector2(-10f, -2f), new Vector2(8f, -2f), new Vector2(22f, -1f),
            new Vector2(38f, -1f), new Vector2(55f, -2f), new Vector2(72f, -1f),
            new Vector2(90f, -1f), new Vector2(108f, -2f)
        };

        EnemyWaypointNode2D[] nodes = new EnemyWaypointNode2D[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject nodeObject = new GameObject("EnemyWaypoint_" + i.ToString("00"));
            nodeObject.transform.SetParent(graphObject.transform);
            nodeObject.transform.position = positions[i];
            nodes[i] = nodeObject.AddComponent<EnemyWaypointNode2D>();
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            List<EnemyWaypointNode2D> neighbors = new List<EnemyWaypointNode2D>();
            if (i > 0)
                neighbors.Add(nodes[i - 1]);
            if (i < nodes.Length - 1)
                neighbors.Add(nodes[i + 1]);
            nodes[i].neighbors = neighbors.ToArray();
        }
    }

    private static GameObject LoadPrefab(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "/" + name + ".prefab");
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite nested)
                return nested;
        }

        assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite nested)
                return nested;
        }
        return null;
    }

    private static Sprite CreateColorSprite(string name, Color color)
    {
        string path = GeneratedPath + "/Sprites/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite[] LoadSprites(params string[] paths)
    {
        Sprite[] sprites = new Sprite[paths.Length];
        int count = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            Sprite sprite = LoadSprite(paths[i]);
            if (sprite != null)
                sprites[count++] = sprite;
        }

        Sprite[] compact = new Sprite[count];
        for (int i = 0; i < count; i++)
            compact[i] = sprites[i];

        return compact;
    }

    private static Sprite[] LoadSpriteSequence(string folder, string prefix, int startInclusive, int endInclusive)
    {
        Sprite[] sprites = new Sprite[endInclusive - startInclusive + 1];
        int count = 0;

        for (int i = startInclusive; i <= endInclusive; i++)
        {
            Sprite sprite = LoadSprite(folder + "/" + prefix + i + ".png");
            if (sprite != null)
                sprites[count++] = sprite;
        }

        Sprite[] compact = new Sprite[count];
        for (int i = 0; i < count; i++)
            compact[i] = sprites[i];

        return compact;
    }
}
#endif
