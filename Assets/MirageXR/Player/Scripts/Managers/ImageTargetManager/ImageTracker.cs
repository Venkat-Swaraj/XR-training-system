using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ImageTracker parent class.
/// </summary>
public abstract class ImageTracker : MonoBehaviour
{
    private List<UniversalImageTarget> imageTargets = new List<UniversalImageTarget>();

    /// <summary>
    /// Gets or sets the list of all tracked image targets.
    /// </summary>
    public List<UniversalImageTarget> ImageTargets
    {
        get { return this.imageTargets; }
        set { this.imageTargets = value; }
    }

    /// <summary>
    /// Used to initialise class.
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// Used to add a new image target.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTarget to be added.</param>
    public abstract void RegisterImage(UniversalImageTarget imageTarget);

    /// <summary>
    /// Used to remove an image target from tracked images.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTarget to be removed.</param>
    public abstract void RemoveImage(UniversalImageTarget imageTarget);

    /// <summary>
    /// Used to replace an image targets displayed prefab.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTarget for prefab to be replaced.</param>
    /// <param name="newPrefab">The new prefab for the image target.</param>
    public abstract void ReplaceImagePrefab(UniversalImageTarget imageTarget, GameObject newPrefab);

    /// <summary>
    /// Returns an image targets prefab.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTarget of the prefab.</param>
    /// <returns>Image target prefab.</returns>
    public abstract GameObject GetTackedImagePrefab(UniversalImageTarget imageTarget);

    /// <summary>
    /// Sets the max number of moving images (Currently only used for ARFoundation tracking).
    /// </summary>
    /// <param name="max">Number of moving images.</param>
    public abstract void SetMovingImages(int max);

    public abstract void PlatformOnDestroy(UniversalImageTarget imageTarget);

    public abstract void PlatformOnDestroy(UniversalImageTarget imageTarget, GameObject newParent);

    /// <summary>
    /// Returns the index of an image target in the tracked images list.
    /// </summary>
    /// <param name="imageTarget">UniversalImageTarget to find the index of.</param
    /// <returns>Image target prefab.</returns>>
    public int FindImageTargetIndex(UniversalImageTarget imageTarget)
    {
        return this.ImageTargets.IndexOf(imageTarget);
    }
}
