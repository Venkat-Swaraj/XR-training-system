using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;
using UnityEngine.UI;

namespace MirageXR
{
    public class ImageMarkerController : MirageXRPrefab
    {
        private string ImgMName;
        private ToggleObject _obj;

        private Detectable detectable;
        private GameObject detectableOB;
        private ImageTargetManager imageTargetManager;
        private UniversalImageTarget imageTarget;

        private void Awake()
        {
            this.imageTargetManager = RootObject.Instance.imageTargetManager;
        }

        public override bool Init(ToggleObject obj)
        {
            this._obj = obj;

            var workplaceManager = RootObject.Instance.workplaceManager;
            this.detectable = workplaceManager.GetDetectable(workplaceManager.GetPlaceFromTaskStationId(_obj.id));
            this.detectableOB = GameObject.Find(this.detectable.id); // GameObject.Find("testCube"); Instantiate(RootObject.Instance.testCube); 

            // Check that url is not empty.
            if (string.IsNullOrEmpty(_obj.url))
            {
                Debug.Log("Content URL not provided.");
                return false;
            }

            // Try to set the parent and if it fails, terminate initialization.
            if (!SetParent(this._obj))
            {
                Debug.Log("Couldn't set the parent.");
                return false;
            }

            // Get the last bit of the url.
            var id = this._obj.url.Split('/')[this._obj.url.Split('/').Length - 1];

            // Rename with the predicate + id to get unique name.
            this.name = this._obj.predicate + "_" + id;

            // Load from resources.
            if (this._obj.url.StartsWith("resources://"))
            {
                // Set image url.
                this.ImgMName = this._obj.url.Replace("resources://", "");
            }

            // Load from external url.
            else
            {
                // Set image url.
                this.ImgMName = this._obj.url;
            }

            if (GameObject.Find(this.ImgMName))
            {
                this.imageTargetManager.RegisterImageTracker(this.imageTargetManager.GetImageTargetFromName(this.ImgMName));
            }
            else
            {
                this.StartCoroutine(nameof(this.LoadImage));
            }

            return base.Init(this._obj);
        }

        private IEnumerator LoadImage()
        {
            byte[] byteArray = File.ReadAllBytes(Path.Combine(RootObject.Instance.activityManager.ActivityPath, this.ImgMName));

            Texture2D loadTexture = new Texture2D((int)_obj.scale, (int)_obj.scale);

            bool isLoaded = loadTexture.LoadImage(byteArray);

            yield return isLoaded;


            if (isLoaded)
            {
                this.imageTarget = this.imageTargetManager.CreateUniversalImageTarget(this.ImgMName, loadTexture, this.detectableOB);
                this.imageTargetManager.RegisterImageTracker(this.imageTarget);

            }
            else
            {
                // debugLog.text += "Failed to load image";
                Debug.Log("Failed to load image");
            }
        }

        public void PlatformOnDestroy()
        {
            this.imageTargetManager.OnPlatfromDestroy(this.imageTarget, GameObject.Find("Detectables"));
        }

        public override void Delete()
        {
            // changed Delete to a virtual method so I could overide it for Image markers as they were being deleted twice when changing activities causeing the new activity not to load
        }
    }
}

[RequireComponent(typeof(TrackableBehaviour))]
public class TrackableEventHandlerEvents : MonoBehaviour
{
    [SerializeField] private TrackableBehaviour _trackableBehaviour;

    public UnityEvent onTrackingFound;
    public UnityEvent onTrackingLost;

    public GameObject augmentation;
    private bool tracked;

    private void Awake()
    {
        if (!_trackableBehaviour) _trackableBehaviour = GetComponent<TrackableBehaviour>();

        if (!_trackableBehaviour)
        {
            Debug.LogError($"This component requires a {nameof(TrackableBehaviour)} !", this);
            return;
        }

        _trackableBehaviour.RegisterOnTrackableStatusChanged(OnTrackableStateChanged);
        tracked = false;
    }




    /// <summary>
    /// called when the tracking state changes.
    /// </summary>
    private void OnTrackableStateChanged(TrackableBehaviour.StatusChangeResult status) // , TrackableBehaviour.Status newStatus)
    {

        switch (status.NewStatus)
        {
            case TrackableBehaviour.Status.DETECTED:
            case TrackableBehaviour.Status.TRACKED:
            case TrackableBehaviour.Status.EXTENDED_TRACKED:
                OnTrackingFound();
                break;

            default:
                OnTrackingLost();
                break;
        }
    }


    protected virtual void OnTrackingFound()
    {
        Debug.Log("Trackable " + _trackableBehaviour.TrackableName + " found");
        // onTrackingFound.Invoke();
        // augmentation.transform.position = _trackableBehaviour.transform.position;//new Vector3(0, 0, 0);
        tracked = true;
    }

    protected virtual void OnTrackingLost()
    {
        Debug.Log("Trackable " + _trackableBehaviour.TrackableName + " lost");
        // onTrackingLost.Invoke();
        tracked = false;
    }
}