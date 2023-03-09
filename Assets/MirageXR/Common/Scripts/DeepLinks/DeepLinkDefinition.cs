using i5.Toolkit.Core.DeepLinkAPI;
using i5.Toolkit.Core.VerboseLogging;
using MirageXR;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Defines deep link paths to which the application should react
/// </summary>
public class DeepLinkDefinition
{
    /// <summary>
    /// Load an activity via a deep link, e.g. wekit:/load?id=123&dowload=somePath
    /// </summary>
    /// <param name="args">Arguments that are passed with the deep link;
    /// should contain the parameters id and download</param>
    [DeepLink(path: "load")]
    public async void LoadActivity(DeepLinkArgs args)
    {
        AppLog.LogInfo("Received deep link for opening an activity: " + args.DeepLink);
        if (args.Parameters.TryGetValue("id", out string activityId)
            && args.Parameters.TryGetValue("download", out string downloadPath))
        {
            if (File.Exists(Path.Combine(Application.persistentDataPath, activityId)))
            {
                AppLog.LogTrace($"Activity with id {activityId} was already downloaded. Skipping download.", this);
                await Open(activityId);
            }
            else
            {
                AppLog.LogInfo($"Downloading and opening activity with id {activityId} because of deep link", this);
                bool success = await Download(downloadPath, activityId);
                if (success)
                {
                    AppLog.LogTrace($"Successfully downloaded activity with id {activityId}. Now opening it.", this);
                    await Open(activityId);
                }
                else
                {
                    AppLog.LogError($"Could not download activity {activityId}", this);
                }
            }
        }
        else
        {
            AppLog.LogError("Deep Link is missing id or download parameter", this);
            DialogWindow.Instance.Show(
            "Info!",
            "Activity launch protocol is not complete",
            new DialogButtonContent("Ok"));
        }
    }

    /// <summary>
    /// Creates a new activity via a deep link, e.g. using wekit:/new
    /// </summary>
    [DeepLink(path: "new")]
    public async void NewActivity()
    {
        AppLog.LogInfo("Received deep link for creating a new activity", this);
        await RootObject.Instance.editorSceneService.LoadEditorAsync();
        await RootObject.Instance.activityManager.CreateNewActivity();
    }

    // opens the given activity
    private async Task Open(string activity)
    {
        AppLog.LogTrace($"Opening activity with id {activity}", this);
        string fullActivityJson = $"{activity}-activity.json";
        PlayerPrefs.SetString("activityUrl", fullActivityJson);
        PlayerPrefs.Save();

        await RootObject.Instance.editorSceneService.LoadEditorAsync();
        await RootObject.Instance.activityManager.LoadActivity(fullActivityJson);
    }

    // downloads the zip file of the activity from the given downloadPath and unzips the file
    private async Task<bool> Download(string downloadPath, string activityId)
    {
        AppLog.LogInfo($"Downloading activity with id {activityId} to path {downloadPath}", this);

        bool success;
        using (SessionDownloader downloader = new SessionDownloader(
            DBManager.domain + "/pluginfile.php/" + downloadPath,
            activityId + ".zip"))
        {
            success = await downloader.DownloadZipFileAsync();

            if (success)
            {
                AppLog.LogTrace($"Successfully downloaded activity {activityId}. Unzipping...", this);
                try
                {
                    await downloader.UnzipFileAsync();
                    AppLog.LogTrace($"Successfully unzipped activity {activityId}", this);
                }
                catch (Exception e)
                {
                    AppLog.LogError($"Unzipping of activity {activityId} failed because of exception {e}", this);
                    success = false;
                }
            }
            else
            {
                AppLog.LogError($"Download of activity {activityId} failed.", this);
                DialogWindow.Instance.Show(
                "Info!",
                "Activity not found! Please check that you are connected to the correct Moodle repository",
                new DialogButtonContent("Ok"));
            }
        }
        return success;
    }
}
