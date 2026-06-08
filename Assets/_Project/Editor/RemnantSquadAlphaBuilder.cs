#if UNITY_EDITOR
using System.Collections.Generic;
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
    private const string PlayableScenesPath = "Assets/Scenes";
    private const string AnimationsPath = BasePath + "/Animations";
    private const string DataPath = BasePath + "/Data";
    private const string NeraPath = BasePath + "/Art/Nera/sprites";
    private const string GenericRunGunPath = BasePath + "/External/GenericRunNGun/Extracted";
    private const string GenericRunGunGeneratedPath = GeneratedPath + "/GenericRunNGun";
    private static readonly Dictionary<string, Sprite> colorSpriteCache = new Dictionary<string, Sprite>();
    private static Sprite grgTerrainTopSprite;
    private static Sprite grgTerrainFillSprite;
    private static Sprite grgTerrainSideSprite;
    private static Sprite grgPlatformTopSprite;
    private static Sprite grgCoverSprite;
    private static Sprite grgOutdoorRuinPanelSprite;
    private static Sprite grgOutdoorRailingSprite;
    private static Sprite grgOutdoorLampSprite;
    private static Sprite grgOutdoorConeSprite;
    private static Sprite grgOutdoorSignSprite;
    private static Sprite grgOutdoorSupportSprite;
    private static Sprite grgOutdoorPipeSprite;
    private static Sprite grgOutdoorDoorSprite;

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
        colorSpriteCache.Clear();
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

        ApplyGenericRunGunArtPack(ref enemySprite, ref bruteSprite, ref groundSprite, ref coverSprite, ref explosionSprite);

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        CreateCamera();

        GameObject bulletHitPrefab = CreateBulletHitPrefab();
        GameObject playerProjectilePrefab = CreatePlayerProjectilePrefab(projectileSprite, bulletHitPrefab);
        GameObject enemyProjectilePrefab = CreateEnemyProjectilePrefab(enemyProjectileSprite, bulletHitPrefab);
        GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);
        GameObject enemyDeathPrefab = CreateEnemyDeathPrefab(explosionSprite);
        GameObject bombPrefab = CreateBombPrefab(bombSprite, explosionPrefab);

        CreateRuntimeTools(playerProjectilePrefab, enemyProjectilePrefab, bulletHitPrefab, enemyDeathPrefab, explosionPrefab);

        GameObject player = CreatePlayer(playerSprite, playerProjectilePrefab, bombPrefab);

        GameObject completePanel = CreateHUD(player.GetComponent<PlayerHealth>());
        CreateDetailedLevel(groundSprite, coverSprite, scenerySprite, hazardSprite, waterSprite, hoverVehicleSprite, companionSprite, checkpointSprite, pickupHealthSprite, pickupAmmoSprite, pickupBombSprite, enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, playerProjectilePrefab, player, powSprite, endSprite, completePanel);

        Camera.main.GetComponent<CameraFollow2D>().target = player.transform;

        string levelOneScenePath = PlayableScenesPath + "/Level_01_Alpha.unity";
        string projectSceneMirrorPath = ScenesPath + "/Level_01_Alpha.unity";
        EditorSceneManager.SaveScene(scene, levelOneScenePath);
        File.Copy(levelOneScenePath, projectSceneMirrorPath, true);
        AssetDatabase.ImportAsset(projectSceneMirrorPath, ImportAssetOptions.ForceUpdate);

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(levelOneScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        const string successMessage = "Vertical-slice Level 1 was generated successfully.\n\nOpen Assets/Scenes/Level_01_Alpha and press Play.\n\nRoute:\nTraining Outpost > First Contact Road > Broken Bridge > POW Camp > Acid Trench > Brute Gate > Lake Descent > Vehicle Depot > Radar Runway > Anti-Air Tower > Breached Hangar > Extract Elevator > Comms Shaft > Pipeline Switchbacks > Carrier Spine > Rail Yard Rush > Cliffside AA Batteries > Final Extraction Lift.\n\nControls:\nA/D or Left Stick = Move\nW/S or Left Stick = Swim/vehicle vertical\nMouse, Arrow Keys, or Right Stick = Aim\nLeft Click, F, or X Button = Shoot\nSpace/W or A Button = Jump\nT or Y Button = Reload\nR or B Button = Bomb\nLeft Shift or LB = Dash\nE or RB = Rescue / Ride";

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
        CreateFolderIfMissing("Assets", "Scenes");
        CreateFolderIfMissing(BasePath, "Animations");
        CreateFolderIfMissing(BasePath, "Data");
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

        Debug.LogWarning("No empty Unity layer slot found for: " + layerName);
    }

    private static void ApplyGenericRunGunArtPack(ref Sprite enemySprite, ref Sprite bruteSprite, ref Sprite groundSprite, ref Sprite coverSprite, ref Sprite explosionSprite)
    {
        if (!AssetDatabase.IsValidFolder(GenericRunGunPath) && !Directory.Exists(GenericRunGunPath))
            return;

        Sprite importedEnemy = CreateCroppedSpriteAsset("GRG_Keth_Grunt_AR_Frame", GenericRunGunPath + "/Enemies/ARMob.png", new RectInt(0, 0, 32, 38), 24f);
        if (importedEnemy != null)
            enemySprite = importedEnemy;

        Sprite importedBrute = CreateCroppedSpriteAsset("GRG_Keth_Brute_RPG_Frame", GenericRunGunPath + "/Enemies/RPGmob.png", new RectInt(0, 0, 44, 44), 24f);
        if (importedBrute != null)
            bruteSprite = importedBrute;

        string outdoorTilesPath = GenericRunGunPath + "/Assets_area_2/tileset/tiles_out.png";
        string subwayTilesPath = GenericRunGunPath + "/Assets_area_1/Tileset/Subway_tiles.png";
        grgTerrainTopSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Terrain_Top_Edge", outdoorTilesPath, new RectInt(16, 0, 16, 16), 16f);
        grgTerrainFillSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Brick_Wall_Fill", outdoorTilesPath, new RectInt(32, 304, 16, 16), 16f);
        grgTerrainSideSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Terrain_Side_Edge", outdoorTilesPath, new RectInt(0, 32, 16, 16), 16f);
        grgPlatformTopSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Platform_Top_Edge", outdoorTilesPath, new RectInt(80, 32, 16, 16), 16f);
        grgCoverSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Crate_Cover", outdoorTilesPath, new RectInt(112, 48, 32, 32), 16f);
        grgOutdoorRuinPanelSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Ruin_Wall_Panel", outdoorTilesPath, new RectInt(0, 272, 112, 128), 16f);
        grgOutdoorRailingSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Railing", outdoorTilesPath, new RectInt(0, 64, 64, 48), 16f);
        grgOutdoorLampSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Lamp_Post", outdoorTilesPath, new RectInt(160, 64, 32, 96), 16f);
        grgOutdoorConeSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Cone", outdoorTilesPath, new RectInt(96, 144, 16, 32), 16f);
        grgOutdoorSignSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Warning_Sign", outdoorTilesPath, new RectInt(176, 144, 32, 48), 16f);
        grgOutdoorSupportSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Service_Support", outdoorTilesPath, new RectInt(64, 0, 48, 96), 16f);
        grgOutdoorPipeSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Pipe_Duct", outdoorTilesPath, new RectInt(0, 48, 80, 16), 16f);
        grgOutdoorDoorSprite = CreateCroppedSpriteAsset("GRG_Outdoor_Service_Door", outdoorTilesPath, new RectInt(0, 144, 32, 96), 16f);

        if (grgTerrainTopSprite == null)
            grgTerrainTopSprite = CreateCroppedSpriteAsset("GRG_Terrain_Top_Edge", subwayTilesPath, new RectInt(112, 160, 16, 16), 16f);

        if (grgTerrainFillSprite == null)
            grgTerrainFillSprite = CreateCroppedSpriteAsset("GRG_Terrain_Wall_Fill", subwayTilesPath, new RectInt(192, 304, 16, 16), 16f);

        if (grgTerrainSideSprite == null)
            grgTerrainSideSprite = CreateCroppedSpriteAsset("GRG_Terrain_Side_Edge", subwayTilesPath, new RectInt(96, 160, 16, 16), 16f);

        if (grgPlatformTopSprite == null)
            grgPlatformTopSprite = CreateCroppedSpriteAsset("GRG_Platform_Top_Edge", subwayTilesPath, new RectInt(144, 160, 16, 16), 16f);

        if (grgCoverSprite == null)
            grgCoverSprite = CreateCroppedSpriteAsset("GRG_Crate_Cover", subwayTilesPath, new RectInt(272, 16, 32, 32), 16f);

        Sprite importedGround = grgTerrainTopSprite != null ? grgTerrainTopSprite : CreateCroppedSpriteAsset("GRG_Subway_Ground_Tile", GenericRunGunPath + "/Assets_area_1/Tileset/Subway_tiles.png", new RectInt(160, 176, 16, 16), 16f);
        if (importedGround != null)
            groundSprite = importedGround;

        Sprite importedCover = grgCoverSprite != null ? grgCoverSprite : CreateCroppedSpriteAsset("GRG_Outdoor_Platform_Tile", GenericRunGunPath + "/Assets_area_2/tileset/tiles_out.png", new RectInt(96, 0, 16, 16), 16f);
        if (importedCover != null)
            coverSprite = importedCover;

        Sprite importedExplosion = CreateCroppedSpriteAsset("GRG_Explosion_Mid_Frame", GenericRunGunPath + "/Enemies/Explosion_Particle.png", new RectInt(96, 0, 32, 32), 32f);
        if (importedExplosion != null)
            explosionSprite = importedExplosion;
    }

    private static Sprite CreateCroppedSpriteAsset(string name, string sourcePath, RectInt topLeftRect, float pixelsPerUnit)
    {
        if (!File.Exists(sourcePath))
            return null;

        ConfigureTextureImporter(sourcePath, pixelsPerUnit, true);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
            return null;

        if (topLeftRect.x < 0 || topLeftRect.y < 0 || topLeftRect.xMax > source.width || topLeftRect.yMax > source.height)
            return null;

        if (!AssetDatabase.IsValidFolder(GenericRunGunGeneratedPath))
            CreateFolderIfMissing(GeneratedPath, "GenericRunNGun");

        string texturePath = GenericRunGunGeneratedPath + "/" + name + ".png";
        int readY = source.height - topLeftRect.y - topLeftRect.height;
        Texture2D cropped = new Texture2D(topLeftRect.width, topLeftRect.height, TextureFormat.RGBA32, false);
        cropped.SetPixels(source.GetPixels(topLeftRect.x, readY, topLeftRect.width, topLeftRect.height));
        cropped.Apply();
        File.WriteAllBytes(texturePath, cropped.EncodeToPNG());
        Object.DestroyImmediate(cropped);

        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        ConfigureTextureImporter(texturePath, pixelsPerUnit, false);
        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static Sprite LoadGenericRunGunSprite(string path, float pixelsPerUnit)
    {
        if (!File.Exists(path))
            return null;

        ConfigureTextureImporter(path, pixelsPerUnit, false);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ConfigureTextureImporter(string path, float pixelsPerUnit, bool readable)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = readable;
        importer.SaveAndReimport();
    }

    private static Sprite CreateColorSprite(string name, Color color)
    {
        if (colorSpriteCache.TryGetValue(name, out Sprite cachedSprite) && cachedSprite != null)
            return cachedSprite;

        string texturePath = GeneratedPath + "/" + name + ".png";
        bool createdTexture = false;

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
            createdTexture = true;
        }

        if (!createdTexture)
        {
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (existingSprite != null)
            {
                colorSpriteCache[name] = existingSprite;
                return existingSprite;
            }
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

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        colorSpriteCache[name] = sprite;
        return sprite;
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
        ApplyTiledColliderVisual(ground, sprite, scale, Color.white, 1, false);
        ground.AddComponent<BoxCollider2D>().size = Vector2.one;
        return ground;
    }

    private static GameObject CreateGround(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject ground = CreateGround(name, sprite, position, scale);
        Color visualColor = IsGenericRunGunSprite(sprite) ? Color.white : color;
        ApplyTerrainColor(ground, visualColor);

        return ground;
    }

    private static GameObject CreateLowCover(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject cover = CreateBoxObject(name, sprite, position, scale, "Ground");
        Color visualColor = IsGenericRunGunSprite(sprite) ? Color.white : color;
        ApplyCoverVisual(cover, sprite, scale, visualColor, 3);

        BoxCollider2D collider = cover.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return cover;
    }

    private static GameObject CreateTerrainFace(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject face = CreateBoxObject(name, sprite, position, scale, "Default");
        Color visualColor = IsGenericRunGunSprite(sprite) ? Color.white : color;
        ApplyTiledColliderVisual(face, sprite, scale, visualColor, 0, false);
        return face;
    }

    private static GameObject CreateOneWayPlatform(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject platform = CreateBoxObject(name, sprite, position, scale, "Ground");
        Color visualColor = IsGenericRunGunSprite(sprite) ? Color.white : color;
        ApplyTiledColliderVisual(platform, sprite, scale, visualColor, 2, true);

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.usedByEffector = true;

        PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 160f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
        effector.sideArc = 1f;
        return platform;
    }

    private static void ApplyTerrainColor(GameObject terrainObject, Color color)
    {
        SpriteRenderer[] renderers = terrainObject.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = color;
            return;
        }
    }

    private static void ApplyTiledColliderVisual(GameObject terrainObject, Sprite sprite, Vector2 scale, Color color, int sortingOrder, bool topOnly)
    {
        if (sprite == null)
            return;

        SpriteRenderer stretchedRenderer = terrainObject.GetComponent<SpriteRenderer>();
        if (stretchedRenderer != null)
            stretchedRenderer.enabled = false;

        int columns = Mathf.Max(1, Mathf.CeilToInt(scale.x));
        int rows = Mathf.Max(1, Mathf.CeilToInt(scale.y));
        Sprite topSprite = topOnly && grgPlatformTopSprite != null ? grgPlatformTopSprite : grgTerrainTopSprite != null ? grgTerrainTopSprite : sprite;
        Sprite fillSprite = grgTerrainFillSprite != null ? grgTerrainFillSprite : sprite;
        Sprite sideSprite = grgTerrainSideSprite != null ? grgTerrainSideSprite : fillSprite;

        GameObject tileRoot = new GameObject("SpriteTileGrid");
        tileRoot.transform.SetParent(terrainObject.transform, false);

        if (!topOnly)
            CreateFilledSpriteTiles(tileRoot.transform, "WallFill", fillSprite, color, sortingOrder, columns, rows);

        CreateSpriteTileRow(tileRoot.transform, "TopEdge", topSprite, color, sortingOrder + 1, columns, rows - 1);

        if (!topOnly && columns > 1)
        {
            CreateSpriteTileColumn(tileRoot.transform, "LeftEdge", sideSprite, color, sortingOrder + 2, 0, columns, rows);
            CreateSpriteTileColumn(tileRoot.transform, "RightEdge", sideSprite, color, sortingOrder + 2, columns - 1, columns, rows);
        }
    }

    private static void ApplyCoverVisual(GameObject coverObject, Sprite sprite, Vector2 scale, Color color, int sortingOrder)
    {
        if (sprite == null)
            return;

        SpriteRenderer stretchedRenderer = coverObject.GetComponent<SpriteRenderer>();
        if (stretchedRenderer != null)
            stretchedRenderer.enabled = false;

        Sprite coverSprite = grgCoverSprite != null ? grgCoverSprite : sprite;
        GameObject visual = new GameObject("CoverVisual");
        visual.transform.SetParent(coverObject.transform, false);

        Vector2 targetSize = new Vector2(Mathf.Max(scale.x, 1.1f), Mathf.Max(scale.y, 0.95f));
        Vector2 spriteSize = coverSprite.bounds.size;
        visual.transform.localPosition = new Vector3(0f, (targetSize.y - scale.y) * 0.5f / Mathf.Max(0.01f, scale.y), 0f);
        visual.transform.localScale = new Vector3(
            targetSize.x / Mathf.Max(0.01f, spriteSize.x * scale.x),
            targetSize.y / Mathf.Max(0.01f, spriteSize.y * scale.y),
            1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = coverSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateFilledSpriteTiles(Transform parent, string name, Sprite sprite, Color color, int sortingOrder, int columns, int rows)
    {
        GameObject layer = CreateSpriteTileLayer(parent, name);
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
                CreateSpriteTile(layer.transform, sprite, color, sortingOrder, x, y, columns, rows);
        }
    }

    private static void CreateSpriteTileRow(Transform parent, string name, Sprite sprite, Color color, int sortingOrder, int columns, int row)
    {
        GameObject layer = CreateSpriteTileLayer(parent, name);
        for (int x = 0; x < columns; x++)
            CreateSpriteTile(layer.transform, sprite, color, sortingOrder, x, row, columns, row + 1);
    }

    private static void CreateSpriteTileColumn(Transform parent, string name, Sprite sprite, Color color, int sortingOrder, int column, int columns, int rows)
    {
        GameObject layer = CreateSpriteTileLayer(parent, name);
        for (int y = 0; y < rows; y++)
            CreateSpriteTile(layer.transform, sprite, color, sortingOrder, column, y, columns, rows);
    }

    private static GameObject CreateSpriteTileLayer(Transform parent, string name)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent, false);
        return layer;
    }

    private static void CreateSpriteTile(Transform parent, Sprite sprite, Color color, int sortingOrder, int x, int y, int columns, int rows)
    {
        if (sprite == null)
            return;

        GameObject tile = new GameObject(sprite.name + "_Tile");
        tile.transform.SetParent(parent, false);
        tile.transform.localPosition = new Vector3(
            -0.5f + (x + 0.5f) / Mathf.Max(1, columns),
            -0.5f + (y + 0.5f) / Mathf.Max(1, rows),
            0f);
        tile.transform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, columns * sprite.bounds.size.x),
            1f / Mathf.Max(0.01f, rows * sprite.bounds.size.y),
            1f);

        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateEnemyWaypointGraph()
    {
        GameObject graphObject = new GameObject("Enemy_AStar_WaypointGraph");
        graphObject.AddComponent<EnemyWaypointGraph2D>();

        Vector2[] positions = new Vector2[]
        {
            new Vector2(-9f, -3.35f), new Vector2(-4.2f, -3.35f), new Vector2(-4.2f, -1.8f), new Vector2(-0.3f, -0.9f),
            new Vector2(2f, -3.35f), new Vector2(6.6f, -1.45f), new Vector2(8.5f, -3.35f), new Vector2(11.5f, -3.35f),
            new Vector2(12.8f, -2.2f), new Vector2(15.6f, -1.4f), new Vector2(18.3f, -0.7f), new Vector2(20.5f, -3.35f),
            new Vector2(26f, -3.35f), new Vector2(27.2f, -1.1f), new Vector2(32f, -3.35f), new Vector2(33.5f, -2.2f),
            new Vector2(36f, -1.5f), new Vector2(38.6f, -2.2f), new Vector2(41f, -3.35f), new Vector2(50f, -3.35f),
            new Vector2(50.2f, -1f), new Vector2(60f, -3.35f), new Vector2(67.5f, -4.6f), new Vector2(73f, -6.05f),
            new Vector2(78.5f, -7.95f), new Vector2(84f, -7.6f), new Vector2(89f, -7.6f), new Vector2(94f, -7.6f),
            new Vector2(99f, -7.95f), new Vector2(83.5f, -13.55f), new Vector2(89.1f, -10.25f), new Vector2(92.5f, -13.55f),
            new Vector2(96.1f, -12.75f), new Vector2(98.3f, -11.55f), new Vector2(100.5f, -10.35f), new Vector2(102.8f, -9.15f),
            new Vector2(105.2f, -7.95f), new Vector2(108.4f, -6.05f), new Vector2(111.1f, -4.65f), new Vector2(113.5f, -3.35f),
            new Vector2(113.5f, -1.3f), new Vector2(125.5f, -1.45f), new Vector2(127f, -3.35f), new Vector2(140.8f, -1.45f),
            new Vector2(143.5f, -3.35f), new Vector2(147.2f, -0.25f), new Vector2(156f, -1.55f), new Vector2(159.5f, -3.35f),
            new Vector2(164f, -0.6f), new Vector2(177.2f, -1.1f), new Vector2(177.5f, -3.35f), new Vector2(194f, -3.35f),
            new Vector2(202.5f, -0.05f), new Vector2(203.4f, -2.85f), new Vector2(206.9f, -2.2f), new Vector2(210.4f, -1.5f),
            new Vector2(211f, 1.05f), new Vector2(218.2f, -1.45f), new Vector2(231f, -1.45f), new Vector2(231f, -0.05f),
            new Vector2(240.6f, -2.25f), new Vector2(245.6f, -2.95f), new Vector2(253.5f, -1.3f), new Vector2(254f, -2.9f),
            new Vector2(262.5f, -2.25f), new Vector2(267.5f, -1.45f), new Vector2(279.5f, -1.45f), new Vector2(279.5f, 0.2f),
            new Vector2(290.5f, -0.35f), new Vector2(291.8f, -2.3f), new Vector2(297.9f, -2.95f), new Vector2(304.4f, -2.15f),
            new Vector2(316f, -1.45f), new Vector2(319.5f, -3.35f), new Vector2(337.5f, -1.45f), new Vector2(342f, -3.35f),
            new Vector2(356.3f, -2.5f), new Vector2(359.4f, -0.15f), new Vector2(361.8f, -1.7f), new Vector2(366.6f, 1.05f),
            new Vector2(367.5f, -0.95f), new Vector2(376f, -0.95f), new Vector2(377f, 0.85f)
        };

        EnemyWaypointNode2D[] nodes = new EnemyWaypointNode2D[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject nodeObject = new GameObject("AI_Node_" + i.ToString("000"));
            nodeObject.transform.SetParent(graphObject.transform);
            nodeObject.transform.position = positions[i];
            nodes[i] = nodeObject.AddComponent<EnemyWaypointNode2D>();
            nodes[i].jumpHint = i > 0 && positions[i].y > positions[Mathf.Max(0, i - 1)].y + 0.7f;
            nodes[i].dropHint = i > 0 && positions[i].y < positions[Mathf.Max(0, i - 1)].y - 0.7f;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            List<EnemyWaypointNode2D> neighbors = new List<EnemyWaypointNode2D>();
            for (int j = 0; j < nodes.Length; j++)
            {
                if (i == j)
                    continue;

                float dx = Mathf.Abs(positions[i].x - positions[j].x);
                float dy = Mathf.Abs(positions[i].y - positions[j].y);
                if (dx <= 8.25f && dy <= 3.05f)
                    neighbors.Add(nodes[j]);
            }

            nodes[i].neighbors = neighbors.ToArray();
        }
    }

    private static GameObject CreateDestructibleBridge(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        GameObject bridge = new GameObject(name);
        bridge.transform.position = position;
        bridge.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        bridge.layer = LayerMask.NameToLayer("Ground");

        GameObject intactRoot = new GameObject("Intact");
        intactRoot.transform.SetParent(bridge.transform, false);
        intactRoot.transform.localPosition = Vector3.zero;

        int segmentCount = 6;
        float segmentWidth = 1f / segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = CreateBoxObject("BridgeSegment_" + i.ToString("00"), sprite, Vector2.zero, new Vector2(segmentWidth * 0.92f, 1f), "Ground");
            segment.transform.SetParent(intactRoot.transform, false);
            segment.transform.localPosition = new Vector3(-0.5f + segmentWidth * 0.5f + segmentWidth * i, 0f, 0f);
            SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
                renderer.sortingOrder = 1;
            }
        }

        BoxCollider2D collider = bridge.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        GameObject destroyedRoot = new GameObject("DestroyedVisual");
        destroyedRoot.transform.SetParent(bridge.transform, false);
        destroyedRoot.transform.localPosition = new Vector3(0f, -0.55f, 0f);

        for (int i = 0; i < 4; i++)
        {
            GameObject fragment = CreateBoxObject("BrokenBridgeFragment_" + i.ToString("00"), sprite, Vector2.zero, new Vector2(0.16f, 0.25f), "Ground");
            fragment.transform.SetParent(destroyedRoot.transform, false);
            fragment.transform.localPosition = new Vector3(-0.34f + i * 0.23f, -0.1f - (i % 2) * 0.2f, 0f);
            fragment.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -18f : 16f);
            SpriteRenderer renderer = fragment.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, color.a);
                renderer.sortingOrder = 0;
            }
        }

        DestructibleBridge2D destructible = bridge.AddComponent<DestructibleBridge2D>();
        destructible.hitPoints = 3;
        destructible.intactRoot = intactRoot;
        destructible.destroyedRoot = destroyedRoot;

        CreateDesignLabel(name + "_Label", "GRENADE BREAK BRIDGE", new Vector3(position.x, position.y + scale.y * 0.5f + 0.45f, 0f)).transform.SetParent(bridge.transform);
        return bridge;
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

        GameObject label = CreateWorldText(name + "_Label", "GROUND VEHICLE", new Vector3(position.x, position.y + 1.65f, 0f), 0.2f);
        label.transform.SetParent(vehicle.transform);
        label.SetActive(false);
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

        GameObject label = CreateWorldText(name + "_Label", "PLANE FLYBY", new Vector3(position.x, position.y + 0.95f, 0f), 0.2f);
        label.transform.SetParent(plane.transform);
        label.SetActive(false);
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

        CreateWorldText("Level_Title", "LEVEL 1 - ASHLINE OUTPOST", new Vector3(-7.4f, 0.4f, 0f), 0.32f).SetActive(false);
        CreateDesignLabel("L1_A_Training_Label", "L1-A TRAINING OUTPOST\nmovement, aim, first safety space", new Vector3(-4.8f, -1.05f, 0f));
        CreateDesignLabel("L1_B_FirstContact_Label", "L1-B FIRST CONTACT ROAD\nalien grunts, clear front lane", new Vector3(4.6f, -0.85f, 0f));
        CreateDesignLabel("L1_C_Bridge_Label", "L1-C BROKEN BRIDGE\njump route, high/low firing", new Vector3(15.2f, -0.8f, 0f));
        CreateDesignLabel("L1_D_Camp_Label", "L1-D POW CAMP\nrescue, ambush, reward", new Vector3(24.8f, -1.05f, 0f));
        CreateDesignLabel("L1_E_Acid_Label", "L1-E ACID TRENCH\ngrenades, hazard pressure", new Vector3(35.7f, -0.8f, 0f));
        CreateDesignLabel("L1_F_Arena_Label", "L1-F BRUTE GATE\nfirst heavy Keth test", new Vector3(49f, -1.05f, 0f));
        CreateDesignLabel("L1_G_Descent_Label", "L1-G LAKE DESCENT\nvertical drop, swim intro", new Vector3(78f, -6.6f, 0f));
        CreateDesignLabel("L1_H_Depot_Label", "L1-H VEHICLE DEPOT\nrideable hovercraft", new Vector3(113.5f, -1.05f, 0f));
        CreateDesignLabel("L1_I_Runway_Label", "L1-I RADAR RUNWAY\nlong open sprint, sniper lanes", new Vector3(128.2f, -1.05f, 0f));
        CreateDesignLabel("L1_J_AntiAirTower_Label", "L1-J ANTI-AIR TOWER\nvertical climb and falling Keth", new Vector3(143f, -1.05f, 0f));
        CreateDesignLabel("L1_K_HangarBreach_Label", "L1-K BREACHED HANGAR\ninterior crossfire, cover pockets", new Vector3(159f, -1.05f, 0f));
        CreateDesignLabel("L1_L_ExtractElevator_Label", "L1-L EXTRACT ELEVATOR\nlast compact holdout", new Vector3(176f, -1.05f, 0f));
        CreateDesignLabel("L1_M_CommsShaft_Label", "L1-M COMMS SHAFT\nvertical climb, enemies above and below", new Vector3(205f, -0.25f, 0f));
        CreateDesignLabel("L1_N_PipelineSwitchbacks_Label", "L1-N PIPELINE SWITCHBACKS\nhigh-low route, cardinal fire lanes", new Vector3(244f, -0.25f, 0f));
        CreateDesignLabel("L1_O_CarrierSpine_Label", "L1-O CRASHED CARRIER SPINE\ndescend through wreckage, mixed ambushes", new Vector3(286f, -0.15f, 0f));
        CreateDesignLabel("L1_P_RailYard_Label", "L1-P RAIL YARD RUSH\nlong flat sprint, staggered enemy doors", new Vector3(328f, -1.05f, 0f));
        CreateDesignLabel("L1_Q_CliffBatteries_Label", "L1-Q CLIFFSIDE AA BATTERIES\nuphill pressure, upper gunners", new Vector3(362f, 0.75f, 0f));
        CreateDesignLabel("L1_R_FinalLift_Label", "L1-R FINAL EXTRACTION LIFT\nlast readable holdout", new Vector3(377f, 0.75f, 0f));

        CreateGenericRunGunBackdrops();
        CreateEnvironmentArtPass(scenerySprite, coverSprite, waterSprite);

        CreateSceneryBlock("Backdrop_Tower_Start", scenerySprite, new Vector2(-8.5f, -1f), new Vector2(1.3f, 5.5f));
        CreateSceneryBlock("Backdrop_Outpost", scenerySprite, new Vector2(4f, -0.6f), new Vector2(4.8f, 3.4f));
        CreateSceneryBlock("Backdrop_Camp", scenerySprite, new Vector2(24.8f, -0.45f), new Vector2(5.5f, 3.8f));
        CreateSceneryBlock("Backdrop_Gate", scenerySprite, new Vector2(49.5f, -0.35f), new Vector2(6.5f, 4.2f));
        CreateSceneryBlock("Backdrop_Extraction", scenerySprite, new Vector2(60.5f, -0.3f), new Vector2(4.5f, 3.7f));
        CreateSceneryBlock("Backdrop_Comms_Shaft", scenerySprite, new Vector2(205f, -0.2f), new Vector2(10f, 5.2f));
        CreateSceneryBlock("Backdrop_Pipeline_Towers", scenerySprite, new Vector2(246f, -0.1f), new Vector2(16f, 4.4f));
        CreateSceneryBlock("Backdrop_Carrier_Wreck", scenerySprite, new Vector2(286f, 0.2f), new Vector2(20f, 5.4f));
        CreateSceneryBlock("Backdrop_RailYard_Cranes", scenerySprite, new Vector2(330f, -0.35f), new Vector2(21f, 4.4f));
        CreateSceneryBlock("Backdrop_Cliff_AA_Batteries", scenerySprite, new Vector2(363f, 1.05f), new Vector2(13f, 5.8f));

        CreateGround("Border_Left", groundSprite, new Vector2(-12.1f, -1.8f), new Vector2(0.45f, 5f), floorColor);
        CreateGround("LandingZone_Floor", groundSprite, new Vector2(-5.2f, -4f), new Vector2(13.8f, 1f), floorColor);
        CreateOneWayPlatform("Training_Platform_Low", groundSprite, new Vector2(-4.2f, -2.35f), new Vector2(3.2f, 0.45f), platformColor);
        CreateOneWayPlatform("Training_Platform_High", groundSprite, new Vector2(-0.3f, -1.45f), new Vector2(2.4f, 0.45f), platformColor);
        CreateLowCover("Training_LowCover", coverSprite, new Vector2(-1.9f, -3.65f), new Vector2(1.25f, 0.55f), coverColor);

        CreateGround("Outpost_Floor", groundSprite, new Vector2(4.6f, -4f), new Vector2(7.2f, 1f), floorColor);
        CreateLowCover("Outpost_LowCover_Left", coverSprite, new Vector2(3.4f, -3.65f), new Vector2(1.35f, 0.55f), coverColor);
        CreateOneWayPlatform("Outpost_Platform", groundSprite, new Vector2(6.6f, -2.1f), new Vector2(3.1f, 0.45f), platformColor);

        CreateGround("Connector_Outpost_To_Bridge", groundSprite, new Vector2(8.45f, -4f), new Vector2(0.6f, 1f), floorColor);
        CreateGround("Bridge_Left_Ledge", groundSprite, new Vector2(10.7f, -4f), new Vector2(4.1f, 1f), floorColor);
        CreateGround("Bridge_Main_Floor_Connector", groundSprite, new Vector2(15.15f, -4f), new Vector2(4.95f, 1f), floorColor);
        CreateOneWayPlatform("Bridge_Step_01", groundSprite, new Vector2(12.8f, -2.85f), new Vector2(2.2f, 0.45f), platformColor);
        CreateOneWayPlatform("Bridge_Step_02", groundSprite, new Vector2(15.6f, -2.05f), new Vector2(2.2f, 0.45f), platformColor);
        CreateOneWayPlatform("Bridge_Step_03", groundSprite, new Vector2(18.3f, -1.35f), new Vector2(2.5f, 0.45f), platformColor);
        CreateGround("Bridge_Right_Ledge", groundSprite, new Vector2(19.8f, -4f), new Vector2(4.4f, 1f), floorColor);

        CreateGround("POW_Camp_Floor", groundSprite, new Vector2(26.1f, -4f), new Vector2(11.8f, 1f), floorColor);
        CreateLowCover("POW_Camp_CageBase", coverSprite, new Vector2(23.5f, -3.62f), new Vector2(1.7f, 0.62f), coverColor);
        CreateLowCover("POW_Camp_LowCover_Right", coverSprite, new Vector2(29.3f, -3.65f), new Vector2(1.25f, 0.55f), coverColor);
        CreateOneWayPlatform("POW_Camp_Roof", groundSprite, new Vector2(27.2f, -1.75f), new Vector2(4.4f, 0.45f), platformColor);

        CreateGround("Acid_Left_Ledge", groundSprite, new Vector2(32f, -4f), new Vector2(3.4f, 1f), floorColor);
        CreateHazard("Acid_Trench", hazardSprite, new Vector2(35.5f, -3.75f), new Vector2(5.9f, 0.55f));
        CreateOneWayPlatform("Acid_Trench_ServiceDeck", groundSprite, new Vector2(36.35f, -3.05f), new Vector2(5.4f, 0.34f), platformColor);
        CreateOneWayPlatform("Acid_Platform_01", groundSprite, new Vector2(33.5f, -2.75f), new Vector2(1.75f, 0.42f), platformColor);
        CreateOneWayPlatform("Acid_Platform_02", groundSprite, new Vector2(36f, -2f), new Vector2(1.8f, 0.42f), platformColor);
        CreateOneWayPlatform("Acid_Platform_03", groundSprite, new Vector2(38.6f, -2.75f), new Vector2(1.75f, 0.42f), platformColor);
        CreateGround("Acid_Right_Ledge", groundSprite, new Vector2(40.8f, -4f), new Vector2(3.7f, 1f), floorColor);

        CreateGround("Arena_Approach_Floor", groundSprite, new Vector2(44.4f, -4f), new Vector2(4.8f, 1f), floorColor);
        CreateGround("Arena_Floor", groundSprite, new Vector2(50.4f, -4f), new Vector2(9.4f, 1f), floorColor);
        CreateLowCover("Arena_LowCover_Left", coverSprite, new Vector2(47.6f, -3.62f), new Vector2(1.45f, 0.62f), coverColor);
        CreateLowCover("Arena_LowCover_Right", coverSprite, new Vector2(53.2f, -3.62f), new Vector2(1.45f, 0.62f), coverColor);
        CreateOneWayPlatform("Arena_Upper_Walkway", groundSprite, new Vector2(50.2f, -1.65f), new Vector2(4.6f, 0.45f), platformColor);

        CreateGround("Connector_Arena_To_Extraction", groundSprite, new Vector2(55.2f, -4f), new Vector2(0.75f, 1f), floorColor);
        CreateGround("Extraction_Floor", groundSprite, new Vector2(60f, -4f), new Vector2(9.4f, 1f), floorColor);
        CreateGround("Downhill_Step_01", groundSprite, new Vector2(67.55f, -5.2f), new Vector2(5.9f, 0.8f), floorColor);
        CreateGround("Downhill_Step_02", groundSprite, new Vector2(73.05f, -6.7f), new Vector2(5.7f, 0.8f), floorColor);
        CreateGround("Lake_Shore_Left", groundSprite, new Vector2(78.55f, -8.6f), new Vector2(6.1f, 0.85f), floorColor);
        CreateWaterZone("Submerged_Lake", waterSprite, new Vector2(89.4f, -11.35f), new Vector2(15.8f, 5.65f));
        CreateDestructibleBridge("Lake_Grenade_Bridge", groundSprite, new Vector2(89.15f, -8.25f), new Vector2(15.5f, 0.36f), new Color(0.55f, 0.46f, 0.28f, 1f));
        CreateGround("Lake_Shore_Right", groundSprite, new Vector2(99.2f, -8.6f), new Vector2(5.8f, 0.85f), floorColor);
        CreateGround("Lake_Bed_Left", groundSprite, new Vector2(83.5f, -14.2f), new Vector2(7.8f, 0.8f), floorColor);
        CreateGround("Lake_Bed_Right", groundSprite, new Vector2(92.5f, -14.2f), new Vector2(7.8f, 0.8f), floorColor);
        CreateOneWayPlatform("Lake_Mid_Rock_01", groundSprite, new Vector2(84.2f, -11.9f), new Vector2(2.2f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Mid_Rock_02", groundSprite, new Vector2(89.1f, -10.8f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Mid_Rock_03", groundSprite, new Vector2(94.1f, -12f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_00", groundSprite, new Vector2(96.1f, -12.65f), new Vector2(2.6f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_01", groundSprite, new Vector2(98.4f, -11.55f), new Vector2(2.6f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_02", groundSprite, new Vector2(100.7f, -10.45f), new Vector2(2.7f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_03", groundSprite, new Vector2(103f, -9.35f), new Vector2(2.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_04", groundSprite, new Vector2(105.3f, -8.25f), new Vector2(2.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_05", groundSprite, new Vector2(107.7f, -6.85f), new Vector2(3f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_06", groundSprite, new Vector2(110.4f, -5.25f), new Vector2(3.2f, 0.35f), platformColor);
        CreateOneWayPlatform("Lake_Exit_SwimRamp_07", groundSprite, new Vector2(112.9f, -4.25f), new Vector2(3.2f, 0.35f), platformColor);
        CreateGround("Lake_Exit_Step_00", groundSprite, new Vector2(96.1f, -13.35f), new Vector2(3.2f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_01", groundSprite, new Vector2(98.3f, -12.15f), new Vector2(3.4f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_02", groundSprite, new Vector2(100.5f, -10.95f), new Vector2(3.6f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_03", groundSprite, new Vector2(102.8f, -9.75f), new Vector2(3.8f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_04", groundSprite, new Vector2(105.2f, -8.6f), new Vector2(4.1f, 0.85f), floorColor);
        CreateGround("Lake_Exit_Step_05", groundSprite, new Vector2(108.4f, -6.7f), new Vector2(4.9f, 0.8f), floorColor);
        CreateGround("Lake_Exit_Step_06", groundSprite, new Vector2(111.1f, -5.25f), new Vector2(3.8f, 0.8f), floorColor);
        CreateGround("Vehicle_Depot_Floor", groundSprite, new Vector2(113.5f, -4f), new Vector2(10.8f, 1f), floorColor);
        CreateGround("FinalRunway_Floor", groundSprite, new Vector2(127f, -4f), new Vector2(16.4f, 1f), floorColor);
        CreateGround("AntiAir_Tower_Base", groundSprite, new Vector2(143.5f, -4f), new Vector2(12.4f, 1f), floorColor);
        CreateGround("Breached_Hangar_Floor", groundSprite, new Vector2(159.5f, -4f), new Vector2(17f, 1f), new Color(0.26f, 0.33f, 0.35f, 1f));
        CreateGround("Extract_Elevator_Floor", groundSprite, new Vector2(177.5f, -4f), new Vector2(18.3f, 1f), new Color(0.36f, 0.44f, 0.42f, 1f));
        CreateGround("Comms_Approach_Floor", groundSprite, new Vector2(194f, -4f), new Vector2(15.4f, 1f), floorColor);
        CreateGround("Comms_Riser_01", groundSprite, new Vector2(203.4f, -3.45f), new Vector2(4.05f, 0.65f), floorColor);
        CreateGround("Comms_Riser_02", groundSprite, new Vector2(206.9f, -2.8f), new Vector2(4.05f, 0.65f), floorColor);
        CreateGround("Comms_Riser_03", groundSprite, new Vector2(210.4f, -2.1f), new Vector2(4.05f, 0.65f), floorColor);
        CreateGround("Comms_Upper_Walkway", groundSprite, new Vector2(218.2f, -2.1f), new Vector2(12.7f, 0.95f), floorColor);
        CreateGround("Pipeline_Upper_Run", groundSprite, new Vector2(231f, -2.1f), new Vector2(14.2f, 0.95f), floorColor);
        CreateGround("Pipeline_Drop_01", groundSprite, new Vector2(240.6f, -2.85f), new Vector2(6.1f, 0.8f), floorColor);
        CreateGround("Pipeline_Drop_02", groundSprite, new Vector2(245.6f, -3.55f), new Vector2(6.1f, 0.8f), floorColor);
        CreateGround("Pipeline_Mid_Run", groundSprite, new Vector2(254f, -3.55f), new Vector2(11.9f, 0.95f), floorColor);
        CreateGround("Pipeline_Rise_01", groundSprite, new Vector2(262.5f, -2.85f), new Vector2(6.1f, 0.8f), floorColor);
        CreateGround("Pipeline_Rise_02", groundSprite, new Vector2(267.5f, -2.1f), new Vector2(6.1f, 0.8f), floorColor);
        CreateGround("Carrier_Spine_Floor", groundSprite, new Vector2(279.5f, -2.1f), new Vector2(19f, 0.95f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateGround("Carrier_Belly_Dip_01", groundSprite, new Vector2(291.8f, -2.9f), new Vector2(6.5f, 0.8f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateGround("Carrier_Belly_Dip_02", groundSprite, new Vector2(297.9f, -3.55f), new Vector2(6.5f, 0.8f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateGround("Carrier_Exit_Ramp", groundSprite, new Vector2(304.4f, -2.8f), new Vector2(7.7f, 0.8f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateGround("RailYard_LongFlat_A", groundSprite, new Vector2(319.5f, -4f), new Vector2(23.6f, 1f), floorColor);
        CreateGround("RailYard_LongFlat_B", groundSprite, new Vector2(342f, -4f), new Vector2(23f, 1f), floorColor);
        CreateGround("Cliff_Rise_01", groundSprite, new Vector2(356.3f, -3.15f), new Vector2(6.6f, 0.85f), floorColor);
        CreateGround("Cliff_Rise_02", groundSprite, new Vector2(361.8f, -2.35f), new Vector2(6.8f, 0.85f), floorColor);
        CreateGround("Cliff_Rise_03", groundSprite, new Vector2(367.5f, -1.55f), new Vector2(6.8f, 0.85f), floorColor);
        CreateGround("Final_Lift_Floor", groundSprite, new Vector2(376f, -1.55f), new Vector2(10.9f, 1f), new Color(0.36f, 0.44f, 0.42f, 1f));
        CreateGround("Border_Right", groundSprite, new Vector2(382.5f, -0.3f), new Vector2(0.45f, 8f), floorColor);
        CreateTerrainFace("Bridge_Step_01_Face", groundSprite, new Vector2(11.65f, -3.35f), new Vector2(0.32f, 1.35f), floorColor);
        CreateTerrainFace("Bridge_Step_02_Face", groundSprite, new Vector2(14.45f, -2.75f), new Vector2(0.32f, 1.25f), floorColor);
        CreateTerrainFace("Bridge_Step_03_Face", groundSprite, new Vector2(17f, -2.25f), new Vector2(0.32f, 1.35f), floorColor);
        CreateTerrainFace("Downhill_Face_Extraction_To_Step01", groundSprite, new Vector2(64.65f, -4.75f), new Vector2(0.35f, 1.55f), floorColor);
        CreateTerrainFace("Downhill_Face_Step01_To_Step02", groundSprite, new Vector2(70.35f, -5.9f), new Vector2(0.35f, 1.9f), floorColor);
        CreateTerrainFace("Downhill_Face_Step02_To_LakeShore", groundSprite, new Vector2(75.75f, -7.55f), new Vector2(0.35f, 2.65f), floorColor);
        CreateTerrainFace("Lake_Left_Retaining_Wall", groundSprite, new Vector2(81.55f, -11.45f), new Vector2(0.35f, 5.8f), floorColor);
        CreateTerrainFace("Lake_Right_Retaining_Wall", groundSprite, new Vector2(96.28f, -11.45f), new Vector2(0.35f, 5.8f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_00", groundSprite, new Vector2(94.5f, -13.75f), new Vector2(0.35f, 1.1f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_01", groundSprite, new Vector2(97.05f, -12.75f), new Vector2(0.35f, 1.5f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_02", groundSprite, new Vector2(99.5f, -11.55f), new Vector2(0.35f, 1.5f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_03", groundSprite, new Vector2(101.75f, -10.35f), new Vector2(0.35f, 1.5f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_04", groundSprite, new Vector2(104.15f, -9.15f), new Vector2(0.35f, 1.45f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_05", groundSprite, new Vector2(106.95f, -7.6f), new Vector2(0.35f, 2.35f), floorColor);
        CreateTerrainFace("Lake_Exit_Face_06", groundSprite, new Vector2(110.2f, -5.95f), new Vector2(0.35f, 1.75f), floorColor);
        CreateTerrainFace("Comms_Face_Approach_To_Riser01", groundSprite, new Vector2(201.55f, -3.65f), new Vector2(0.35f, 1.25f), floorColor);
        CreateTerrainFace("Comms_Face_Riser01_To_Riser02", groundSprite, new Vector2(205.35f, -3.1f), new Vector2(0.35f, 1.25f), floorColor);
        CreateTerrainFace("Comms_Face_Riser02_To_Riser03", groundSprite, new Vector2(208.85f, -2.45f), new Vector2(0.35f, 1.25f), floorColor);
        CreateTerrainFace("Pipeline_Face_Upper_To_Drop01", groundSprite, new Vector2(237.85f, -2.45f), new Vector2(0.35f, 1.45f), floorColor);
        CreateTerrainFace("Pipeline_Face_Drop01_To_Drop02", groundSprite, new Vector2(243.55f, -3.15f), new Vector2(0.35f, 1.35f), floorColor);
        CreateTerrainFace("Pipeline_Face_Rise01_To_Rise02", groundSprite, new Vector2(265.55f, -2.45f), new Vector2(0.35f, 1.35f), floorColor);
        CreateTerrainFace("Carrier_Face_Spine_To_Belly01", groundSprite, new Vector2(289.15f, -2.5f), new Vector2(0.35f, 1.5f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateTerrainFace("Carrier_Face_Belly01_To_Belly02", groundSprite, new Vector2(295.2f, -3.25f), new Vector2(0.35f, 1.4f), new Color(0.34f, 0.42f, 0.45f, 1f));
        CreateTerrainFace("Carrier_Face_Exit_To_RailYard", groundSprite, new Vector2(308.2f, -3.25f), new Vector2(0.35f, 2.2f), floorColor);
        CreateTerrainFace("Cliff_Face_RailYard_To_Rise01", groundSprite, new Vector2(353.3f, -3.6f), new Vector2(0.35f, 1.25f), floorColor);
        CreateTerrainFace("Cliff_Face_Rise01_To_Rise02", groundSprite, new Vector2(359.5f, -2.8f), new Vector2(0.35f, 1.25f), floorColor);
        CreateTerrainFace("Cliff_Face_Rise02_To_Rise03", groundSprite, new Vector2(365.1f, -2f), new Vector2(0.35f, 1.25f), floorColor);
        CreateOneWayPlatform("Downhill_CrateWalk_01", groundSprite, new Vector2(69.5f, -3.2f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Downhill_CrateWalk_02", groundSprite, new Vector2(76.5f, -5.1f), new Vector2(2.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Depot_TruckTop_Platform", groundSprite, new Vector2(113.5f, -1.95f), new Vector2(3.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Final_Runway_Service_Platform", groundSprite, new Vector2(125.5f, -2.1f), new Vector2(4.6f, 0.35f), platformColor);
        CreateOneWayPlatform("AntiAir_Tower_Lower_Gantry", groundSprite, new Vector2(140.8f, -2.1f), new Vector2(3.4f, 0.35f), platformColor);
        CreateOneWayPlatform("AntiAir_Tower_Upper_Gantry", groundSprite, new Vector2(147.2f, -0.9f), new Vector2(3.2f, 0.35f), platformColor);
        CreateOneWayPlatform("Hangar_Broken_Wing_Catwalk", groundSprite, new Vector2(156f, -2.2f), new Vector2(3.7f, 0.35f), new Color(0.62f, 0.65f, 0.7f, 1f));
        CreateOneWayPlatform("Hangar_Overhead_Crane_Rail", groundSprite, new Vector2(164f, -1.25f), new Vector2(5.2f, 0.35f), new Color(0.5f, 0.58f, 0.64f, 1f));
        CreateOneWayPlatform("Extract_Elevator_Upper_ServiceDeck", groundSprite, new Vector2(177.2f, -1.75f), new Vector2(5.2f, 0.35f), platformColor);
        CreateOneWayPlatform("Comms_Shaft_Lower_Gantry", groundSprite, new Vector2(202.5f, -0.65f), new Vector2(3.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Comms_Shaft_Upper_Gantry", groundSprite, new Vector2(211f, 0.45f), new Vector2(3.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Pipeline_Upper_Service_01", groundSprite, new Vector2(231f, -0.65f), new Vector2(4f, 0.35f), platformColor);
        CreateOneWayPlatform("Pipeline_Lower_Service_02", groundSprite, new Vector2(253.5f, -1.9f), new Vector2(4.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Carrier_Spine_Broken_Wing_01", groundSprite, new Vector2(279.5f, -0.4f), new Vector2(5.2f, 0.35f), new Color(0.62f, 0.65f, 0.7f, 1f));
        CreateOneWayPlatform("Carrier_Spine_Broken_Wing_02", groundSprite, new Vector2(290.5f, -0.95f), new Vector2(5.6f, 0.35f), new Color(0.62f, 0.65f, 0.7f, 1f));
        CreateOneWayPlatform("RailYard_FreightCar_Top_01", groundSprite, new Vector2(316f, -2.05f), new Vector2(4.2f, 0.35f), platformColor);
        CreateOneWayPlatform("RailYard_FreightCar_Top_02", groundSprite, new Vector2(337.5f, -2.05f), new Vector2(4.4f, 0.35f), platformColor);
        CreateOneWayPlatform("Cliff_AA_Lower_Battery", groundSprite, new Vector2(359.4f, -0.75f), new Vector2(3.8f, 0.35f), platformColor);
        CreateOneWayPlatform("Cliff_AA_Upper_Battery", groundSprite, new Vector2(366.6f, 0.45f), new Vector2(4f, 0.35f), platformColor);
        CreateOneWayPlatform("Final_Lift_Upper_ServiceDeck", groundSprite, new Vector2(377f, 0.3f), new Vector2(5f, 0.35f), platformColor);
        CreateEnemyWaypointGraph();
        CreateGroundVehicle("Slug_Tank_Prototype", coverSprite, new Vector2(111.2f, -3.25f));
        CreatePlane("Keth_Drop_Plane", coverSprite, new Vector2(120f, 0.8f));
        CreatePlane("Keth_Drop_Plane_Comms_Flyby", coverSprite, new Vector2(207f, 2.2f));
        CreatePlane("Keth_Drop_Plane_Carrier_Flyby", coverSprite, new Vector2(292f, 2.7f));
        CreatePlane("Evac_Plane_Final_Flyby", coverSprite, new Vector2(366f, 2.4f));
        CreateSceneryBlock("AntiAir_Tower_Shaft", scenerySprite, new Vector2(144.5f, -0.45f), new Vector2(1.2f, 5.3f));
        CreateSceneryBlock("AntiAir_Radar_Dish", coverSprite, new Vector2(147.3f, 1.8f), new Vector2(2.1f, 0.55f));
        CreateSceneryBlock("Hangar_Door_Frame_Left", scenerySprite, new Vector2(153.8f, -1.7f), new Vector2(0.8f, 4.2f));
        CreateSceneryBlock("Hangar_Door_Frame_Right", scenerySprite, new Vector2(167.2f, -1.7f), new Vector2(0.8f, 4.2f));
        CreateSceneryBlock("Extract_Elevator_Backplate", scenerySprite, new Vector2(177.5f, -1.75f), new Vector2(6.2f, 3.9f));
        CreateSceneryBlock("Comms_Tower_Spine_Left", scenerySprite, new Vector2(202f, -0.7f), new Vector2(0.75f, 5.6f));
        CreateSceneryBlock("Comms_Tower_Spine_Right", scenerySprite, new Vector2(212.2f, -0.55f), new Vector2(0.75f, 5.9f));
        CreateSceneryBlock("Pipeline_Generator_Block", coverSprite, new Vector2(248.5f, -1.05f), new Vector2(3.6f, 1.3f));
        CreateSceneryBlock("Carrier_Broken_Cockpit", coverSprite, new Vector2(276.5f, -0.75f), new Vector2(4.5f, 1.2f));
        CreateSceneryBlock("RailYard_Signal_Tower", scenerySprite, new Vector2(326f, -0.55f), new Vector2(1.2f, 4.7f));
        CreateSceneryBlock("Final_Lift_Backplate", scenerySprite, new Vector2(377f, 0.05f), new Vector2(6.4f, 4.6f));
        CreateHoverVehicle("Player_Hovercraft", hoverVehicleSprite, playerProjectilePrefab, new Vector2(115.5f, -2.45f));
        CreateCompanion("Companion_Mika", companionSprite, playerProjectilePrefab, player, new Vector2(-9.8f, -2.3f));
        CreateAmbientMover("L1_Civilian_Evac_Runner_01", powSprite, new Vector2(7.5f, -3.25f), new Vector2(0.35f, 0.65f), new Color(1f, 0.86f, 0.35f, 1f), new Vector2(-0.72f, 0f), 7f, "CIVILIAN EVAC");
        CreateAmbientMover("L1_Civilian_Evac_Runner_02", powSprite, new Vector2(26.8f, -3.25f), new Vector2(0.35f, 0.65f), new Color(1f, 0.86f, 0.35f, 1f), new Vector2(0.68f, 0f), 6.5f, "CIVILIAN EVAC");
        CreateAmbientMover("L1_Background_Comms_Convoy", hoverVehicleSprite, new Vector2(208f, -0.15f), new Vector2(2.4f, 0.5f), new Color(0.78f, 0.92f, 1f, 0.82f), new Vector2(1.15f, 0f), 18f, "BG CONVOY");
        CreateAmbientMover("L1_Background_Pipeline_ServiceDrone", checkpointSprite, new Vector2(245f, 1.6f), new Vector2(0.42f, 0.42f), new Color(0.95f, 1f, 0.65f, 0.9f), new Vector2(0f, -0.65f), 2.6f, "SERVICE DRONE");
        CreateAmbientMover("L1_Background_Carrier_Dropship", hoverVehicleSprite, new Vector2(282f, 2.05f), new Vector2(2.9f, 0.58f), new Color(0.72f, 0.82f, 0.95f, 0.78f), new Vector2(-1.7f, -0.12f), 24f, "BG DROPSHIP");
        CreateAmbientMover("L1_Background_Rail_Engine", hoverVehicleSprite, new Vector2(328f, -2.55f), new Vector2(3.2f, 0.55f), new Color(0.85f, 0.9f, 0.82f, 0.86f), new Vector2(1.35f, 0f), 22f, "BG RAIL");
        CreateAmbientMover("L1_Background_Final_EvacCraft", hoverVehicleSprite, new Vector2(370f, 1.9f), new Vector2(2.6f, 0.52f), new Color(0.95f, 0.96f, 0.82f, 0.86f), new Vector2(-1.65f, 0.08f), 21f, "BG EVAC");
        CreateAmbientMover("L1_Resistance_Soldier_Runner", companionSprite, new Vector2(151f, -3.25f), new Vector2(0.34f, 0.6f), new Color(0.8f, 0.95f, 1f, 1f), new Vector2(2.15f, 0f), 14f, "ALLY RUNNER");
        CreateAmbientMover("L1_Hangar_Worker_Panic_Run", powSprite, new Vector2(160.5f, -3.25f), new Vector2(0.35f, 0.65f), new Color(1f, 0.82f, 0.45f, 1f), new Vector2(-1.25f, 0f), 8f, "HANGAR PANIC");
        CreateAmbientMover("L1_Extract_Warning_Drone", checkpointSprite, new Vector2(176.5f, -0.35f), new Vector2(0.34f, 0.34f), new Color(1f, 0.55f, 0.2f, 0.95f), new Vector2(0f, 0.65f), 2.2f, "WARNING DRONE");
        CreateAmbientMover("L1_Final_Tech_Evac_Runner", powSprite, new Vector2(371.5f, -0.8f), new Vector2(0.35f, 0.65f), new Color(1f, 0.86f, 0.35f, 1f), new Vector2(-0.7f, 0f), 9f, "EVAC RUNNER");

        CreatePickup("Pickup_Ammo_Training", pickupAmmoSprite, new Vector2(-0.2f, -0.95f), 0, 14, 0, 50);
        CreatePickup("Pickup_Health_Bridge", pickupHealthSprite, new Vector2(18.3f, -0.75f), 1, 0, 0, 75);
        CreatePickup("Pickup_Bomb_Camp", pickupBombSprite, new Vector2(27.2f, -1.15f), 0, 0, 2, 75);
        CreatePickup("Pickup_Ammo_Arena", pickupAmmoSprite, new Vector2(45f, -3.1f), 0, 18, 0, 50);
        CreatePickup("Pickup_Health_Extraction", pickupHealthSprite, new Vector2(57.7f, -3.1f), 1, 0, 0, 100);
        CreatePickup("Pickup_Ammo_Downhill", pickupAmmoSprite, new Vector2(76.5f, -4.55f), 0, 20, 0, 50);
        CreatePickup("Pickup_Bomb_Underwater", pickupBombSprite, new Vector2(89.1f, -10.2f), 0, 0, 2, 100);
        CreatePickup("Pickup_Health_Depot", pickupHealthSprite, new Vector2(107f, -5.8f), 1, 0, 0, 100);
        CreatePickup("Pickup_Ammo_Runway", pickupAmmoSprite, new Vector2(126f, -3.1f), 0, 24, 0, 100);
        CreatePickup("Pickup_Health_CommsShaft", pickupHealthSprite, new Vector2(211f, 1.05f), 1, 0, 0, 125);
        CreatePickup("Pickup_Ammo_PipelineSwitchbacks", pickupAmmoSprite, new Vector2(253.5f, -1.3f), 0, 26, 0, 125);
        CreatePickup("Pickup_Bomb_CarrierSpine", pickupBombSprite, new Vector2(279.5f, 0.2f), 0, 0, 2, 150);
        CreatePickup("Pickup_Health_RailYard", pickupHealthSprite, new Vector2(337.5f, -1.45f), 1, 0, 0, 150);
        CreatePickup("Pickup_Ammo_CliffBatteries", pickupAmmoSprite, new Vector2(366.6f, 1.05f), 0, 30, 0, 175);

        CreateCheckpoint("Checkpoint_Bridge", checkpointSprite, new Vector2(12.2f, -3.15f), new Vector2(12.2f, -3.05f));
        CreateCheckpoint("Checkpoint_Camp_Clear", checkpointSprite, new Vector2(31.4f, -3.15f), new Vector2(31.4f, -3.05f));
        CreateCheckpoint("Checkpoint_Arena", checkpointSprite, new Vector2(43.1f, -3.15f), new Vector2(43.1f, -3.05f));
        CreateCheckpoint("Checkpoint_Descent", checkpointSprite, new Vector2(67.3f, -4.45f), new Vector2(67.3f, -4.25f));
        CreateCheckpoint("Checkpoint_LakeExit", checkpointSprite, new Vector2(103f, -8.15f), new Vector2(103f, -8f));
        CreateCheckpoint("Checkpoint_Depot", checkpointSprite, new Vector2(113.5f, -3.15f), new Vector2(113.5f, -3.05f));
        CreateCheckpoint("Checkpoint_AntiAirTower", checkpointSprite, new Vector2(139f, -3.15f), new Vector2(139f, -3.05f));
        CreateCheckpoint("Checkpoint_HangarBreach", checkpointSprite, new Vector2(155f, -3.15f), new Vector2(155f, -3.05f));
        CreateCheckpoint("Checkpoint_ExtractElevator", checkpointSprite, new Vector2(174f, -3.15f), new Vector2(174f, -3.05f));
        CreateCheckpoint("Checkpoint_CommsShaft", checkpointSprite, new Vector2(196f, -3.15f), new Vector2(196f, -3.05f));
        CreateCheckpoint("Checkpoint_PipelineSwitchbacks", checkpointSprite, new Vector2(225f, -1.25f), new Vector2(225f, -1.15f));
        CreateCheckpoint("Checkpoint_CarrierSpine", checkpointSprite, new Vector2(271f, -1.25f), new Vector2(271f, -1.15f));
        CreateCheckpoint("Checkpoint_RailYard", checkpointSprite, new Vector2(309f, -3.15f), new Vector2(309f, -3.05f));
        CreateCheckpoint("Checkpoint_CliffBatteries", checkpointSprite, new Vector2(354.5f, -2.4f), new Vector2(354.5f, -2.25f));
        CreateCheckpoint("Checkpoint_FinalLift", checkpointSprite, new Vector2(374f, -0.75f), new Vector2(374f, -0.6f));

        GameObject[] l1AntiAirWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_AntiAir_Left_Rusher", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(136.5f, -3.35f), 2, 120, true), 1),
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_AntiAir_Right_Rusher", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(151f, -3.35f), 2, 120, true), -1),
            CreateEnemy("L1_Wave_AntiAir_Gantry", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(147.8f, -1.05f), 2, 150, true),
            CreateEnemy("L1_Wave_AntiAir_Falling_Keth", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(144f, 0.2f), 2, 160, true)
        };
        CreateEnemyWaveTrigger("L1_WaveTrigger_AntiAir_Tower", new Vector2(137.5f, -2.3f), new Vector2(1.3f, 5f), l1AntiAirWave, new Vector2[] { new Vector2(136.5f, -3.35f), new Vector2(151f, -3.35f), new Vector2(147.8f, -1.05f), new Vector2(144f, 0.2f) }, 0.42f);

        GameObject[] l1HangarWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_Hangar_BackSpawn_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(151.4f, -3.35f), 2, 130, true), 1),
            CreateEnemy("L1_Wave_Hangar_CraneRail_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(156f, -1.55f), 2, 150, true),
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_Hangar_FrontSpawn_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(168.8f, -3.35f), 2, 130, true), -1),
            CreateEnemy("L1_Wave_Hangar_Brute_Blocker", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(164f, -3.2f), 10, 500, true, new Vector2(1.05f, 1.45f))
        };
        CreateEnemyWaveTrigger("L1_WaveTrigger_Hangar_Crossfire", new Vector2(153.3f, -2.3f), new Vector2(1.3f, 5f), l1HangarWave, new Vector2[] { new Vector2(151.4f, -3.35f), new Vector2(156f, -1.55f), new Vector2(168.8f, -3.35f), new Vector2(164f, -3.2f) }, 0.36f);

        GameObject[] l1ExtractWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_Extract_Left_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(170f, -3.35f), 2, 150, true), 1),
            SetEnemyPatrolDirection(CreateEnemy("L1_Wave_Extract_Right_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(183f, -3.35f), 2, 150, true), -1),
            CreateEnemy("L1_Wave_Extract_Upper_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(177.2f, -1.1f), 2, 175, true),
            CreateEnemy("L1_Wave_Extract_Core_Brute", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(179.5f, -3.2f), 14, 750, true, new Vector2(1.12f, 1.55f))
        };
        CreateEnemyWaveTrigger("L1_WaveTrigger_Extraction_Holdout", new Vector2(171.8f, -2.3f), new Vector2(1.3f, 5f), l1ExtractWave, new Vector2[] { new Vector2(170f, -3.35f), new Vector2(183f, -3.35f), new Vector2(177.2f, -1.1f), new Vector2(179.5f, -3.2f) }, 0.32f);

        Vector2[] l1ContactRushPositions = new Vector2[] { new Vector2(5.6f, -3.35f), new Vector2(8.7f, -3.35f), new Vector2(6.6f, -1.45f), new Vector2(12.4f, -3.35f), new Vector2(15.6f, -1.4f), new Vector2(18.9f, -3.35f), new Vector2(22.4f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_FirstContact_Rush", 2.6f, CreateEnemyWaveSet("L1_Cam_FirstContact", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1ContactRushPositions, new int[] { -1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, false, true }, null), l1ContactRushPositions, 0.12f, 0.28f);
        Vector2[] l1ContactRearPositions = new Vector2[] { new Vector2(1.8f, -3.35f), new Vector2(3.6f, -3.35f), new Vector2(6.1f, -3.35f), new Vector2(6.6f, -1.45f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_FirstContact_RearChase", 7.2f, CreateEnemyWaveSet("L1_Cam_FirstContactRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1ContactRearPositions, new int[] { 1, 1, 1, 1 }, new bool[] { false, false, true, true }, null), l1ContactRearPositions, 0.2f, 0.2f);

        Vector2[] l1BridgeCrossfirePositions = new Vector2[] { new Vector2(8.9f, -3.35f), new Vector2(12.8f, -2.2f), new Vector2(16f, -3.35f), new Vector2(18.3f, -0.7f), new Vector2(21.4f, -3.35f), new Vector2(24.6f, -3.35f), new Vector2(27.2f, -1.1f), new Vector2(30.8f, -3.35f), new Vector2(34.6f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Bridge_Crossfire", 9.6f, CreateEnemyWaveSet("L1_Cam_Bridge", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1BridgeCrossfirePositions, new int[] { 1, -1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, false, true, true, false, true, false, true }, null), l1BridgeCrossfirePositions, 0.1f, 0.25f);
        Vector2[] l1BridgeRearPositions = new Vector2[] { new Vector2(10.2f, -3.35f), new Vector2(12.8f, -2.2f), new Vector2(15.2f, -3.35f), new Vector2(18.3f, -0.7f), new Vector2(20.2f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Bridge_RearStagger", 17.2f, CreateEnemyWaveSet("L1_Cam_BridgeRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1BridgeRearPositions, new int[] { 1, 1, 1, 1, 1 }, new bool[] { false, true, false, true, true }, null), l1BridgeRearPositions, 0.15f, 0.22f);

        Vector2[] l1CampPressurePositions = new Vector2[] { new Vector2(20.2f, -3.35f), new Vector2(23.8f, -3.35f), new Vector2(27.2f, -1.1f), new Vector2(31.8f, -3.35f), new Vector2(36f, -1.5f), new Vector2(39.2f, -3.35f), new Vector2(25.5f, -3.25f), new Vector2(34.2f, -3.35f), new Vector2(42.5f, -3.35f), new Vector2(46.4f, -3.25f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_POWCamp_Pressure", 21.2f, CreateEnemyWaveSet("L1_Cam_Camp", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CampPressurePositions, new int[] { 1, 1, -1, -1, -1, -1, 1, -1, -1, -1 }, new bool[] { false, true, true, true, true, false, true, false, true, true }, new bool[] { false, false, false, false, false, false, true, false, false, false }), l1CampPressurePositions, 0.16f, 0.27f);
        Vector2[] l1CampRearPositions = new Vector2[] { new Vector2(22.2f, -3.35f), new Vector2(24.8f, -3.35f), new Vector2(27.2f, -1.1f), new Vector2(30.2f, -3.35f), new Vector2(34f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_POWCamp_RearPincer", 31.5f, CreateEnemyWaveSet("L1_Cam_CampRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CampRearPositions, new int[] { 1, 1, 1, 1, 1 }, new bool[] { false, true, true, false, true }, null), l1CampRearPositions, 0.1f, 0.2f);

        Vector2[] l1AcidRunPositions = new Vector2[] { new Vector2(31.8f, -3.35f), new Vector2(33.5f, -2.25f), new Vector2(36f, -1.5f), new Vector2(38.6f, -2.25f), new Vector2(41.2f, -3.35f), new Vector2(44.4f, -3.35f), new Vector2(47.4f, -3.35f), new Vector2(50.8f, -3.25f), new Vector2(53.6f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_AcidTrench_DangerLoop", 30.4f, CreateEnemyWaveSet("L1_Cam_Acid", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1AcidRunPositions, new int[] { 1, -1, -1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, true, false, true, false, true }, null), l1AcidRunPositions, 0.08f, 0.24f);

        Vector2[] l1LakeAmbushPositions = new Vector2[] { new Vector2(70.8f, -6.05f), new Vector2(77.6f, -7.95f), new Vector2(84.2f, -11.35f), new Vector2(89.1f, -10.25f), new Vector2(94.2f, -11.45f), new Vector2(99.8f, -9.9f), new Vector2(105.5f, -7.6f), new Vector2(111.5f, -4.7f), new Vector2(116.4f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Lake_Ambush", 67.5f, CreateEnemyWaveSet("L1_Cam_Lake", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1LakeAmbushPositions, new int[] { 1, 1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { true, false, true, true, true, false, true, false, true }, null), l1LakeAmbushPositions, 0.15f, 0.3f);
        Vector2[] l1LakeRearPositions = new Vector2[] { new Vector2(73.2f, -6.05f), new Vector2(78.4f, -7.95f), new Vector2(84.2f, -11.35f), new Vector2(89.1f, -10.25f), new Vector2(96.1f, -12.0f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Lake_RearDive", 84f, CreateEnemyWaveSet("L1_Cam_LakeRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1LakeRearPositions, new int[] { 1, 1, 1, 1, 1 }, new bool[] { false, true, true, true, false }, null), l1LakeRearPositions, 0.15f, 0.25f);

        Vector2[] l1RunwayPositions = new Vector2[] { new Vector2(108.8f, -3.35f), new Vector2(113.5f, -1.3f), new Vector2(119.4f, -3.35f), new Vector2(125.5f, -1.45f), new Vector2(131.5f, -3.2f), new Vector2(136.8f, -3.35f), new Vector2(142.8f, -3.35f), new Vector2(147.2f, -0.3f), new Vector2(151.5f, -3.35f), new Vector2(156.8f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Runway_ChaoticPush", 106f, CreateEnemyWaveSet("L1_Cam_Runway", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1RunwayPositions, new int[] { 1, -1, -1, -1, -1, 1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, true, false, true, true, false, true }, new bool[] { false, false, false, false, true, false, false, false, false, false }), l1RunwayPositions, 0.1f, 0.24f);
        Vector2[] l1RunwayRearPositions = new Vector2[] { new Vector2(108.8f, -3.35f), new Vector2(113.5f, -1.3f), new Vector2(118.4f, -3.35f), new Vector2(124.5f, -3.35f), new Vector2(128.5f, -3.35f), new Vector2(133.5f, -3.2f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Runway_RearDoorFlood", 126f, CreateEnemyWaveSet("L1_Cam_RunwayRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1RunwayRearPositions, new int[] { 1, 1, 1, 1, 1, 1 }, new bool[] { false, true, false, true, false, true }, null), l1RunwayRearPositions, 0.1f, 0.18f);

        Vector2[] l1HangarPressurePositions = new Vector2[] { new Vector2(136.4f, -3.35f), new Vector2(141.2f, -3.35f), new Vector2(147.2f, -0.3f), new Vector2(151.8f, -3.35f), new Vector2(156f, -1.55f), new Vector2(162.4f, -3.35f), new Vector2(169.8f, -3.35f), new Vector2(176.8f, -1.1f), new Vector2(181.2f, -3.2f), new Vector2(186.8f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Hangar_ElevatorPressure", 135.5f, CreateEnemyWaveSet("L1_Cam_Hangar", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1HangarPressurePositions, new int[] { 1, 1, -1, 1, -1, 1, -1, 1, -1, -1 }, new bool[] { false, true, true, false, true, true, true, false, true, true }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l1HangarPressurePositions, 0.08f, 0.24f);

        Vector2[] l1CommsShaftPositions = new Vector2[] { new Vector2(192f, -3.35f), new Vector2(198.4f, -3.35f), new Vector2(202.5f, -0.05f), new Vector2(206.9f, -2.15f), new Vector2(211f, 0.95f), new Vector2(216.2f, -1.45f), new Vector2(222.2f, -1.45f), new Vector2(226.5f, -1.45f), new Vector2(218.8f, -1.35f), new Vector2(232.6f, -1.45f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_CommsShaft_VerticalPinch", 188f, CreateEnemyWaveSet("L1_Cam_Comms", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CommsShaftPositions, new int[] { 1, 1, -1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, true, false, true, true, false }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l1CommsShaftPositions, 0.1f, 0.26f);
        Vector2[] l1CommsRearPositions = new Vector2[] { new Vector2(194f, -3.35f), new Vector2(202.5f, -0.05f), new Vector2(203.4f, -2.85f), new Vector2(211f, 1.05f), new Vector2(218.2f, -1.45f), new Vector2(224.8f, -1.45f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_CommsShaft_RearDrop", 207f, CreateEnemyWaveSet("L1_Cam_CommsRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CommsRearPositions, new int[] { 1, 1, 1, 1, 1, 1 }, new bool[] { false, true, false, true, false, true }, null), l1CommsRearPositions, 0.1f, 0.2f);

        Vector2[] l1PipelinePositions = new Vector2[] { new Vector2(225f, -1.45f), new Vector2(231f, -1.45f), new Vector2(238.5f, -2.2f), new Vector2(245f, -2.9f), new Vector2(253.5f, -2.9f), new Vector2(262.5f, -2.2f), new Vector2(268.5f, -1.45f), new Vector2(255f, -1.3f), new Vector2(272f, -1.45f), new Vector2(276.8f, -1.45f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_Pipeline_SwitchbackCrossfire", 224.5f, CreateEnemyWaveSet("L1_Cam_Pipeline", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1PipelinePositions, new int[] { 1, -1, 1, -1, -1, 1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, false, true, true, true, false, true }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l1PipelinePositions, 0.1f, 0.26f);

        Vector2[] l1CarrierPositions = new Vector2[] { new Vector2(270.5f, -1.45f), new Vector2(277f, -1.45f), new Vector2(279.5f, 0.2f), new Vector2(286.5f, -1.45f), new Vector2(290.5f, -0.35f), new Vector2(296.5f, -2.9f), new Vector2(302.5f, -2.15f), new Vector2(309f, -2.15f), new Vector2(313.8f, -3.35f), new Vector2(318.6f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_CarrierSpine_WreckAmbush", 267.5f, CreateEnemyWaveSet("L1_Cam_Carrier", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CarrierPositions, new int[] { 1, 1, -1, -1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, true, false, true, true, false }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l1CarrierPositions, 0.08f, 0.24f);

        Vector2[] l1RailYardPositions = new Vector2[] { new Vector2(308.8f, -3.35f), new Vector2(314f, -3.35f), new Vector2(316f, -1.45f), new Vector2(322f, -3.35f), new Vector2(328f, -3.35f), new Vector2(337.5f, -1.45f), new Vector2(343f, -3.35f), new Vector2(350f, -3.35f), new Vector2(354.5f, -2.5f), new Vector2(346f, -3.2f), new Vector2(358.8f, -2.5f), new Vector2(363.5f, -1.65f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_RailYard_StaggeredDoors", 305f, CreateEnemyWaveSet("L1_Cam_RailYard", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1RailYardPositions, new int[] { 1, 1, -1, 1, -1, -1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, true, false, true, true, true, false, true }, new bool[] { false, false, false, false, false, false, false, false, false, true, false, false }), l1RailYardPositions, 0.08f, 0.22f);
        Vector2[] l1RailRearPositions = new Vector2[] { new Vector2(309f, -3.35f), new Vector2(316f, -1.45f), new Vector2(321.5f, -3.35f), new Vector2(328f, -3.35f), new Vector2(337.5f, -1.45f), new Vector2(342f, -3.35f), new Vector2(350f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_RailYard_RearDoorFlood", 329f, CreateEnemyWaveSet("L1_Cam_RailRear", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1RailRearPositions, new int[] { 1, 1, 1, 1, 1, 1, 1 }, new bool[] { false, true, false, true, true, false, true }, null), l1RailRearPositions, 0.08f, 0.18f);

        Vector2[] l1CliffPositions = new Vector2[] { new Vector2(352.5f, -2.5f), new Vector2(356f, -2.5f), new Vector2(359.4f, -0.15f), new Vector2(362f, -1.65f), new Vector2(366.6f, 1.05f), new Vector2(370f, -0.75f), new Vector2(374.5f, -0.75f), new Vector2(378.5f, -0.75f), new Vector2(372.5f, -0.75f), new Vector2(381.2f, -0.75f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_CliffBatteries_UphillPressure", 348.5f, CreateEnemyWaveSet("L1_Cam_Cliff", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1CliffPositions, new int[] { 1, 1, -1, 1, -1, -1, 1, -1, -1, -1 }, new bool[] { false, true, true, false, true, true, false, true, true, true }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l1CliffPositions, 0.1f, 0.24f);

        Vector2[] l1FinalLiftPositions = new Vector2[] { new Vector2(366.6f, 1.05f), new Vector2(370.5f, -0.75f), new Vector2(374f, -0.75f), new Vector2(377f, 0.85f), new Vector2(379f, -0.75f), new Vector2(381f, -0.75f), new Vector2(372.2f, -0.75f), new Vector2(376.2f, -0.75f) };
        CreateCameraEnemyWaveTrigger("L1_CameraWave_FinalLift_LastHoldout", 362.5f, CreateEnemyWaveSet("L1_Cam_FinalLift", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l1FinalLiftPositions, new int[] { -1, 1, -1, -1, -1, -1, 1, -1 }, new bool[] { true, false, true, true, true, true, false, true }, new bool[] { false, false, true, false, false, false, false, false }), l1FinalLiftPositions, 0.2f, 0.22f);

        CreatePOW("POW_Camp_Prisoner", powSprite, new Vector2(22.5f, -3.3f));
        CreatePOW("POW_Arena_Prisoner", powSprite, new Vector2(46.1f, -3.3f));
        CreatePOW("POW_Hangar_Prisoner", powSprite, new Vector2(160.8f, -3.3f));
        CreatePOW("POW_Comms_Engineer", powSprite, new Vector2(216f, -1.4f));
        CreatePOW("POW_RailYard_Mechanic", powSprite, new Vector2(331f, -3.3f));
        CreatePOW("POW_Final_Evacuee", powSprite, new Vector2(371.5f, -0.8f));
        CreateEndTrigger(endSprite, new Vector2(378.5f, -0.75f), completePanel);
    }

    private static void CreateBloomBasinLevel(
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
        Color basinGround = new Color(0.26f, 0.62f, 0.48f, 1f);
        Color alienSoil = new Color(0.43f, 0.38f, 0.68f, 1f);
        Color platformColor = new Color(0.82f, 0.72f, 0.34f, 1f);
        Color hatcheryColor = new Color(0.42f, 0.72f, 0.86f, 1f);
        Color coverColor = new Color(0.56f, 0.66f, 0.78f, 1f);
        Color alienGlow = new Color(0.78f, 0.25f, 0.92f, 0.55f);

        CreateWorldText("Level_Title", "LEVEL 2 - KETH BLOOM BASIN", new Vector3(-6.5f, 0.45f, 0f), 0.32f).SetActive(false);
        CreateDesignLabel("L2_A_Overlook_Label", "L2-A BASIN OVERLOOK\ncalm reveal, alien landmark", new Vector3(-4.7f, -0.9f, 0f));
        CreateDesignLabel("L2_B_BloomFields_Label", "L2-B BLOOM FIELDS\npatrols, pods, short rests", new Vector3(12.8f, -0.7f, 0f));
        CreateDesignLabel("L2_C_Pumpworks_Label", "L2-C VERTICAL PUMPWORKS\nup/down routes, cardinal fire lanes", new Vector3(36.5f, -0.45f, 0f));
        CreateDesignLabel("L2_D_SporeVent_Label", "L2-D SPORE VENT SHAFT\ndry vertical descent, toxic pulses", new Vector3(67f, -7.2f, 0f));
        CreateDesignLabel("L2_E_CompanionCut_Label", "L2-E COMPANION CUT\noptional upper reward route", new Vector3(96f, -4.4f, 0f));
        CreateDesignLabel("L2_F_SkimmerCanyon_Label", "L2-F XENO-SKIMMER CANYON\ncaptured alien ride, open air", new Vector3(123f, -0.5f, 0f));
        CreateDesignLabel("L2_G_Hatchery_Label", "L2-G KETH HATCHERY\nalien interior, POW objective", new Vector3(150f, -0.35f, 0f));
        CreateDesignLabel("L2_H_BruteArena_Label", "L2-H BRUTE PAIR ARENA\nlane control, heavy aliens", new Vector3(174f, -0.45f, 0f));
        CreateDesignLabel("L2_I_Core_Label", "L2-I TERRAFORMER CORE\ncore guard and reactor hazard", new Vector3(195f, -0.45f, 0f));
        CreateDesignLabel("L2_J_ReactorRun_Label", "L2-J REACTOR RUN\nmoving platforms and swarm waves", new Vector3(219f, -0.45f, 0f));
        CreateDesignLabel("L2_K_Skybridge_Label", "L2-K SKYBRIDGE SWARM\nopen lanes, flyers, no lake", new Vector3(239f, -0.45f, 0f));
        CreateDesignLabel("L2_L_CarrierMouth_Label", "L2-L CARRIER MOUTH\nfinal alien escape gate", new Vector3(258f, -0.45f, 0f));

        CreateSceneryBlock("L2_Backdrop_Basin_Rim", scenerySprite, new Vector2(-5.5f, -0.55f), new Vector2(5.5f, 3.8f));
        CreateSceneryBlock("L2_Backdrop_Bloom_Canopy", scenerySprite, new Vector2(14f, -0.35f), new Vector2(11f, 4.5f));
        CreateSceneryBlock("L2_Backdrop_Pump_Towers", scenerySprite, new Vector2(39f, -0.2f), new Vector2(9f, 5.5f));
        CreateSceneryBlock("L2_Backdrop_Flooded_Ruins", scenerySprite, new Vector2(69f, -5.8f), new Vector2(12f, 5.8f));
        CreateSceneryBlock("L2_Backdrop_Hatchery_Wall", scenerySprite, new Vector2(151f, -0.1f), new Vector2(12f, 5.5f));
        CreateSceneryBlock("L2_Backdrop_Terraformer_Core", scenerySprite, new Vector2(196f, -0.1f), new Vector2(9f, 6.2f));
        CreateSceneryBlock("L2_Backdrop_Reactor_Run", scenerySprite, new Vector2(221f, -0.1f), new Vector2(14f, 6.2f));
        CreateSceneryBlock("L2_Backdrop_Skybridge", scenerySprite, new Vector2(241f, 0.15f), new Vector2(14f, 5.8f));
        CreateSceneryBlock("L2_Backdrop_Carrier_Mouth", scenerySprite, new Vector2(260f, -0.1f), new Vector2(11f, 6.5f));

        CreateAlienMarker("L2_AlienGrowth_Overlook_01", scenerySprite, new Vector2(-0.2f, -3.2f), new Vector2(0.7f, 1.4f), alienGlow, "ALIEN GROWTH");
        CreateAlienMarker("L2_AlienGrowth_Bloom_01", scenerySprite, new Vector2(9.4f, -3.05f), new Vector2(0.75f, 1.7f), alienGlow, "BLOOM POD");
        CreateAlienMarker("L2_AlienGrowth_Bloom_02", scenerySprite, new Vector2(18.5f, -3.05f), new Vector2(0.75f, 1.7f), alienGlow, "BLOOM POD");
        CreateAlienMarker("L2_AlienGrowth_Hatchery_01", scenerySprite, new Vector2(145f, -3.05f), new Vector2(1.1f, 1.9f), alienGlow, "HATCHERY POD");
        CreateAlienMarker("L2_AlienGrowth_Hatchery_02", scenerySprite, new Vector2(154f, -3.05f), new Vector2(1.1f, 1.9f), alienGlow, "HATCHERY POD");
        CreateAlienMarker("L2_Core_Pylon_Left", scenerySprite, new Vector2(190f, -2.2f), new Vector2(0.8f, 3.5f), alienGlow, "CORE PYLON");
        CreateAlienMarker("L2_Core_Pylon_Right", scenerySprite, new Vector2(199f, -2.2f), new Vector2(0.8f, 3.5f), alienGlow, "CORE PYLON");
        CreateAlienMarker("L2_Reactor_Tube_01", scenerySprite, new Vector2(217f, -2.15f), new Vector2(0.85f, 3.4f), alienGlow, "REACTOR TUBE");
        CreateAlienMarker("L2_Reactor_Tube_02", scenerySprite, new Vector2(225f, -2.15f), new Vector2(0.85f, 3.4f), alienGlow, "REACTOR TUBE");
        CreateAlienMarker("L2_Carrier_Maw_Left", scenerySprite, new Vector2(253.8f, -2.15f), new Vector2(1.1f, 3.8f), alienGlow, "CARRIER MAW");
        CreateAlienMarker("L2_Carrier_Maw_Right", scenerySprite, new Vector2(263.2f, -2.15f), new Vector2(1.1f, 3.8f), alienGlow, "CARRIER MAW");

        CreateGround("L2_Border_Left", groundSprite, new Vector2(-12.1f, -1.8f), new Vector2(0.45f, 5f), basinGround);
        CreateGround("L2_A_Overlook_Floor", groundSprite, new Vector2(-4.8f, -4f), new Vector2(14.5f, 1f), basinGround);
        CreateGround("L2_A_Overlook_Cover", coverSprite, new Vector2(-1.8f, -3.2f), new Vector2(0.65f, 1.6f), coverColor);
        CreateOneWayPlatform("L2_A_Overlook_UpperTraining_Platform", groundSprite, new Vector2(1.8f, -2.15f), new Vector2(3f, 0.4f), platformColor);

        CreateGround("L2_Connector_Overlook_To_Bloom", groundSprite, new Vector2(3.65f, -4f), new Vector2(2.5f, 1f), basinGround);
        CreateGround("L2_B_BloomField_Floor_01", groundSprite, new Vector2(9.2f, -4f), new Vector2(9f, 1f), alienSoil);
        CreateGround("L2_B_BloomField_Floor_02", groundSprite, new Vector2(19.5f, -4f), new Vector2(10f, 1f), alienSoil);
        CreateGround("L2_B_Bloom_Cover_Left", coverSprite, new Vector2(12.5f, -3.25f), new Vector2(0.6f, 1.45f), coverColor);
        CreateGround("L2_B_Bloom_Cover_Right", coverSprite, new Vector2(22.5f, -3.25f), new Vector2(0.6f, 1.45f), coverColor);
        CreateOneWayPlatform("L2_B_Bloom_Canopy_Platform_01", groundSprite, new Vector2(12.8f, -1.9f), new Vector2(3.8f, 0.4f), platformColor);
        CreateOneWayPlatform("L2_B_Bloom_Canopy_Platform_02", groundSprite, new Vector2(20.6f, -2.15f), new Vector2(4f, 0.4f), platformColor);
        CreateDesignLabel("L2_B_Component_Label", "COMPONENT: bloom pods create landmarks and cover spacing", new Vector3(16f, -5.05f, 0f));

        CreateGround("L2_Connector_Bloom_To_Pumpworks", groundSprite, new Vector2(25f, -4f), new Vector2(1.2f, 1f), basinGround);
        CreateGround("L2_C_Pumpworks_Entry_Floor", groundSprite, new Vector2(29f, -4f), new Vector2(7f, 1f), basinGround);
        CreateGround("L2_C_Pumpworks_Lower_Floor", groundSprite, new Vector2(38f, -5.3f), new Vector2(8.5f, 0.8f), basinGround);
        CreateGround("L2_C_Pumpworks_Exit_Floor", groundSprite, new Vector2(50.2f, -4f), new Vector2(9f, 1f), basinGround);
        CreateOneWayPlatform("L2_C_Pump_Lift_01", groundSprite, new Vector2(33.7f, -2.4f), new Vector2(2.7f, 0.36f), platformColor);
        CreateOneWayPlatform("L2_C_Pump_Lift_02", groundSprite, new Vector2(38.5f, -1.2f), new Vector2(2.7f, 0.36f), platformColor);
        CreateOneWayPlatform("L2_C_Pump_Lift_03", groundSprite, new Vector2(43.3f, -2.35f), new Vector2(2.7f, 0.36f), platformColor);
        CreateGround("L2_C_Pump_Cover_Lower", coverSprite, new Vector2(39.7f, -4.55f), new Vector2(0.65f, 1.45f), coverColor);
        CreateDesignLabel("L2_C_Component_Label", "COMPONENT: vertical platforms test up/down shots", new Vector3(39f, -6.4f, 0f));

        CreateGround("L2_D_SporeShaft_EntryLip", groundSprite, new Vector2(57f, -5.2f), new Vector2(5.6f, 0.8f), basinGround);
        CreateGround("L2_D_SporeShaft_LeftShelf", groundSprite, new Vector2(62.2f, -7.1f), new Vector2(4.7f, 0.75f), basinGround);
        CreateGround("L2_D_SporeShaft_RightShelf", groundSprite, new Vector2(69.2f, -9.05f), new Vector2(4.7f, 0.75f), basinGround);
        CreateGround("L2_D_SporeShaft_LowBridge", groundSprite, new Vector2(78.2f, -11.6f), new Vector2(8.6f, 0.75f), new Color(0.32f, 0.44f, 0.38f, 1f));
        CreateHazard("L2_D_SporeVent_Pulse_01", hazardSprite, new Vector2(66f, -6.7f), new Vector2(1.25f, 2.7f));
        CreateHazard("L2_D_SporeVent_Pulse_02", hazardSprite, new Vector2(74f, -9.4f), new Vector2(1.25f, 3.1f));
        CreateHazard("L2_D_SporeVent_Pulse_03", hazardSprite, new Vector2(83.8f, -10.8f), new Vector2(1.25f, 2.5f));
        CreateOneWayPlatform("L2_D_SporeShelf_01", groundSprite, new Vector2(64.2f, -4.65f), new Vector2(2.2f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_D_SporeShelf_02", groundSprite, new Vector2(70.4f, -6.1f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_D_SporeShelf_03", groundSprite, new Vector2(76.6f, -8.25f), new Vector2(2.4f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_D_SporeShelf_04", groundSprite, new Vector2(83.2f, -7.2f), new Vector2(2.5f, 0.35f), platformColor);
        CreateGround("L2_D_SporeShaft_ExitRamp_01", groundSprite, new Vector2(89f, -9.25f), new Vector2(4.8f, 0.8f), basinGround);
        CreateGround("L2_D_SporeShaft_ExitRamp_02", groundSprite, new Vector2(93.4f, -7.1f), new Vector2(4.8f, 0.8f), basinGround);

        CreateGround("L2_E_CompanionCut_Floor", groundSprite, new Vector2(99.5f, -6.2f), new Vector2(10.5f, 0.85f), basinGround);
        CreateOneWayPlatform("L2_E_Optional_UpperRoute_01", groundSprite, new Vector2(97f, -3.7f), new Vector2(3.4f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_E_Optional_UpperRoute_02", groundSprite, new Vector2(103.5f, -2.85f), new Vector2(3.4f, 0.35f), platformColor);
        CreateGround("L2_E_Reward_Cache_Block", coverSprite, new Vector2(104.8f, -5.35f), new Vector2(0.75f, 1.45f), coverColor);
        CreateDesignLabel("L2_E_Component_Label", "COMPONENT: companion covers optional reward route", new Vector3(101.2f, -7.25f, 0f));

        CreateGround("L2_F_Canyon_Dock", groundSprite, new Vector2(113.5f, -4f), new Vector2(9f, 1f), basinGround);
        CreateGround("L2_F_Canyon_Floor_Low", groundSprite, new Vector2(126f, -6.5f), new Vector2(12f, 0.8f), basinGround);
        CreateGround("L2_F_Canyon_Exit", groundSprite, new Vector2(138f, -4f), new Vector2(8f, 1f), basinGround);
        CreateOneWayPlatform("L2_F_AirLane_Service_01", groundSprite, new Vector2(121f, -1.7f), new Vector2(3.8f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_F_AirLane_Service_02", groundSprite, new Vector2(130f, -2.3f), new Vector2(3.8f, 0.35f), platformColor);
        CreateAlienSkimmerVehicle("L2_Captured_XenoSkimmer", hoverVehicleSprite, playerProjectilePrefab, new Vector2(113.2f, -2.45f));
        CreateAlienMarker("L2_F_Floating_Rib_Arch", scenerySprite, new Vector2(128f, -0.8f), new Vector2(4.8f, 1.3f), alienGlow, "RIB ARCH");

        CreateGround("L2_G_Hatchery_Floor", groundSprite, new Vector2(150f, -4f), new Vector2(15f, 1f), hatcheryColor);
        CreateGround("L2_G_Hatchery_Upper_Walkway", groundSprite, new Vector2(151f, -1.65f), new Vector2(7.8f, 0.45f), platformColor);
        CreateGround("L2_G_Hatchery_Left_Cover", coverSprite, new Vector2(145.9f, -3.2f), new Vector2(0.65f, 1.55f), coverColor);
        CreateGround("L2_G_Hatchery_Right_Cover", coverSprite, new Vector2(155.2f, -3.2f), new Vector2(0.65f, 1.55f), coverColor);
        CreateHazard("L2_G_BioAcid_Pit", hazardSprite, new Vector2(150.3f, -3.62f), new Vector2(3.4f, 0.45f));
        CreateDesignLabel("L2_G_Component_Label", "COMPONENT: hatchery is an alien objective room", new Vector3(150f, -5.05f, 0f));

        CreateGround("L2_H_Arena_Approach", groundSprite, new Vector2(163f, -4f), new Vector2(8f, 1f), basinGround);
        CreateGround("L2_H_BruteArena_Floor", groundSprite, new Vector2(174f, -4f), new Vector2(13f, 1f), basinGround);
        CreateGround("L2_H_BruteArena_Left_Cover", coverSprite, new Vector2(169.2f, -3.2f), new Vector2(0.65f, 1.55f), coverColor);
        CreateGround("L2_H_BruteArena_Right_Cover", coverSprite, new Vector2(178.6f, -3.2f), new Vector2(0.65f, 1.55f), coverColor);
        CreateOneWayPlatform("L2_H_BruteArena_Overlook_Platform", groundSprite, new Vector2(174f, -1.7f), new Vector2(5.6f, 0.4f), platformColor);
        CreateDesignLabel("L2_H_Component_Label", "COMPONENT: two Brutes force lane switching", new Vector3(174f, -5.05f, 0f));

        CreateGround("L2_I_Core_Entry_Floor", groundSprite, new Vector2(186f, -4f), new Vector2(7.5f, 1f), hatcheryColor);
        CreateGround("L2_I_Terraformer_Core_Floor", groundSprite, new Vector2(196f, -4f), new Vector2(12f, 1f), hatcheryColor);
        CreateGround("L2_I_Core_Exit_Floor", groundSprite, new Vector2(206f, -4f), new Vector2(7f, 1f), hatcheryColor);
        CreateOneWayPlatform("L2_I_Core_Upper_Left", groundSprite, new Vector2(192f, -1.55f), new Vector2(3.4f, 0.4f), platformColor);
        CreateOneWayPlatform("L2_I_Core_Upper_Right", groundSprite, new Vector2(200f, -1.55f), new Vector2(3.4f, 0.4f), platformColor);
        CreateHazard("L2_I_Core_Energy_Hazard", hazardSprite, new Vector2(196f, -3.55f), new Vector2(4.5f, 0.42f));
        CreateGround("L2_J_Reactor_Run_Floor_01", groundSprite, new Vector2(218f, -4f), new Vector2(10f, 1f), hatcheryColor);
        CreateGround("L2_J_Reactor_Run_Floor_02", groundSprite, new Vector2(229f, -5.2f), new Vector2(10f, 0.8f), hatcheryColor);
        GameObject movingReactorPlatformA = CreateOneWayPlatform("L2_J_Moving_Reactor_Platform_01", groundSprite, new Vector2(216f, -1.7f), new Vector2(3.4f, 0.35f), platformColor);
        movingReactorPlatformA.AddComponent<SimpleMover2D>().localMoveOffset = new Vector2(0f, 1.4f);
        GameObject movingReactorPlatformB = CreateOneWayPlatform("L2_J_Moving_Reactor_Platform_02", groundSprite, new Vector2(224f, -2.15f), new Vector2(3.4f, 0.35f), platformColor);
        movingReactorPlatformB.AddComponent<SimpleMover2D>().localMoveOffset = new Vector2(0f, -1.2f);
        CreateHazard("L2_J_Reactor_Pulse_Hazard", hazardSprite, new Vector2(222f, -3.55f), new Vector2(4.2f, 0.42f));
        CreateGround("L2_K_Skybridge_Floor_01", groundSprite, new Vector2(240f, -4f), new Vector2(13f, 1f), basinGround);
        CreateOneWayPlatform("L2_K_Skybridge_Upper_Left", groundSprite, new Vector2(235f, -1.8f), new Vector2(4.2f, 0.35f), platformColor);
        CreateOneWayPlatform("L2_K_Skybridge_Upper_Right", groundSprite, new Vector2(246f, -1.95f), new Vector2(4.2f, 0.35f), platformColor);
        CreateGround("L2_L_Carrier_Mouth_Floor", groundSprite, new Vector2(258.5f, -4f), new Vector2(16f, 1f), hatcheryColor);
        CreateOneWayPlatform("L2_L_Carrier_Upper_Mandible", groundSprite, new Vector2(258.5f, -1.55f), new Vector2(6f, 0.4f), platformColor);
        CreateHazard("L2_L_Carrier_Acid_Maw", hazardSprite, new Vector2(258.5f, -3.52f), new Vector2(4.5f, 0.42f));
        CreateGround("L2_Border_Right", groundSprite, new Vector2(268.5f, -1.8f), new Vector2(0.45f, 5f), basinGround);

        CreatePickup("L2_Pickup_Ammo_Overlook", pickupAmmoSprite, new Vector2(1.8f, -1.55f), 0, 18, 0, 50);
        CreatePickup("L2_Pickup_Bomb_Bloom", pickupBombSprite, new Vector2(20.6f, -1.55f), 0, 0, 2, 75);
        CreatePickup("L2_Pickup_Health_Pumpworks", pickupHealthSprite, new Vector2(43.3f, -1.75f), 1, 0, 0, 75);
        CreatePickup("L2_Pickup_Ammo_SporeShaft", pickupAmmoSprite, new Vector2(77.4f, -7.65f), 0, 24, 0, 100);
        CreatePickup("L2_Pickup_Health_CompanionCut", pickupHealthSprite, new Vector2(103.5f, -2.25f), 1, 0, 0, 125);
        CreatePickup("L2_Pickup_Bomb_Hatchery", pickupBombSprite, new Vector2(151f, -1.05f), 0, 0, 2, 125);
        CreatePickup("L2_Pickup_Ammo_Core", pickupAmmoSprite, new Vector2(192f, -0.95f), 0, 28, 0, 125);
        CreatePickup("L2_Pickup_Health_ReactorRun", pickupHealthSprite, new Vector2(224f, -1.55f), 1, 0, 0, 150);
        CreatePickup("L2_Pickup_Bomb_Skybridge", pickupBombSprite, new Vector2(246f, -1.35f), 0, 0, 2, 150);
        CreatePickup("L2_Pickup_Ammo_CarrierMouth", pickupAmmoSprite, new Vector2(258.5f, -0.95f), 0, 30, 0, 175);

        CreateCheckpoint("L2_Checkpoint_BloomFields", checkpointSprite, new Vector2(7f, -3.15f), new Vector2(7f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_Pumpworks", checkpointSprite, new Vector2(29.2f, -3.15f), new Vector2(29.2f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_FloodedRuins", checkpointSprite, new Vector2(61.2f, -6.35f), new Vector2(61.2f, -6.2f));
        CreateCheckpoint("L2_Checkpoint_CompanionCut", checkpointSprite, new Vector2(96f, -5.45f), new Vector2(96f, -5.3f));
        CreateCheckpoint("L2_Checkpoint_HoverDock", checkpointSprite, new Vector2(112f, -3.15f), new Vector2(112f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_Hatchery", checkpointSprite, new Vector2(143.5f, -3.15f), new Vector2(143.5f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_Core", checkpointSprite, new Vector2(186.4f, -3.15f), new Vector2(186.4f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_ReactorRun", checkpointSprite, new Vector2(214f, -3.15f), new Vector2(214f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_Skybridge", checkpointSprite, new Vector2(236f, -3.15f), new Vector2(236f, -3.05f));
        CreateCheckpoint("L2_Checkpoint_CarrierMouth", checkpointSprite, new Vector2(252f, -3.15f), new Vector2(252f, -3.05f));

        CreateCompanion("Companion_Mika_L2", companionSprite, playerProjectilePrefab, player, new Vector2(-9.8f, -2.3f));
        CreateAlienMarker("L2_E_BoneBridge_Relic", scenerySprite, new Vector2(107.2f, -5.2f), new Vector2(2.8f, 0.75f), alienGlow, "BONE RELIC");
        CreateAmbientMover("L2_Captured_Civilian_Line_01", powSprite, new Vector2(146f, -3.25f), new Vector2(0.33f, 0.62f), new Color(1f, 0.78f, 0.35f, 1f), new Vector2(1.1f, 0f), 8f, "CAPTIVE LINE");
        CreateAmbientMover("L2_Keth_Larva_Swarm_01", enemySprite, new Vector2(232f, -2.35f), new Vector2(0.42f, 0.28f), new Color(0.85f, 0.35f, 1f, 0.95f), new Vector2(-2.1f, 0.15f), 13f, "ALIEN SWARM");
        CreateAmbientMover("L2_Keth_Larva_Swarm_02", enemySprite, new Vector2(262f, -1.95f), new Vector2(0.42f, 0.28f), new Color(0.85f, 0.35f, 1f, 0.95f), new Vector2(-2.6f, -0.1f), 16f, "ALIEN SWARM");

        CreateEnemy("L2_Keth_Alien_Grunt_Overlook", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(2.9f, -3.35f), 2, 120, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Bloom_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(11.2f, -3.35f), 2, 120, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Bloom_Canopy", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(20.6f, -1.5f), 2, 150, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Pump_Lower", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(39.5f, -4.65f), 2, 140, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Pump_Upper", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(43.3f, -1.8f), 2, 160, true);
        CreateEnemy("L2_Keth_Alien_Brute_PumpExit", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(51.6f, -3.2f), 9, 420, true, new Vector2(1.05f, 1.42f));
        CreateEnemy("L2_Keth_Alien_Grunt_Descent", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(62.2f, -6.45f), 2, 140, true);
        CreateSwimmingEnemy("L2_Keth_Aquatic_Eel_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(72.5f, -12.5f), new Vector2(3.4f, 0.2f));
        CreateSwimmingEnemy("L2_Keth_Aquatic_Eel_Right", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(82.5f, -13.2f), new Vector2(-3.8f, 0.4f));
        CreateEnemy("L2_Keth_Alien_Grunt_CompanionCut", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(101f, -5.55f), 2, 150, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Canyon_Dock", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(116.5f, -3.35f), 2, 150, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Canyon_Low", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(130f, -5.85f), 2, 150, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Hatchery_Walkway", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(151f, -1f), 2, 170, true);
        CreateEnemy("L2_Keth_Alien_Brute_Hatchery", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(156.8f, -3.2f), 10, 520, true, new Vector2(1.05f, 1.45f));
        CreateEnemy("L2_Keth_Alien_Brute_Arena_Left", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(170.5f, -3.2f), 11, 560, true, new Vector2(1.05f, 1.45f));
        CreateEnemy("L2_Keth_Alien_Brute_Arena_Right", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(178f, -3.2f), 11, 560, true, new Vector2(1.05f, 1.45f));
        CreateEnemy("L2_Keth_Alien_Grunt_Core_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(192f, -3.35f), 2, 180, true);
        CreateEnemy("L2_Keth_Alien_Brute_Core_Guard", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(198.5f, -3.2f), 14, 750, true, new Vector2(1.12f, 1.55f));
        CreateEnemy("L2_Keth_Alien_Grunt_Reactor_Low", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(222f, -3.35f), 2, 180, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Skybridge_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(236f, -3.35f), 2, 180, true);
        CreateEnemy("L2_Keth_Alien_Grunt_Skybridge_Upper", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(246f, -1.35f), 2, 190, true);
        CreateEnemy("L2_Keth_Alien_Brute_CarrierMouth", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(260f, -3.2f), 15, 850, true, new Vector2(1.15f, 1.6f));

        GameObject[] l2BloomWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Bloom_Left_Rusher", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(7.4f, -3.35f), 2, 120, true), 1),
            CreateEnemy("L2_Wave_Bloom_Canopy_Shooter", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(12.8f, -1.3f), 2, 150, true),
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Bloom_Right_Rusher", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(24.5f, -3.35f), 2, 120, true), -1)
        };
        CreateEnemyWaveTrigger("L2_WaveTrigger_BloomField_Swarm", new Vector2(6.8f, -2.2f), new Vector2(1.3f, 5f), l2BloomWave, new Vector2[] { new Vector2(7.4f, -3.35f), new Vector2(12.8f, -1.3f), new Vector2(24.5f, -3.35f) }, 0.34f);

        GameObject[] l2PumpWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Pump_Lower_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(32f, -3.35f), 2, 140, true), 1),
            CreateEnemy("L2_Wave_Pump_Upper_Mid", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(38.5f, -0.6f), 2, 160, true),
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Pump_Lower_Right", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(47f, -3.35f), 2, 140, true), -1),
            CreateEnemy("L2_Wave_Pump_Falling_Brute", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(43.3f, 0.4f), 9, 460, true, new Vector2(1.05f, 1.42f))
        };
        CreateEnemyWaveTrigger("L2_WaveTrigger_Pumpworks_Crossfire", new Vector2(31.5f, -2.2f), new Vector2(1.3f, 5.5f), l2PumpWave, new Vector2[] { new Vector2(32f, -3.35f), new Vector2(38.5f, -0.6f), new Vector2(47f, -3.35f), new Vector2(43.3f, 0.4f) }, 0.38f);

        GameObject[] l2HatcheryWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Hatchery_BackSpawn", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(141.2f, -3.35f), 2, 170, true), 1),
            CreateEnemy("L2_Wave_Hatchery_Walkway", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(151f, -1f), 2, 190, true),
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Hatchery_FrontSpawn", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(159f, -3.35f), 2, 170, true), -1),
            CreateEnemy("L2_Wave_Hatchery_Brute_Pod", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(154f, -3.2f), 10, 560, true, new Vector2(1.05f, 1.45f))
        };
        CreateEnemyWaveTrigger("L2_WaveTrigger_Hatchery_PodBurst", new Vector2(142.2f, -2.2f), new Vector2(1.3f, 5f), l2HatcheryWave, new Vector2[] { new Vector2(141.2f, -3.35f), new Vector2(151f, -1f), new Vector2(159f, -3.35f), new Vector2(154f, -3.2f) }, 0.33f);

        GameObject[] l2ReactorWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Reactor_Left", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(212f, -3.35f), 2, 180, true), 1),
            CreateEnemy("L2_Wave_Reactor_MovingPlatform", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(224f, -1.55f), 2, 200, true),
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Reactor_Right", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(232f, -4.55f), 2, 180, true), -1)
        };
        CreateEnemyWaveTrigger("L2_WaveTrigger_Reactor_Run", new Vector2(213f, -2.2f), new Vector2(1.3f, 5f), l2ReactorWave, new Vector2[] { new Vector2(212f, -3.35f), new Vector2(224f, -1.55f), new Vector2(232f, -4.55f) }, 0.28f);

        GameObject[] l2CarrierWave = new GameObject[]
        {
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Carrier_Left_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(249f, -3.35f), 2, 190, true), 1),
            SetEnemyPatrolDirection(CreateEnemy("L2_Wave_Carrier_Right_01", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(265f, -3.35f), 2, 190, true), -1),
            CreateEnemy("L2_Wave_Carrier_Upper", enemySprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(258.5f, -0.95f), 2, 210, true),
            CreateEnemy("L2_Wave_Carrier_Final_Brute", bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, new Vector2(260.8f, -3.2f), 16, 900, true, new Vector2(1.15f, 1.6f))
        };
        CreateEnemyWaveTrigger("L2_WaveTrigger_Carrier_Mouth", new Vector2(250.5f, -2.2f), new Vector2(1.3f, 5f), l2CarrierWave, new Vector2[] { new Vector2(249f, -3.35f), new Vector2(265f, -3.35f), new Vector2(258.5f, -0.95f), new Vector2(260.8f, -3.2f) }, 0.3f);

        Vector2[] l2BloomRushPositions = new Vector2[] { new Vector2(6.8f, -3.35f), new Vector2(10.6f, -3.35f), new Vector2(12.8f, -1.3f), new Vector2(16.2f, -3.35f), new Vector2(20.6f, -1.55f), new Vector2(24.8f, -3.35f), new Vector2(27.5f, -3.35f), new Vector2(30.8f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_Bloom_SwarmRush", 5f, CreateEnemyWaveSet("L2_Cam_Bloom", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2BloomRushPositions, new int[] { 1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, false, true, false }, null), l2BloomRushPositions, 0.1f, 0.2f);

        Vector2[] l2PumpworksPositions = new Vector2[] { new Vector2(31.6f, -3.35f), new Vector2(34f, -1.85f), new Vector2(38.5f, -0.55f), new Vector2(42.8f, -1.8f), new Vector2(47.4f, -3.35f), new Vector2(51.5f, -3.2f), new Vector2(44f, 0.35f), new Vector2(53.2f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_Pumpworks_VerticalCrossfire", 29.8f, CreateEnemyWaveSet("L2_Cam_Pump", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2PumpworksPositions, new int[] { 1, -1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, false, true, true, false }, new bool[] { false, false, false, false, false, true, false, false }), l2PumpworksPositions, 0.12f, 0.23f);

        Vector2[] l2SporeShaftPositions = new Vector2[] { new Vector2(58f, -4.55f), new Vector2(63.4f, -6.45f), new Vector2(68.5f, -8.35f), new Vector2(72.3f, -5.45f), new Vector2(77.4f, -7.6f), new Vector2(83f, -6.6f), new Vector2(88f, -8.65f), new Vector2(93.6f, -6.45f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_SporeVent_ShaftAmbush", 55f, CreateEnemyWaveSet("L2_Cam_SporeShaft", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2SporeShaftPositions, new int[] { -1, -1, -1, 1, -1, -1, -1, -1 }, new bool[] { false, true, false, true, true, false, true, false }, null), l2SporeShaftPositions, 0.15f, 0.3f);

        Vector2[] l2SkimmerPositions = new Vector2[] { new Vector2(111f, -3.35f), new Vector2(116.5f, -3.35f), new Vector2(121f, -1.1f), new Vector2(126f, -5.85f), new Vector2(130f, -1.65f), new Vector2(136f, -3.35f), new Vector2(139.2f, -3.35f), new Vector2(142.5f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_XenoSkimmer_OpenAir", 108f, CreateEnemyWaveSet("L2_Cam_Skimmer", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2SkimmerPositions, new int[] { 1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, false, true, false, true, false }, null), l2SkimmerPositions, 0.08f, 0.22f);

        Vector2[] l2HatcheryPositions = new Vector2[] { new Vector2(141.2f, -3.35f), new Vector2(146f, -3.35f), new Vector2(151f, -1f), new Vector2(154f, -3.2f), new Vector2(159f, -3.35f), new Vector2(163f, -3.35f), new Vector2(157.6f, -0.5f), new Vector2(166.8f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_Hatchery_PodBurstPressure", 139f, CreateEnemyWaveSet("L2_Cam_Hatchery", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2HatcheryPositions, new int[] { 1, 1, -1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, false, true, true, false }, new bool[] { false, false, false, true, false, false, false, false }), l2HatcheryPositions, 0.1f, 0.21f);

        Vector2[] l2CorePositions = new Vector2[] { new Vector2(184f, -3.35f), new Vector2(190f, -2.1f), new Vector2(192f, -3.35f), new Vector2(196f, -3.2f), new Vector2(199f, -2.1f), new Vector2(204f, -3.35f), new Vector2(209f, -3.35f), new Vector2(212.5f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_Core_GuardCollapse", 182f, CreateEnemyWaveSet("L2_Cam_Core", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2CorePositions, new int[] { 1, -1, 1, -1, -1, -1, -1, -1 }, new bool[] { false, true, true, true, true, false, true, false }, new bool[] { false, false, false, true, false, false, false, false }), l2CorePositions, 0.12f, 0.22f);

        Vector2[] l2FinalRushPositions = new Vector2[] { new Vector2(214f, -3.35f), new Vector2(224f, -1.55f), new Vector2(232f, -4.55f), new Vector2(236f, -3.35f), new Vector2(246f, -1.35f), new Vector2(249f, -3.35f), new Vector2(258.5f, -0.95f), new Vector2(265f, -3.35f), new Vector2(260.8f, -3.2f), new Vector2(267f, -3.35f) };
        CreateCameraEnemyWaveTrigger("L2_CameraWave_Final_CarrierFlood", 211f, CreateEnemyWaveSet("L2_Cam_Final", enemySprite, bruteSprite, enemyProjectilePrefab, enemyDeathPrefab, l2FinalRushPositions, new int[] { 1, -1, -1, 1, -1, 1, -1, -1, -1, -1 }, new bool[] { false, true, false, true, true, false, true, true, true, false }, new bool[] { false, false, false, false, false, false, false, false, true, false }), l2FinalRushPositions, 0.08f, 0.19f);

        CreatePOW("L2_POW_Hatchery_Captive", powSprite, new Vector2(146.8f, -3.3f));
        CreatePOW("L2_POW_Core_Technician", powSprite, new Vector2(188.8f, -3.3f));
        CreatePOW("L2_POW_Carrier_Mouth_Captive", powSprite, new Vector2(254.5f, -3.3f));
        CreateEndTrigger(endSprite, new Vector2(266f, -3.25f), completePanel);
    }

    private static void CreateWaterZone(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        CreateWaterZone(name, sprite, position, scale, "LAKE - SWIM WITH W/S OR LEFT STICK", new Color(0.1f, 0.55f, 0.95f, 0.48f));
    }

    private static void CreateWaterZone(string name, Sprite sprite, Vector2 position, Vector2 scale, string labelText, Color color)
    {
        GameObject water = CreateBoxObject(name, sprite, position, scale, "Water");
        SpriteRenderer renderer = water.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 2;
            renderer.color = color;
        }

        BoxCollider2D collider = water.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        water.AddComponent<WaterZone2D>();

        GameObject label = CreateWorldText(name + "_Label", labelText, new Vector3(position.x, position.y + scale.y * 0.5f + 0.35f, 0f), 0.2f);
        label.transform.SetParent(water.transform);
        label.SetActive(false);
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

    private static void CreateAlienSkimmerVehicle(string name, Sprite sprite, GameObject projectilePrefab, Vector2 position)
    {
        GameObject vehicle = new GameObject(name);
        vehicle.transform.position = position;
        vehicle.layer = LayerMask.NameToLayer("Player");

        Rigidbody2D rb = vehicle.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D bodyCollider = vehicle.AddComponent<BoxCollider2D>();
        bodyCollider.size = new Vector2(2.9f, 0.78f);
        bodyCollider.offset = new Vector2(0f, -0.05f);

        BoxCollider2D trigger = vehicle.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3.9f, 2.2f);

        CreateVehiclePart(name + "_LivingCore", sprite, vehicle.transform, Vector2.zero, new Vector2(2.55f, 0.62f), new Color(0.62f, 0.35f, 0.9f, 1f), 7);
        CreateVehiclePart(name + "_CrescentWing_Upper", sprite, vehicle.transform, new Vector2(-0.35f, 0.38f), new Vector2(2.2f, 0.22f), new Color(0.36f, 0.85f, 0.78f, 1f), 7);
        CreateVehiclePart(name + "_CrescentWing_Lower", sprite, vehicle.transform, new Vector2(-0.35f, -0.38f), new Vector2(2.2f, 0.22f), new Color(0.36f, 0.85f, 0.78f, 1f), 7);
        CreateVehiclePart(name + "_BioCockpit", sprite, vehicle.transform, new Vector2(-0.35f, 0.26f), new Vector2(0.85f, 0.36f), new Color(0.75f, 1f, 0.92f, 1f), 8);
        CreateVehiclePart(name + "_NeedleGun_Left", sprite, vehicle.transform, new Vector2(1.35f, 0.17f), new Vector2(0.62f, 0.1f), new Color(0.95f, 0.65f, 1f, 1f), 9);
        CreateVehiclePart(name + "_NeedleGun_Right", sprite, vehicle.transform, new Vector2(1.35f, -0.13f), new Vector2(0.62f, 0.1f), new Color(0.95f, 0.65f, 1f, 1f), 9);

        GameObject seat = new GameObject("SeatPoint");
        seat.transform.SetParent(vehicle.transform);
        seat.transform.localPosition = new Vector3(-0.35f, 0.36f, 0f);

        GameObject leftGun = new GameObject("LeftGunPoint");
        leftGun.transform.SetParent(vehicle.transform);
        leftGun.transform.localPosition = new Vector3(1.72f, 0.17f, 0f);

        GameObject rightGun = new GameObject("RightGunPoint");
        rightGun.transform.SetParent(vehicle.transform);
        rightGun.transform.localPosition = new Vector3(1.72f, -0.13f, 0f);

        GameObject prompt = CreateWorldText(name + "_Prompt", "Press E/RB to ride alien skimmer", new Vector3(position.x, position.y + 1.25f, 0f), 0.2f);
        prompt.transform.SetParent(vehicle.transform);

        PlayerHoverVehicle2D hover = vehicle.AddComponent<PlayerHoverVehicle2D>();
        hover.projectilePrefab = projectilePrefab;
        hover.seatPoint = seat.transform;
        hover.leftGun = leftGun.transform;
        hover.rightGun = rightGun.transform;
        hover.promptText = prompt.GetComponent<TextMesh>();
        hover.moveSpeed = 6.6f;
        hover.verticalSpeed = 5f;
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
        shooter.obstacleLayers = LayerMask.GetMask("Ground");
        shooter.followOffset = new Vector3(-1.25f, 1.05f, 0f);
        shooter.fireCooldown = 0.85f;
        shooter.damageMultiplier = 0.1f;

        GameObject label = CreateWorldText(name + "_Label", "COMPANION", new Vector3(position.x, position.y + 0.7f, 0f), 0.18f);
        label.transform.SetParent(companion.transform);
        label.SetActive(false);
    }

    private static GameObject CreateAmbientMover(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, Vector2 velocity, float travelDistance, string labelText)
    {
        GameObject mover = CreateBoxObject(name, sprite, position, scale, "Ground");
        SpriteRenderer renderer = mover.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 4;
            renderer.color = color;
        }

        AmbientLooper2D looper = mover.AddComponent<AmbientLooper2D>();
        looper.velocity = velocity;
        looper.travelDistance = travelDistance;

        if (!string.IsNullOrEmpty(labelText))
        {
            GameObject label = CreateWorldText(name + "_Label", labelText, new Vector3(position.x, position.y + scale.y * 0.5f + 0.35f, 0f), 0.16f);
            label.transform.SetParent(mover.transform);
            label.SetActive(false);
        }

        return mover;
    }

    private static void CreateRuntimeTools(GameObject playerProjectilePrefab, GameObject enemyProjectilePrefab, GameObject bulletHitPrefab, GameObject enemyDeathPrefab, GameObject explosionPrefab)
    {
        GameObject tools = new GameObject("RuntimeTools");

        ObjectPool2D pool = tools.AddComponent<ObjectPool2D>();
        pool.Prewarm(playerProjectilePrefab, 48);
        pool.Prewarm(enemyProjectilePrefab, 80);
        pool.Prewarm(bulletHitPrefab, 48);
        pool.Prewarm(enemyDeathPrefab, 20);
        pool.Prewarm(explosionPrefab, 12);

        GameplayDebugOverlay2D debugOverlay = tools.AddComponent<GameplayDebugOverlay2D>();
        debugOverlay.showOverlay = false;

        tools.AddComponent<PhysicsLayerSetup2D>();

        GameObject label = CreateWorldText("RuntimeTools_Label", "RUNTIME TOOLS: F1 DEBUG / POOLS / AI VISUALIZATION", new Vector3(-8.5f, 1.8f, 0f), 0.18f);
        label.transform.SetParent(tools.transform);
        label.SetActive(false);
    }

    private static GameObject CreateEnemyWaveTrigger(string name, Vector2 triggerPosition, Vector2 triggerSize, GameObject[] spawnObjects, Vector2[] spawnPositions, float spawnInterval)
    {
        GameObject trigger = new GameObject(name);
        trigger.transform.position = triggerPosition;

        BoxCollider2D collider = trigger.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = triggerSize;

        Transform[] spawnPoints = new Transform[spawnObjects != null ? spawnObjects.Length : 0];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject point = new GameObject("SpawnPoint_" + i.ToString("00"));
            point.transform.SetParent(trigger.transform);
            Vector2 spawnPosition = spawnPositions != null && i < spawnPositions.Length ? spawnPositions[i] : triggerPosition;
            point.transform.position = spawnPosition;
            spawnPoints[i] = point.transform;
        }

        EnemyWaveTrigger2D wave = trigger.AddComponent<EnemyWaveTrigger2D>();
        wave.spawnObjects = spawnObjects;
        wave.spawnPoints = spawnPoints;
        wave.spawnInterval = spawnInterval;
        wave.resolveBlockedSpawns = false;
        wave.spawnBlockLayers = LayerMask.GetMask("Ground");

        CreateDesignLabel(name + "_Label", "WAVE TRIGGER: " + name, new Vector3(triggerPosition.x, triggerPosition.y + triggerSize.y * 0.5f + 0.35f, 0f)).transform.SetParent(trigger.transform);
        return trigger;
    }

    private static GameObject CreateCameraEnemyWaveTrigger(string name, float cameraXTrigger, GameObject[] spawnObjects, Vector2[] spawnPositions, float initialDelay, float spawnInterval)
    {
        Vector2 triggerPosition = new Vector2(cameraXTrigger, 0f);
        GameObject trigger = CreateEnemyWaveTrigger(name, triggerPosition, new Vector2(0.35f, 16f), spawnObjects, spawnPositions, spawnInterval);

        EnemyWaveTrigger2D wave = trigger.GetComponent<EnemyWaveTrigger2D>();
        if (wave != null)
        {
            wave.triggerWhenCameraReachesX = true;
            wave.cameraXTrigger = cameraXTrigger;
            wave.initialDelay = initialDelay;
        }

        return trigger;
    }

    private static GameObject[] CreateEnemyWaveSet(string prefix, Sprite gruntSprite, Sprite bruteSprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2[] positions, int[] directions, bool[] shooters, bool[] brutes)
    {
        GameObject[] enemies = new GameObject[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            bool isBrute = brutes != null && i < brutes.Length && brutes[i];
            bool canShoot = shooters == null || i >= shooters.Length || shooters[i];
            Sprite sprite = isBrute ? bruteSprite : gruntSprite;
            Vector2 scale = isBrute ? new Vector2(1.05f, 1.45f) : new Vector2(0.58f, 1.1f);
            int health = isBrute ? 10 : 2;
            int score = isBrute ? 500 : 120;

            enemies[i] = CreateEnemy(prefix + "_" + i.ToString("00"), sprite, enemyProjectilePrefab, deathEffectPrefab, positions[i], health, score, canShoot, scale);
            if (directions != null && i < directions.Length)
                SetEnemyPatrolDirection(enemies[i], directions[i]);
        }

        return enemies;
    }

    private static GameObject SetEnemyPatrolDirection(GameObject enemy, int direction)
    {
        EnemyPatrol2D patrol = enemy != null ? enemy.GetComponent<EnemyPatrol2D>() : null;
        if (patrol != null)
            patrol.startingDirection = direction >= 0 ? 1 : -1;

        return enemy;
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
        EnemyAutoDespawn2D despawn = enemy.AddComponent<EnemyAutoDespawn2D>();
        despawn.behindCameraDistance = 22f;

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

    private static void CreateEnvironmentArtPass(Sprite fallbackStructure, Sprite fallbackProp, Sprite waterSprite)
    {
        Sprite subwayBackground = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_1/Background/subway_BG.png", 32f);
        Sprite subwayWall = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_1/Background/wall_subway.png", 48f);
        Sprite cloudsNear = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_1.png", 32f);
        Sprite cloudsMid = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_2.png", 32f);
        Sprite cloudsFar = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_3.png", 32f);

        CreateBackdropRun("ENV_Interior_TrainingOutpost", subwayBackground, -13f, 63f, -0.45f, new Vector2(1.05f, 1.05f), new Color(0.82f, 0.96f, 0.82f, 0.86f), -18);
        CreateBackdropRun("ENV_Exterior_LakeClouds_Far", cloudsFar, 63f, 116f, -5.85f, new Vector2(1.35f, 1.08f), new Color(1f, 0.82f, 0.72f, 0.92f), -20);
        CreateBackdropRun("ENV_Exterior_RunwayClouds_Mid", cloudsMid, 112f, 150f, -0.5f, new Vector2(1.35f, 1.05f), new Color(1f, 0.82f, 0.72f, 0.9f), -20);
        CreateBackdropRun("ENV_Interior_HangarComms", subwayWall, 149f, 223f, -0.15f, new Vector2(0.9f, 0.9f), new Color(0.82f, 0.98f, 0.88f, 0.88f), -18);
        CreateBackdropRun("ENV_Exterior_PipelineCarrierClouds", cloudsNear, 222f, 310f, -0.2f, new Vector2(1.35f, 1.08f), new Color(1f, 0.82f, 0.72f, 0.9f), -20);
        CreateBackdropRun("ENV_Exterior_RailCliffClouds", cloudsMid, 306f, 386f, -0.3f, new Vector2(1.35f, 1.08f), new Color(1f, 0.82f, 0.72f, 0.9f), -20);

        Sprite panel = grgOutdoorRuinPanelSprite != null ? grgOutdoorRuinPanelSprite : fallbackStructure;
        CreateDecorRun("ENV_RuinWall_BridgeCamp", panel, 8f, 43f, -1.2f, new Vector2(7f, 4.8f), new Color(0.72f, 0.86f, 0.68f, 0.88f), -9);
        CreateDecorRun("ENV_RuinWall_LakeRetaining", panel, 77f, 104f, -9.8f, new Vector2(6.8f, 4.9f), new Color(0.68f, 0.84f, 0.72f, 0.9f), -9);
        CreateDecorRun("ENV_RuinWall_Runway", panel, 114f, 149f, -1.25f, new Vector2(7.2f, 4.6f), new Color(0.72f, 0.84f, 0.68f, 0.88f), -9);
        CreateDecorRun("ENV_RuinWall_RailYard", panel, 309f, 351f, -1.25f, new Vector2(7.2f, 4.6f), new Color(0.72f, 0.84f, 0.68f, 0.88f), -9);
        CreateDecorRun("ENV_RuinWall_CliffLift", panel, 351f, 386f, 0.2f, new Vector2(7.2f, 4.9f), new Color(0.72f, 0.84f, 0.68f, 0.88f), -9);

        CreateInteriorTechPanels(fallbackStructure);
        CreateExteriorProps(fallbackProp, waterSprite);
        CreateSpawnDoorSilhouettes(fallbackStructure);
    }

    private static void CreateInteriorTechPanels(Sprite fallbackStructure)
    {
        Sprite pipe = grgOutdoorPipeSprite != null ? grgOutdoorPipeSprite : fallbackStructure;
        Sprite support = grgOutdoorSupportSprite != null ? grgOutdoorSupportSprite : fallbackStructure;

        CreateDecorRun("ENV_Pipes_Training", pipe, -10f, 58f, -0.95f, new Vector2(4.8f, 0.5f), new Color(0.65f, 0.78f, 0.68f, 0.72f), -7);
        CreateDecorRun("ENV_Pipes_Hangar", pipe, 150f, 184f, -0.65f, new Vector2(5.2f, 0.55f), new Color(0.65f, 0.78f, 0.72f, 0.74f), -7);
        CreateDecorRun("ENV_Pipes_Comms", pipe, 194f, 220f, 0.7f, new Vector2(4.8f, 0.5f), new Color(0.65f, 0.78f, 0.72f, 0.74f), -7);
        CreateDecorRun("ENV_Pipes_Pipeline", pipe, 228f, 269f, -0.25f, new Vector2(5.4f, 0.55f), new Color(0.68f, 0.78f, 0.78f, 0.76f), -7);

        CreateDecorSprite("ENV_Comms_LeftSupport", support, new Vector2(201.2f, -0.55f), new Vector2(1.1f, 4.2f), new Color(0.55f, 0.68f, 0.7f, 0.82f), -6, false);
        CreateDecorSprite("ENV_Comms_RightSupport", support, new Vector2(212.4f, -0.4f), new Vector2(1.1f, 4.5f), new Color(0.55f, 0.68f, 0.7f, 0.82f), -6, true);
        CreateDecorSprite("ENV_AntiAir_ServiceSupport", support, new Vector2(144.7f, -0.75f), new Vector2(1.2f, 4.7f), new Color(0.55f, 0.68f, 0.7f, 0.82f), -6, false);
        CreateDecorSprite("ENV_FinalLift_ServiceSupport", support, new Vector2(377.4f, 0.2f), new Vector2(1.2f, 4.5f), new Color(0.55f, 0.68f, 0.7f, 0.82f), -6, true);
    }

    private static void CreateExteriorProps(Sprite fallbackProp, Sprite waterSprite)
    {
        Sprite railing = grgOutdoorRailingSprite != null ? grgOutdoorRailingSprite : fallbackProp;
        Sprite lamp = grgOutdoorLampSprite != null ? grgOutdoorLampSprite : fallbackProp;
        Sprite cone = grgOutdoorConeSprite != null ? grgOutdoorConeSprite : fallbackProp;
        Sprite sign = grgOutdoorSignSprite != null ? grgOutdoorSignSprite : fallbackProp;
        Sprite support = grgOutdoorSupportSprite != null ? grgOutdoorSupportSprite : fallbackProp;

        float[] lampXs = new float[] { -7.5f, 1.8f, 13.4f, 25.4f, 48.5f, 116.2f, 129.5f, 318.5f, 341.2f, 360.4f, 374.2f };
        for (int i = 0; i < lampXs.Length; i++)
            CreateDecorSprite("ENV_Lamp_" + i.ToString("00"), lamp, new Vector2(lampXs[i], -2.75f), new Vector2(0.8f, 2.4f), Color.white, -3, i % 2 == 1);

        CreateDecorRun("ENV_Bridge_Railing", railing, 10.2f, 21.8f, -3.15f, new Vector2(2.8f, 0.75f), Color.white, 3);
        CreateDecorRun("ENV_POW_Railing", railing, 22.5f, 30.8f, -3.12f, new Vector2(2.7f, 0.75f), Color.white, 3);
        CreateDecorRun("ENV_Lake_Bridge_Railing", railing, 82f, 96.3f, -7.62f, new Vector2(2.9f, 0.72f), Color.white, 3);
        CreateDecorRun("ENV_Runway_Railing", railing, 118f, 134.6f, -3.15f, new Vector2(2.9f, 0.72f), Color.white, 3);
        CreateDecorRun("ENV_RailYard_Railing_A", railing, 314f, 328f, -3.15f, new Vector2(2.9f, 0.72f), Color.white, 3);
        CreateDecorRun("ENV_RailYard_Railing_B", railing, 336f, 348f, -3.15f, new Vector2(2.9f, 0.72f), Color.white, 3);

        CreateDecorSprite("ENV_Acid_WarningSign", sign, new Vector2(31.8f, -2.55f), new Vector2(0.8f, 1.2f), Color.white, 4, false);
        CreateDecorSprite("ENV_Lake_WarningSign", sign, new Vector2(80.2f, -7.05f), new Vector2(0.8f, 1.2f), Color.white, 4, false);
        CreateDecorSprite("ENV_Runway_WarningSign", sign, new Vector2(133.8f, -2.55f), new Vector2(0.8f, 1.2f), Color.white, 4, true);
        CreateDecorSprite("ENV_FinalLift_WarningSign", sign, new Vector2(371.2f, -0.25f), new Vector2(0.8f, 1.2f), Color.white, 4, false);

        float[] coneXs = new float[] { 33.2f, 34.3f, 37.7f, 39f, 81.1f, 96.8f, 122.5f, 126.2f, 141.4f, 149.2f, 368.4f };
        float[] coneYs = new float[] { -3.35f, -3.35f, -3.35f, -3.35f, -7.95f, -7.95f, -3.35f, -3.35f, -3.35f, -3.35f, -0.9f };
        for (int i = 0; i < coneXs.Length; i++)
            CreateDecorSprite("ENV_Cone_" + i.ToString("00"), cone, new Vector2(coneXs[i], coneYs[i]), new Vector2(0.38f, 0.7f), Color.white, 4, i % 2 == 0);

        CreateDecorSprite("ENV_Bridge_UnderSupport_01", support, new Vector2(14.6f, -4.35f), new Vector2(1.2f, 2.2f), new Color(0.82f, 0.78f, 0.6f, 0.92f), -2, false);
        CreateDecorSprite("ENV_Lake_BridgeSupport_01", support, new Vector2(85.5f, -9.1f), new Vector2(1.25f, 2.9f), new Color(0.82f, 0.78f, 0.6f, 0.92f), -2, false);
        CreateDecorSprite("ENV_Lake_BridgeSupport_02", support, new Vector2(92.8f, -9.1f), new Vector2(1.25f, 2.9f), new Color(0.82f, 0.78f, 0.6f, 0.92f), -2, true);
        CreateDecorSprite("ENV_Runway_ServiceSupport_01", support, new Vector2(125.2f, -4.2f), new Vector2(1.15f, 2.1f), new Color(0.82f, 0.78f, 0.6f, 0.92f), -2, false);

        if (waterSprite != null)
            CreateDecorSprite("ENV_Lake_Surface_Glint", waterSprite, new Vector2(89.3f, -8.45f), new Vector2(15.2f, 0.18f), new Color(0.72f, 0.9f, 1f, 0.55f), 4, false);
    }

    private static void CreateSpawnDoorSilhouettes(Sprite fallbackStructure)
    {
        Sprite door = grgOutdoorDoorSprite != null ? grgOutdoorDoorSprite : fallbackStructure;
        Vector2[] doors = new Vector2[]
        {
            new Vector2(2.2f, -2.65f), new Vector2(8.2f, -2.65f), new Vector2(22.2f, -2.65f), new Vector2(30.8f, -2.65f),
            new Vector2(151.2f, -2.65f), new Vector2(168.2f, -2.65f), new Vector2(194.2f, -2.65f), new Vector2(221.8f, -1.4f),
            new Vector2(315.4f, -2.65f), new Vector2(337.2f, -2.65f), new Vector2(354.5f, -1.8f), new Vector2(366.9f, -0.1f)
        };

        for (int i = 0; i < doors.Length; i++)
            CreateDecorSprite("ENV_SpawnDoor_" + i.ToString("00"), door, doors[i], new Vector2(0.95f, 2.0f), new Color(0.56f, 0.68f, 0.66f, 0.82f), -4, i % 2 == 1);
    }

    private static void CreateBackdropRun(string name, Sprite sprite, float startX, float endX, float y, Vector2 scale, Color tint, int sortingOrder)
    {
        if (sprite == null)
            return;

        float step = Mathf.Max(2f, sprite.bounds.size.x * scale.x * 0.96f);
        int index = 0;
        for (float x = startX; x <= endX; x += step)
            CreateBackdropSprite(name + "_" + index++.ToString("00"), sprite, new Vector2(x, y), scale, tint, sortingOrder);
    }

    private static void CreateDecorRun(string name, Sprite sprite, float startX, float endX, float y, Vector2 targetSize, Color tint, int sortingOrder)
    {
        if (sprite == null)
            return;

        float step = Mathf.Max(0.8f, targetSize.x * 0.92f);
        int index = 0;
        for (float x = startX; x <= endX; x += step)
            CreateDecorSprite(name + "_" + index++.ToString("00"), sprite, new Vector2(x, y), targetSize, tint, sortingOrder, index % 2 == 0);
    }

    private static GameObject CreateDecorSprite(string name, Sprite sprite, Vector2 position, Vector2 targetSize, Color tint, int sortingOrder, bool flipX)
    {
        if (sprite == null)
            return null;

        Vector2 spriteSize = sprite.bounds.size;
        Vector2 scale = new Vector2(
            targetSize.x / Mathf.Max(0.01f, spriteSize.x),
            targetSize.y / Mathf.Max(0.01f, spriteSize.y));

        GameObject decor = CreateBoxObject(name, sprite, position, scale, "Default");
        SpriteRenderer renderer = decor.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = flipX;
        }

        return decor;
    }

    private static void CreateGenericRunGunBackdrops()
    {
        Sprite subwayBackground = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_1/Background/subway_BG.png", 32f);
        Sprite subwayWall = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_1/Background/wall_subway.png", 48f);
        Sprite cloudsNear = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_1.png", 32f);
        Sprite cloudsMid = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_2.png", 32f);
        Sprite cloudsFar = LoadGenericRunGunSprite(GenericRunGunPath + "/Assets_area_2/backgrounds/nuvens_3.png", 32f);

        Color exteriorTint = new Color(0.9f, 1f, 1f, 1f);
        Color interiorTint = new Color(0.95f, 1f, 0.95f, 1f);
        Color wallTint = new Color(0.95f, 1f, 0.9f, 1f);

        CreateBackdropSprite("GRG_Backdrop_Training_Wall", subwayBackground, new Vector2(-3f, -0.2f), Vector2.one, wallTint, -12);
        CreateBackdropSprite("GRG_Backdrop_Outpost_Wall", subwayBackground, new Vector2(12f, -0.2f), Vector2.one, wallTint, -12);
        CreateBackdropSprite("GRG_Backdrop_POW_SubwayWall", subwayWall, new Vector2(27f, -0.15f), Vector2.one, interiorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Gate_SubwayWall", subwayWall, new Vector2(50f, -0.15f), Vector2.one, interiorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Lake_Clouds_Far", cloudsFar, new Vector2(84f, -6.1f), new Vector2(1.2f, 1f), exteriorTint, -14);
        CreateBackdropSprite("GRG_Backdrop_Lake_Clouds_Near", cloudsNear, new Vector2(101f, -5.9f), new Vector2(1.2f, 1f), exteriorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Depot_Wall", subwayBackground, new Vector2(119f, -0.2f), Vector2.one, wallTint, -12);
        CreateBackdropSprite("GRG_Backdrop_Runway_Clouds_Mid", cloudsMid, new Vector2(137f, -0.2f), new Vector2(1.35f, 1f), exteriorTint, -14);
        CreateBackdropSprite("GRG_Backdrop_Hangar_Wall_Left", subwayWall, new Vector2(158f, -0.15f), Vector2.one, interiorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Hangar_Wall_Right", subwayWall, new Vector2(175f, -0.15f), Vector2.one, interiorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Comms_Wall", subwayWall, new Vector2(207f, -0.15f), new Vector2(1.15f, 1f), interiorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Pipeline_Clouds", cloudsNear, new Vector2(248f, -0.25f), new Vector2(1.45f, 1f), exteriorTint, -14);
        CreateBackdropSprite("GRG_Backdrop_Carrier_Wall_A", subwayBackground, new Vector2(284f, 0.05f), new Vector2(1.25f, 1f), wallTint, -12);
        CreateBackdropSprite("GRG_Backdrop_Carrier_Wall_B", subwayBackground, new Vector2(302f, 0.05f), new Vector2(1.25f, 1f), wallTint, -12);
        CreateBackdropSprite("GRG_Backdrop_RailYard_Clouds_A", cloudsMid, new Vector2(324f, -0.15f), new Vector2(1.35f, 1f), exteriorTint, -14);
        CreateBackdropSprite("GRG_Backdrop_RailYard_Clouds_B", cloudsNear, new Vector2(344f, -0.1f), new Vector2(1.35f, 1f), exteriorTint, -13);
        CreateBackdropSprite("GRG_Backdrop_Cliff_Clouds", cloudsFar, new Vector2(363f, 0.65f), new Vector2(1.2f, 1f), exteriorTint, -14);
    }

    private static void CreateBackdropSprite(string name, Sprite sprite, Vector2 position, Vector2 scale, Color tint, int sortingOrder)
    {
        if (sprite == null)
            return;

        GameObject backdrop = CreateBoxObject(name, sprite, position, scale, "Default");
        SpriteRenderer renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;
        }
    }

    private static bool IsGenericRunGunSprite(Sprite sprite)
    {
        return sprite != null && sprite.name.StartsWith("GRG_");
    }

    private static void CreateAlienMarker(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, string labelText)
    {
        GameObject marker = CreateBoxObject(name, sprite, position, scale, "Ground");
        SpriteRenderer renderer = marker.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = -1;
            renderer.color = color;
        }

        GameObject label = CreateWorldText(name + "_Label", labelText, new Vector3(position.x, position.y + scale.y * 0.5f + 0.35f, 0f), 0.16f);
        label.transform.SetParent(marker.transform);
        label.SetActive(false);
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
        label.SetActive(false);
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
        text.SetActive(false);
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
        label.SetActive(false);
    }

    private static GameObject CreatePlayerProjectilePrefab(Sprite sprite, GameObject hitEffectPrefab)
    {
        GameObject projectile = CreateBoxObject("PlayerProjectile", sprite, Vector2.zero, new Vector2(0.55f, 0.28f), "Projectile");
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;

        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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

        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
        playerCollider.size = new Vector2(0.55f, 1.05f);
        playerCollider.offset = new Vector2(0f, -0.16f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.72f, 0f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0.62f, 0.14f, 0f);

        GameObject throwPoint = new GameObject("ThrowPoint");
        throwPoint.transform.SetParent(player.transform);
        throwPoint.transform.localPosition = new Vector3(0.45f, 0.25f, 0f);

        GameObject legsObject = new GameObject("Legs");
        legsObject.transform.SetParent(player.transform);
        legsObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        SpriteRenderer legsRenderer = legsObject.AddComponent<SpriteRenderer>();
        legsRenderer.sprite = sprite;
        legsRenderer.sortingOrder = 2;

        GameObject torsoObject = new GameObject("Torso");
        torsoObject.transform.SetParent(player.transform);
        torsoObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
        torsoRenderer.sprite = LoadSprite(NeraPath + "/torso/idle/torso_idle0.png");
        torsoRenderer.sortingOrder = 3;

        if (torsoRenderer.sprite != null)
            rootRenderer.enabled = false;

        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.moveSpeed = 3.5f;
        controller.jumpForce = 10.5f;
        controller.crouchMoveMultiplier = 0.5f;
        controller.groundCheckRadius = 0.2f;
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
        health.fallDeathY = -28f;

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController("PlayerVolkov", new string[] { "Idle", "Run", "JumpFall", "Dash", "Shoot", "Hurt", "Death" });
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

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2 position, int health, int score, bool canShoot)
    {
        return CreateEnemy(name, sprite, enemyProjectilePrefab, deathEffectPrefab, position, health, score, canShoot, new Vector2(0.58f, 1.1f));
    }

    private static GameObject CreateEnemy(string name, Sprite sprite, GameObject enemyProjectilePrefab, GameObject deathEffectPrefab, Vector2 position, int health, int score, bool canShoot, Vector2 scale)
    {
        GameObject enemy = CreateBoxObject(name, sprite, position, scale, "Enemy");
        SpriteRenderer enemyRenderer = enemy.GetComponent<SpriteRenderer>();
        if (enemyRenderer != null && sprite != null && sprite.name.StartsWith("GRG_"))
            enemyRenderer.color = scale.x > 1f ? new Color(0.72f, 0.95f, 1f, 1f) : new Color(0.78f, 1f, 0.66f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        Damageable damageable = enemy.AddComponent<Damageable>();
        damageable.maxHealth = health;
        damageable.scoreValue = score;
        damageable.deathDelay = 0.45f;
        damageable.knockbackForce = scale.x > 1f ? 1.1f : 2.5f;
        damageable.deathEffectPrefab = deathEffectPrefab;

        enemy.AddComponent<DamageOnContact>().damage = 1;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(enemy.transform);
        groundCheck.transform.localPosition = new Vector3(-0.75f, -0.58f, 0f);

        GameObject wallCheck = new GameObject("WallCheck");
        wallCheck.transform.SetParent(enemy.transform);
        wallCheck.transform.localPosition = new Vector3(-0.85f, 0f, 0f);

        EnemyPatrol2D patrol = enemy.AddComponent<EnemyPatrol2D>();
        patrol.moveSpeed = scale.x > 1f ? 0.6f : 1.25f;
        patrol.canJumpObstacles = true;
        patrol.jumpForce = scale.x > 1f ? 6.6f : 7.4f;
        patrol.jumpCooldown = scale.x > 1f ? 1.25f : 0.85f;
        patrol.groundCheck = groundCheck.transform;
        patrol.wallCheck = wallCheck.transform;
        patrol.groundLayer = LayerMask.GetMask("Ground");
        patrol.obstacleLayer = LayerMask.GetMask("Ground");
        patrol.enemyLayer = LayerMask.GetMask("Enemy");
        patrol.detectionRange = scale.x > 1f ? 55f : 70f;
        patrol.preferredShootDistance = scale.x > 1f ? 10.5f : 9f;
        patrol.closePressureDistance = scale.x > 1f ? 1.8f : 1.25f;
        patrol.rushStopDistance = scale.x > 1f ? 1.55f : 1.15f;
        patrol.canDropToReachPlayer = true;
        patrol.dropWhenPlayerBelowBy = scale.x > 1f ? 0.95f : 0.75f;
        patrol.dropHorizontalWindow = scale.x > 1f ? 11f : 15f;

        if (canShoot)
        {
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(enemy.transform);
            firePoint.transform.localPosition = new Vector3(-0.6f, 0.15f, 0f);

            EnemyShooter2D shooter = enemy.AddComponent<EnemyShooter2D>();
            shooter.enemyProjectilePrefab = enemyProjectilePrefab;
            shooter.firePoint = firePoint.transform;
            shooter.range = scale.x > 1f ? 12f : 11f;
            shooter.fireCooldown = scale.x > 1f ? 2.2f : 1.25f;
            shooter.burstCount = scale.x > 1f ? 3 : 1;
            shooter.burstSpacing = scale.x > 1f ? 0.22f : 0.16f;
            shooter.projectileScaleMultiplier = scale.x > 1f ? 1.45f : 1f;
            shooter.horizontalShotHeight = scale.x > 1f ? 0.28f : 0.22f;
            shooter.verticalShotOffset = scale.x > 1f ? 0.55f : 0.35f;
            shooter.obstacleLayers = LayerMask.GetMask("Ground");
        }

        Animator animator = enemy.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController(scale.x > 1f ? "KethBrute" : "KethGrunt", new string[] { "Idle", "Walk", "Shoot", "Hurt", "Death" });

        EnemyAnimationDriver animationDriver = enemy.AddComponent<EnemyAnimationDriver>();
        animationDriver.isBrute = scale.x > 1f;

        return enemy;
    }

    private static void AddNeraEnemyVisual(GameObject enemy)
    {
        SpriteRenderer rootRenderer = enemy.GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;

        GameObject legsObject = new GameObject("NeraEnemy_Legs");
        legsObject.transform.SetParent(enemy.transform);
        legsObject.transform.localPosition = Vector3.zero;
        SpriteRenderer legsRenderer = legsObject.AddComponent<SpriteRenderer>();
        legsRenderer.sortingOrder = 2;

        GameObject torsoObject = new GameObject("NeraEnemy_Torso");
        torsoObject.transform.SetParent(enemy.transform);
        torsoObject.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
        torsoRenderer.sortingOrder = 3;

        NeraEnemyVisual visual = enemy.AddComponent<NeraEnemyVisual>();
        visual.legsRenderer = legsRenderer;
        visual.torsoRenderer = torsoRenderer;
        visual.torsoOffset = Vector3.zero;
        visual.legsIdle = LoadSprites(NeraPath + "/legs/idle/leg_idle.png");
        visual.legsRun = LoadSpriteSequence(NeraPath + "/legs/run", "leg_run", 0, 7);
        visual.legsHurt = LoadSpriteSequence(NeraPath + "/legs/hurt ground", "hurt_ground", 1, 4);
        visual.legsDeath = LoadSpriteSequence(NeraPath + "/legs/death ground", "death", 1, 12);
        visual.torsoIdle = LoadSpriteSequence(NeraPath + "/torso/idle", "torso_idle", 0, 3);
        visual.torsoRun = LoadSpriteSequence(NeraPath + "/torso/run", "torso_run", 0, 7);
        visual.torsoShoot = LoadSpriteSequence(NeraPath + "/torso/shoot", "shoot", 0, 3);
        visual.wholeHurt = LoadSpriteSequence(NeraPath + "/whole body/hurt ground", "hurt_ground", 1, 4);
        visual.wholeDeath = LoadSpriteSequence(NeraPath + "/whole body/death ground", "death", 1, 12);
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

    private static GameObject CreateDesignLabel(string name, string text, Vector3 position)
    {
        GameObject label = CreateWorldText(name, text, position, 0.16f);
        label.SetActive(false);
        TextMesh textMesh = label.GetComponent<TextMesh>();
        if (textMesh != null)
        {
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = new Color(0.9f, 1f, 1f, 1f);
        }

        return label;
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

        CreateWorldText("End_Label", "END", new Vector3(position.x, position.y + 1.2f, 0f), 0.3f).SetActive(false);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.25f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.13f, 1f);

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-6f, -1.1f, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.offset = new Vector3(2f, 1.2f, -10f);
        follow.smoothSpeed = 8f;
        follow.useBounds = true;
        follow.minBounds = new Vector2(-9f, -15.8f);
        follow.maxBounds = new Vector2(384f, 7.2f);
    }
}
#endif
