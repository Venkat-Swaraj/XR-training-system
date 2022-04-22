﻿using System;
using MirageXR;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Action = MirageXR.Action;

public class ImageEditor : MonoBehaviour
{
    [SerializeField] private Button captureButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private AudioSource shutterPlayer;
    [SerializeField] private UnityEngine.UI.Image previewImage;
    [SerializeField] private Text processingText;
    [SerializeField] private Text label;
    [SerializeField] private Transform annotationStartingPoint;

    private Action _action;
    private ToggleObject _annotationToEdit;
    private Texture2D _capturedImage;
    private string _saveFileName;

    public bool IsThumbnail
    {
        get; set;
    }

    public void SetAnnotationStartingPoint(Transform startingPoint)
    {
        annotationStartingPoint = startingPoint;
    }

    public void Close()
    {
        _action = null;
        _annotationToEdit = null;
        _saveFileName = string.Empty;
        gameObject.SetActive(false);

        Destroy(gameObject);
    }

    public void Open(Action action, ToggleObject annotation)
    {
        _capturedImage = null;
        if (IsThumbnail)
        {
            //show the last thumbnail on previewImage
            var thumbnailPath = Path.Combine(ActivityManager.Instance.Path, "thumbnail.jpg");
            if (File.Exists(thumbnailPath))
            {
                var spriteTexture = Utilities.LoadTexture(thumbnailPath);
                var thumbSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100.0f);
                previewImage.sprite = thumbSprite;
            }

            label.text = "Edit Thumbnail";
        }
        else
        {
            previewImage.sprite = null;
            label.text = "Edit Image";
        }

        //Check if any character will use this image
        if(annotation != null)
        {
            foreach (var character in FindObjectsOfType<MirageXR.CharacterController>())
            {
                if (character.IsImageAssignModeActive)
                    character.SetImage(annotation);
            }
        }

        gameObject.SetActive(true);
        _action = action;
        _annotationToEdit = annotation;
        processingText.transform.parent.gameObject.SetActive(false);
        captureButton.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
    }

    public void OnAccept()
    {
        const string httpPrefix = "http://";
        
        // close without saving if no image was taken
        if (_capturedImage == null)
        {
            Close();
            return;
        }

        if (_annotationToEdit != null)
        {
            EventManager.DeactivateObject(_annotationToEdit);

            // delete the previous image file
            var imageName = _annotationToEdit.url;
            var originalFileName = Path.GetFileName(imageName.Remove(0, httpPrefix.Length));
            var originalFilePath = Path.Combine(ActivityManager.Instance.Path, originalFileName);
            if (File.Exists(originalFilePath))
            {
                File.Delete(originalFilePath);
            }
        }
        else if (!IsThumbnail)
        {
            var detectable = WorkplaceManager.Instance.GetDetectable(WorkplaceManager.Instance.GetPlaceFromTaskStationId(_action.id));
            var originT = GameObject.Find(detectable.id);
            
            var startPointTr = annotationStartingPoint.transform;
            var offset = Utilities.CalculateOffset(startPointTr.position, startPointTr.rotation,
                originT.transform.position, originT.transform.rotation);

            _annotationToEdit = ActivityManager.Instance.AddAnnotation(_action, offset);
            _annotationToEdit.predicate = "image";
        }

        SaveImage();

        //dont add the thumbnail to the activity
        if (!IsThumbnail)
        {
            _annotationToEdit.url = httpPrefix + _saveFileName;
            _annotationToEdit.scale = 0.5f;
            EventManager.ActivateObject(_annotationToEdit);
            EventManager.NotifyActionModified(_action);
        }

        Close();
    }

    private async Task ShowCountdown()
    {
        processingText.transform.parent.gameObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            processingText.text = i.ToString();
            await Task.Delay(1000);
        }
        processingText.text = "...";
    }

    public async void CaptureImageAsync()
    {
        Maggie.Speak("Taking a photo in 3 seconds");

        Debug.Log("\n\n\n\nStartPhotoCapture\n\n\n\n");

        VuforiaBehaviour.Instance.enabled = false;
        Debug.Log("Vuforia enabled?: " + VuforiaBehaviour.Instance.enabled);

        captureButton.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);

#if UNITY_WSA || UNITY_EDITOR
        await ShowCountdown();
#endif
        if (_capturedImage) Destroy(_capturedImage);
        NativeCameraController.TakePicture(OnPictureTaken, IsThumbnail);
    }
    
    private void OnPictureTaken(bool result, Texture2D texture2D)
    {
        PlayCameraSound();
        if (result)
        {
            _capturedImage = texture2D;
            var sprite = Sprite.Create(_capturedImage, new Rect(0, 0, _capturedImage.width, _capturedImage.height),
                new Vector2(0.5f, 0.5f), 100.0f);
            previewImage.sprite = sprite;
        }

        VuforiaBehaviour.Instance.enabled = true;

        processingText.text = string.Empty;
        processingText.transform.parent.gameObject.SetActive(false);
        captureButton.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
    }
    
    private void PlayCameraSound()
    {
        shutterPlayer.Play();
    }

    private void SaveImage()
    {
        _saveFileName = IsThumbnail ? "thumbnail.jpg" : $"MirageXR_Image_{DateTime.Now.ToFileTimeUtc()}.jpg";
        var outputPath = Path.Combine(ActivityManager.Instance.Path, _saveFileName);
        File.WriteAllBytes(outputPath, _capturedImage.EncodeToJPG());
    }
}
