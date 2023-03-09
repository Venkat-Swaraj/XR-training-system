using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.VerboseLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivitySelectionSceneReferenceService : IService
{
    public ActivitySelectionSceneReferenceServiceConfiguration References { get; private set; }

    public ActivitySelectionSceneReferenceService(ActivitySelectionSceneReferenceServiceConfiguration configuration)
    {
        References = configuration;
    }

    public void Cleanup()
    {
        AppLog.LogTrace("Cleaned up activity selection scene reference service", this);
    }

    public void Initialize(IServiceManager owner)
    {
        AppLog.LogTrace("Initialized activity selection scene reference service", this);
    }
}
