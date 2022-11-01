using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MirageXR;

public class ImageTargetManagerUseExample : MonoBehaviour
{

    private ImageTargetManager imageTargetManager;

    private List<string> targetName;
    private List<Texture2D> targetImage;  
    private List<GameObject> targetPrefab;

    private List<UniversalImageTarget> targets;

    void Start()
    {
        imageTargetManager = RootObject.Instance.imageTargetManager;

        UniversalImageTarget target = imageTargetManager.CreateUniversalImageTarget(targetName[0], targetImage[0], targetPrefab[0]);
        targets.Add(target);

        imageTargetManager.RegisterImageTracker(target);
    }

    public void AddSecondTracker()
    {
        UniversalImageTarget target = imageTargetManager.CreateUniversalImageTarget(targetName[1], targetImage[1], targetPrefab[1]);
        targets.Add(target);

        imageTargetManager.RegisterImageTracker(target);
    }

    public void DeleteSecondTracker()
    {
        imageTargetManager.RemoveImagetracker(targets[1]);
    }

    public void changeImagePrefab()
    {
        imageTargetManager.ReplaceTrackedImagePrefab(targets[0], targetPrefab[2]);
    }


    public void getTracker1Location(){

        Debug.Log("Tracker is here: " + imageTargetManager.TrackedImagePrefab(targets[0]).transform.position);
    
    }


}
