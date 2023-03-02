using i5.Toolkit.Core.DeepLinkAPI;
using i5.Toolkit.Core.ExperienceAPI;
using i5.Toolkit.Core.OpenIDConnectClient;
using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.VerboseLogging;
using System;
using UnityEngine;

namespace MirageXR
{
    public class MirageXRServiceBootstrapper : BaseServiceBootstrapper
    {
        [SerializeField]
        private VestServiceConfiguration vestServiceConfiguration;
        [SerializeField]
        private ExperienceAPIClientCredentials xAPICredentialsWEKIT;
        [SerializeField]
        private ExperienceAPIClientCredentials xAPICredentialsARETE;

        [SerializeField] private DeepLinkDefinition deepLinkAPI;

        private void OnEnable()
        {
            EventManager.XAPIChanged += ChangeXAPI;
        }

        private void OnDisable()
        {
            EventManager.XAPIChanged -= ChangeXAPI;
        }

        protected override void RegisterServices()
        {
#if UNITY_EDITOR
            AppLog.MinimumLogLevel = LogLevel.TRACE;
#else
            AppLog.MinimumLogLevel = LogLevel.INFO;
#endif

            PrintStartupInfos();

            ServiceManager.RegisterService(new WorldAnchorService());
            ServiceManager.RegisterService(new KeywordService());

            ServiceManager.RegisterService(new VestService
            {
                VestEnabled = vestServiceConfiguration.vestEnabled
            });

            if (xAPICredentialsWEKIT != null)
            {
                ServiceManager.RegisterService(new ExperienceService(CreateXAPIClient("WEKIT")));
            }
            else
            {
                AppLog.LogWarning("xAPI credentials not set. You will not be able to use the ExperienceService and xAPI analytics", this);
            }

            ServiceManager.RegisterService(new VideoAudioTrackGlobalService());

            OpenIDConnectService oidc = new OpenIDConnectService
            {
                OidcProvider = new SketchfabOidcProvider()
            };

#if !UNITY_EDITOR
            oidc.RedirectURI = "https://wekit-community.org/sketchfab/callback.php";
#else
            // here could be the link to a nicer web page that tells the user to return to the app
#endif

            ServiceManager.RegisterService(oidc);

            DeepLinkingService deepLinks = new DeepLinkingService();
            ServiceManager.RegisterService(deepLinks);

            deepLinkAPI = new DeepLinkDefinition();
            deepLinks.AddDeepLinkListener(deepLinkAPI);
        }

        protected override void UnRegisterServices()
        {
            ServiceManager.GetService<DeepLinkingService>().RemoveDeepLinkListener(deepLinkAPI);
            ServiceManager.RemoveService<DeepLinkingService>();
        }

        private ExperienceAPIClient CreateXAPIClient(string client) {

            ExperienceAPIClient xAPIClient = null;

            switch (client)
            {
                case "WEKIT":
                    xAPIClient = new ExperienceAPIClient
                    {
                        XApiEndpoint = new System.Uri("https://lrs.wekit-ecs.com/data/xAPI"),
                        AuthorizationToken = xAPICredentialsWEKIT.authToken,
                        Version = "1.0.3",
                    };
                    break;
                case "ARETE":
                    xAPIClient = new ExperienceAPIClient
                    {
                        XApiEndpoint = new System.Uri("https://learninglocker.vicomtech.org/data/xAPI"),
                        AuthorizationToken = xAPICredentialsARETE.authToken,
                        Version = "1.0.3",
                    };
                    break;
            }

            AppLog.LogInfo("Initialized LRS client for " + client, this);

            return xAPIClient;
        }


        private void ChangeXAPI(DBManager.LearningRecordStores selectedLRS)
        {
            ServiceManager.RemoveService<ExperienceService>();

            switch (selectedLRS)
            {
                case DBManager.LearningRecordStores.WEKIT:
                    ServiceManager.RegisterService(new ExperienceService(CreateXAPIClient("WEKIT")));
                    break;

                case DBManager.LearningRecordStores.ARETE:
                    ServiceManager.RegisterService(new ExperienceService(CreateXAPIClient("ARETE")));
                    break;
            }

            AppLog.LogInfo($"Switched to {selectedLRS} LRS", this);
        }

        private void PrintStartupInfos()
        {
            AppLog.LogInfo("============== MirageXR ==============\n" +
                $"App Version: {Application.version}\n" +
                $"Compiled with Unity version: {Application.unityVersion}\n" +
                $"Platform: {Application.platform}\n" +
                $"Startup time: {DateTime.Now}\n");
            AppLog.LogInfo("System Specs:\n" +
                $"Device Model: {SystemInfo.deviceModel}\n" +
                $"Device Name: {SystemInfo.deviceName}\n" +
                $"Device Type: {SystemInfo.deviceType}\n" +
                $"OS: {SystemInfo.operatingSystem}\n" +
                $"System Memory: {SystemInfo.systemMemorySize} MB\n" +
                $"System Graphics Memory: {SystemInfo.graphicsMemorySize} MB\n" +
                $"Graphics Device Name: {SystemInfo.graphicsDeviceName}\n" +
                $"Graphics Device Vendor: {SystemInfo.graphicsDeviceVendor}\n" +
                $"Graphics Device Version: {SystemInfo.graphicsDeviceVersion}\n" +
                $"Processor Count: {SystemInfo.processorCount}\n" +
                $"Processor Type: {SystemInfo.processorType}");
        }
    }
}