using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using System.IO;

public class ImageTrackerHololens : ImageTracker
{
    private GameObject IM;
    private ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
    private TrackableBehaviour trackableBehaviour;

    private int selectedTracker;

    public override void RegisterImage(Texture2D texture, string imageName, float scale, GameObject prefab)
    {
        trackedImageNames.Add(imageName);
        trackedImageImages.Add(texture);
        trackedImagePrefabs.Add(prefab);

        selectedTracker = trackedImageImages.Count;

        VuforiaARController.Instance.RegisterVuforiaStartedCallback(HoloLensCreateImageTargetFromImageFile);
    }


    public override void RemoveImage(string imageName)
    {
        int i = 0;

        while (i < trackedImageNames.Count)
        {
            if (trackedImageNames[i] == imageName)
            {
                GameObject.Find(trackedImageNames[i]).SetActive(false);

                trackedImageNames.Remove(trackedImageNames[i]);
                trackedImageImages.Remove(trackedImageImages[i]);
                trackedImagePrefabs.Remove(trackedImagePrefabs[i]);

                selectedTracker = i;
            }
            i++;
        }

    }

    public override void ReplaceImagePrefab(string imageName, GameObject newPrefab)
    {

    }

    public override GameObject GetTackedObject(string imageName)
    {
        return null;
    }

    private void HoloLensCreateImageTargetFromImageFile()
    {

        objectTracker.Start();

        Debug.Log("is tracker active = " + objectTracker.IsActive);


        var runtimeImageSource = objectTracker.RuntimeImageSource;
        bool result = runtimeImageSource.SetImage(trackedImageImages[selectedTracker], trackedImageImages[selectedTracker].width, trackedImageNames[selectedTracker]);
        // get the runtime image source and set the texture to load

        Debug.Log("Result: " + result);

        var dataset = objectTracker.CreateDataSet();

        if (result)
        {
            trackableBehaviour = dataset.CreateTrackable(runtimeImageSource, trackedImageNames[selectedTracker]);
            // use dataset and use the source to create a new trackable image target called ImageTarget

            trackableBehaviour.gameObject.AddComponent<TrackableEventHandlerEvents>();

            SetTrackableBehaviourPrefab();
        }
        objectTracker.ActivateDataSet(dataset);
    }


    private void SetTrackableBehaviourPrefab()
    {
        GameObject augmentation;


        if (!GameObject.Find(trackedImagePrefabs[selectedTracker].name))
        {
            augmentation = Instantiate(trackedImagePrefabs[selectedTracker], new Vector3(0, 0, 0), Quaternion.identity);//GameObject.Find(detectable.id);
        }
        else
        {
            augmentation = GameObject.Find(trackedImagePrefabs[selectedTracker].name);
        }

        augmentation.transform.parent = trackableBehaviour.transform;

        augmentation.transform.localPosition = new Vector3(0, 0.1f, 0);

    }

}
