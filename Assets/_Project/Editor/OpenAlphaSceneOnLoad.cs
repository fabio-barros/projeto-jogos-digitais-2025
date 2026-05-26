#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OpenAlphaSceneOnLoad
{
    private const string AlphaScenePath = "Assets/_Project/Scenes/Level_01_Alpha.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PreferenceKey = "RemnantSquadAlpha.OpenedAlphaSceneOnLoad";

    static OpenAlphaSceneOnLoad()
    {
        EditorApplication.delayCall += OpenAlphaSceneIfSampleSceneIsActive;
    }

    private static void OpenAlphaSceneIfSampleSceneIsActive()
    {
        if (SessionState.GetBool(PreferenceKey, false))
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != SampleScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            SessionState.SetBool(PreferenceKey, true);
            EditorSceneManager.OpenScene(AlphaScenePath);
        }
    }
}
#endif
