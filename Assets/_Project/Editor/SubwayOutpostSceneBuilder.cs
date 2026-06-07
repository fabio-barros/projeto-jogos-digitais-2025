#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class SubwayOutpostSceneBuilder
{
    private const string BasePath = "Assets/_Project";
    private const string GeneratedPath = BasePath + "/Generated/SubwayOutpost";
    private const string AnimationsPath = BasePath + "/Animations";
    private const string PrefabsPath = BasePath + "/Prefabs";
    private const string ScenePath = "Assets/Scenes/Level_01_SubwayOutpost.unity";
    private const string MirrorScenePath = BasePath + "/Scenes/Level_01_SubwayOutpost.unity";
    private const string PackPath = BasePath + "/External/GenericRunNGun/Extracted";
    private const string NeraPath = BasePath + "/Art/Nera/sprites";

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();

    [MenuItem("Remnant Squad/Generate Subway Outpost Scene")]
    public static void GenerateSceneMenu()
    {
        GenerateScene(false);
    }

    public static void GenerateSceneBatch()
    {
        GenerateScene(true);
    }

    private static void GenerateScene(bool batch)
    {
        EnsureFolders();
        EnsureLayers();
        spriteCache.Clear();
        tileCache.Clear();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_01_SubwayOutpost";

        Sprite playerSprite = LoadSprite(NeraPath + "/whole body/idle alt/idle_alt0.png") ??
                              LoadSprite(NeraPath + "/whole body/crouch idle/crouch_idle0.png") ??
                              CreateColorSprite("Fallback_Player", new Color(0.1f, 0.7f, 1f, 1f));
        Sprite bulletSprite = LoadSprite(NeraPath + "/effects/bullet/bullet.png") ?? CreateColorSprite("Fallback_Bullet", Color.yellow);
        Sprite enemyBulletSprite = CreateColorSprite("Fallback_EnemyBullet", new Color(1f, 0.25f, 0.1f, 1f));
        Sprite grenadeSprite = CreateColorSprite("Fallback_Grenade", new Color(0.05f, 1f, 0.95f, 1f));
        Sprite explosionSprite = SliceSprite("Enemy_Explosion_00", PackPath + "/Enemies/Explosion_Particle.png", new RectInt(96, 0, 32, 32), 32f) ?? CreateColorSprite("Fallback_Explosion", new Color(1f, 0.55f, 0.1f, 1f));
        Sprite powSprite = CreateColorSprite("Subway_POW", new Color(1f, 0.84f, 0.25f, 1f));
        Sprite checkpointSprite = CreateColorSprite("Subway_Checkpoint", new Color(0.05f, 0.95f, 1f, 1f));
        Sprite pickupHealthSprite = CreateColorSprite("Subway_Health", new Color(0.15f, 1f, 0.2f, 1f));
        Sprite pickupAmmoSprite = CreateColorSprite("Subway_Ammo", new Color(1f, 0.92f, 0.15f, 1f));
        Sprite pickupBombSprite = CreateColorSprite("Subway_Bomb", new Color(0.2f, 0.75f, 1f, 1f));
        Sprite endSprite = CreateColorSprite("Subway_End", new Color(0.1f, 1f, 0.35f, 0.65f));

        SubwayTiles tiles = LoadSubwayTiles();
        CreateCamera();
        CreateBackgrounds();

        GameObject bulletHitPrefab = CreateBulletHitPrefab(explosionSprite);
        GameObject playerProjectilePrefab = CreatePlayerProjectilePrefab(bulletSprite, bulletHitPrefab);
        GameObject enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyBulletSprite, bulletHitPrefab);
        GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);
        GameObject bombPrefab = CreateBombPrefab(grenadeSprite, explosionPrefab);
        GameObject enemyDeathPrefab = CreateEnemyDeathPrefab(explosionSprite);
        CreateRuntimePools(playerProjectilePrefab, enemyProjectilePrefab, bulletHitPrefab, explosionPrefab, enemyDeathPrefab);

        GameObject player = CreatePlayer(playerSprite, playerProjectilePrefab, bombPrefab);
        GameObject completePanel = CreateHUD(player.GetComponent<PlayerHealth>());

        BuildTilemapLevel(tiles);
        BuildArtDressing(tiles);
        BuildGameplay(player, powSprite, checkpointSprite, pickupHealthSprite, pickupAmmoSprite, pickupBombSprite, endSprite, completePanel);
        BuildEnemies(enemyProjectilePrefab, enemyDeathPrefab);
        BuildWaypointGraph();

        Camera.main.GetComponent<CameraFollow2D>().target = player.transform;

        EditorSceneManager.SaveScene(scene, ScenePath);
        File.Copy(ScenePath, MirrorScenePath, true);
        AssetDatabase.ImportAsset(MirrorScenePath, ImportAssetOptions.ForceUpdate);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Subway Outpost scene generated successfully. Open Assets/Scenes/Level_01_SubwayOutpost.unity and press Play.");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "_Project");
        CreateFolderIfMissing(BasePath, "Generated");
        CreateFolderIfMissing(BasePath + "/Generated", "SubwayOutpost");
        CreateFolderIfMissing(GeneratedPath, "Tiles");
        CreateFolderIfMissing(GeneratedPath, "Sprites");
        CreateFolderIfMissing(GeneratedPath, "EnemyFrames");
        CreateFolderIfMissing(BasePath, "Animations");
        CreateFolderIfMissing(BasePath, "Prefabs");
        CreateFolderIfMissing(BasePath, "Scenes");
        CreateFolderIfMissing("Assets", "Scenes");
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
        EnsureLayer("POW");
        EnsureLayer("Pickup");
        EnsureLayer("Hazard");
        EnsureLayer("Water");

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

    private static SubwayTiles LoadSubwayTiles()
    {
        string sheet = PackPath + "/Assets_area_1/Tileset/Subway_tiles.png";
        return new SubwayTiles
        {
            floor = CreateTile("Subway_Floor_Edge", SliceSprite("Subway_Floor_Edge", sheet, new RectInt(16, 112, 16, 16), 16f)),
            floorFill = CreateTile("Subway_Floor_Fill", SliceSprite("Subway_Floor_Fill", sheet, new RectInt(32, 128, 16, 16), 16f)),
            wall = CreateTile("Subway_Wall", SliceSprite("Subway_Wall", sheet, new RectInt(128, 160, 16, 16), 16f)),
            wallDark = CreateTile("Subway_Wall_Dark", SliceSprite("Subway_Wall_Dark", sheet, new RectInt(176, 192, 16, 16), 16f)),
            platform = CreateTile("Subway_Platform_Edge", SliceSprite("Subway_Platform_Edge", sheet, new RectInt(64, 96, 16, 16), 16f)),
            platformFill = CreateTile("Subway_Platform_Fill", SliceSprite("Subway_Platform_Fill", sheet, new RectInt(64, 112, 16, 16), 16f)),
            pipe = SliceSprite("Subway_Pipe", sheet, new RectInt(0, 48, 80, 16), 16f),
            lamp = SliceSprite("Subway_Lamp", sheet, new RectInt(48, 64, 16, 48), 16f),
            greenDoor = SliceSprite("Subway_Green_Door", sheet, new RectInt(304, 64, 64, 64), 16f),
            crate = SliceSprite("Subway_Crates", sheet, new RectInt(112, 304, 48, 32), 16f),
            warningPoster = SliceSprite("Subway_Poster", sheet, new RectInt(176, 112, 32, 48), 16f),
            arrowSign = SliceSprite("Subway_ArrowSign", sheet, new RectInt(288, 176, 32, 16), 16f),
            chainPlatform = SliceSprite("Subway_Hanging_Platform", sheet, new RectInt(192, 64, 32, 32), 16f),
            chains = SliceSprite("Subway_Chains", sheet, new RectInt(224, 0, 16, 96), 16f),
            railing = SliceSprite("Subway_Railing", sheet, new RectInt(0, 256, 64, 32), 16f),
            grate = SliceSprite("Subway_Grate", sheet, new RectInt(304, 192, 32, 48), 16f)
        };
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.0f;
        camera.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-5f, -1f, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.offset = new Vector3(2.0f, 1.0f, -10f);
        follow.smoothSpeed = 8f;
        follow.useBounds = true;
        follow.minBounds = new Vector2(-8f, -12f);
        follow.maxBounds = new Vector2(112f, 5.8f);
    }

    private static void CreateBackgrounds()
    {
        Sprite subwayBackground = LoadGenericSprite(PackPath + "/Assets_area_1/Background/subway_BG.png", 32f);
        Sprite subwayWall = LoadGenericSprite(PackPath + "/Assets_area_1/Background/wall_subway.png", 48f);

        for (int i = 0; i < 9; i++)
        {
            CreateDecorSprite("BG_Subway_Main_" + i.ToString("00"), subwayBackground, new Vector2(-4f + i * 14.8f, -0.2f), new Vector2(15.5f, 8.5f), new Color(0.62f, 0.78f, 0.58f, 0.82f), -30, false);
        }

        for (int i = 0; i < 6; i++)
        {
            CreateDecorSprite("BG_Subway_DeepWall_" + i.ToString("00"), subwayWall, new Vector2(5f + i * 20f, -0.5f), new Vector2(12f, 10f), new Color(0.45f, 0.62f, 0.45f, 0.7f), -31, i % 2 == 1);
        }
    }

    private static void BuildTilemapLevel(SubwayTiles tiles)
    {
        GameObject gridObject = new GameObject("Grid_Level01_SubwayOutpost");
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap collision = CreateTilemap(gridObject.transform, "TM_Collision_Foreground", "Ground", 8, true, false);
        Tilemap visual = CreateTilemap(gridObject.transform, "TM_Visual_Foreground", "Default", 6, false, false);
        Tilemap midground = CreateTilemap(gridObject.transform, "TM_Midground_Walls", "Default", -4, false, false);
        Tilemap platforms = CreateTilemap(gridObject.transform, "TM_OneWay_Platforms", "Ground", 7, true, true);

        FillRect(midground, tiles.wallDark, -12, -8, 128, 14);
        FillRoomPanel(midground, tiles.wall, -10, -4, 22, 7);
        FillRoomPanel(midground, tiles.wall, 16, -4, 20, 7);
        FillRoomPanel(midground, tiles.wall, 42, -4, 18, 7);
        FillRoomPanel(midground, tiles.wall, 64, -8, 20, 11);
        FillRoomPanel(midground, tiles.wall, 88, -4, 26, 7);

        CreateSolidGround(collision, visual, tiles, -12, -5, 20, 2);
        CreateSolidGround(collision, visual, tiles, 8, -5, 14, 2);
        CreateSolidGround(collision, visual, tiles, 25, -4, 12, 2);
        CreateSolidGround(collision, visual, tiles, 40, -5, 14, 2);
        CreateSolidGround(collision, visual, tiles, 57, -7, 13, 2);
        CreateSolidGround(collision, visual, tiles, 73, -5, 16, 2);
        CreateSolidGround(collision, visual, tiles, 92, -4, 22, 2);

        CreateSolidGround(collision, visual, tiles, 22, -8, 3, 5);
        CreateSolidGround(collision, visual, tiles, 37, -8, 3, 4);
        CreateSolidGround(collision, visual, tiles, 54, -9, 3, 5);
        CreateSolidGround(collision, visual, tiles, 70, -10, 3, 5);
        CreateSolidGround(collision, visual, tiles, 89, -8, 3, 4);

        CreateOneWayPlatformTiles(platforms, tiles, -2, -2, 7);
        CreateOneWayPlatformTiles(platforms, tiles, 12, -2, 6);
        CreateOneWayPlatformTiles(platforms, tiles, 28, -1, 7);
        CreateOneWayPlatformTiles(platforms, tiles, 45, -2, 6);
        CreateOneWayPlatformTiles(platforms, tiles, 62, -4, 7);
        CreateOneWayPlatformTiles(platforms, tiles, 78, -2, 7);
        CreateOneWayPlatformTiles(platforms, tiles, 100, 0, 6);

        CreateOneWayPlatformTiles(platforms, tiles, 48, 1, 3);
        CreateOneWayPlatformTiles(platforms, tiles, 53, -1, 3);
        CreateOneWayPlatformTiles(platforms, tiles, 58, 1, 3);
    }

    private static Tilemap CreateTilemap(Transform parent, string name, string layerName, int sortingOrder, bool collider, bool oneWay)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            obj.layer = layer;

        Tilemap tilemap = obj.AddComponent<Tilemap>();
        TilemapRenderer renderer = obj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (collider)
        {
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = obj.AddComponent<Rigidbody2D>();

            TilemapCollider2D tilemapCollider = obj.AddComponent<TilemapCollider2D>();
            CompositeCollider2D composite = obj.AddComponent<CompositeCollider2D>();
            rb.bodyType = RigidbodyType2D.Static;
#pragma warning disable 0618
            tilemapCollider.usedByComposite = true;
#pragma warning restore 0618

            if (oneWay)
            {
                PlatformEffector2D effector = obj.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useOneWayGrouping = true;
                effector.surfaceArc = 160f;
                composite.usedByEffector = true;
            }
        }

        return tilemap;
    }

    private static void FillRect(Tilemap map, TileBase tile, int x, int y, int width, int height)
    {
        for (int ix = x; ix < x + width; ix++)
        {
            for (int iy = y; iy < y + height; iy++)
                map.SetTile(new Vector3Int(ix, iy, 0), tile);
        }
    }

    private static void FillRoomPanel(Tilemap map, TileBase tile, int x, int y, int width, int height)
    {
        FillRect(map, tile, x, y, width, height);
    }

    private static void CreateSolidGround(Tilemap collision, Tilemap visual, SubwayTiles tiles, int x, int y, int width, int height)
    {
        FillRect(collision, tiles.floorFill, x, y, width, height);
        for (int ix = x; ix < x + width; ix++)
        {
            visual.SetTile(new Vector3Int(ix, y + height - 1, 0), tiles.floor);
            for (int iy = y; iy < y + height - 1; iy++)
                visual.SetTile(new Vector3Int(ix, iy, 0), tiles.floorFill);
        }
    }

    private static void CreateOneWayPlatformTiles(Tilemap map, SubwayTiles tiles, int x, int y, int width)
    {
        for (int ix = x; ix < x + width; ix++)
            map.SetTile(new Vector3Int(ix, y, 0), tiles.platform);
    }

    private static void BuildArtDressing(SubwayTiles tiles)
    {
        for (int i = 0; i < 14; i++)
            CreateDecorSprite("Lamp_Row_" + i.ToString("00"), tiles.lamp, new Vector2(-9f + i * 8f, 1.4f), new Vector2(0.7f, 2.1f), Color.white, 10, i % 2 == 0);

        CreateDecorSprite("GreenDoor_Intro", tiles.greenDoor, new Vector2(-7.5f, -2.9f), new Vector2(3.2f, 3.2f), Color.white, 1, false);
        CreateDecorSprite("GreenDoor_Mid", tiles.greenDoor, new Vector2(74f, -2.9f), new Vector2(3.2f, 3.2f), Color.white, 1, true);
        CreateDecorSprite("WarningPoster_Intro", tiles.warningPoster, new Vector2(16.5f, -2.1f), new Vector2(1.0f, 1.5f), Color.white, 9, false);
        CreateDecorSprite("ArrowSign_01", tiles.arrowSign, new Vector2(22f, -1.2f), new Vector2(1.4f, 0.7f), Color.white, 9, false);
        CreateDecorSprite("ArrowSign_02", tiles.arrowSign, new Vector2(84f, -1.2f), new Vector2(1.4f, 0.7f), Color.white, 9, false);

        CreateDecorSprite("Crates_Intro", tiles.crate, new Vector2(-5.5f, -3.95f), new Vector2(2.4f, 1.6f), Color.white, 9, false);
        CreateDecorSprite("Crates_Ambush", tiles.crate, new Vector2(33f, -2.95f), new Vector2(2.4f, 1.6f), Color.white, 9, true);
        CreateDecorSprite("Crates_Final", tiles.crate, new Vector2(97f, -2.95f), new Vector2(2.4f, 1.6f), Color.white, 9, false);
        CreateDecorSprite("Railing_Shaft", tiles.railing, new Vector2(50f, -5.2f), new Vector2(4f, 1.2f), Color.white, 9, false);
        CreateDecorSprite("Grate_Final", tiles.grate, new Vector2(105f, -2.2f), new Vector2(1.5f, 2.2f), Color.white, 9, false);

        for (int i = 0; i < 4; i++)
        {
            float x = 49f + i * 5f;
            CreateDecorSprite("Shaft_Chains_" + i.ToString("00"), tiles.chains, new Vector2(x, 1.4f), new Vector2(0.55f, 5.0f), Color.white, 5, false);
            CreateDecorSprite("Shaft_HangingPlatform_" + i.ToString("00"), tiles.chainPlatform, new Vector2(x, -0.8f - (i % 2) * 1.8f), new Vector2(1.3f, 1.3f), Color.white, 8, false);
        }
    }

    private static void BuildGameplay(GameObject player, Sprite powSprite, Sprite checkpointSprite, Sprite healthSprite, Sprite ammoSprite, Sprite bombSprite, Sprite endSprite, GameObject completePanel)
    {
        CreateCheckpoint("Checkpoint_Intro", checkpointSprite, new Vector2(3f, -3.3f));
        CreateCheckpoint("Checkpoint_Shaft", checkpointSprite, new Vector2(64f, -5.3f));
        CreateCheckpoint("Checkpoint_Final", checkpointSprite, new Vector2(92f, -2.3f));
        CreatePickup("Pickup_Ammo_Intro", ammoSprite, new Vector2(14f, -1.3f), 0, 18, 0, 50);
        CreatePickup("Pickup_Bomb_Shaft", bombSprite, new Vector2(53f, 0.4f), 0, 0, 2, 100);
        CreatePickup("Pickup_Health_Final", healthSprite, new Vector2(101f, 0.8f), 1, 0, 0, 150);
        CreatePOW("POW_Subway_Cell", powSprite, new Vector2(20.5f, -3.3f));
        CreateEndTrigger(endSprite, new Vector2(111f, -2.2f), completePanel);
    }

    private static void BuildEnemies(GameObject enemyProjectilePrefab, GameObject deathPrefab)
    {
        Sprite[] gruntFrames = SliceHorizontalFrames("ARGrunt", PackPath + "/Enemies/ARMob.png", 32, 38, 24, 32f);
        Sprite[] bruteFrames = SliceHorizontalFrames("RPGBrute", PackPath + "/Enemies/RPGmob.png", 44, 44, 10, 32f);
        Sprite[] sniperFrames = SliceHorizontalFrames("Sniper", PackPath + "/Enemies/SniperMob.png", 44, 44, 14, 32f);

        RuntimeAnimatorController gruntController = CreateEnemyAnimator("Subway_ARGrunt", gruntFrames);
        RuntimeAnimatorController bruteController = CreateEnemyAnimator("Subway_RPGBrute", bruteFrames);
        RuntimeAnimatorController sniperController = CreateEnemyAnimator("Subway_Sniper", sniperFrames);

        CreateEnemy("Grunt_Intro_01", gruntFrames[0], gruntController, enemyProjectilePrefab, deathPrefab, new Vector2(9f, -3.35f), 2, 120, true, new Vector2(0.74f, 1.05f), -1);
        CreateEnemy("Grunt_Intro_Rear", gruntFrames[0], gruntController, enemyProjectilePrefab, deathPrefab, new Vector2(-1f, -3.35f), 2, 120, true, new Vector2(0.74f, 1.05f), 1);
        CreateEnemy("Sniper_Platform_01", sniperFrames[0], sniperController, enemyProjectilePrefab, deathPrefab, new Vector2(29f, -0.35f), 2, 180, true, new Vector2(0.82f, 1.05f), -1);
        CreateEnemy("Grunt_Shaft_Lower", gruntFrames[0], gruntController, enemyProjectilePrefab, deathPrefab, new Vector2(44f, -3.35f), 2, 140, true, new Vector2(0.74f, 1.05f), 1);
        CreateEnemy("Grunt_Shaft_Platform", gruntFrames[0], gruntController, enemyProjectilePrefab, deathPrefab, new Vector2(63f, -3.35f), 2, 140, true, new Vector2(0.74f, 1.05f), -1);
        CreateEnemy("Brute_Final_Blocker", bruteFrames[0], bruteController, enemyProjectilePrefab, deathPrefab, new Vector2(94f, -2.25f), 10, 600, true, new Vector2(1.1f, 1.32f), -1);
        CreateEnemy("Sniper_Final_Upper", sniperFrames[0], sniperController, enemyProjectilePrefab, deathPrefab, new Vector2(102f, 0.65f), 3, 220, true, new Vector2(0.82f, 1.05f), -1);
    }

    private static GameObject CreateEnemy(string name, Sprite sprite, RuntimeAnimatorController controller, GameObject enemyProjectilePrefab, GameObject deathPrefab, Vector2 position, int health, int score, bool canShoot, Vector2 scale, int direction)
    {
        GameObject enemy = CreateBoxObject(name, sprite, position, scale, "Enemy");
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 12;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.92f);
        collider.offset = new Vector2(0f, -0.04f);

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = health;
        damageable.scoreValue = score;
        damageable.deathDelay = 0.28f;
        damageable.deathEffectPrefab = deathPrefab;
        enemy.AddComponent<DamageOnContact>().damage = 1;

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(enemy.transform);
        groundCheck.localPosition = new Vector3(direction * 0.42f, -0.58f, 0f);
        Transform wallCheck = new GameObject("WallCheck").transform;
        wallCheck.SetParent(enemy.transform);
        wallCheck.localPosition = new Vector3(direction * 0.48f, -0.05f, 0f);

        EnemyPatrol2D patrol = enemy.AddComponent<EnemyPatrol2D>();
        patrol.moveSpeed = 1.55f;
        patrol.startingDirection = direction;
        patrol.groundCheck = groundCheck;
        patrol.wallCheck = wallCheck;
        patrol.groundLayer = LayerMask.GetMask("Ground");
        patrol.obstacleLayer = LayerMask.GetMask("Ground");
        patrol.jumpForce = 6.5f;
        patrol.useWaypointNavigation = false;

        if (canShoot)
        {
            Transform firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(enemy.transform);
            firePoint.localPosition = new Vector3(direction * 0.56f, 0.12f, 0f);
            EnemyShooter2D shooter = enemy.AddComponent<EnemyShooter2D>();
            shooter.enemyProjectilePrefab = enemyProjectilePrefab;
            shooter.firePoint = firePoint;
            shooter.obstacleLayers = LayerMask.GetMask("Ground");
            shooter.range = 9f;
            shooter.fireCooldown = 1.5f;
        }

        Animator animator = enemy.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        enemy.AddComponent<EnemyAnimationDriver>();
        return enemy;
    }

    private static RuntimeAnimatorController CreateEnemyAnimator(string name, Sprite[] frames)
    {
        string controllerPath = AnimationsPath + "/" + name + ".controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        EnsureEnemyAnimatorParameters(controller);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        for (int i = stateMachine.states.Length - 1; i >= 0; i--)
            stateMachine.RemoveState(stateMachine.states[i].state);

        AnimationClip idle = CreateSpriteClip(name + "_Idle", frames, 0, Mathf.Min(3, frames.Length - 1), 7f, true);
        AnimationClip walk = CreateSpriteClip(name + "_Walk", frames, 0, Mathf.Min(7, frames.Length - 1), 10f, true);
        AnimationClip shoot = CreateSpriteClip(name + "_Shoot", frames, Mathf.Min(8, frames.Length - 1), Mathf.Min(11, frames.Length - 1), 12f, true);
        AnimationClip death = CreateSpriteClip(name + "_Death", frames, Mathf.Max(0, frames.Length - 4), frames.Length - 1, 10f, false);

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(220f, 0f, 0f));
        idleState.motion = idle;
        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(220f, 70f, 0f));
        walkState.motion = walk;
        AnimatorState shootState = stateMachine.AddState("Shoot", new Vector3(220f, 140f, 0f));
        shootState.motion = shoot;
        AnimatorState deathState = stateMachine.AddState("Death", new Vector3(220f, 210f, 0f));
        deathState.motion = death;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
        AnimatorStateTransition anyToShoot = stateMachine.AddAnyStateTransition(shootState);
        anyToShoot.hasExitTime = false;
        anyToShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shooting");
        AnimatorStateTransition shootToIdle = shootState.AddTransition(idleState);
        shootToIdle.hasExitTime = true;
        shootToIdle.exitTime = 0.85f;
        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.hasExitTime = false;
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
        return controller;
    }

    private static void EnsureEnemyAnimatorParameters(AnimatorController controller)
    {
        EnsureAnimatorParameter(controller, "Moving", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Shooting", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Hurt", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Dead", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Brute", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "DeathVariant", AnimatorControllerParameterType.Int);
    }

    private static void EnsureAnimatorParameter(AnimatorController controller, string parameterName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName)
                return;
        }

        controller.AddParameter(parameterName, type);
    }

    private static AnimationClip CreateSpriteClip(string name, Sprite[] frames, int start, int end, float frameRate, bool loop)
    {
        string path = AnimationsPath + "/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        int count = Mathf.Max(1, end - start + 1);
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[Mathf.Clamp(start + i, 0, frames.Length - 1)]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void BuildWaypointGraph()
    {
        GameObject graphObject = new GameObject("Enemy_WaypointGraph_SubwayOutpost");
        graphObject.AddComponent<EnemyWaypointGraph2D>();
        Vector2[] points = new Vector2[]
        {
            new Vector2(-8f, -3.35f), new Vector2(2f, -3.35f), new Vector2(10f, -3.35f), new Vector2(15f, -1.4f),
            new Vector2(28f, -0.4f), new Vector2(31f, -2.35f), new Vector2(44f, -3.35f), new Vector2(50f, 1.5f),
            new Vector2(55f, -0.3f), new Vector2(63f, -3.35f), new Vector2(78f, -3.35f), new Vector2(84f, -1.4f),
            new Vector2(96f, -2.35f), new Vector2(102f, 0.6f), new Vector2(110f, -2.35f)
        };

        for (int i = 0; i < points.Length; i++)
        {
            GameObject nodeObject = new GameObject("Node_" + i.ToString("00"));
            nodeObject.transform.SetParent(graphObject.transform);
            nodeObject.transform.position = points[i];
            EnemyWaypointNode2D node = nodeObject.AddComponent<EnemyWaypointNode2D>();
            node.neighbors = new EnemyWaypointNode2D[0];
        }
    }

    private static GameObject CreatePlayer(Sprite sprite, GameObject projectilePrefab, GameObject bombPrefab)
    {
        GameObject player = CreateBoxObject("Player_Nera", sprite, new Vector2(-9f, -3.25f), Vector2.one, "Player");
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 20;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 3f;
        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.62f, 1.12f);
        collider.offset = new Vector2(0f, -0.08f);

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, -0.68f, 0f);

        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.moveSpeed = 2.55f;
        controller.jumpForce = 11.5f;
        controller.groundCheck = groundCheck;
        controller.groundLayer = LayerMask.GetMask("Ground");
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.maxHealth = 3;
        health.maxLives = 3;

        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(player.transform);
        firePoint.localPosition = new Vector3(0.6f, 0.08f, 0f);
        PlayerShooter2D shooter = player.AddComponent<PlayerShooter2D>();
        shooter.projectilePrefab = projectilePrefab;
        shooter.firePoint = firePoint;
        shooter.maxAmmo = 40;

        PlayerBombThrower2D bombs = player.AddComponent<PlayerBombThrower2D>();
        bombs.bombPrefab = bombPrefab;
        bombs.maxBombs = 3;
        Transform throwPoint = new GameObject("ThrowPoint").transform;
        throwPoint.SetParent(player.transform);
        throwPoint.localPosition = new Vector3(0.52f, 0.22f, 0f);
        bombs.throwPoint = throwPoint;

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationsPath + "/PlayerVolkov.controller");
        player.AddComponent<PlayerAnimationDriver>();
        return player;
    }

    private static GameObject CreateBoxObject(string name, Sprite sprite, Vector2 position, Vector2 scale, string layerName)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            obj.layer = layer;
        return obj;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite nested)
                return nested;
        }

        return null;
    }

    private static Sprite LoadGenericSprite(string path, float ppu)
    {
        ConfigureImporter(path, ppu, false);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite SliceSprite(string name, string sourcePath, RectInt topLeftRect, float ppu)
    {
        string key = name + sourcePath + topLeftRect;
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        if (!File.Exists(sourcePath))
            return null;

        ConfigureImporter(sourcePath, ppu, true);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null || topLeftRect.xMax > source.width || topLeftRect.yMax > source.height)
            return null;

        string texturePath = GeneratedPath + "/Sprites/" + name + ".png";
        int readY = source.height - topLeftRect.y - topLeftRect.height;
        Texture2D cropped = new Texture2D(topLeftRect.width, topLeftRect.height, TextureFormat.RGBA32, false);
        cropped.SetPixels(source.GetPixels(topLeftRect.x, readY, topLeftRect.width, topLeftRect.height));
        cropped.Apply();
        File.WriteAllBytes(texturePath, cropped.EncodeToPNG());
        Object.DestroyImmediate(cropped);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        ConfigureImporter(texturePath, ppu, false);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        spriteCache[key] = sprite;
        return sprite;
    }

    private static Sprite[] SliceHorizontalFrames(string prefix, string sourcePath, int frameWidth, int frameHeight, int frameCount, float ppu)
    {
        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
            frames[i] = SliceSprite(prefix + "_Frame_" + i.ToString("00"), sourcePath, new RectInt(i * frameWidth, 0, frameWidth, frameHeight), ppu);

        return frames;
    }

    private static void ConfigureImporter(string path, float ppu, bool readable)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.spriteImportMode != SpriteImportMode.Single ||
                       Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f ||
                       importer.filterMode != FilterMode.Point ||
                       importer.textureCompression != TextureImporterCompression.Uncompressed ||
                       importer.mipmapEnabled ||
                       importer.alphaIsTransparency != true ||
                       importer.isReadable != readable;

        if (!changed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = readable;
        importer.SaveAndReimport();
    }

    private static TileBase CreateTile(string name, Sprite sprite)
    {
        if (sprite == null)
            return null;

        if (tileCache.TryGetValue(name, out TileBase cached))
            return cached;

        string path = GeneratedPath + "/Tiles/" + name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        EditorUtility.SetDirty(tile);
        tileCache[name] = tile;
        return tile;
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
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        ConfigureImporter(path, 16f, false);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreatePlayerProjectilePrefab(Sprite sprite, GameObject hitPrefab)
    {
        GameObject projectile = CreateBoxObject("PlayerProjectile_Subway", sprite, Vector2.zero, new Vector2(0.55f, 0.28f), "Projectile");
        projectile.AddComponent<BoxCollider2D>().isTrigger = true;
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Projectile2D projectile2D = projectile.AddComponent<Projectile2D>();
        projectile2D.speed = 10f;
        projectile2D.damage = 1;
        projectile2D.hitEffectPrefab = hitPrefab;
        return SavePrefab(projectile, "PlayerProjectile_Subway");
    }

    private static GameObject CreateEnemyProjectilePrefab(Sprite sprite, GameObject hitPrefab)
    {
        GameObject projectile = CreateBoxObject("EnemyProjectile_Subway", sprite, Vector2.zero, new Vector2(0.42f, 0.2f), "Projectile");
        projectile.AddComponent<BoxCollider2D>().isTrigger = true;
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        EnemyProjectile2D projectile2D = projectile.AddComponent<EnemyProjectile2D>();
        projectile2D.speed = 7f;
        projectile2D.damage = 1;
        projectile2D.hitEffectPrefab = hitPrefab;
        return SavePrefab(projectile, "EnemyProjectile_Subway");
    }

    private static GameObject CreateBulletHitPrefab(Sprite sprite)
    {
        GameObject hit = CreateBoxObject("BulletHit_Subway", sprite, Vector2.zero, new Vector2(0.65f, 0.65f), "Projectile");
        SpriteAnimationOnce animation = hit.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = hit.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 18f;
        return SavePrefab(hit, "BulletHit_Subway");
    }

    private static GameObject CreateExplosionPrefab(Sprite sprite)
    {
        GameObject explosion = CreateBoxObject("Explosion_Subway", sprite, Vector2.zero, new Vector2(1.8f, 1.8f), "Projectile");
        SpriteAnimationOnce animation = explosion.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = explosion.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 18f;
        return SavePrefab(explosion, "Explosion_Subway");
    }

    private static GameObject CreateBombPrefab(Sprite sprite, GameObject explosionPrefab)
    {
        GameObject bomb = CreateBoxObject("Bomb_Subway", sprite, Vector2.zero, new Vector2(0.34f, 0.34f), "Projectile");
        bomb.AddComponent<CircleCollider2D>();
        Rigidbody2D rb = bomb.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BombProjectile2D bomb2D = bomb.AddComponent<BombProjectile2D>();
        bomb2D.explosionVisual = explosionPrefab;
        bomb2D.damageLayers = LayerMask.GetMask("Enemy");
        bomb2D.explosionRadius = 2.2f;
        bomb2D.damage = 3;
        return SavePrefab(bomb, "Bomb_Subway");
    }

    private static GameObject CreateEnemyDeathPrefab(Sprite sprite)
    {
        GameObject death = CreateBoxObject("EnemyDeath_Subway", sprite, Vector2.zero, new Vector2(1.25f, 1.25f), "Projectile");
        SpriteAnimationOnce animation = death.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = death.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 12f;
        return SavePrefab(death, "EnemyDeath_Subway");
    }

    private static GameObject SavePrefab(GameObject obj, string name)
    {
        string path = PrefabsPath + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static void CreateRuntimePools(params GameObject[] prefabs)
    {
        GameObject pools = new GameObject("Runtime_Pools");
        ObjectPool2D pool = pools.AddComponent<ObjectPool2D>();
        for (int i = 0; i < prefabs.Length; i++)
            pool.Prewarm(prefabs[i], 24);

        pools.AddComponent<PhysicsLayerSetup2D>();
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

        Text scoreText = CreateUIText("ScoreText", canvasObject.transform, "Score: 0", new Vector2(24f, -18f), TextAnchor.UpperLeft, 22, font);
        Text healthText = CreateUIText("HealthText", canvasObject.transform, "HP", new Vector2(24f, -52f), TextAnchor.UpperLeft, 17, font);
        Text livesText = CreateUIText("LivesText", canvasObject.transform, "Lives", new Vector2(24f, -78f), TextAnchor.UpperLeft, 17, font);
        Text ammoText = CreateUIText("AmmoText", canvasObject.transform, "Ammo", new Vector2(220f, -52f), TextAnchor.UpperLeft, 17, font);
        Text bombText = CreateUIText("BombText", canvasObject.transform, "Bombs", new Vector2(220f, -78f), TextAnchor.UpperLeft, 17, font);
        Text gameOverText = CreateUIText("GameOverText", canvasObject.transform, "GAME OVER\nPress Enter or Start", Vector2.zero, TextAnchor.MiddleCenter, 40, font);
        gameOverText.gameObject.SetActive(false);

        GameObject completePanel = new GameObject("AlphaCompletePanel");
        completePanel.transform.SetParent(canvasObject.transform);
        RectTransform completeRect = completePanel.AddComponent<RectTransform>();
        completeRect.anchorMin = Vector2.zero;
        completeRect.anchorMax = Vector2.one;
        completeRect.offsetMin = Vector2.zero;
        completeRect.offsetMax = Vector2.zero;
        completePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        Text completeText = CreateUIText("AlphaCompleteText", completePanel.transform, "SUBWAY OUTPOST COMPLETE", Vector2.zero, TextAnchor.MiddleCenter, 42, font);
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
        rect.sizeDelta = alignment == TextAnchor.MiddleCenter ? new Vector2(720f, 220f) : new Vector2(360f, 36f);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = font;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        return uiText;
    }

    private static void CreateCheckpoint(string name, Sprite sprite, Vector2 position)
    {
        GameObject checkpoint = CreateBoxObject(name, sprite, position, new Vector2(0.45f, 1.1f), "Pickup");
        checkpoint.AddComponent<BoxCollider2D>().isTrigger = true;
        checkpoint.AddComponent<Checkpoint2D>();
    }

    private static void CreatePickup(string name, Sprite sprite, Vector2 position, int healthReward, int ammoReward, int bombReward, int scoreReward)
    {
        GameObject pickup = CreateBoxObject(name, sprite, position, new Vector2(0.42f, 0.42f), "Pickup");
        pickup.AddComponent<BoxCollider2D>().isTrigger = true;
        Pickup2D pickup2D = pickup.AddComponent<Pickup2D>();
        pickup2D.healthReward = healthReward;
        pickup2D.ammoReward = ammoReward;
        pickup2D.bombReward = bombReward;
        pickup2D.scoreReward = scoreReward;
    }

    private static void CreatePOW(string name, Sprite sprite, Vector2 position)
    {
        GameObject pow = CreateBoxObject(name, sprite, position, new Vector2(0.75f, 1f), "POW");
        pow.AddComponent<BoxCollider2D>().isTrigger = true;
        POWRescue rescue = pow.AddComponent<POWRescue>();
        rescue.scoreReward = 500;
        rescue.healReward = 1;
        rescue.ammoReward = 10;
        rescue.bombReward = 1;
    }

    private static void CreateEndTrigger(Sprite sprite, Vector2 position, GameObject completePanel)
    {
        GameObject end = CreateBoxObject("End_Level_Trigger", sprite, position, new Vector2(0.8f, 1.6f), "POW");
        end.AddComponent<BoxCollider2D>().isTrigger = true;
        EndLevelTrigger trigger = end.AddComponent<EndLevelTrigger>();
        trigger.alphaCompletePanel = completePanel;
    }

    private static GameObject CreateDecorSprite(string name, Sprite sprite, Vector2 position, Vector2 targetSize, Color tint, int sortingOrder, bool flipX)
    {
        if (sprite == null)
            return null;

        Vector2 spriteSize = sprite.bounds.size;
        GameObject obj = CreateBoxObject(name, sprite, position, new Vector2(targetSize.x / Mathf.Max(0.01f, spriteSize.x), targetSize.y / Mathf.Max(0.01f, spriteSize.y)), "Default");
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        renderer.color = tint;
        renderer.sortingOrder = sortingOrder;
        renderer.flipX = flipX;
        return obj;
    }

    private class SubwayTiles
    {
        public TileBase floor;
        public TileBase floorFill;
        public TileBase wall;
        public TileBase wallDark;
        public TileBase platform;
        public TileBase platformFill;
        public Sprite pipe;
        public Sprite lamp;
        public Sprite greenDoor;
        public Sprite crate;
        public Sprite warningPoster;
        public Sprite arrowSign;
        public Sprite chainPlatform;
        public Sprite chains;
        public Sprite railing;
        public Sprite grate;
    }
}
#endif
