#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
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
    private const string AnimationsPath = BasePath + "/Animations";
    private const string NeraPath = BasePath + "/Art/Nera/sprites";

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

        ImportNeraSpritesIfPresent();

        Sprite playerSprite = LoadSprite(NeraPath + "/legs/idle/leg_idle.png");
        if (playerSprite == null)
            playerSprite = CreateColorSprite("Sprite_Player_Volkov", new Color(0.1f, 0.7f, 1f, 1f));

        Sprite enemySprite = CreateColorSprite("Sprite_Keth_Grunt", new Color(0.9f, 0.2f, 0.25f, 1f));
        Sprite bruteSprite = CreateColorSprite("Sprite_Keth_Brute", new Color(0.6f, 0.1f, 0.8f, 1f));
        Sprite groundSprite = CreateColorSprite("Sprite_Ground", new Color(0.3f, 0.3f, 0.3f, 1f));
        Sprite projectileSprite = LoadSprite(NeraPath + "/effects/bullet/bullet.png");
        if (projectileSprite == null)
            projectileSprite = CreateColorSprite("Sprite_Projectile", new Color(1f, 0.95f, 0.2f, 1f));

        Sprite enemyProjectileSprite = CreateColorSprite("Sprite_Enemy_Projectile", new Color(1f, 0.2f, 0.1f, 1f));
        Sprite bombSprite = CreateColorSprite("Sprite_Bomb_Visible", new Color(0.05f, 1f, 0.95f, 1f));
        Sprite explosionSprite = CreateColorSprite("Sprite_Explosion", new Color(1f, 0.55f, 0.05f, 1f));
        Sprite powSprite = CreateColorSprite("Sprite_POW", new Color(1f, 0.85f, 0.15f, 1f));
        Sprite endSprite = CreateColorSprite("Sprite_EndTrigger", new Color(0.1f, 1f, 0.3f, 0.55f));
        Sprite hazardSprite = CreateColorSprite("Sprite_Hazard_Acid", new Color(0.1f, 0.95f, 0.22f, 0.9f));
        Sprite coverSprite = CreateColorSprite("Sprite_Cover", new Color(0.12f, 0.16f, 0.18f, 1f));
        Sprite checkpointSprite = CreateColorSprite("Sprite_Checkpoint", new Color(0.05f, 0.95f, 1f, 1f));
        Sprite pickupHealthSprite = CreateColorSprite("Sprite_Pickup_Health", new Color(0.15f, 1f, 0.2f, 1f));
        Sprite pickupAmmoSprite = CreateColorSprite("Sprite_Pickup_Ammo", new Color(1f, 0.92f, 0.15f, 1f));
        Sprite pickupBombSprite = CreateColorSprite("Sprite_Pickup_Bomb", new Color(0.2f, 0.75f, 1f, 1f));
        Sprite scenerySprite = CreateColorSprite("Sprite_Background_Structure", new Color(0.06f, 0.08f, 0.11f, 1f));
        Sprite waterSprite = CreateColorSprite("Sprite_Water_Lake", new Color(0.1f, 0.55f, 0.95f, 0.68f));
        Sprite hoverVehicleSprite = CreateColorSprite("Sprite_Player_HoverVehicle", new Color(0.82f, 0.92f, 1f, 1f));
        Sprite companionSprite = CreateColorSprite("Sprite_Companion", new Color(0.95f, 0.9f, 0.72f, 1f));

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        CreateCamera();

        GameObject bulletHitPrefab = CreateBulletHitPrefab();
        GameObject playerProjectilePrefab = CreatePlayerProjectilePrefab(projectileSprite, bulletHitPrefab);
        GameObject enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyProjectileSprite, bulletHitPrefab);
        GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);
        GameObject enemyDeathPrefab = CreateEnemyDeathPrefab(explosionSprite);
        GameObject bombPrefab = CreateBombPrefab(bombSprite, explosionPrefab);

        GameObject player = CreatePlayer(playerSprite, playerProjectilePrefab, bombPrefab);

        GameObject completePanel = CreateHUD(player.GetComponent<PlayerHealth>());
        CreateDetailedLevel(groundSprite, coverSprite, scenerySprite, hazardSprite, waterSprite, hoverVehicleSprite, companionSprite, checkpointSprite, pickupHealthSprite, pickupAmmoSprite, pickupBombSprite, enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, playerProjectilePrefab, player, powSprite, endSprite, completePanel);

        Camera.main.GetComponent<CameraFollow2D>().target = player.transform;

        string scenePath = ScenesPath + "/Level_01_Alpha.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        const string successMessage = "Level_01_Alpha was generated successfully.\n\nOpen Assets/_Project/Scenes/Level_01_Alpha and press Play.\n\nDemo route:\nTraining Outpost > Broken Bridge > POW Camp > Acid Trench > Brute Gate > Lake Descent > Vehicle Depot > Runway Extraction.\n\nControls:\nA/D or Left Stick = Move\nW/S or Left Stick = Swim/vehicle vertical\nMouse, Arrow Keys, or Right Stick = Aim\nLeft Click, F, or X Button = Shoot\nSpace/W or A Button = Jump\nT or Y Button = Reload\nR or B Button = Bomb\nLeft Shift or LB = Dash\nE or RB = Rescue / Ride";

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
        CreateFolderIfMissing(BasePath, "Animations");
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
        EnsureLayer("Pickup");
        EnsureLayer("Hazard");
        EnsureLayer("Water");
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

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite nestedSprite = assets[i] as Sprite;
            if (nestedSprite != null)
                return nestedSprite;
        }

        assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite nestedSprite = assets[i] as Sprite;
            if (nestedSprite != null)
                return nestedSprite;
        }

        return null;
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

        if (count == sprites.Length)
            return sprites;

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

    private static void ImportNeraSpritesIfPresent()
    {
        if (!AssetDatabase.IsValidFolder(BasePath + "/Art/Nera"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { BasePath + "/Art/Nera" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 50;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
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

    private static GameObject CreateGround(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject ground = CreateBoxObject(name, sprite, position, scale, "Ground");
        ground.AddComponent<BoxCollider2D>().size = Vector2.one;
        return ground;
    }

    private static GameObject CreateGround(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject ground = CreateGround(name, sprite, position, scale);
        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = color;

        return ground;
    }

    private static GameObject CreateOneWayPlatform(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject platform = CreateBoxObject(name, sprite, position, scale, "Ground");
        SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = color;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.usedByEffector = true;

        PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 160f;
        return platform;
    }

    private static GameObject CreateGroundVehicle(string name, Sprite sprite, Vector2 position)
    {
        GameObject vehicle = new GameObject(name);
        vehicle.transform.position = position;
        vehicle.layer = LayerMask.NameToLayer("Ground");

        CreateVehiclePart(name + "_Body", sprite, vehicle.transform, new Vector2(0f, 0.2f), new Vector2(3.3f, 0.95f), new Color(0.78f, 0.88f, 0.82f, 1f), 3);
        CreateVehiclePart(name + "_Turret", sprite, vehicle.transform, new Vector2(0.45f, 0.85f), new Vector2(1.25f, 0.45f), new Color(0.68f, 0.8f, 0.76f, 1f), 4);
        CreateVehiclePart(name + "_Cannon", sprite, vehicle.transform, new Vector2(1.4f, 0.88f), new Vector2(1.25f, 0.18f), new Color(0.52f, 0.62f, 0.62f, 1f), 4);
        CreateVehiclePart(name + "_Tread_Left", sprite, vehicle.transform, new Vector2(-0.85f, -0.35f), new Vector2(0.9f, 0.32f), new Color(0.55f, 0.58f, 0.58f, 1f), 4);
        CreateVehiclePart(name + "_Tread_Right", sprite, vehicle.transform, new Vector2(0.85f, -0.35f), new Vector2(0.9f, 0.32f), new Color(0.55f, 0.58f, 0.58f, 1f), 4);

        BoxCollider2D collider = vehicle.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3.3f, 0.9f);
        collider.offset = new Vector2(0f, 0.2f);

        SimpleMover2D mover = vehicle.AddComponent<SimpleMover2D>();
        mover.localMoveOffset = new Vector2(2.6f, 0f);
        mover.speed = 0.55f;

        CreateWorldText(name + "_Label", "GROUND VEHICLE", new Vector3(position.x, position.y + 1.65f, 0f), 0.2f).transform.SetParent(vehicle.transform);
        return vehicle;
    }

    private static GameObject CreatePlane(string name, Sprite sprite, Vector2 position)
    {
        GameObject plane = new GameObject(name);
        plane.transform.position = position;

        CreateVehiclePart(name + "_Fuselage", sprite, plane.transform, Vector2.zero, new Vector2(4.4f, 0.48f), new Color(0.78f, 0.86f, 0.95f, 0.85f), -2);
        CreateVehiclePart(name + "_Wing", sprite, plane.transform, new Vector2(-0.2f, -0.05f), new Vector2(2.4f, 0.22f), new Color(0.68f, 0.78f, 0.9f, 0.85f), -1);
        CreateVehiclePart(name + "_Nose", sprite, plane.transform, new Vector2(2.25f, 0f), new Vector2(0.55f, 0.36f), new Color(0.92f, 0.95f, 1f, 0.85f), -1);
        CreateVehiclePart(name + "_Tail", sprite, plane.transform, new Vector2(-2.1f, 0.35f), new Vector2(0.55f, 0.65f), new Color(0.68f, 0.78f, 0.9f, 0.85f), -1);

        SimpleMover2D mover = plane.AddComponent<SimpleMover2D>();
        mover.localMoveOffset = new Vector2(-7f, 0.45f);
        mover.speed = 0.38f;

        CreateWorldText(name + "_Label", "PLANE FLYBY", new Vector3(position.x, position.y + 0.95f, 0f), 0.2f).transform.SetParent(plane.transform);
        return plane;
    }

    private static void CreateVehiclePart(string name, Sprite sprite, Transform parent, Vector2 localPosition, Vector2 scale, Color color, int sortingOrder)
    {
        GameObject part = CreateBoxObject(name, sprite, Vector2.zero, scale, "Ground");
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;

        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static void CreateDetailedLevel(
        Sprite groundSprite,
        Sprite coverSprite,
        Sprite scenerySprite,
        Sprite hazardSprite,
        Sprite waterSprite,
        Sprite hoverVehicleSprite,
        Sprite companionSprite,
        Sprite checkpointSprite,
        Sprite pickupHealthSprite,
        Sprite pickupAmmoSprite,
        Sprite pickupBombSprite,
        Sprite enemySprite,
        Sprite bruteSprite,
        GameObject enemyProjectilePrefab,
        GameObject enemyDeathPrefab,
        GameObject playerProjectilePrefab,
        GameObject player,
        Sprite powSprite,
        Sprite endSprite,
        GameObject completePanel)
    {
        Color floorColor = new Color(0.22f, 0.58f, 0.42f, 1f);
        Color platformColor = new Color(0.72f, 0.66f, 0.28f, 1f);
        Color coverColor = new Color(0.48f, 0.62f, 0.68f, 1f);

        CreateWorldText("Level_Title", "REMNANT SQUAD - ALPHA DEMO ROUTE", new Vector3(-7.4f, 0.4f, 0f), 0.32f);
        CreateWorldText("Section_Training_Label", "TRAINING OUTPOST", new Vector3(-4.8f, -1.1f, 0f), 0.24f);
        CreateWorldText("Section_Bridge_Label", "BROKEN BRIDGE", new Vector3(13.4f, -1.1f, 0f), 0.24f);
        CreateWorldText("Section_Camp_Label", "POW CAMP", new Vector3(24.8f, -1.05f, 0f), 0.24f);
        CreateWorldText("Section_Acid_Label", "ACID TRENCH", new Vector3(34.4f, -0.9f, 0f), 0.24f);
        CreateWorldText("Section_Arena_Label", "BRUTE GATE", new Vector3(49f, -1.05f, 0f), 0.24f);
        CreateWorldText("Section_Extraction_Label", "EXTRACTION", new Vector3(59.2f, -1.05f, 0f), 0.24f);

        CreateSceneryBlock("Backdrop_Tower_Start", scenerySprite, new Vector2(-8.5f, -1f), new Vector2(1.3f, 5.5f));
        CreateSceneryBlock("Backdrop_Outpost", scenerySprite, new Vector2(4f, -0.6f), new Vector2(4.8f, 3.4f));
        CreateSceneryBlock("Backdrop_Camp", scenerySprite, new Vector2(24.8f, -0.45f), new Vector2(5.5f, 3.8f));
        CreateSceneryBlock("Backdrop_Gate", scenerySprite, new Vector2(49.5f, -0.35f), new Vector2(6.5f, 4.2f));
        CreateSceneryBlock("Backdrop_Extraction", scenerySprite, new Vector2(60.5f, -0.3f), new Vector2(4.5f, 3.7f));

        CreateGround("Border_Left", groundSprite, new Vector2(-12.1f, -1.8f), new Vector2(0.45f, 5f), floorColor);
        CreateGround("LandingZone_Floor", groundSprite, new Vector2(-5.2f, -4f), new Vector2(13.8f, 1f), floorColor);
        CreateGround("Training_Platform_Low", groundSprite, new Vector2(-4.2f, -2.35f), new Vector2(3.2f, 0.45f), platformColor);
        CreateGround("Training_Platform_High", groundSprite, new Vector2(-0.3f, -1.45f), new Vector2(2.4f, 0.45f), platformColor);
        CreateGround("Training_Cover", coverSprite, new Vector2(-1.9f, -3.25f), new Vector2(0.55f, 1.5f), coverColor);

        CreateGround("Outpost_Floor", groundSprite, new Vector2(4.6f, -4f), new Vector2(7.2f, 1f), floorColor);
        CreateGround("Outpost_Cover_Left", coverSprite, new Vector2(3.4f, -3.25f), new Vector2(0.55f, 1.4f), coverColor);
        CreateGround("Outpost_Platform", groundSprite, new Vector2(6.6f, -2.1f), new Vector2(3.1f, 0.45f), platformColor);

        CreateGround("Bridge_Left_Ledge", groundSprite, new Vector2(10.7f, -4f), new Vector2(4.1f, 1f), floorColor);
        CreateGround("Bridge_Step_01", groundSprite, new Vector2(12.8f, -2.85f), new Vector2(2.2f, 0.45f), platformColor);
        CreateGround("Bridge_Step_02", groundSprite, new Vector2(15.6f, -2.05f), new Vector2(2.2f, 0.45f), platformColor);
        CreateGround("Bridge_Step_03", groundSprite, new Vector2(18.3f, -1.35f), new Vector2(2.5f, 0.45f), platformColor);
        CreateGround("Bridge_Right_Ledge", groundSprite, new Vector2(19.8f, -4f), new Vector2(4.4f, 1f), floorColor);

        CreateGround("POW_Camp_Floor", groundSprite, new Vector2(26.1f, -4f), new Vector2(11.8f, 1f), floorColor);
        CreateGround("POW_Camp_CageBase", coverSprite, new Vector2(23.5f, -3.15f), new Vector2(1.2f, 1.6f), coverColor);
        CreateGround("POW_Camp_Cover_Right", coverSprite, new Vector2(29.3f, -3.25f), new Vector2(0.55f, 1.45f), coverColor);
        CreateGround("POW_Camp_Roof", groundSprite, new Vector2(27.2f, -1.75f), new Vector2(4.4f, 0.45f), platformColor);

        CreateGround("Acid_Left_Ledge", groundSprite, new Vector2(32f, -4f), new Vector2(3.4f, 1f), floorColor);
        CreateHazard("Acid_Trench", hazardSprite, new Vector2(35.5f, -3.75f), new Vector2(5.9f, 0.55f));
        CreateGround("Acid_Platform_01", groundSprite, new Vector2(33.5f, -2.75f), new Vector2(1.75f, 0.42f), platformColor);
        CreateGround("Acid_Platform_02", groundSprite, new Vector2(36f, -2f), new Vector2(1.8f, 0.42f), platformColor);
        CreateGround("Acid_Platform_03", groundSprite, new Vector2(38.6f, -2.75f), new Vector2(1.75f, 0.42f), platformColor);
        CreateGround("Acid_Right_Ledge", groundSprite, new Vector2(40.8f, -4f), new Vector2(3.7f, 1f), floorColor);

        CreateGround("Arena_Approach_Floor", groundSprite, new Vector2(44.4f, -4f), new Vector2(4.8f, 1f), floorColor);
        CreateGround("Arena_Floor", groundSprite, new Vector2(50.4f, -4f), new Vector2(9.4f, 1f), floorColor);
        CreateGround("Arena_Cover_Left", coverSprite, new Vector2(47.6f, -3.15f), new Vector2(0.65f, 1.65f), coverColor);
        CreateGround("Arena_Cover_Right", coverSprite, new Vector2(53.2f, -3.15f), new Vector2(0.65f, 1.65f), coverColor);
        CreateGround("Arena_Upper_Walkway", groundSprite, new Vector2(50.2f, -1.65f), new Vector2(4.6f, 0.45f), platformColor);

        CreateGround("Extraction_Floor", groundSprite, new Vector2(60f, -4f), new Vector2(9.4f, 1f), floorColor);
        CreateGround("Downhill_Step_01", groundSprite, new Vector2(67.5f, -5.2f), new Vector2(5.6f, 0.8f), floorColor);
        CreateGround("Downhill_Step_02", groundSprite, new Vector2(73f, -6.7f), new Vector2(5.4f, 0.8f), floorColor);
        CreateGround("Lake_Shore_Left", groundSprite, new Vector2(78.5f, -8.6f), new Vector2(6f, 0.85f), floorColor);
        CreateWaterZone("Submerged_Lake", waterSprite, new Vector2(87.5f, -11.4f), new Vector2(17f, 5.5f));
        CreateGround("Lake_Bed_Left", groundSprite, new Vector2(83.5f, -14.2f), new Vector2(7.8f, 0.8f), floorColor);
        CreateGround("Lake_Bed_Right", groundSprite, new Vector2(92.5f, -14.2f), new Vector2(7.8f, 0.8f), floorColor);
        CreateOneWayPlatform("Lake_Mid_Rock_01", groundSprite, new Vector2(84.2f, -11.9f), new Vector2(2.2f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Mid_Rock_02", groundSprite, new Vector2(89.1f, -10.8f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Mid_Rock_03", groundSprite, new Vector2(94.1f, -12f), new Vector2(2.4f, 0.35f), platformColor);
        CreateGround("Lake_Exit_Step_01", groundSprite, new Vector2(99f, -11.2f), new Vector2(4.8f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_02", groundSprite, new Vector2(103f, -8.8f), new Vector2(4.8f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_03", groundSprite, new Vector2(107f, -6.5f), new Vector2(5f, 0.8f), floorColor);
        CreateGround("Vehicle_Depot_Floor", groundSprite, new Vector2(113.5f, -4f), new Vector2(10.5f, 1f), floorColor);
        CreateGround("FinalRunway_Floor", groundSprite, new Vector2(127f, -4f), new Vector2(16f, 1f), floorColor);
        CreateGround("Border_Right", groundSprite, new Vector2(136.2f, -1.8f), new Vector2(0.45f, 5f), floorColor);
        CreateOneWayPlatform("Downhill_CrateWalk_01", groundSprite, new Vector2(69.5f, -3.2f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Downhill_CrateWalk_02", groundSprite, new Vector2(76.5f, -5.1f), new Vector2(2.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Depot_TruckTop_Platform", groundSprite, new Vector2(113.5f, -1.95f), new Vector2(3.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Final_Runway_Service_Platform", groundSprite, new Vector2(125.5f, -2.1f), new Vector2(4.6f, 0.35f), platformColor);
        CreateGroundVehicle("Slug_Tank_Prototype", coverSprite, new Vector2(111.2f, -3.25f));
        CreatePlane("Keth_Drop_Plane", coverSprite, new Vector2(120f, 0.8f));
        CreateHoverVehicle("Player_Hovercraft", hoverVehicleSprite, playerProjectilePrefab, new Vector2(115.5f, -2.45f));
        CreateCompanion("Companion_Mika", companionSprite, playerProjectilePrefab, player, new Vector2(-9.8f, -2.3f));

        CreatePickup("Pickup_Ammo_Training", pickupAmmoSprite, new Vector2(-0.2f, -0.95f), 0, 14, 0, 50);
        CreatePickup("Pickup_Health_Bridge", pickupHealthSprite, new Vector2(18.3f, -0.75f), 1, 0, 0, 75);
        CreatePickup("Pickup_Bomb_Camp", pickupBombSprite, new Vector2(27.2f, -1.15f), 0, 0, 2, 75);
        CreatePickup("Pickup_Ammo_Arena", pickupAmmoSprite, new Vector2(45f, -3.1f), 0, 18, 0, 50);
        CreatePickup("Pickup_Health_Extraction", pickupHealthSprite, new Vector2(57.7f, -3.1f), 1, 0, 0, 100);
        CreatePickup("Pickup_Ammo_Downhill", pickupAmmoSprite, new Vector2(76.5f, -4.55f), 0, 20, 0, 50);
        CreatePickup("Pickup_Bomb_Underwater", pickupBombSprite, new Vector2(89.1f, -10.2f), 0, 0, 2, 100);
        CreatePickup("Pickup_Health_Depot", pickupHealthSprite, new Vector2(107f, -5.8f), 1, 0, 0, 100);
        CreatePickup("Pickup_Ammo_Runway", pickupAmmoSprite, new Vector2(126f, -3.1f), 0, 24, 0, 100);

        CreateCheckpoint("Checkpoint_Bridge", checkpointSprite, new Vector2(12.2f, -3.15f), new Vector2(12.2f, -3.05f));
        CreateCheckpoint("Checkpoint_Camp_Clear", checkpointSprite, new Vector2(31.4f, -3.15f), new Vector2(31.4f, -3.05f));
        CreateCheckpoint("Checkpoint_Arena", checkpointSprite, new Vector2(43.1f, -3.15f), new Vector2(43.1f, -3.05f));
        CreateCheckpoint("Checkpoint_Descent", checkpointSprite, new Vector2(67.3f, -4.45f), new Vector2(67.3f, -4.25f));
        CreateCheckpoint("Checkpoint_LakeExit", checkpointSprite, new Vector2(103f, -8.15f), new Vector2(103f, -8f));
        CreateCheckpoint("Checkpoint_Depot", checkpointSprite, new Vector2(113.5f, -3.15f), new Vector2(113.5f, -3.05f));

        CreateEnemy("Keth_Grunt_Training", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(-0.8f, -3.35f), 2, 100, false);
        CreateEnemy("Keth_Grunt_Outpost", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(4.9f, -3.35f), 2, 100, true);
        CreateEnemy("Keth_Grunt_Platform", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(6.8f, -1.45f), 2, 120, true);
        CreateEnemy("Keth_Grunt_Bridge", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(19.7f, -3.35f), 2, 100, false);
        CreateEnemy("Keth_Grunt_Camp_Roof", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(27f, -1.1f), 2, 140, true);
        CreateEnemy("Keth_Grunt_Camp_Ground", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(30.1f, -3.35f), 2, 120, true);
        CreateEnemy("Keth_Grunt_AcidExit", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(40.7f, -3.35f), 2, 120, true);
        CreateEnemy("Keth_Grunt_Arena_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(47.1f, -3.35f), 2, 120, true);
        CreateEnemy("Keth_Grunt_Arena_Walkway", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(50.2f, -1f), 2, 150, true);
        CreateEnemy("Keth_Brute_Camp", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(24.5f, -3.2f), 8, 350, true, new Vector2(1.2f, 1.35f));
        CreateEnemy("Keth_Brute_Gate", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(52f, -3.2f), 12, 550, true, new Vector2(1.35f, 1.45f));
        CreateEnemy("Keth_Grunt_Descent_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(74f, -6.05f), 2, 120, true);
        CreateSwimmingEnemy("Keth_Eel_Underwater_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(84.5f, -11.1f), new Vector2(3.4f, 0f));
        CreateSwimmingEnemy("Keth_Eel_Underwater_02", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(92f, -12.3f), new Vector2(-3.6f, 0.5f));
        CreateEnemy("Keth_Grunt_Depot_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(110.2f, -3.35f), 2, 120, false);
        CreateEnemy("Keth_Grunt_Depot_Right", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(118.4f, -3.35f), 2, 120, true);
        CreateEnemy("Keth_Brute_Runway", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(129.4f, -3.2f), 12, 600, true, new Vector2(1.35f, 1.45f));

        CreatePOW("POW_Camp_Prisoner", powSprite, new Vector2(22.5f, -3.3f));
        CreatePOW("POW_Arena_Prisoner", powSprite, new Vector2(46.1f, -3.3f));
        CreateEndTrigger(endSprite, new Vector2(134f, -3.25f), completePanel);
    }

    private static void CreateWaterZone(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject water = CreateBoxObject(name, sprite, position, scale, "Water");
        SpriteRenderer renderer = water.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 2;
            renderer.color = new Color(0.1f, 0.55f, 0.95f, 0.48f);
        }

        BoxCollider2D collider = water.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        water.AddComponent<WaterZone2D>();

        GameObject label = CreateWorldText(name + "_Label", "LAKE - SWIM WITH W/S OR LEFT STICK", new Vector3(position.x, position.y + scale.y * 0.5f + 0.35f, 0f), 0.2f);
        label.transform.SetParent(water.transform);
    }

    private static void CreateHoverVehicle(string name, Sprite sprite, GameObject projectilePrefab, Vector2 position)
    {
        GameObject vehicle = new GameObject(name);
        vehicle.transform.position = position;
        vehicle.layer = LayerMask.NameToLayer("Player");

        Rigidbody2D rb = vehicle.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D bodyCollider = vehicle.AddComponent<BoxCollider2D>();
        bodyCollider.size = new Vector2(2.8f, 0.8f);
        bodyCollider.offset = new Vector2(0f, -0.05f);

        BoxCollider2D trigger = vehicle.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3.8f, 2.2f);
        trigger.offset = Vector2.zero;

        CreateVehiclePart(name + "_Core", sprite, vehicle.transform, Vector2.zero, new Vector2(2.8f, 0.72f), new Color(0.82f, 0.92f, 1f, 1f), 7);
        CreateVehiclePart(name + "_LeftWing", sprite, vehicle.transform, new Vector2(-0.8f, -0.28f), new Vector2(1.35f, 0.24f), new Color(0.68f, 0.82f, 0.95f, 1f), 7);
        CreateVehiclePart(name + "_RightWing", sprite, vehicle.transform, new Vector2(0.8f, -0.28f), new Vector2(1.35f, 0.24f), new Color(0.68f, 0.82f, 0.95f, 1f), 7);
        CreateVehiclePart(name + "_Cockpit", sprite, vehicle.transform, new Vector2(-0.2f, 0.38f), new Vector2(0.9f, 0.36f), new Color(0.96f, 0.98f, 1f, 1f), 8);
        CreateVehiclePart(name + "_FrontGun_Left", sprite, vehicle.transform, new Vector2(1.45f, 0.16f), new Vector2(0.55f, 0.12f), new Color(0.42f, 0.55f, 0.66f, 1f), 9);
        CreateVehiclePart(name + "_FrontGun_Right", sprite, vehicle.transform, new Vector2(1.45f, -0.12f), new Vector2(0.55f, 0.12f), new Color(0.42f, 0.55f, 0.66f, 1f), 9);

        GameObject seat = new GameObject("SeatPoint");
        seat.transform.SetParent(vehicle.transform);
        seat.transform.localPosition = new Vector3(-0.2f, 0.38f, 0f);

        GameObject leftGun = new GameObject("LeftGunPoint");
        leftGun.transform.SetParent(vehicle.transform);
        leftGun.transform.localPosition = new Vector3(1.75f, 0.16f, 0f);

        GameObject rightGun = new GameObject("RightGunPoint");
        rightGun.transform.SetParent(vehicle.transform);
        rightGun.transform.localPosition = new Vector3(1.75f, -0.12f, 0f);

        GameObject prompt = CreateWorldText(name + "_Prompt", "Press E/RB to ride", new Vector3(position.x, position.y + 1.25f, 0f), 0.2f);
        prompt.transform.SetParent(vehicle.transform);

        PlayerHoverVehicle2D hover = vehicle.AddComponent<PlayerHoverVehicle2D>();
        hover.projectilePrefab = projectilePrefab;
        hover.seatPoint = seat.transform;
        hover.leftGun = leftGun.transform;
        hover.rightGun = rightGun.transform;
        hover.promptText = prompt.GetComponent<TextMesh>();
        hover.moveSpeed = 6f;
        hover.verticalSpeed = 4.5f;
    }

    private static void CreateCompanion(string name, Sprite sprite, GameObject projectilePrefab, GameObject player, Vector2 position)
    {
        GameObject companion = CreateBoxObject(name, sprite, position, new Vector2(0.38f, 0.52f), "Player");
        SpriteRenderer renderer = companion.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 8;
            renderer.color = new Color(0.95f, 0.9f, 0.72f, 1f);
        }

        CompanionShooter2D shooter = companion.AddComponent<CompanionShooter2D>();
        shooter.target = player != null ? player.transform : null;
        shooter.projectilePrefab = projectilePrefab;
        shooter.enemyLayers = LayerMask.GetMask("Enemy");
        shooter.followOffset = new Vector3(-1.25f, 1.05f, 0f);
        shooter.fireCooldown = 0.85f;

        GameObject label = CreateWorldText(name + "_Label", "COMPANION", new Vector3(position.x, position.y + 0.7f, 0f), 0.18f);
        label.transform.SetParent(companion.transform);
    }

    private static void CreateSwimmingEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2 position, Vector2 swimOffset)
    {
        GameObject enemy = CreateBoxObject(name, sprite, position, new Vector2(0.85f, 0.42f), "Enemy");
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(0.45f, 0.95f, 0.9f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = 3;
        damageable.scoreValue = 180;
        damageable.deathDelay = 0.35f;
        damageable.knockbackForce = 1.2f;
        damageable.deathEffectPrefab = deathEffectPrefab;

        enemy.AddComponent<DamageOnContact>().damage = 1;

        SimpleMover2D mover = enemy.AddComponent<SimpleMover2D>();
        mover.localMoveOffset = swimOffset;
        mover.speed = 0.75f;

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(enemy.transform);
        firePoint.transform.localPosition = new Vector3(-0.55f, 0f, 0f);

        EnemyShooter2D shooter = enemy.AddComponent<EnemyShooter2D>();
        shooter.enemyProjectilePrefab = enemyProjectilePrefab;
        shooter.firePoint = firePoint.transform;
        shooter.range = 7f;
        shooter.fireCooldown = 2.1f;
        shooter.horizontalShotHeight = 0f;
        shooter.verticalShotOffset = 0.25f;
    }

    private static void CreateSceneryBlock(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject scenery = CreateBoxObject(name, sprite, position, scale, "Ground");
        SpriteRenderer renderer = scenery.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = -5;
            renderer.color = new Color(0.35f, 0.45f, 0.52f, 0.38f);
        }
    }

    private static void CreateHazard(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject hazard = CreateBoxObject(name, sprite, position, scale, "Hazard");
        SpriteRenderer renderer = hazard.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 1;

        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        HazardDamageZone damageZone = hazard.AddComponent<HazardDamageZone>();
        damageZone.damage = 1;
        damageZone.tickInterval = 0.45f;

        GameObject label = CreateWorldText(name + "_Label", "HAZARD", new Vector3(position.x, position.y + 0.5f, 0f), 0.18f);
        label.transform.SetParent(hazard.transform);
    }

    private static void CreatePickup(string name, Sprite sprite, Vector2 position, int healthReward, int ammoReward, int bombReward, int scoreReward)
    {
        GameObject pickup = CreateBoxObject(name, sprite, position, new Vector2(0.42f, 0.42f), "Pickup");
        SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 6;

        CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.55f;

        Pickup2D pickupScript = pickup.AddComponent<Pickup2D>();
        pickupScript.healthReward = healthReward;
        pickupScript.ammoReward = ammoReward;
        pickupScript.bombReward = bombReward;
        pickupScript.scoreReward = scoreReward;

        string label = healthReward > 0 ? "HP" : ammoReward > 0 ? "AMMO" : bombReward > 0 ? "BOMB" : "SCORE";
        GameObject text = CreateWorldText(name + "_Label", label, new Vector3(position.x, position.y + 0.55f, 0f), 0.18f);
        text.transform.SetParent(pickup.transform);
    }

    private static void CreateCheckpoint(string name, Sprite sprite, Vector2 position, Vector2 respawnPosition)
    {
        GameObject checkpoint = CreateBoxObject(name, sprite, position, new Vector2(0.48f, 1.2f), "Pickup");
        SpriteRenderer renderer = checkpoint.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 5;
            renderer.color = new Color(0.05f, 0.95f, 1f, 0.55f);
        }

        BoxCollider2D collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 1.6f);

        GameObject respawn = new GameObject("RespawnPoint");
        respawn.transform.SetParent(checkpoint.transform);
        respawn.transform.position = respawnPosition;

        GameObject activeVisual = CreateBoxObject(name + "_ActiveBeam", sprite, position + new Vector2(0f, 0.78f), new Vector2(0.7f, 0.18f), "Pickup");
        SpriteRenderer activeRenderer = activeVisual.GetComponent<SpriteRenderer>();
        if (activeRenderer != null)
        {
            activeRenderer.sortingOrder = 7;
            activeRenderer.color = new Color(0.25f, 1f, 0.45f, 0.9f);
        }

        activeVisual.transform.SetParent(checkpoint.transform);

        Checkpoint2D checkpointScript = checkpoint.AddComponent<Checkpoint2D>();
        checkpointScript.respawnPoint = respawn.transform;
        checkpointScript.activeVisual = activeVisual;
        checkpointScript.scoreReward = 100;

        GameObject label = CreateWorldText(name + "_Label", "CHECKPOINT", new Vector3(position.x, position.y + 1f, 0f), 0.18f);
        label.transform.SetParent(checkpoint.transform);
    }

    private static GameObject CreatePlayerProjectilePrefab(Sprite sprite, GameObject hitEffectPrefab)
    {
        GameObject projectile = CreateBoxObject("PlayerProjectile", sprite, Vector2.zero, new Vector2(0.55f, 0.28f), "Projectile");
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;

        Projectile2D script = projectile.AddComponent<Projectile2D>();
        script.speed = 10f;
        script.damage = 1;
        script.lifetime = 2.5f;
        script.hitLayers = LayerMask.GetMask("Ground");
        script.hitEffectPrefab = hitEffectPrefab;

        string path = PrefabsPath + "/PlayerProjectile.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);
        return prefab;
    }

    private static GameObject CreateBulletHitPrefab()
    {
        Sprite[] hitSprites = LoadSpriteSequence(NeraPath + "/effects/bullet_hit", "hit", 1, 5);
        Sprite firstSprite = hitSprites.Length > 0 ? hitSprites[0] : CreateColorSprite("Sprite_BulletHit", new Color(1f, 0.75f, 0.2f, 1f));

        GameObject hit = CreateBoxObject("BulletHit", firstSprite, Vector2.zero, new Vector2(0.7f, 0.7f), "Projectile");
        SpriteRenderer renderer = hit.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 9;

        SpriteAnimationOnce animation = hit.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = renderer;
        animation.frames = hitSprites.Length > 0 ? hitSprites : new Sprite[] { firstSprite };
        animation.frameRate = 18f;

        string path = PrefabsPath + "/BulletHit.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(hit, path);
        Object.DestroyImmediate(hit);
        return prefab;
    }

    private static GameObject CreateEnemyProjectilePrefab(Sprite sprite, GameObject hitEffectPrefab)
    {
        GameObject projectile = CreateBoxObject("EnemyProjectile", sprite, Vector2.zero, new Vector2(0.36f, 0.18f), "Projectile");
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;

        EnemyProjectile2D script = projectile.AddComponent<EnemyProjectile2D>();
        script.speed = 6f;
        script.damage = 1;
        script.lifetime = 3f;
        script.hitLayers = LayerMask.GetMask("Ground");
        script.hitEffectPrefab = hitEffectPrefab;

        string path = PrefabsPath + "/EnemyProjectile.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
        Object.DestroyImmediate(projectile);
        return prefab;
    }

    private static GameObject CreateEnemyDeathPrefab(Sprite sprite)
    {
        GameObject effect = CreateBoxObject("EnemyDeathEffect", sprite, Vector2.zero, new Vector2(1.4f, 1.4f), "Projectile");
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 9;
        renderer.color = new Color(1f, 0.35f, 0.08f, 0.95f);

        SpriteAnimationOnce animation = effect.AddComponent<SpriteAnimationOnce>();
        animation.targetRenderer = renderer;
        animation.frames = new Sprite[] { sprite, sprite, sprite, sprite };
        animation.frameRate = 12f;

        string path = PrefabsPath + "/EnemyDeathEffect.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(effect, path);
        Object.DestroyImmediate(effect);
        return prefab;
    }

    private static GameObject CreateExplosionPrefab(Sprite sprite)
    {
        GameObject explosion = CreateBoxObject("ExplosionVisual", sprite, Vector2.zero, new Vector2(1.8f, 1.8f), "Projectile");
        SpriteRenderer renderer = explosion.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 8;

        string path = PrefabsPath + "/ExplosionVisual.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(explosion, path);
        Object.DestroyImmediate(explosion);
        return prefab;
    }

    private static GameObject CreateBombPrefab(Sprite sprite, GameObject explosionPrefab)
    {
        GameObject bomb = CreateBoxObject("PlayerBomb", sprite, Vector2.zero, new Vector2(0.32f, 0.32f), "Projectile");

        Rigidbody2D rb = bomb.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.freezeRotation = false;

        CircleCollider2D collider = bomb.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        BombProjectile2D script = bomb.AddComponent<BombProjectile2D>();
        script.fuseTime = 1.1f;
        script.explosionRadius = 1.8f;
        script.damage = 3;
        script.damageLayers = LayerMask.GetMask("Enemy");
        script.explosionVisual = explosionPrefab;
        script.visibleColor = new Color(0.05f, 1f, 0.95f, 1f);
        script.trailColor = new Color(1f, 0.95f, 0.1f, 1f);

        string path = PrefabsPath + "/PlayerBomb.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bomb, path);
        Object.DestroyImmediate(bomb);
        return prefab;
    }

    private static GameObject CreatePlayer(Sprite sprite, GameObject projectilePrefab, GameObject bombPrefab)
    {
        GameObject player = CreateBoxObject("Player_Volkov", sprite, new Vector2(-8.5f, -3.25f), Vector2.one, "Player");
        player.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
        SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();
        rootRenderer.sortingOrder = 2;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        BoxCollider2D playerCollider = player.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.55f, 0.95f);
        playerCollider.offset = new Vector2(0f, -0.05f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.56f, 0f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0.62f, 0.14f, 0f);

        GameObject throwPoint = new GameObject("ThrowPoint");
        throwPoint.transform.SetParent(player.transform);
        throwPoint.transform.localPosition = new Vector3(0.45f, 0.25f, 0f);

        GameObject legsObject = new GameObject("Legs");
        legsObject.transform.SetParent(player.transform);
        legsObject.transform.localPosition = Vector3.zero;
        SpriteRenderer legsRenderer = legsObject.AddComponent<SpriteRenderer>();
        legsRenderer.sprite = sprite;
        legsRenderer.sortingOrder = 2;

        GameObject torsoObject = new GameObject("Torso");
        torsoObject.transform.SetParent(player.transform);
        torsoObject.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
        torsoRenderer.sprite = LoadSprite(NeraPath + "/torso/idle/torso_idle0.png");
        torsoRenderer.sortingOrder = 3;

        if (torsoRenderer.sprite != null)
            rootRenderer.enabled = false;

        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.moveSpeed = 3.5f;
        controller.crouchMoveMultiplier = 0.5f;
        controller.groundCheck = groundCheck.transform;
        controller.groundLayer = LayerMask.GetMask("Ground");

        PlayerShooter2D shooter = player.AddComponent<PlayerShooter2D>();
        shooter.projectilePrefab = projectilePrefab;
        shooter.firePoint = firePoint.transform;
        shooter.maxAmmo = 30;
        shooter.reloadTime = 1.1f;

        PlayerBombThrower2D bombThrower = player.AddComponent<PlayerBombThrower2D>();
        bombThrower.bombPrefab = bombPrefab;
        bombThrower.throwPoint = throwPoint.transform;

        PlayerMeleeAttack2D melee = player.AddComponent<PlayerMeleeAttack2D>();
        melee.enemyLayers = LayerMask.GetMask("Enemy");

        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.maxHealth = 3;
        health.maxLives = 3;
        health.deathReloadDelay = 1.35f;
        health.fallDeathY = -18f;

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController("PlayerVolkov", new string[] { "Idle", "Run", "JumpFall", "Dash", "Shoot", "Hurt", "Death" });
        player.AddComponent<PlayerAnimationDriver>();

        NeraPlayerVisual neraVisual = player.AddComponent<NeraPlayerVisual>();
        neraVisual.legsRenderer = legsRenderer;
        neraVisual.torsoRenderer = torsoRenderer;
        neraVisual.torsoOffset = Vector3.zero;
        neraVisual.runShootTorsoOffset = Vector3.zero;
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

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2 position, int health, int score, bool canShoot)
    {
        return CreateEnemy(name, sprite, enemyProjectilePrefab, deathEffectPrefab, position, health, score, canShoot, new Vector2(0.85f, 1.1f));
    }

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2 position, int health, int score, bool canShoot, Vector2 scale)
    {
        GameObject enemy = CreateBoxObject(name, sprite, position, scale, "Enemy");

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        enemy.AddComponent<BoxCollider2D>().size = Vector2.one;

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = health;
        damageable.scoreValue = score;
        damageable.deathDelay = 0.45f;
        damageable.knockbackForce = scale.x > 1f ? 1.1f : 2.5f;
        damageable.deathEffectPrefab = deathEffectPrefab;

        enemy.AddComponent<DamageOnContact>().damage = 1;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(enemy.transform);
        groundCheck.transform.localPosition = new Vector3(-0.45f, -0.58f, 0f);

        GameObject wallCheck = new GameObject("WallCheck");
        wallCheck.transform.SetParent(enemy.transform);
        wallCheck.transform.localPosition = new Vector3(-0.55f, 0f, 0f);

        EnemyPatrol2D patrol = enemy.AddComponent<EnemyPatrol2D>();
        patrol.moveSpeed = scale.x > 1f ? 0.6f : 1.25f;
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
            shooter.range = scale.x > 1f ? 9f : 8f;
            shooter.fireCooldown = scale.x > 1f ? 2.8f : 1.7f;
            shooter.burstCount = scale.x > 1f ? 3 : 1;
            shooter.burstSpacing = scale.x > 1f ? 0.22f : 0.16f;
            shooter.projectileScaleMultiplier = scale.x > 1f ? 1.45f : 1f;
            shooter.horizontalShotHeight = scale.x > 1f ? 0.38f : 0.28f;
            shooter.verticalShotOffset = scale.x > 1f ? 0.55f : 0.35f;
        }

        Animator animator = enemy.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController(scale.x > 1f ? "KethBrute" : "KethGrunt", new string[] { "Idle", "Walk", "Shoot", "Hurt", "Death" });

        EnemyAnimationDriver animationDriver = enemy.AddComponent<EnemyAnimationDriver>();
        animationDriver.isBrute = scale.x > 1f;

        return enemy;
    }

    private static void CreatePOW(string name, Sprite sprite, Vector2 position)
    {
        GameObject pow = CreateBoxObject(name, sprite, position, new Vector2(0.75f, 1f), "POW");
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
        rescue.bombReward = 1;
        rescue.ammoReward = 10;
        rescue.rescuePrompt = prompt;
        rescue.rescuedVisual = rescued;

        Animator animator = pow.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController("POW", new string[] { "CapturedIdle", "Celebrate", "RunAway" });
        pow.AddComponent<POWAnimationDriver>();

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

    private static RuntimeAnimatorController CreateAnimatorController(string name, string[] stateNames)
    {
        string controllerPath = AnimationsPath + "/" + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        EnsureAnimatorParameters(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        for (int i = stateMachine.states.Length - 1; i >= 0; i--)
            stateMachine.RemoveState(stateMachine.states[i].state);

        for (int i = 0; i < stateNames.Length; i++)
        {
            AnimationClip clip = CreatePlaceholderClip(name + "_" + stateNames[i]);
            AnimatorState state = stateMachine.AddState(stateNames[i], new Vector3(250f, i * 55f, 0f));
            state.motion = clip;

            if (i == 0)
                stateMachine.defaultState = state;
        }

        return controller;
    }

    private static void EnsureAnimatorParameters(AnimatorController controller)
    {
        EnsureAnimatorParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureAnimatorParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
        EnsureAnimatorParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Dashing", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Shooting", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Throwing", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Hurt", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Dead", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "AimX", AnimatorControllerParameterType.Float);
        EnsureAnimatorParameter(controller, "AimY", AnimatorControllerParameterType.Float);
        EnsureAnimatorParameter(controller, "Moving", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Brute", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "DeathVariant", AnimatorControllerParameterType.Int);
        EnsureAnimatorParameter(controller, "PlayerNearby", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Rescued", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "Running", AnimatorControllerParameterType.Bool);
    }

    private static void EnsureAnimatorParameter(AnimatorController controller, string parameterName, AnimatorControllerParameterType parameterType)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName)
                return;
        }

        controller.AddParameter(parameterName, parameterType);
    }

    private static AnimationClip CreatePlaceholderClip(string name)
    {
        string clipPath = AnimationsPath + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            clip.frameRate = 8f;
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static GameObject CreateHUD(PlayerHealth playerHealth)
    {
        GameObject canvasObject = new GameObject("Canvas_HUD");
        canvasObject.transform.localScale = Vector3.one;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject statusPanel = CreateUIPanel("StatusPanel", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(460f, 138f), new Color(0.02f, 0.03f, 0.04f, 0.78f));
        CreateUIAccent("StatusAccent", statusPanel.transform, new Color(0.05f, 1f, 0.95f, 0.9f), true);
        Text scoreText = CreateUIText("ScoreText", statusPanel.transform, "Score: 0", new Vector2(18f, -10f), TextAnchor.UpperLeft, 20, font);
        Text healthText = CreateUIText("HealthText", statusPanel.transform, "HP 3/3", new Vector2(54f, -42f), TextAnchor.UpperLeft, 15, font);
        Image healthFill = CreateUIIconBar("HealthBar", statusPanel.transform, "H", new Vector2(18f, -42f), new Color(0.1f, 1f, 0.25f, 1f), font);
        Text livesText = CreateUIText("LivesText", statusPanel.transform, "Lives 3/3", new Vector2(54f, -74f), TextAnchor.UpperLeft, 15, font);
        Image livesFill = CreateUIIconBar("LivesBar", statusPanel.transform, "L", new Vector2(18f, -74f), new Color(0.95f, 0.25f, 0.25f, 1f), font);
        Text ammoText = CreateUIText("AmmoText", statusPanel.transform, "Ammo 30/30", new Vector2(264f, -42f), TextAnchor.UpperLeft, 15, font);
        Image ammoFill = CreateUIIconBar("AmmoBar", statusPanel.transform, "A", new Vector2(228f, -42f), new Color(1f, 0.9f, 0.15f, 1f), font);
        Text bombText = CreateUIText("BombText", statusPanel.transform, "Bombs 3/3", new Vector2(264f, -74f), TextAnchor.UpperLeft, 15, font);
        Image bombFill = CreateUIIconBar("BombBar", statusPanel.transform, "B", new Vector2(228f, -74f), new Color(0.1f, 0.72f, 1f, 1f), font);

        GameObject controlsPanel = CreateUIPanel("ControlsPanel", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(390f, 196f), new Color(0.02f, 0.03f, 0.04f, 0.78f));
        CreateUIText("ControlsTitle", controlsPanel.transform, "CONTROLS", new Vector2(0f, -10f), TextAnchor.UpperCenter, 19, font);
        CreateUIText("ControlsMove", controlsPanel.transform, "MOVE A/D/LS   SWIM W/S", new Vector2(16f, -42f), TextAnchor.UpperLeft, 15, font);
        CreateUIText("ControlsAim", controlsPanel.transform, "AIM Mouse / Arrows / RS", new Vector2(16f, -66f), TextAnchor.UpperLeft, 15, font);
        CreateUIText("ControlsShoot", controlsPanel.transform, "SHOOT Click/F/X   MELEE C/LT", new Vector2(16f, -90f), TextAnchor.UpperLeft, 15, font);
        CreateUIText("ControlsTools", controlsPanel.transform, "BOMB R/B   RELOAD T/Y", new Vector2(16f, -114f), TextAnchor.UpperLeft, 15, font);
        CreateUIText("ControlsAction", controlsPanel.transform, "JUMP Space/A DASH Shift/LB RESCUE/RIDE E/RB", new Vector2(16f, -138f), TextAnchor.UpperLeft, 14, font);
        CreateUIText("ControlsSystem", controlsPanel.transform, "RESTART Enter/Start on Game Over", new Vector2(16f, -158f), TextAnchor.UpperLeft, 13, font);

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

        GameObject gameOverPanel = CreateUIPanel("GameOverPanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 180f), new Color(0f, 0f, 0f, 0.84f));
        Text gameOverText = CreateUIText("GameOverText", gameOverPanel.transform, "GAME OVER\nPress Enter or Start to restart", Vector2.zero, TextAnchor.MiddleCenter, 34, font);
        RectTransform gameOverTextRect = gameOverText.GetComponent<RectTransform>();
        gameOverTextRect.anchorMin = Vector2.zero;
        gameOverTextRect.anchorMax = Vector2.one;
        gameOverTextRect.offsetMin = Vector2.zero;
        gameOverTextRect.offsetMax = Vector2.zero;
        gameOverPanel.SetActive(false);

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
        hud.healthFill = healthFill;
        hud.livesFill = livesFill;
        hud.ammoFill = ammoFill;
        hud.bombFill = bombFill;

        return completePanel;
    }

    private static GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private static Image CreateUIIconBar(string name, Transform parent, string iconText, Vector2 anchoredPosition, Color fillColor, Font font)
    {
        GameObject icon = new GameObject(name + "_Icon");
        icon.transform.SetParent(parent);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = anchoredPosition;
        iconRect.sizeDelta = new Vector2(26f, 22f);
        icon.AddComponent<Image>().color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.9f);

        Text iconLabel = CreateUIText(name + "_IconText", icon.transform, iconText, Vector2.zero, TextAnchor.MiddleCenter, 14, font);
        RectTransform iconLabelRect = iconLabel.GetComponent<RectTransform>();
        iconLabelRect.anchorMin = Vector2.zero;
        iconLabelRect.anchorMax = Vector2.one;
        iconLabelRect.offsetMin = Vector2.zero;
        iconLabelRect.offsetMax = Vector2.zero;
        iconLabel.color = Color.black;

        GameObject background = new GameObject(name + "_Back");
        background.transform.SetParent(parent);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.anchoredPosition = anchoredPosition + new Vector2(36f, -2f);
        backgroundRect.sizeDelta = new Vector2(130f, 10f);
        background.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject fill = new GameObject(name + "_Fill");
        fill.transform.SetParent(background.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    private static void CreateUIAccent(string name, Transform parent, Color color, bool left)
    {
        GameObject accent = new GameObject(name);
        accent.transform.SetParent(parent);
        RectTransform rect = accent.AddComponent<RectTransform>();
        rect.anchorMin = left ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
        rect.anchorMax = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.pivot = left ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = left ? new Vector2(4f, 0f) : new Vector2(0f, 4f);
        accent.AddComponent<Image>().color = color;
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
            rect.sizeDelta = new Vector2(900f, 80f);
        }
        else if (alignment == TextAnchor.UpperCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(340f, 34f);
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
        follow.useBounds = true;
        follow.minBounds = new Vector2(-9f, -11.6f);
        follow.maxBounds = new Vector2(132f, 3.2f);
    }
}
#endif
