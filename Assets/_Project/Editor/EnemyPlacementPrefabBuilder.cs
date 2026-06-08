#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnemyPlacementPrefabBuilder
{
    private const string SourceGruntPath = "Assets/_Project/Prefabs/RelayGruntPrefab.prefab";
    private const string SourceBrutePath = "Assets/_Project/Prefabs/RelayBrutePrefab.prefab";
    private const string OutputFolder = "Assets/_Project/Prefabs/Placeable";

    [MenuItem("Remnant Squad/Build Placeable Enemy Prefabs")]
    public static void BuildMenu()
    {
        BuildPlaceablePrefabs();
    }

    public static void BuildBatch()
    {
        BuildPlaceablePrefabs();
    }

    private static void BuildPlaceablePrefabs()
    {
        EnsureFolder(OutputFolder);

        GameObject grunt = AssetDatabase.LoadAssetAtPath<GameObject>(SourceGruntPath);
        GameObject brute = AssetDatabase.LoadAssetAtPath<GameObject>(SourceBrutePath);

        CreateManualPrefab(grunt, "Enemy_Grunt_Rifle", EnemyShooter2D.RunNGunShotVariant.DefaultRifle);
        CreateManualPrefab(grunt, "Enemy_Grunt_MultiRifle", EnemyShooter2D.RunNGunShotVariant.MultiRifle);
        CreateManualPrefab(grunt, "Enemy_Grunt_Blade", EnemyShooter2D.RunNGunShotVariant.Blade);
        CreateManualPrefab(grunt, "Enemy_Grunt_Fireball", EnemyShooter2D.RunNGunShotVariant.Fireball);
        CreateManualPrefab(grunt, "Enemy_Grunt_Energy", EnemyShooter2D.RunNGunShotVariant.Energy);
        CreateManualPrefab(brute, "Enemy_Brute_BigShot", EnemyShooter2D.RunNGunShotVariant.BigShot);
        CreateManualPrefab(brute, "Enemy_Brute_MultiRifle", EnemyShooter2D.RunNGunShotVariant.MultiRifle);

        CreateAmbushPrefab(grunt, "Enemy_Grunt_Rifle_BushAmbush", -1, EnemyShooter2D.RunNGunShotVariant.DefaultRifle);
        CreateAmbushPrefab(grunt, "Enemy_Grunt_Fireball_BushAmbush", -1, EnemyShooter2D.RunNGunShotVariant.Fireball);
        CreateAmbushPrefab(grunt, "Enemy_Grunt_Blade_BushAmbush", -1, EnemyShooter2D.RunNGunShotVariant.Blade);
        CreateAmbushPrefab(brute, "Enemy_Brute_BigShot_BushAmbush", -1, EnemyShooter2D.RunNGunShotVariant.BigShot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built placeable enemy prefabs in " + OutputFolder);
    }

    private static void CreateManualPrefab(GameObject sourcePrefab, string outputName, EnemyShooter2D.RunNGunShotVariant shotVariant)
    {
        if (sourcePrefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        instance.name = outputName;
        PrepareManualEnemy(instance, shotVariant);
        PrefabUtility.SaveAsPrefabAsset(instance, OutputFolder + "/" + outputName + ".prefab");
        Object.DestroyImmediate(instance);
    }

    private static void CreateAmbushPrefab(GameObject sourcePrefab, string outputName, int emergeDirection, EnemyShooter2D.RunNGunShotVariant shotVariant)
    {
        if (sourcePrefab == null)
            return;

        GameObject root = new GameObject(outputName);

        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, root.transform);
        enemy.name = "HiddenEnemy";
        enemy.transform.localPosition = Vector3.zero;
        enemy.transform.localRotation = Quaternion.identity;
        PrepareManualEnemy(enemy, shotVariant);

        EnemyAmbushReveal2D reveal = enemy.GetComponent<EnemyAmbushReveal2D>();
        if (reveal == null)
            reveal = enemy.AddComponent<EnemyAmbushReveal2D>();
        reveal.hiddenOnAwake = true;
        reveal.disableGameplayWhileHidden = true;
        reveal.emergeDirection = emergeDirection;
        reveal.emergeDistance = 1.1f;
        reveal.emergeSpeed = 2.1f;
        reveal.becomeStationaryAfterEmerge = false;

        GameObject triggerObject = new GameObject("RevealTrigger_MoveThisAheadOfBush");
        triggerObject.transform.SetParent(root.transform);
        triggerObject.transform.localPosition = new Vector3(3f, 0f, 0f);

        BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.5f, 3f);
        triggerCollider.offset = new Vector2(0f, 0.8f);

        EnemyAmbushTrigger2D trigger = triggerObject.AddComponent<EnemyAmbushTrigger2D>();
        trigger.ambushEnemies = new EnemyAmbushReveal2D[] { reveal };
        trigger.triggerOnce = true;

        PrefabUtility.SaveAsPrefabAsset(root, OutputFolder + "/" + outputName + ".prefab");
        Object.DestroyImmediate(root);
    }

    private static void PrepareManualEnemy(GameObject enemy, EnemyShooter2D.RunNGunShotVariant shotVariant)
    {
        if (enemy == null)
            return;

        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol != null)
        {
            patrol.enabled = true;
            patrol.useRunNGunStationaryBehaviour = false;
            patrol.canJumpObstacles = false;
            patrol.canDropToReachPlayer = false;
            patrol.moveOnlyWhenPlayerDetected = false;
            patrol.onlyEngageOnSameLevel = true;
            patrol.onlyEngageWhenPlayerInFront = true;
            patrol.minimumPlayerDistance = 3f;
            patrol.preferredShootDistance = 3f;
            patrol.rushStopDistance = 3f;
            patrol.patrolRadius = 2f;
            patrol.stopAtLedges = true;
        }

        EnemyShooter2D shooter = enemy.GetComponent<EnemyShooter2D>();
        if (shooter != null)
        {
            shooter.enabled = true;
            shooter.useRunNGunShootCycle = true;
            shooter.allowVerticalShots = false;
            shooter.requireSameLevelToShoot = true;
            shooter.requirePlayerInFrontToShoot = true;
            shooter.applyRunNGunShotVariant = true;
            shooter.shotVariant = shotVariant;
            shooter.ApplyRunNGunShotVariant();
        }

        EnemyAutoDespawn2D despawn = enemy.GetComponent<EnemyAutoDespawn2D>();
        if (despawn == null)
            enemy.AddComponent<EnemyAutoDespawn2D>();

        enemy.SetActive(true);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
