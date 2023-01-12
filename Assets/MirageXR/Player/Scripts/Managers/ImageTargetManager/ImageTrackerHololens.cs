using UnityEngine;
using Vuforia;

/// <summary>
/// ImageTracker for HoloLens builds using Vuforia.
/// </summary>
public class ImageTrackerHololens : ImageTracker
{
    private ObjectTracker objectTracker;
    private TrackableBehaviour trackableBehaviour;

    private int selectedTracker;

    /// <summary>
    /// sets up object tracker.
    /// </summary>
    public override void Init()
    {
        this.objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
    }

    /// <summary>
    /// Adds a new image target and creates a Vuforia image target or finds an existing image target and adds the tracked object.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTaget used to create a new image target.</param>
    public override void RegisterImage(UniversalImageTarget imageTarget)
    {
        if (this.ImageTargets.Contains(imageTarget))
        {
            this.selectedTracker = this.FindImageTargetIndex(imageTarget);

            this.SetTrackableBehaviourPrefab();
        }
        else
        {
            this.ImageTargets.Add(imageTarget);

            this.selectedTracker = this.ImageTargets.IndexOf(imageTarget);

            VuforiaARController.Instance.RegisterVuforiaStartedCallback(this.HoloLensCreateImageTargetFromImageFile);
        }
    }

    /// <summary>
    /// Removes a UniversalImageTarget from image targets.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTaget to be removed.</param>
    public override void RemoveImage(UniversalImageTarget imageTarget)
    {
        int i = 0;

        while (i < this.ImageTargets.Count)
        {
            if (this.ImageTargets[i] == imageTarget)
            {
                GameObject.Find(this.ImageTargets[i].TargetName).SetActive(false);

                this.ImageTargets.Remove(this.ImageTargets[i]);

                this.selectedTracker = i;
            }

            i++;
        }
    }

    /// <summary>
    /// Replaces an image targets prefap.
    /// </summary>
    /// <param name="imageTarget">Image target that prefab will be replaced on.</param>
    /// <param name="newPrefab">The new prefab for the image target.</param>
    public override void ReplaceImagePrefab(UniversalImageTarget imageTarget, GameObject newPrefab)
    {
        this.ImageTargets[this.FindImageTargetIndex(imageTarget)].Prefab = newPrefab;
    }

    /// <summary>
    /// Returns the current game object of the image target prefab.
    /// </summary>
    /// <param name="imageTarget">The first name to join.</param>
    /// <returns>Image target prefab.</returns>
    public override GameObject GetTackedImagePrefab(UniversalImageTarget imageTarget)
    {
        return this.ImageTargets[this.FindImageTargetIndex(imageTarget)].Prefab;
    }

    /// <summary>
    /// Used to set the max number of moving images (not applicaple for HoloLens).
    /// </summary>
    /// <param name="max">The first name to join.</param>
    public override void SetMovingImages(int max)
    {
        Debug.Log("N/A On HoloLens");
    }

    public override void PlatformOnDestroy(UniversalImageTarget imageTarget)
    {
        //imageTarget.Prefab.transform.parent = newParent.transform;
    }

    public override void PlatformOnDestroy(UniversalImageTarget imageTarget, GameObject newParent)
    {
        imageTarget.Prefab.transform.parent = newParent.transform;
    }

    /// <summary>
    /// Instatiates tracked image prefab and sets its parent to the tracked image object. If the tracked image prefab already exists in the scene it is set as a child but not Instatiated.
    /// </summary>
    private void SetTrackableBehaviourPrefab()
    {
        GameObject augmentation;

        if (!GameObject.Find(this.ImageTargets[this.selectedTracker].Prefab.name))
        {
            augmentation = Instantiate(this.ImageTargets[this.selectedTracker].Prefab, new Vector3(0, 0, 0), Quaternion.identity);
        }
        else
        {
            augmentation = GameObject.Find(this.ImageTargets[this.selectedTracker].Prefab.name);
        }

        augmentation.transform.parent = GameObject.Find(this.ImageTargets[this.selectedTracker].TargetName).transform;

        augmentation.transform.localPosition = new Vector3(0, 0.1f, 0);
    }

    /// <summary>
    /// Creates a vuforia image target.
    /// </summary>
    private void HoloLensCreateImageTargetFromImageFile()
    {
        if (this.objectTracker == null)
        {
            this.objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        }

        this.objectTracker.Start();

        var runtimeImageSource = this.objectTracker.RuntimeImageSource;

        // get the runtime image source and set the texture to load
        bool result = runtimeImageSource.SetImage(this.ImageTargets[this.selectedTracker].Image, this.ImageTargets[this.selectedTracker].Image.width, this.ImageTargets[this.selectedTracker].TargetName);
        Debug.Log("Result: " + result);

        var dataset = this.objectTracker.CreateDataSet();

        if (result)
        {
            // use dataset and use the source to create a new trackable image target called ImageTarget
            this.trackableBehaviour = dataset.CreateTrackable(runtimeImageSource, this.ImageTargets[this.selectedTracker].TargetName);

            this.trackableBehaviour.gameObject.AddComponent<TrackableEventHandlerEvents>();

            this.SetTrackableBehaviourPrefab();
        }

        this.objectTracker.ActivateDataSet(dataset);
    }
}
