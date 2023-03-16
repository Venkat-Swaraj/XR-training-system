using i5.Toolkit.Core.VerboseLogging;
using MirageXR;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class that handles the selection of the new scene
/// This script should exist in the AppSelection scene and decides where to go from there
/// </summary>
public class AppSelection : MonoBehaviour
{
    private static string activityToLoad = null;

    public static string ActivityToLoad => activityToLoad;

    /// <summary>
    /// Start MirageXR Recorder
    /// </summary>
    public void StartRecorder()
    {
        AppLog.LogInfo("Starting the recorder");
        EventManager.Click();
        SpatialMappingHelper.ActivateSpatialMapping();
        SceneManager.LoadSceneAsync("Recorder", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("AppSelection");
    }

    /// <summary>
    /// Start MirageXR Player
    /// </summary>
    public void StartPlayer()
    {
        AppLog.LogInfo("Starting the player");
        EventManager.Click();
        SpatialMappingHelper.DeactivateSpatialMapping();


        SceneManager.LoadSceneAsync(PlatformManager.Instance.PlayerSceneName, LoadSceneMode.Additive);

        SceneManager.UnloadSceneAsync("AppSelection");
    }

    public void StartLoader()
    {
        AppLog.LogInfo("Starting ARLEM loading scene");
        EventManager.Click();
        SpatialMappingHelper.DeactivateSpatialMapping();
        SceneManager.LoadSceneAsync("ArlemLoading", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("AppSelection");
    }
}
