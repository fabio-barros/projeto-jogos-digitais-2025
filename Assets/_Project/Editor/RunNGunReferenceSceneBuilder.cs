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

public static class RunNGunReferenceSceneBuilder
{
    private const string BasePath = "Assets/_Project";
    private const string GeneratedPath = BasePath + "/Generated/RunNGunRelay";
    private const string ExternalPath = BasePath + "/External/RunNGunReference/SpriteSheets";
    private const string AnimationsPath = BasePath + "/Animations";
    private const string PrefabsPath = BasePath + "/Prefabs";
    private const string OriginalEnemyControllerPath = BasePath + "/External/RunNGunOriginal/Resources/Animations/Enemies/AR Enemy/AREnemyCont.controller";
    private const string ScenePath = "Assets/Scenes/Level_02_RunNGun_Relay.unity";
    private const string MirrorScenePath = BasePath + "/Scenes/Level_02_RunNGun_Relay.unity";
    private const string NeraPath = BasePath + "/Art/Nera/sprites";

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();

    [MenuItem("Remnant Squad/Generate RunNGun Relay Scene")]
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
        scene.name = "Level_02_RunNGun_Relay";

        RelayTiles tiles = LoadTiles();
        Sprite playerSprite = LoadSprite(NeraPath + "/whole body/idle alt/idle_alt0.png") ??
                              LoadSprite(NeraPath + "/whole body/crouch idle/crouch_idle0.png") ??
                              SliceSprite("Reference_PlayerFallback", ExternalPath + "/SpriteSheet_player.png", new RectInt(0, 0, 32, 32), 32f) ??
                              CreateColorSprite("Reference_PlayerFallback", new Color(0.12f, 0.72f, 1f, 1f));
        Sprite playerBullet = SliceSprite("Reference_PlayerBullet", ExternalPath + "/Bullets.png", new RectInt(0, 0, 16, 16), 32f) ??
                              LoadSprite(NeraPath + "/effects/bullet/bullet.png") ??
                              CreateColorSprite("Reference_PlayerBullet", Color.yellow);
        Sprite enemyBullet = SliceSprite("Reference_EnemyBullet", ExternalPath + "/Bullets.png", new RectInt(32, 0, 16, 16), 32f) ??
                             CreateColorSprite("Reference_EnemyBullet", new Color(1f, 0.2f, 0.05f, 1f));
        Sprite coinSprite = SliceSprite("Reference_Coin", ExternalPath + "/Coins.png", new RectInt(0, 0, 16, 16), 32f) ??
                            CreateColorSprite("Reference_Coin", new Color(1f, 0.82f, 0.12f, 1f));
        Sprite healthSprite = LoadGenericSprite(ExternalPath + "/HealthPickup.png", 32f) ??
                              CreateColorSprite("Reference_Health", new Color(0.1f, 1f, 0.2f, 1f));
        Sprite bombSprite = CreateColorSprite("Reference_BombPickup", new Color(0.1f, 0.7f, 1f, 1f));
        Sprite checkpointSprite = CreateColorSprite("Reference_Checkpoint", new Color(0.15f, 1f, 1f, 0.9f));
        Sprite endSprite = SliceSprite("Reference_Flag", ExternalPath + "/flag animation.png", new RectInt(0, 0, 32, 48), 32f) ??
                           CreateColorSprite("Reference_End", new Color(0.1f, 1f, 0.4f, 0.9f));
        Sprite explosionSprite = SliceSprite("Reference_Impact", ExternalPath + "/Bullets.png", new RectInt(64, 0, 16, 16), 32f) ??
                                 CreateColorSprite("Reference_Impact", new Color(1f, 0.55f, 0.1f, 1f));

        CreateCamera();
        CreateGameManager();
        CreateBackgrounds();

        GameObject bulletHitPrefab = CreateBulletHitPrefab(explosionSprite);
        GameObject playerProjectilePrefab = CreatePlayerProjectilePrefab(playerBullet, bulletHitPrefab);
        GameObject enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyBullet, bulletHitPrefab);
        GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);
        GameObject bombPrefab = CreateBombPrefab(CreateColorSprite("Reference_GrenadeBlue", new Color(0.05f, 0.85f, 1f, 1f)), explosionPrefab);
        GameObject deathPrefab = CreateEnemyDeathPrefab(explosionSprite);
        CreateRuntimePools(playerProjectilePrefab, enemyProjectilePrefab, bulletHitPrefab, explosionPrefab, deathPrefab);

        GameObject player = CreatePlayer(playerSprite, playerProjectilePrefab, bombPrefab);
        GameObject completePanel = CreateHUD(player.GetComponent<PlayerHealth>());

        BuildTilemapLevel(tiles);
        BuildDressing(tiles);
        BuildGameplay(coinSprite, healthSprite, bombSprite, checkpointSprite, endSprite, completePanel);
        BuildEnemies(enemyProjectilePrefab, deathPrefab);
        BuildWaypointGraph();

        Camera.main.GetComponent<CameraFollow2D>().target = player.transform;

        EditorSceneManager.SaveScene(scene, ScenePath);
        File.Copy(ScenePath, MirrorScenePath, true);
        AssetDatabase.ImportAsset(MirrorScenePath, ImportAssetOptions.ForceUpdate);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("RunNGun Relay generated. Open Assets/Scenes/Level_02_RunNGun_Relay.unity and press Play.");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "_Project");
        CreateFolderIfMissing(BasePath, "Generated");
        CreateFolderIfMissing(BasePath + "/Generated", "RunNGunRelay");
        CreateFolderIfMissing(GeneratedPath, "Sprites");
        CreateFolderIfMissing(GeneratedPath, "Tiles");
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
        EnsureLayer("Pickup");
        EnsureLayer("POW");
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

    private static RelayTiles LoadTiles()
    {
        string outside = ExternalPath + "/Level 1/tiles_out.png";
        string subway = ExternalPath + "/Level 2/Subway_tiles.png";
        return new RelayTiles
        {
            outdoorTop = CreateTile("Relay_OutdoorTop", SliceSprite("Relay_OutdoorTop", outside, new RectInt(16, 0, 16, 16), 16f)),
            outdoorFill = CreateTile("Relay_OutdoorFill", SliceSprite("Relay_OutdoorFill", outside, new RectInt(32, 304, 16, 16), 16f)),
            outdoorWall = CreateTile("Relay_OutdoorWall", SliceSprite("Relay_OutdoorWall", outside, new RectInt(0, 32, 16, 16), 16f)),
            outdoorPlatform = CreateTile("Relay_OutdoorPlatform", SliceSprite("Relay_OutdoorPlatform", outside, new RectInt(80, 32, 16, 16), 16f)),
            subwayTop = CreateTile("Relay_SubwayTop", SliceSprite("Relay_SubwayTop", subway, new RectInt(16, 112, 16, 16), 16f)),
            subwayFill = CreateTile("Relay_SubwayFill", SliceSprite("Relay_SubwayFill", subway, new RectInt(32, 128, 16, 16), 16f)),
            subwayWall = CreateTile("Relay_SubwayWall", SliceSprite("Relay_SubwayWall", subway, new RectInt(128, 160, 16, 16), 16f)),
            subwayPlatform = CreateTile("Relay_SubwayPlatform", SliceSprite("Relay_SubwayPlatform", subway, new RectInt(64, 96, 16, 16), 16f)),
            crate = SliceSprite("Relay_Crate", subway, new RectInt(112, 304, 48, 32), 16f),
            lamp = SliceSprite("Relay_Lamp", subway, new RectInt(48, 64, 16, 48), 16f),
            door = SliceSprite("Relay_Door", subway, new RectInt(304, 64, 64, 64), 16f),
            poster = SliceSprite("Relay_Poster", subway, new RectInt(176, 112, 32, 48), 16f),
            sign = SliceSprite("Relay_Sign", subway, new RectInt(288, 176, 32, 16), 16f),
            railing = SliceSprite("Relay_Railing", subway, new RectInt(0, 256, 64, 32), 16f),
            hangingPlatform = SliceSprite("Relay_HangingPlatform", subway, new RectInt(192, 64, 32, 32), 16f),
            chains = SliceSprite("Relay_Chains", subway, new RectInt(224, 0, 16, 96), 16f)
        };
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.65f;
        camera.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-8f, -1.4f, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.offset = new Vector3(1.75f, 0.75f, -10f);
        follow.smoothSpeed = 9f;
        follow.useBounds = true;
        follow.minBounds = new Vector2(-12f, -8f);
        follow.maxBounds = new Vector2(140f, 6f);
    }

    private static void CreateGameManager()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private static void CreateBackgrounds()
    {
        Sprite clouds1 = LoadGenericSprite(ExternalPath + "/Level 1/nuvens_1.png", 32f);
        Sprite clouds2 = LoadGenericSprite(ExternalPath + "/Level 1/nuvens_2.png", 32f);
        Sprite clouds3 = LoadGenericSprite(ExternalPath + "/Level 1/nuvens_3.png", 32f);
        Sprite subwayBg = LoadGenericSprite(ExternalPath + "/Level 2/subway_BG.png", 32f);
        Sprite subwayWall = LoadGenericSprite(ExternalPath + "/Level 2/wall_subway.png", 48f);

        for (int i = 0; i < 6; i++)
        {
            CreateDecorSprite("BG_CloudLayerA_" + i.ToString("00"), clouds1, new Vector2(-8f + i * 15f, 2.2f), new Vector2(14f, 5f), new Color(0.85f, 0.9f, 1f, 0.45f), -40, false);
            CreateDecorSprite("BG_CloudLayerB_" + i.ToString("00"), clouds2, new Vector2(-1f + i * 15f, 1.15f), new Vector2(12f, 4f), new Color(0.72f, 0.78f, 0.92f, 0.45f), -39, i % 2 == 0);
            CreateDecorSprite("BG_CloudLayerC_" + i.ToString("00"), clouds3, new Vector2(4f + i * 15f, 0.15f), new Vector2(9f, 2.5f), new Color(0.56f, 0.65f, 0.78f, 0.5f), -38, false);
        }

        for (int i = 0; i < 6; i++)
        {
            CreateDecorSprite("BG_SubwayMain_" + i.ToString("00"), subwayBg, new Vector2(55f + i * 14.8f, -0.2f), new Vector2(15.5f, 8.5f), new Color(0.58f, 0.78f, 0.55f, 0.82f), -37, false);
            CreateDecorSprite("BG_SubwayWall_" + i.ToString("00"), subwayWall, new Vector2(60f + i * 16.5f, -0.7f), new Vector2(10f, 8.5f), new Color(0.46f, 0.62f, 0.45f, 0.68f), -38, i % 2 == 1);
        }
    }

    private static void BuildTilemapLevel(RelayTiles tiles)
    {
        GameObject gridObject = new GameObject("Grid_Level02_RunNGunRelay");
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap collision = CreateTilemap(gridObject.transform, "TM_Collision", "Ground", 8, true, false);
        Tilemap visuals = CreateTilemap(gridObject.transform, "TM_Visuals", "Default", 9, false, false);
        Tilemap midground = CreateTilemap(gridObject.transform, "TM_BackgroundArchitecture", "Default", -5, false, false);
        Tilemap platforms = CreateTilemap(gridObject.transform, "TM_OneWayPlatforms", "Ground", 10, true, true);

        CreateGround(collision, visuals, tiles.outdoorTop, tiles.outdoorFill, -14, -5, 30, 3);
        CreateGround(collision, visuals, tiles.outdoorTop, tiles.outdoorFill, 16, -4, 16, 2);
        CreateGround(collision, visuals, tiles.outdoorTop, tiles.outdoorFill, 32, -3, 14, 2);
        CreateGround(collision, visuals, tiles.subwayTop, tiles.subwayFill, 46, -4, 22, 3);
        CreateGround(collision, visuals, tiles.subwayTop, tiles.subwayFill, 68, -6, 20, 3);
        CreateGround(collision, visuals, tiles.subwayTop, tiles.subwayFill, 88, -4, 24, 3);
        CreateGround(collision, visuals, tiles.subwayTop, tiles.subwayFill, 112, -3, 20, 2);
        CreateGround(collision, visuals, tiles.subwayTop, tiles.subwayFill, 132, -4, 12, 3);

        CreateWall(collision, visuals, tiles.outdoorWall, 15, -5, 1, 2);
        CreateWall(collision, visuals, tiles.outdoorWall, 31, -4, 1, 1);
        CreateWall(collision, visuals, tiles.subwayWall, 45, -4, 1, 2);
        CreateWall(collision, visuals, tiles.subwayWall, 67, -6, 1, 3);
        CreateWall(collision, visuals, tiles.subwayWall, 87, -6, 1, 3);
        CreateWall(collision, visuals, tiles.subwayWall, 111, -4, 1, 2);
        CreateWall(collision, visuals, tiles.subwayWall, 131, -4, 1, 1);

        CreateOneWayPlatform(platforms, tiles.outdoorPlatform, -2, -1, 8);
        CreateOneWayPlatform(platforms, tiles.outdoorPlatform, 10, 1, 6);
        CreateOneWayPlatform(platforms, tiles.outdoorPlatform, 24, 0, 8);
        CreateOneWayPlatform(platforms, tiles.subwayPlatform, 52, -1, 8);
        CreateOneWayPlatform(platforms, tiles.subwayPlatform, 70, -2, 6);
        CreateOneWayPlatform(platforms, tiles.subwayPlatform, 81, 0, 8);
        CreateOneWayPlatform(platforms, tiles.subwayPlatform, 102, 1, 9);
        CreateOneWayPlatform(platforms, tiles.subwayPlatform, 121, 0, 8);

        FillRect(midground, tiles.subwayWall, 48, -1, 84, 5);
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
            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tilemapCollider = obj.AddComponent<TilemapCollider2D>();
            CompositeCollider2D composite = obj.AddComponent<CompositeCollider2D>();
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

    private static void CreateGround(Tilemap collision, Tilemap visual, TileBase top, TileBase fill, int x, int y, int width, int height)
    {
        FillRect(collision, fill, x, y, width, height);
        for (int ix = x; ix < x + width; ix++)
        {
            visual.SetTile(new Vector3Int(ix, y + height - 1, 0), top);
            for (int iy = y; iy < y + height - 1; iy++)
                visual.SetTile(new Vector3Int(ix, iy, 0), fill);
        }
    }

    private static void CreateWall(Tilemap collision, Tilemap visual, TileBase wall, int x, int y, int width, int height)
    {
        FillRect(collision, wall, x, y, width, height);
        FillRect(visual, wall, x, y, width, height);
    }

    private static void CreateOneWayPlatform(Tilemap platforms, TileBase tile, int x, int y, int width)
    {
        for (int ix = x; ix < x + width; ix++)
            platforms.SetTile(new Vector3Int(ix, y, 0), tile);
    }

    private static void FillRect(Tilemap map, TileBase tile, int x, int y, int width, int height)
    {
        if (tile == null)
            return;

        for (int ix = x; ix < x + width; ix++)
        {
            for (int iy = y; iy < y + height; iy++)
                map.SetTile(new Vector3Int(ix, iy, 0), tile);
        }
    }

    private static void BuildDressing(RelayTiles tiles)
    {
        for (int i = 0; i < 8; i++)
            CreateDecorSprite("Relay_Lamp_" + i.ToString("00"), tiles.lamp, new Vector2(50f + i * 10f, 1.15f), new Vector2(0.7f, 2.1f), Color.white, 12, i % 2 == 1);

        CreateDecorSprite("Relay_EntryDoor", tiles.door, new Vector2(47.2f, -2.35f), new Vector2(3.2f, 3.2f), Color.white, 2, false);
        CreateDecorSprite("Relay_ExitDoor", tiles.door, new Vector2(134f, -1.35f), new Vector2(3.2f, 3.2f), Color.white, 2, true);
        CreateDecorSprite("Relay_Poster_01", tiles.poster, new Vector2(57f, -1.4f), new Vector2(1f, 1.5f), Color.white, 11, false);
        CreateDecorSprite("Relay_Sign_Forward_01", tiles.sign, new Vector2(63f, -0.6f), new Vector2(1.4f, 0.7f), Color.white, 11, false);
        CreateDecorSprite("Relay_Sign_Forward_02", tiles.sign, new Vector2(117f, 0.4f), new Vector2(1.4f, 0.7f), Color.white, 11, false);

        CreateDecorSprite("Relay_Crates_Intro", tiles.crate, new Vector2(8f, -3.2f), new Vector2(2.2f, 1.45f), Color.white, 12, false);
        CreateDecorSprite("Relay_Crates_Ramp", tiles.crate, new Vector2(37f, -1.7f), new Vector2(2.2f, 1.45f), Color.white, 12, true);
        CreateDecorSprite("Relay_Crates_SubwayLow", tiles.crate, new Vector2(76f, -4.2f), new Vector2(2.2f, 1.45f), Color.white, 12, false);
        CreateDecorSprite("Relay_Railing_Final", tiles.railing, new Vector2(125f, -2.0f), new Vector2(4f, 1.2f), Color.white, 12, false);

        for (int i = 0; i < 4; i++)
        {
            float x = 93f + i * 5f;
            CreateDecorSprite("Relay_Chains_" + i.ToString("00"), tiles.chains, new Vector2(x, 1.6f), new Vector2(0.55f, 4.8f), Color.white, 7, false);
            CreateDecorSprite("Relay_HangingPlatform_" + i.ToString("00"), tiles.hangingPlatform, new Vector2(x, -0.35f - (i % 2) * 1.4f), new Vector2(1.3f, 1.3f), Color.white, 11, false);
        }
    }

    private static void BuildGameplay(Sprite coin, Sprite health, Sprite bomb, Sprite checkpoint, Sprite end, GameObject completePanel)
    {
        CreateCheckpoint("Checkpoint_Relay_Intro", checkpoint, new Vector2(-6f, -2.2f));
        CreateCheckpoint("Checkpoint_Relay_Subway", checkpoint, new Vector2(54f, -0.2f));
        CreateCheckpoint("Checkpoint_Relay_Final", checkpoint, new Vector2(116f, -1.2f));

        CreatePickup("Pickup_Coin_Intro_01", coin, new Vector2(4f, -2.1f), 0, 0, 0, 100);
        CreatePickup("Pickup_Coin_Intro_02", coin, new Vector2(12f, 0.0f), 0, 0, 0, 100);
        CreatePickup("Pickup_Ammo_Ramp", coin, new Vector2(29f, 0.95f), 0, 20, 0, 100);
        CreatePickup("Pickup_Health_Subway", health, new Vector2(73f, -1.05f), 1, 0, 0, 150);
        CreatePickup("Pickup_Bombs_Shaft", bomb, new Vector2(105f, 2.0f), 0, 0, 2, 150);
        CreatePickup("Pickup_Ammo_Final", coin, new Vector2(126f, 1.05f), 0, 20, 0, 100);
        CreateEndTrigger(end, new Vector2(140f, -1.4f), completePanel);
    }

    private static void BuildEnemies(GameObject projectilePrefab, GameObject deathPrefab)
    {
        Sprite[] gruntFrames = SliceHorizontalFrames("Relay_ARGrunt", ExternalPath + "/ARMob.png", 32, 38, 24, 24f);
        Sprite[] bruteFrames = SliceHorizontalFrames("Relay_RPGBrute", ExternalPath + "/RPGmob.png", 44, 44, 10, 24f);
        RuntimeAnimatorController gruntController = CreateEnemyAnimator("Relay_ARGrunt", gruntFrames);
        RuntimeAnimatorController bruteController = CreateEnemyAnimator("Relay_RPGBrute", bruteFrames);

        GameObject gruntPrefab = CreateEnemyPrefab("RelayGruntPrefab", gruntFrames[0], gruntController, projectilePrefab, deathPrefab, 2, 120, new Vector2(0.7f, 1.0f), 1.6f, 1.1f);
        GameObject brutePrefab = CreateEnemyPrefab("RelayBrutePrefab", bruteFrames[0], bruteController, projectilePrefab, deathPrefab, 8, 450, new Vector2(1.0f, 1.18f), 1.1f, 1.9f);

        CreateWave("Wave_01_FirstContact", 4f, 0.2f, 0.45f, new GameObject[] { gruntPrefab, gruntPrefab, gruntPrefab }, new Vector2[] { new Vector2(10f, -1.8f), new Vector2(13f, -1.8f), new Vector2(-3f, -2.8f) }, new int[] { -1, -1, 1 });
        CreateWave("Wave_02_RampCrossfire", 26f, 0.1f, 0.35f, new GameObject[] { gruntPrefab, gruntPrefab, gruntPrefab, brutePrefab }, new Vector2[] { new Vector2(36f, -0.8f), new Vector2(41f, -0.8f), new Vector2(22f, -2.7f), new Vector2(44f, -0.8f) }, new int[] { -1, -1, 1, -1 });
        CreateWave("Wave_03_SubwayEntry", 51f, 0.1f, 0.33f, new GameObject[] { gruntPrefab, gruntPrefab, gruntPrefab, gruntPrefab }, new Vector2[] { new Vector2(61f, -1.8f), new Vector2(64f, -1.8f), new Vector2(48f, -1.8f), new Vector2(55f, 0.15f) }, new int[] { -1, -1, 1, 1 });
        CreateWave("Wave_04_LowTunnel", 75f, 0.15f, 0.3f, new GameObject[] { gruntPrefab, gruntPrefab, brutePrefab, gruntPrefab, gruntPrefab }, new Vector2[] { new Vector2(82f, -3.8f), new Vector2(86f, -3.8f), new Vector2(89f, -3.8f), new Vector2(72f, -3.8f), new Vector2(75f, -1.2f) }, new int[] { -1, -1, -1, 1, 1 });
        CreateWave("Wave_05_FinalPincer", 108f, 0.05f, 0.28f, new GameObject[] { gruntPrefab, gruntPrefab, gruntPrefab, brutePrefab, gruntPrefab, gruntPrefab }, new Vector2[] { new Vector2(119f, -1.8f), new Vector2(123f, -1.8f), new Vector2(128f, -0.8f), new Vector2(132f, -0.8f), new Vector2(103f, -1.8f), new Vector2(98f, 0.2f) }, new int[] { -1, -1, -1, -1, 1, 1 });
    }

    private static GameObject CreateEnemyPrefab(string prefabName, Sprite sprite, RuntimeAnimatorController controller, GameObject projectilePrefab, GameObject deathPrefab, int health, int score, Vector2 visualScale, float speed, float cooldown)
    {
        GameObject enemy = CreateBoxObject(prefabName, sprite, Vector2.zero, visualScale, "Enemy");
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 18;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.58f, 0.92f);
        collider.offset = new Vector2(0f, -0.05f);

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = health;
        damageable.scoreValue = score;
        damageable.deathDelay = 0.25f;
        damageable.deathEffectPrefab = deathPrefab;

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(enemy.transform);
        groundCheck.localPosition = new Vector3(0.36f, -0.58f, 0f);
        Transform wallCheck = new GameObject("WallCheck").transform;
        wallCheck.SetParent(enemy.transform);
        wallCheck.localPosition = new Vector3(0.44f, -0.04f, 0f);

        EnemyPatrol2D patrol = enemy.AddComponent<EnemyPatrol2D>();
        patrol.moveSpeed = speed;
        patrol.groundCheck = groundCheck;
        patrol.wallCheck = wallCheck;
        patrol.groundLayer = LayerMask.GetMask("Ground");
        patrol.obstacleLayer = LayerMask.GetMask("Ground");
        patrol.jumpForce = 6.7f;
        patrol.canJumpObstacles = false;
        patrol.useRunNGunStationaryBehaviour = false;
        patrol.useWaypointNavigation = true;
        patrol.separationRadius = 1.2f;
        patrol.separationStrength = 0.85f;

        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(enemy.transform);
        firePoint.localPosition = new Vector3(0.55f, 0.1f, 0f);
        EnemyShooter2D shooter = enemy.AddComponent<EnemyShooter2D>();
        shooter.enemyProjectilePrefab = projectilePrefab;
        shooter.firePoint = firePoint;
        shooter.obstacleLayers = LayerMask.GetMask("Ground");
        shooter.range = 10f;
        shooter.fireCooldown = health > 3 ? 0.28f : 0.22f;
        shooter.burstCount = health > 3 ? 2 : 1;
        shooter.horizontalShotHeight = 0.3f;
        shooter.verticalShotOffset = 0.78f;
        shooter.allowVerticalShots = true;
        shooter.useRunNGunShootCycle = true;
        shooter.activeShootTime = health > 3 ? 0.9f : 0.5f;
        shooter.waitShootTime = health > 3 ? 0.5f : 1f;

        Animator animator = enemy.AddComponent<Animator>();
        RuntimeAnimatorController originalEnemyController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(OriginalEnemyControllerPath);
        animator.runtimeAnimatorController = originalEnemyController != null ? originalEnemyController : controller;
        enemy.AddComponent<EnemyAnimationDriver>();

        return SavePrefab(enemy, prefabName);
    }

    private static void CreateWave(string name, float cameraX, float initialDelay, float interval, GameObject[] prefabs, Vector2[] positions, int[] directions)
    {
        GameObject triggerObject = new GameObject(name);
        EnemyWaveTrigger2D trigger = triggerObject.AddComponent<EnemyWaveTrigger2D>();
        trigger.triggerWhenCameraReachesX = true;
        trigger.cameraXTrigger = cameraX;
        trigger.initialDelay = initialDelay;
        trigger.spawnInterval = interval;
        trigger.spawnBlockLayers = LayerMask.GetMask("Ground");
        trigger.spawnObjects = new GameObject[prefabs.Length];
        trigger.spawnPoints = new Transform[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            Vector2 position = positions[Mathf.Min(i, positions.Length - 1)];
            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
            enemy.name = name + "_Enemy_" + i.ToString("00");
            enemy.transform.position = position;
            SetEnemyDirection(enemy, directions[Mathf.Min(i, directions.Length - 1)]);
            enemy.SetActive(false);
            trigger.spawnObjects[i] = enemy;

            GameObject point = new GameObject(name + "_SpawnPoint_" + i.ToString("00"));
            point.transform.SetParent(triggerObject.transform);
            point.transform.position = position;
            trigger.spawnPoints[i] = point.transform;
        }
    }

    private static void SetEnemyDirection(GameObject enemy, int direction)
    {
        int facing = direction >= 0 ? 1 : -1;
        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol != null)
            patrol.startingDirection = facing;
        Vector3 scale = enemy.transform.localScale;
        enemy.transform.localScale = new Vector3(Mathf.Abs(scale.x) * facing, scale.y, scale.z);
    }

    private static RuntimeAnimatorController CreateEnemyAnimator(string name, Sprite[] frames)
    {
        string controllerPath = AnimationsPath + "/" + name + ".controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        EnsureEnemyAnimatorParameters(controller);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        for (int i = machine.states.Length - 1; i >= 0; i--)
            machine.RemoveState(machine.states[i].state);

        AnimationClip idle = CreateSpriteClip(name + "_Idle", frames, 0, Mathf.Min(3, frames.Length - 1), 7f, true);
        AnimationClip walk = CreateSpriteClip(name + "_Walk", frames, 0, Mathf.Min(7, frames.Length - 1), 10f, true);
        AnimationClip shoot = CreateSpriteClip(name + "_Shoot", frames, Mathf.Min(8, frames.Length - 1), Mathf.Min(11, frames.Length - 1), 12f, true);
        AnimationClip death = CreateSpriteClip(name + "_Death", frames, Mathf.Max(0, frames.Length - 4), frames.Length - 1, 10f, false);

        AnimatorState idleState = machine.AddState("Idle", new Vector3(220f, 0f, 0f));
        idleState.motion = idle;
        AnimatorState walkState = machine.AddState("Walk", new Vector3(220f, 70f, 0f));
        walkState.motion = walk;
        AnimatorState shootState = machine.AddState("Shoot", new Vector3(220f, 140f, 0f));
        shootState.motion = shoot;
        AnimatorState deathState = machine.AddState("Death", new Vector3(220f, 210f, 0f));
        deathState.motion = death;
        machine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
        AnimatorStateTransition idleToShoot = idleState.AddTransition(shootState);
        idleToShoot.hasExitTime = false;
        idleToShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shooting");
        AnimatorStateTransition walkToShoot = walkState.AddTransition(shootState);
        walkToShoot.hasExitTime = false;
        walkToShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shooting");
        AnimatorStateTransition shootToIdle = shootState.AddTransition(idleState);
        shootToIdle.hasExitTime = false;
        shootToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Shooting");
        AnimatorStateTransition anyToDeath = machine.AddAnyStateTransition(deathState);
        anyToDeath.hasExitTime = false;
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void EnsureEnemyAnimatorParameters(AnimatorController controller)
    {
        EnsureParameter(controller, "Moving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Shooting", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Dead", AnimatorControllerParameterType.Bool);
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name)
                return;
        }
        controller.AddParameter(name, type);
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

        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
        float step = 1f / frameRate;
        for (int i = start; i <= end && i < frames.Length; i++)
        {
            if (frames[i] == null)
                continue;
            keyframes.Add(new ObjectReferenceKeyframe { time = (i - start) * step, value = frames[i] });
        }

        if (keyframes.Count == 0 && frames.Length > 0 && frames[0] != null)
            keyframes.Add(new ObjectReferenceKeyframe { time = 0f, value = frames[0] });

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void BuildWaypointGraph()
    {
        GameObject graphObject = new GameObject("Enemy_WaypointGraph_RunNGunRelay");
        graphObject.AddComponent<EnemyWaypointGraph2D>();
        Vector2[] positions =
        {
            new Vector2(-8f, -2.2f), new Vector2(10f, -2.2f), new Vector2(24f, -1.2f),
            new Vector2(38f, -0.2f), new Vector2(56f, -1.2f), new Vector2(76f, -3.2f),
            new Vector2(96f, -1.2f), new Vector2(120f, -0.2f), new Vector2(136f, -1.2f)
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

    private static GameObject CreatePlayer(Sprite sprite, GameObject projectilePrefab, GameObject bombPrefab)
    {
        GameObject player = CreateBoxObject("Player_Nera", sprite, new Vector2(-10f, -2.2f), Vector2.one, "Player");
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 24;

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
        controller.crouchMoveMultiplier = 0.45f;
        controller.jumpForce = 11.2f;
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

        Transform throwPoint = new GameObject("ThrowPoint").transform;
        throwPoint.SetParent(player.transform);
        throwPoint.localPosition = new Vector3(0.52f, 0.22f, 0f);
        PlayerBombThrower2D bombs = player.AddComponent<PlayerBombThrower2D>();
        bombs.bombPrefab = bombPrefab;
        bombs.throwPoint = throwPoint;
        bombs.maxBombs = 3;

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationsPath + "/PlayerVolkov.controller");
        player.AddComponent<PlayerAnimationDriver>();
        return player;
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

        GameObject completePanel = new GameObject("LevelCompletePanel");
        completePanel.transform.SetParent(canvasObject.transform);
        RectTransform completeRect = completePanel.AddComponent<RectTransform>();
        completeRect.anchorMin = Vector2.zero;
        completeRect.anchorMax = Vector2.one;
        completeRect.offsetMin = Vector2.zero;
        completeRect.offsetMax = Vector2.zero;
        completePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        Text completeText = CreateUIText("CompleteText", completePanel.transform, "RUNNGUN RELAY COMPLETE", Vector2.zero, TextAnchor.MiddleCenter, 42, font);
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
        rect.sizeDelta = alignment == TextAnchor.MiddleCenter ? new Vector2(760f, 220f) : new Vector2(360f, 36f);
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
        GameObject pickup = CreateBoxObject(name, sprite, position, new Vector2(0.5f, 0.5f), "Pickup");
        pickup.AddComponent<BoxCollider2D>().isTrigger = true;
        Pickup2D pickup2D = pickup.AddComponent<Pickup2D>();
        pickup2D.healthReward = healthReward;
        pickup2D.ammoReward = ammoReward;
        pickup2D.bombReward = bombReward;
        pickup2D.scoreReward = scoreReward;
    }

    private static void CreateEndTrigger(Sprite sprite, Vector2 position, GameObject completePanel)
    {
        GameObject end = CreateBoxObject("End_Level_Trigger", sprite, position, new Vector2(0.9f, 1.6f), "POW");
        end.AddComponent<BoxCollider2D>().isTrigger = true;
        EndLevelTrigger trigger = end.AddComponent<EndLevelTrigger>();
        trigger.alphaCompletePanel = completePanel;
    }

    private static GameObject CreatePlayerProjectilePrefab(Sprite sprite, GameObject hitPrefab)
    {
        GameObject projectile = CreateBoxObject("PlayerProjectile_Relay", sprite, Vector2.zero, new Vector2(0.56f, 0.28f), "Projectile");
        projectile.AddComponent<BoxCollider2D>().isTrigger = true;
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Projectile2D projectile2D = projectile.AddComponent<Projectile2D>();
        projectile2D.speed = 10f;
        projectile2D.damage = 1;
        projectile2D.hitEffectPrefab = hitPrefab;
        projectile2D.hitLayers = LayerMask.GetMask("Enemy", "Ground");
        return SavePrefab(projectile, "PlayerProjectile_Relay");
    }

    private static GameObject CreateEnemyProjectilePrefab(Sprite sprite, GameObject hitPrefab)
    {
        GameObject projectile = CreateBoxObject("EnemyProjectile_Relay", sprite, Vector2.zero, new Vector2(0.44f, 0.22f), "Projectile");
        projectile.AddComponent<BoxCollider2D>().isTrigger = true;
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        EnemyProjectile2D projectile2D = projectile.AddComponent<EnemyProjectile2D>();
        projectile2D.speed = 7f;
        projectile2D.damage = 1;
        projectile2D.hitEffectPrefab = hitPrefab;
        projectile2D.hitLayers = LayerMask.GetMask("Player", "Ground");
        return SavePrefab(projectile, "EnemyProjectile_Relay");
    }

    private static GameObject CreateBulletHitPrefab(Sprite sprite)
    {
        GameObject hit = CreateBoxObject("BulletHit_Relay", sprite, Vector2.zero, new Vector2(0.65f, 0.65f), "Projectile");
        SpriteAnimationOnce animation = hit.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = hit.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 18f;
        return SavePrefab(hit, "BulletHit_Relay");
    }

    private static GameObject CreateExplosionPrefab(Sprite sprite)
    {
        GameObject explosion = CreateBoxObject("Explosion_Relay", sprite, Vector2.zero, new Vector2(1.8f, 1.8f), "Projectile");
        SpriteAnimationOnce animation = explosion.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = explosion.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 18f;
        return SavePrefab(explosion, "Explosion_Relay");
    }

    private static GameObject CreateBombPrefab(Sprite sprite, GameObject explosionPrefab)
    {
        GameObject bomb = CreateBoxObject("Bomb_Relay", sprite, Vector2.zero, new Vector2(0.34f, 0.34f), "Projectile");
        bomb.AddComponent<CircleCollider2D>();
        Rigidbody2D rb = bomb.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BombProjectile2D bomb2D = bomb.AddComponent<BombProjectile2D>();
        bomb2D.explosionVisual = explosionPrefab;
        bomb2D.damageLayers = LayerMask.GetMask("Enemy");
        bomb2D.explosionRadius = 2.2f;
        bomb2D.damage = 3;
        return SavePrefab(bomb, "Bomb_Relay");
    }

    private static GameObject CreateEnemyDeathPrefab(Sprite sprite)
    {
        GameObject death = CreateBoxObject("EnemyDeath_Relay", sprite, Vector2.zero, new Vector2(1.25f, 1.25f), "Projectile");
        SpriteAnimationOnce animation = death.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = death.GetComponent<SpriteRenderer>();
        animation.frames = new Sprite[] { sprite };
        animation.frameRate = 12f;
        return SavePrefab(death, "EnemyDeath_Relay");
    }

    private static void CreateRuntimePools(params GameObject[] prefabs)
    {
        GameObject pools = new GameObject("Runtime_Pools");
        ObjectPool2D pool = pools.AddComponent<ObjectPool2D>();
        for (int i = 0; i < prefabs.Length; i++)
            pool.Prewarm(prefabs[i], 24);
        pools.AddComponent<PhysicsLayerSetup2D>();
    }

    private static GameObject SavePrefab(GameObject obj, string name)
    {
        string path = PrefabsPath + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
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

    private class RelayTiles
    {
        public TileBase outdoorTop;
        public TileBase outdoorFill;
        public TileBase outdoorWall;
        public TileBase outdoorPlatform;
        public TileBase subwayTop;
        public TileBase subwayFill;
        public TileBase subwayWall;
        public TileBase subwayPlatform;
        public Sprite crate;
        public Sprite lamp;
        public Sprite door;
        public Sprite poster;
        public Sprite sign;
        public Sprite railing;
        public Sprite hangingPlatform;
        public Sprite chains;
    }
}
#endif
