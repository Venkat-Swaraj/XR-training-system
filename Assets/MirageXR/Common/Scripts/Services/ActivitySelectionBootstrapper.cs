using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.VerboseLogging;
using UnityEngine;

public class ActivitySelectionBootstrapper : BaseServiceBootstrapper
{
    [SerializeField] private ActivitySelectionSceneReferenceServiceConfiguration referenceServiceConfiguration;

    protected override void RegisterServices()
    {
        ActivitySelectionSceneReferenceService referenceService = new ActivitySelectionSceneReferenceService(referenceServiceConfiguration);
        ServiceManager.RegisterService(referenceService);
        AppLog.LogTrace("Added services for activity selection scene", this);
    }

    protected override void UnRegisterServices()
    {
        if (ServiceManager.ServiceExists<ActivitySelectionSceneReferenceService>())
        {
            ServiceManager.RemoveService<ActivitySelectionSceneReferenceService>();
            AppLog.LogTrace("Removed services for activity selection scene", this);
        }
    }
}
