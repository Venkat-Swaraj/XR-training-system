namespace MirageXR
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Class used to manage image targets. Designed to be used for both Vuforia and ARFoundations tracking (and any other tracking solutions used in the future).
    /// </summary>
    public class ImageTargetManager : MonoBehaviour
    {
        [SerializeField] private GameObject imageTrackerMobile;
        [SerializeField] private GameObject imageTrackerHololens;

        private ImageTracker imageTracker;
        private Texture2D tex;

        /// <summary>
        /// Used to create a new UniversalImageTarget.
        /// </summary>
        /// <param name="targetName">Image target name.</param>
        /// <param name="texture">Texture2D of the image target.</param>
        /// <param name="prefab">Prefab that will be displayed when the image target is being tracked.</param>
        /// <param name="path">The path of the image target image file.</param>
        /// <returns> New UniversalImageTarget.</returns>
        public UniversalImageTarget CreateUniversalImageTarget(string targetName, Texture2D texture, GameObject prefab, string path)
        {
            UniversalImageTarget imageTarget = new UniversalImageTarget
            {
                TargetName = targetName,
                Path = path,
                Image = texture,
                Prefab = prefab,
                Scale = texture.width,
            };

            return imageTarget;
        }

        /// <summary>
        /// Used to create a new UniversalImageTarget.
        /// </summary>
        /// <param name="targetName">Image target name.</param>
        /// <param name="prefab">Prefab that will be displayed when the image target is being tracked.</param>
        /// <param name="path">The path of the image target image file.</param>
        /// <returns> New UniversalImageTarget.</returns>
        public UniversalImageTarget CreateUniversalImageTarget(string targetName, GameObject prefab, string path)
        {
            Texture2D t = this.LoadImage(path, targetName);

            UniversalImageTarget imageTarget = new UniversalImageTarget
            {
                TargetName = targetName,
                Path = path,
                Image = t,
                Prefab = prefab,
                Scale = t.width,
            };

            return imageTarget;
        }


        /// <summary>
        /// Used to create a new UniversalImageTarget.
        /// </summary>
        /// <param name="targetName">Image target name.</param>
        /// <param name="texture">Texture2D of the image target.</param>
        /// <param name="prefab">Prefab that will be displayed when the image target is being tracked.</param>
        /// <returns> New UniversalImageTarget.</returns>
        public UniversalImageTarget CreateUniversalImageTarget(string targetName, Texture2D texture, GameObject prefab)
        {
            UniversalImageTarget imageTarget = new UniversalImageTarget
            {
                TargetName = targetName,
                Path = "NOT SET",
                Image = texture,
                Prefab = prefab,
                Scale = texture.width,
            };

            return imageTarget;
        }

        /// <summary>
        /// Adds UniversalImagetarget to list of tracked image targets.
        /// </summary>
        /// <param name="imageTarget">Image target to be added.</param>
        public void RegisterImageTracker(UniversalImageTarget imageTarget)
        {
            this.ImageTrackerInstance().RegisterImage(imageTarget);
        }

        /// <summary>
        /// Removes UniversalImagetarget from list of tracked image targets.
        /// </summary>
        /// <param name="imageTarget">Image target to be removed.</param>
        public void RemoveImagetracker(UniversalImageTarget imageTarget)
        {
            this.ImageTrackerInstance().RemoveImage(imageTarget);
        }

        /// <summary>
        /// Returns the prefab displayed when image target is tracked.
        /// </summary>
        /// <param name="imageTarget">Image target to be added.</param>
        /// <returns> Tracked image prefab.</returns>
        public GameObject TrackedImagePrefab(UniversalImageTarget imageTarget)
        {
            return this.ImageTrackerInstance().GetTackedImagePrefab(imageTarget);
        }

        /// <summary>
        /// Replace the prefab that is displayed when an image target is being tracked.
        /// </summary>
        /// <param name="imageTarget">Image target for new prefab.</param>
        /// <param name="newPrefab">New prefab to be shown.</param>
        public void ReplaceTrackedImagePrefab(UniversalImageTarget imageTarget, GameObject newPrefab)
        {
            this.ImageTrackerInstance().ReplaceImagePrefab(imageTarget, newPrefab);
        }

        /// <summary>
        /// Sets the max number of moving images (Currently only used for ARFoundation tracking).
        /// </summary>
        /// <param name="max">Number of moving images.</param>
        public void SetMaxMovingImages(int max)
        {
            this.ImageTrackerInstance().SetMovingImages(max);
        }

        /// <summary>
        /// Returns the UniversalImageTarget when given its name.
        /// </summary>
        /// <param name="name">Image target to be found.</param>
        /// <returns> Found UniversalImageTarget.</returns>
        public UniversalImageTarget GetImageTargetFromName(string name)
        {
            UniversalImageTarget imageTarget = null;

            foreach (var target in this.ImageTrackerInstance().ImageTargets)
            {
                if (target.TargetName == name)
                {
                    imageTarget = target;
                }
            }

            return imageTarget;
        }

        /// <summary>
        /// Used to destroy an image target in both the Vuforia and ARFoundations way. Image target prefab will be destroyed.
        /// </summary>
        /// <param name="imageTarget">Image target to be destoryed.</param>
        public void OnPlatfromDestroy(UniversalImageTarget imageTarget)
        {
            this.ImageTrackerInstance().PlatformOnDestroy(imageTarget);
        }

        /// <summary>
        /// Used to destroy an image target in both the Vuforia and ARFoundations way. Image target prefab will move to new parent.
        /// </summary>
        /// <param name="imageTarget">Image target to be destoryed.</param>
        /// <param name="parentPrefab">New parent for image target prefab.</param>
        public void OnPlatfromDestroy(UniversalImageTarget imageTarget, GameObject parentPrefab)
        {
            this.ImageTrackerInstance().PlatformOnDestroy(imageTarget, parentPrefab);
        }

        /// <summary>
        /// Returns the list of tracked UniversalImageTargets.
        /// </summary>
        /// <returns> List of all tracked UniversalImageTargets.</returns>
        public List<UniversalImageTarget> GetImageTargetList()
        {
            return this.ImageTrackerInstance().ImageTargets;
        }

        private void Start()
        {
            this.CreateImageTracker();
        }

        private void CreateImageTracker()
        {
#if UNITY_ANDROID || UNITY_IOS
            this.imageTracker = new ImageTrackerMobile();
#else
            this.imageTracker = new ImageTrackerHololens();
#endif
            this.imageTracker.Init();
        }

        private ImageTracker ImageTrackerInstance()
        {
            return this.imageTracker;
        }

        private Texture2D LoadImage(string path, string name)
        {
            Texture2D loadTexture = new Texture2D(2, 2);

            byte[] byteArray = File.ReadAllBytes(Path.Combine(path, name));
            loadTexture.LoadImage(byteArray);

            this.tex = loadTexture;
            return loadTexture;
        }
    }
}