#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OpenAlphaSceneOnLoad
{
    private const string AlphaScenePath = "Assets/Scenes/Level_01_SubwayOutpost.unity";
    private const string LegacyAlphaScenePath = "Assets/_Project/Scenes/Level_01_SubwayOutpost.unity";
    private const string PreviousAlphaScenePath = "Assets/Scenes/Level_01_Alpha.unity";
    private const string PreviousLegacyAlphaScenePath = "Assets/_Project/Scenes/Level_01_Alpha.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PreferenceKey = "RemnantSquadAlpha.OpenedAlphaSceneOnLoad";
    private const string GenericRunGunRefreshKey = "RemnantSquadAlpha.GenericRunGunSceneRefresh.2";

    static OpenAlphaSceneOnLoad()
    {
        EditorApplication.delayCall += EnsureAlphaSceneIsReady;
    }

    private static void EnsureAlphaSceneIsReady()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (activeScene.path == LegacyAlphaScenePath ||
            activeScene.path == PreviousAlphaScenePath ||
            activeScene.path == PreviousLegacyAlphaScenePath ||
            (activeScene.path == SampleScenePath && !SessionState.GetBool(PreferenceKey, false)))
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SessionState.SetBool(PreferenceKey, true);
                EditorSceneManager.OpenScene(AlphaScenePath);
            }
        }

        RegenerateAlphaSceneIfGenericRunGunArtIsMissing();
    }

    private static void RegenerateAlphaSceneIfGenericRunGunArtIsMissing()
    {
        if (SessionState.GetBool(GenericRunGunRefreshKey, false))
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != AlphaScenePath || SceneContainsObjectNamed("TM_Collision_Foreground"))
            return;

        SessionState.SetBool(GenericRunGunRefreshKey, true);
        SubwayOutpostSceneBuilder.GenerateSceneBatch();
    }

    private static bool SceneContainsRootObjectPrefix(string prefix)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name.StartsWith(prefix))
                return true;
        }

        return false;
    }

    private static bool SceneContainsObjectNamed(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (ContainsObjectNamedRecursive(roots[i].transform, objectName))
                return true;
        }

        return false;
    }

    private static bool ContainsObjectNamedRecursive(Transform transform, string objectName)
    {
        if (transform.name == objectName)
            return true;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (ContainsObjectNamedRecursive(transform.GetChild(i), objectName))
                return true;
        }

        return false;
    }
}
#endif
