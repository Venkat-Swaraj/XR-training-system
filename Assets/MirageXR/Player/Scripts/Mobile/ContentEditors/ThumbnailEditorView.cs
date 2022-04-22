using System;
using System.IO;
using MirageXR;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Action = System.Action;
using Image = UnityEngine.UI.Image;

public class ThumbnailEditorView : PopupBase
{
    private const string THUMBNAIL_FILE_NAME = "thumbnail.jpg"; 
    
    [SerializeField] private Transform _imageHolder;
    [SerializeField] private Image _image;
    [SerializeField] private Button _btnCaptureImage;
    [SerializeField] protected Button _btnAccept;
    [SerializeField] protected Button _btnClose;

    private string _imagePath;
    private Action<string> _onAccept;
    private Texture2D _texture2D;
    
    protected override bool TryToGetArguments(params object[] args)
    {
        try
        {
            _onAccept = (Action<string>)args[0];
        }
        catch (Exception)
        {
            return false;
        }
        try
        {
            _imagePath = (string)args[1];
        }
        catch (Exception) { /*ignore*/ }
        return true;
    }
    
    public override void Init(Action<PopupBase> onClose, params object[] args)
    {
        base.Init(onClose, args);
        _btnCaptureImage.onClick.AddListener(OnCaptureImage);
        _btnAccept.onClick.AddListener(OnAccept);
        _btnClose.onClick.AddListener(Close);
        if (File.Exists(_imagePath))
        {
            SetPreview(Utilities.LoadTexture(_imagePath));
        }
    }

    private void OnAccept()
    {
        var path = Path.Combine(ActivityManager.Instance.Path, THUMBNAIL_FILE_NAME);
        File.WriteAllBytes(path, _texture2D.EncodeToJPG());
        _onAccept.Invoke(path);
        Close();
    }
    
    private void OnCaptureImage()
    {
        CaptureImage();
    }

    private void CaptureImage()
    {
        VuforiaBehaviour.Instance.enabled = false;
        NativeCameraController.TakePicture(OnPictureTaken);
    }
    
    private void OnPictureTaken(bool result, Texture2D texture2D)
    {
        VuforiaBehaviour.Instance.enabled = true;
        if (!result) return;
        SetPreview(texture2D);
    }

    private void SetPreview(Texture2D texture2D)
    {
        if (_texture2D) Destroy(_texture2D);

        _texture2D = texture2D;
        var sprite = Utilities.TextureToSprite(_texture2D);
        _image.sprite = sprite;

        var rtImageHolder = (RectTransform) _imageHolder.transform;
        var rtImage = (RectTransform) _image.transform;
        var height = rtImage.rect.width / _texture2D.width * _texture2D.height + (rtImage.sizeDelta.y * -1);
        rtImageHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}