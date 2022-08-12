using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackerMobile : ImageTracker
{

    [SerializeField] private XRReferenceImageLibrary serializedLibrary;
    private ARTrackedImageManager trackedImageManager;
    private MutableRuntimeReferenceImageLibrary mutableRuntimeReferenceImageLibrary;

    private void Awake()
    {
        GameObject tracker = GameObject.Find("MixedRealityPlayspace");

        if (tracker.GetComponent<ARTrackedImageManager>())
        {
            trackedImageManager = tracker.GetComponent<ARTrackedImageManager>();
        }
        else
        {
            trackedImageManager = tracker.AddComponent<ARTrackedImageManager>();
        }

        buildTrackedImageManager();
    }

    public void buildTrackedImageManager() {
        trackedImageManager.referenceLibrary = trackedImageManager.CreateRuntimeLibrary(serializedLibrary);
        trackedImageManager.maxNumberOfMovingImages = 5;
        trackedImageManager.enabled = true;
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

        mutableRuntimeReferenceImageLibrary = trackedImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            SetTrackedImagePrefab(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            trackedImage.transform.Rotate(Vector3.up, 180);
            SetTrackedImagePrefab(trackedImage);
        }
    }

    private void SetTrackedImagePrefab(ARTrackedImage trackedImage)
    {

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            GameObject TrackedImagePrefab = null;

            foreach (var prefab in trackedImagePrefabs)
            {
                if (prefab.name == trackedImage.name)
                {
                    if (!GameObject.Find(prefab.name))
                    {
                        TrackedImagePrefab = Instantiate(prefab, trackedImage.transform.position, trackedImage.transform.rotation);
                    }
                    else {
                        TrackedImagePrefab = GameObject.Find(prefab.name);
                    }

                    //trackedImageManager.trackedImagePrefab = prefab;
                }
            }

            TrackedImagePrefab.transform.position = trackedImage.transform.position;
            TrackedImagePrefab.transform.rotation = trackedImage.transform.rotation;
        }

    }

    private void AddImagesToLibrary(List<Texture2D> imageList, List<string> imageNames, List<GameObject> prefabList)
    {

        int i = 0;

        while (i < imageNames.Count)
        {

            RegisterImage(imageList[i], imageNames[i], imageList[i].width, prefabList[i]);

            i++;
        }
    }



    public override void RegisterImage(Texture2D texture, string imageName, float scale, GameObject prefab)
    {

        var jobHandle = mutableRuntimeReferenceImageLibrary.ScheduleAddImageJob(texture, imageName, scale);

        prefab.name = imageName;

        trackedImageNames.Add(imageName);
        trackedImageImages.Add(texture);
        trackedImagePrefabs.Add(prefab);
    }


    public override void RemoveImage(string imageName) {

        int i = 0;

        while (i < trackedImageNames.Count)
        {
            if (trackedImageNames[i] == imageName)
            {
                trackedImageNames.Remove(trackedImageNames[i]);
                trackedImageImages.Remove(trackedImageImages[i]);
                trackedImagePrefabs.Remove(trackedImagePrefabs[i]);
            }
            i++;
        }

        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;

        buildTrackedImageManager();

        AddImagesToLibrary(trackedImageImages, trackedImageNames, trackedImagePrefabs);
    }

    public override void ReplaceImagePrefab(string imageName, GameObject newPrefab)
    {
        int i = 0;
        while (i < trackedImageNames.Count)
        {
            if (trackedImageNames[i] == imageName)
            {
                trackedImagePrefabs.Remove(trackedImagePrefabs[i]);
                trackedImagePrefabs.Insert(i, newPrefab);
            }
            i++;
        }
    }

    public override GameObject GetTackedObject(string imageName)
    {
        return null;
    }


    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

}

