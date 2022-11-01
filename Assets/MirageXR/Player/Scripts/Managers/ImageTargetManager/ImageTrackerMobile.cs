using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackerMobile : ImageTracker
{

    [SerializeField] private XRReferenceImageLibrary serializedLibrary;
    private ARTrackedImageManager trackedImageManager;
    private MutableRuntimeReferenceImageLibrary mutableRuntimeReferenceImageLibrary;
    private Text IMT;

    private void Awake()
    {

        IMT = GameObject.Find("IMText").GetComponent<Text>();

        IMT.text = "TEXT OBJECT FOUND";

        GameObject tracker = GameObject.Find("MixedRealityPlayspace");

        if (tracker.GetComponent<ARTrackedImageManager>())
        {
            trackedImageManager = tracker.GetComponent<ARTrackedImageManager>();
        }
        else
        {
            trackedImageManager = tracker.AddComponent<ARTrackedImageManager>();
        }

        
    }

    public override void Init()
    {
        buildTrackedImageManager(1);
    }

    public void buildTrackedImageManager(int NumberOfMovingImages) 
    {
        trackedImageManager.referenceLibrary = trackedImageManager.CreateRuntimeLibrary(serializedLibrary);
        trackedImageManager.maxNumberOfMovingImages = NumberOfMovingImages;
        trackedImageManager.enabled = true;
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

        mutableRuntimeReferenceImageLibrary = trackedImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;

        if (mutableRuntimeReferenceImageLibrary == null)
        {
            IMT.text = "\n MRL NULL";
        }
        else {
            IMT.text = "\n MRL MADE";
        }     
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            SetTrackedImagePrefab(trackedImage);

            IMT.text = trackedImage + " ADDED";
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            SetTrackedImagePrefab(trackedImage);
            IMT.text = trackedImage + " TRACKED";
        }
    }

    private void SetTrackedImagePrefab(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            GameObject TrackedImagePrefab = null;

            foreach (var imageTarget in ImageTargets)
            {
                if (imageTarget.TargetName == trackedImage.name)
                {
                    if (!GameObject.Find(imageTarget.TargetName))
                    {
                        TrackedImagePrefab = Instantiate(imageTarget.Prefab, trackedImage.transform.position, trackedImage.transform.rotation);
                    }
                    else {
                        TrackedImagePrefab = GameObject.Find(imageTarget.TargetName);
                    }

                    //trackedImageManager.trackedImagePrefab = prefab;
                }
            }

            TrackedImagePrefab.transform.position = trackedImage.transform.position;
            TrackedImagePrefab.transform.rotation = trackedImage.transform.rotation;
        }

    }


    public async override void RegisterImage(UniversalImageTarget imageTarget)
    {
        var jobHandle = mutableRuntimeReferenceImageLibrary.ScheduleAddImageJob(imageTarget.Image, imageTarget.TargetName, imageTarget.Scale);

        //imageTarget.Prefab.name = imageTarget.TargetName;

        ImageTargets.Add(imageTarget);

        jobHandle.Complete();

        if (jobHandle.IsCompleted)
        {
            IMT.text =  imageTarget.TargetName + "\n" + " ADDED";
        }
        else {
            IMT.text = imageTarget.TargetName + "\n" + " NOT ADDED";
        }
        
    }


    public override void RemoveImage(UniversalImageTarget imageTarget) {

        int index = FindImageTargetIndex(imageTarget);

        ImageTargets.Remove(imageTarget);

        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;

        buildTrackedImageManager(trackedImageManager.maxNumberOfMovingImages);

        AddImagesToLibrary(ImageTargets);
    }

    public override void ReplaceImagePrefab(UniversalImageTarget imageTarget, GameObject newPrefab)
    {
        int index = FindImageTargetIndex(imageTarget);

        ImageTargets[index].Prefab = newPrefab;
    }

    public override GameObject GetTackedImagePrefab(UniversalImageTarget imageTarget)
    {
        return ImageTargets[FindImageTargetIndex(imageTarget)].Prefab;
    }

    public override void SetMovingImages(int max)
    {
        buildTrackedImageManager(max);
    }

    private void AddImagesToLibrary(List<UniversalImageTarget> imageTargets)
    {

        int i = 0;

        while (i < imageTargets.Count)
        {

            RegisterImage(imageTargets[i]);

            i++;
        }
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

}

