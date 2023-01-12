using MirageXR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackerMobile : ImageTracker
{
    private ARTrackedImageManager trackedImageManager;
    private MutableRuntimeReferenceImageLibrary mutableRuntimeReferenceImageLibrary;
    private Text IMT;

    private void Awake()
    {

    }

    public override void Init()
    {
        this.IMT = RootObject.Instance.IMT;

        this.IMT.text = "TEXT OBJECT FOUND";

        GameObject tracker = GameObject.Find("MixedRealityPlayspace");

        if (tracker.GetComponent<ARTrackedImageManager>())
        {
            this.trackedImageManager = tracker.GetComponent<ARTrackedImageManager>();
        }
        else
        {
            this.trackedImageManager = tracker.AddComponent<ARTrackedImageManager>();
        }

        this.buildTrackedImageManager(1);
    }

    public void buildTrackedImageManager(int numberOfMovingImages)
    {
        this.trackedImageManager.referenceLibrary = this.trackedImageManager.CreateRuntimeLibrary(RootObject.Instance.serializedLibrary);
        this.trackedImageManager.requestedMaxNumberOfMovingImages = numberOfMovingImages;
        this.trackedImageManager.enabled = true;
        this.trackedImageManager.trackedImagesChanged += this.OnTrackedImagesChanged;

        this.mutableRuntimeReferenceImageLibrary = this.trackedImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;

        if (this.mutableRuntimeReferenceImageLibrary == null)
        {
            this.IMT.text = "\n MRL NULL";
        }
        else
        {
            this.IMT.text = this.IMT.text + "\n MRL MADE";
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            this.SetTrackedImagePrefab(trackedImage);

            this.IMT.text = trackedImage.referenceImage.name + " ADDED";
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            this.SetTrackedImagePrefab(trackedImage);
            this.IMT.text = " updated tracking: \n" + trackedImage.referenceImage.name + "\n looking for target: \n" + ImageTargets[0].TargetName;
        }
    }

    private void SetTrackedImagePrefab(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            this.IMT.text = " TRACKING \n" + trackedImage.referenceImage.name + "\n looking for target: \n" + ImageTargets[0].TargetName;
            GameObject trackedImagePrefab = null;

            foreach (var imageTarget in this.ImageTargets)
            {
                if (imageTarget.TargetName == trackedImage.referenceImage.name)
                {
                    this.IMT.text = "\n IT Exists";
                    if (!GameObject.Find(imageTarget.Prefab.name))
                    {
                        trackedImagePrefab = Instantiate(imageTarget.Prefab, trackedImage.transform.position, trackedImage.transform.rotation);
                        this.IMT.text = this.IMT.text + "\n CREATING PREFAB: " + imageTarget.Prefab.name;
                    }
                    else
                    {
                        this.IMT.text = this.IMT.text + "\n finding PREFAB: " + imageTarget.Prefab.name;
                        trackedImagePrefab = GameObject.Find(imageTarget.Prefab.name);
                    }

                    trackedImagePrefab.transform.position = trackedImage.transform.position;
                    trackedImagePrefab.transform.rotation = trackedImage.transform.rotation * Quaternion.Euler(0, 90, 0);
                }
            }
        }

    }

    public async override void RegisterImage(UniversalImageTarget imageTarget)
    {
        this.ImageTargets.Add(imageTarget);

        var jobHandle = mutableRuntimeReferenceImageLibrary.ScheduleAddImageJob(imageTarget.Image, imageTarget.TargetName, imageTarget.Scale);

        jobHandle.Complete();

        if (jobHandle.IsCompleted)
        {
            this.IMT.text = imageTarget.TargetName + "\n" + " ADDED";
        }
        else
        {
            this.IMT.text = imageTarget.TargetName + "\n" + " NOT ADDED";
        }
    }

    public override void RemoveImage(UniversalImageTarget imageTarget)
    {
        int index = this.FindImageTargetIndex(imageTarget);

        this.ImageTargets.Remove(imageTarget);
        this.IMT.text = imageTarget.TargetName + "\n" + " Removed";

        this.trackedImageManager.trackedImagesChanged -= this.OnTrackedImagesChanged;

        this.buildTrackedImageManager(this.trackedImageManager.currentMaxNumberOfMovingImages);

        this.AddImagesToLibrary(this.ImageTargets);
    }

    public override void ReplaceImagePrefab(UniversalImageTarget imageTarget, GameObject newPrefab)
    {
        int index = this.FindImageTargetIndex(imageTarget);

        this.ImageTargets[index].Prefab = newPrefab;
    }

    public override GameObject GetTackedImagePrefab(UniversalImageTarget imageTarget)
    {
        return this.ImageTargets[this.FindImageTargetIndex(imageTarget)].Prefab;
    }

    public override void SetMovingImages(int max)
    {
        this.buildTrackedImageManager(max);
    }

    private void AddImagesToLibrary(List<UniversalImageTarget> imageTargets)
    {
        foreach (var imageTarget in imageTargets)
        {
            this.RegisterImage(imageTarget);
        }
    }

    private void OnDisable()
    {
        //this.trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    public override void PlatformOnDestroy(UniversalImageTarget imageTarget)
    {
        this.RemoveImage(imageTarget);
    }

    public override void PlatformOnDestroy(UniversalImageTarget imageTarget, GameObject newParent)
    {
        this.RemoveImage(imageTarget);
    }
}