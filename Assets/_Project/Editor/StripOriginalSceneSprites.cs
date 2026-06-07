#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StripOriginalSceneSprites
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level_01_Alpha.unity",
        "Assets/_Project/Scenes/Level_01_Alpha.unity"
    };

    [MenuItem("Remnant Squad/Strip Sprites From Original Scene 1")]
    public static void StripMenu()
    {
        StripBatch();
    }

    public static void StripBatch()
    {
        for (int i = 0; i < ScenePaths.Length; i++)
            StripScene(ScenePaths[i]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Removed generated GRG sprite objects from original Level_01_Alpha scene copies.");
    }

    private static void StripScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        List<GameObject> objectsToRemove = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            CollectGeneratedSpriteObjects(roots[i].transform, objectsToRemove);

        for (int i = 0; i < objectsToRemove.Count; i++)
        {
            if (objectsToRemove[i] != null)
                Object.DestroyImmediate(objectsToRemove[i]);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Stripped " + objectsToRemove.Count + " generated sprite objects from " + scenePath);
    }

    private static void CollectGeneratedSpriteObjects(Transform transform, List<GameObject> objectsToRemove)
    {
        if (transform.name.StartsWith("GRG_"))
        {
            objectsToRemove.Add(transform.gameObject);
            return;
        }

        SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
            if (spritePath.StartsWith("Assets/_Project/Generated/GenericRunNGun") ||
                spritePath.StartsWith("Assets/_Project/External/GenericRunNGun"))
            {
                objectsToRemove.Add(transform.gameObject);
                return;
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            CollectGeneratedSpriteObjects(transform.GetChild(i), objectsToRemove);
    }
}
#endif
