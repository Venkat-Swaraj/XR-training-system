using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.VerboseLogging;
using System;
using System.IO;
using TiltBrush;
using UnityEngine;

namespace MirageXR
{
    public class VestService : IService
    {
        public bool VestEnabled { get; set; }

        public Sensor VestConfig { get; private set; }

        public void Initialize(IServiceManager owner)
        {
            var filePath = Path.Combine(Application.persistentDataPath, "vestconfig.json");

            AppLog.LogDebug("Trying to load vest configuration from " + filePath, this);

            // If config file doesn't yet exist in the HoloLens folder...
            if (!File.Exists(filePath))
            {
                // Copy the default config file to the HoloLens folder.
                var defaultFile = Resources.Load<TextAsset>("vestconfig");

                File.WriteAllText(filePath, defaultFile.text);

                AppLog.LogDebug("Vest config did not exist, so a default config file was created", this);
            }

            try
            {
                var sensorConfig = File.ReadAllText(filePath);

                VestConfig = JsonUtility.FromJson<Sensor>(sensorConfig);

                AppLog.LogInfo("VEST CONFIG LOADED!");
            }
            catch (Exception e)
            {
                AppLog.LogError("Error while loading the vest configuration: " + e, this);
                Maggie.Speak("Vest configuration doesn't seem to be valid. You will not be able to enable vest.");
            }
        }

        public void Cleanup()
        {
        }

        private void EnableVest()
        {
            AppLog.LogTrace("Trying to enable vest...", this);
            if (VestConfig != null)
            {
                VestEnabled = true;
                RootObject.Instance.activityManager.PlayerReset().AsAsyncVoid();
                Maggie.Speak("Vest enabled.");
                AppLog.LogInfo("Vest was enabled", this);
            }
            else
            {
                Maggie.Speak("Can not enable vest since a valid configuration file was not found.");
                AppLog.LogError("Cannot enable vest since a valid configuration file was not found.", this);
            }
        }

        private void DisableVest()
        {
            AppLog.LogTrace("Trying to disable vest...", this);
            if (VestConfig != null)
            {
                VestEnabled = false;
                RootObject.Instance.activityManager.PlayerReset().AsAsyncVoid();
                Maggie.Speak("Vest disabled.");
                AppLog.LogInfo("Vest was disabled", this);
            }
            else
            {
                Maggie.Speak("Vest already disabled since a valid configuration file was not found.");
                AppLog.LogWarning("Vest already disabled since a valid configuration file was not found.", this);
            }
        }
    }
}