﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using i5.Toolkit.Core.OpenIDConnectClient;
using i5.Toolkit.Core.ServiceCore;
using MirageXR;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelEditorView : PopupEditorBase
{
    private const int RESULTS_PER_PAGE = 20;
    
    public override ContentType editorForType => ContentType.MODEL;
    
    [SerializeField] private Transform _contentContainer;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private ModelListItem _modelListItemPrefab;
    [SerializeField] private DirectLoginPopup _directLoginPopupPrefab;
    [SerializeField] private GameObject _loadMorePrefab;
    [SerializeField] private Button _btnSearch;
    [SerializeField] private TMP_InputField _inputSearch;
    [SerializeField] private Toggle _toggleLocal;
    [SerializeField] private ClientDataObject _clientDataObject;
    [SerializeField] private ClientDataObject _clientDirectLoginDataObject;

    private string _token;
    private string _renewToken;
    private string _searchString;
    private string _searchOption = "models";
    private GameObject _loadMoreObject;
    private ModelPreviewItem _previewItem;
    private int _pageIndex;

    private readonly List<ModelListItem> _items = new List<ModelListItem>();
    
    public override void Init(Action<PopupBase> onClose, params object[] args)
    {
        base.Init(onClose, args);

        if (!CheckAndLoadCredentials()) return; 

        _btnSearch.onClick.AddListener(OnSearchClicked);
        _pageIndex = 0;
        _toggleLocal.isOn = false;
        _toggleLocal.onValueChanged.AddListener(OnToggleLocalValueChanged);
        if (LocalFiles.TryGetPassword("sketchfab", out _renewToken, out _token))
        {
            if (!string.IsNullOrEmpty(_renewToken) && DBManager.isNeedToRenewSketchfabToken)
            {
                RenewToken();
            }
        }
        else
        {
            DialogWindow.Instance.Show("Login to Sketchfab",
                new DialogButtonContent("Via the browser", LoginToSketchfab),
                new DialogButtonContent("With password", ShowDirectLoginPopup),
                new DialogButtonContent("Cancel", Close));
        }
        Clear();
    }

    private async void RenewToken()
    {
        var clientId = _clientDirectLoginDataObject.clientData.ClientId;
        var clientSecret = _clientDirectLoginDataObject.clientData.ClientSecret;
        
        var (result, json) = await MirageXR.Sketchfab.RenewTokenAsync(_renewToken, clientId, clientSecret);
        var response = JsonConvert.DeserializeObject<MirageXR.Sketchfab.SketchfabResponse>(json);
        if (result)
        {
            if (response.error == null && response.access_token != null)
            {
                _token = response.access_token;
                _renewToken = response.refresh_token;
                LocalFiles.SaveLoginDetails("sketchfab", _renewToken, _token);
                DBManager.sketchfabLastTokenRenewDate = DateTime.Today;
            }
        }
    }
    
    private bool CheckAndLoadCredentials()
    {
        if (_clientDataObject == null)
        {
            var json = Resources.Load<TextAsset>("Credentials/SketchfabClient");
            var appCredentials = JsonUtility.FromJson<SketchfabManager.AppCredentials>(json.ToString());
            if (appCredentials != null)
            {
                _clientDataObject = ScriptableObject.CreateInstance<ClientDataObject>();
                _clientDataObject.clientData = new ClientData(appCredentials.client_id, appCredentials.client_secret);
            }
        }

        if (_clientDataObject == null)
        {
            DialogWindow.Instance.Show("Sketchfab client asset not found or reference is missing.\n\nContact the MirageXR development team for more information or access to the file.",
                new DialogButtonContent("Close", Close));
            return false;
        }

        return true;
    }
    
    private void OnToggleLocalValueChanged(bool value)
    {
        _inputSearch.text = string.Empty;
        if (value)
        {
            ShowLocalModels();
        }
        else
        {
            ShowRemoteModels();
        }
    }  
    
    private void ShowLocalModels()
    {
        Clear();
        var previewItems = MirageXR.Sketchfab.GetLocalModels();
        if (previewItems.Count == 0)
        {
            Toast.Instance.Show("Nothing found");
            return;
        }
        AddItems(previewItems, true);
    }

    private void ShowRemoteModels()
    {
        Clear();
    }
    
    private void AddLoadMoreButton()
    {
        _loadMoreObject = Instantiate(_loadMorePrefab, _contentContainer);
        _loadMoreObject.GetComponentInChildren<Button>().onClick.AddListener(OnLoadMoreButtonClicked);
    }
    
    private void OnSearchClicked()
    {
        if (_toggleLocal.isOn)
        {
            SearchLocal();
        }
        else
        {
            SearchRemote();   
        }
    }

    private void SearchLocal()
    {
        foreach (var item in _items)
        {
            var active = string.IsNullOrEmpty(_inputSearch.text) || item.previewItem.name.Contains(_inputSearch.text);
            item.gameObject.SetActive(active);
        }
    }
    
    private async void SearchRemote()
    {
        _searchString = _inputSearch.text;
        
        if (string.IsNullOrEmpty(_searchString))
        {
            Toast.Instance.Show("Search field cannot be empty");
            return;
        }

        _pageIndex = 0;
        var (result, content) = await MirageXR.Sketchfab.SearchModelsAsync(_token, _searchOption, _searchString, _pageIndex, RESULTS_PER_PAGE);
        if (!result)
        {
            Toast.Instance.Show("Error when trying to get a list of models.");
            return;
        }

        Clear();
        var previewItems = MirageXR.Sketchfab.ReadWebResults(content, _searchOption);
        if (previewItems.Count == 0)
        {
            Toast.Instance.Show("Nothing found");
            return;
        }
        
        AddItems(previewItems);
        if (previewItems.Count >= RESULTS_PER_PAGE)
        {
            AddLoadMoreButton();
        }
    }
    
    private async void OnLoadMoreButtonClicked()
    {
        _pageIndex++;
        var cursorPosition = _pageIndex * RESULTS_PER_PAGE + 1;
        
        var (result, content) = await MirageXR.Sketchfab.SearchModelsAsync(_token, _searchOption, _searchString, cursorPosition, RESULTS_PER_PAGE);
        if (!result)
        {
            Toast.Instance.Show("Error when trying to get a list of models.");
            return;
        }

        var previewItems = MirageXR.Sketchfab.ReadWebResults(content, _searchOption);
        if (previewItems.Count == 0)
        {
            _loadMoreObject.SetActive(false);
            Toast.Instance.Show("Nothing to download");
            return;
        }

        AddItems(previewItems);
        
        if (previewItems.Count < RESULTS_PER_PAGE)
        {
            _loadMoreObject.SetActive(false);
        }
        
        _loadMoreObject.transform.SetAsLastSibling();
    }

    private void Clear()
    {
        _pageIndex = 0;
        _scroll.normalizedPosition = Vector2.up;
        for (int i = _contentContainer.childCount - 1; i >= 0; i--)
        {
            var child = _contentContainer.GetChild(i);
            Destroy(child.gameObject);
        }
        _items.Clear();
    }
    
    private void AddItems(IEnumerable<ModelPreviewItem> previewItems, bool isDownloaded = false)
    {
        foreach (var item in previewItems)
        {
            var model = Instantiate(_modelListItemPrefab, _contentContainer);
            model.Init(item, isDownloaded, DownloadItem, Accept);
            _items.Add(model);
        }

        UpdateModelsItemView();
    }

    private async void UpdateModelsItemView()
    {
        const int cooldown = 25;
        foreach (var item in _items)
        {
            await item.LoadImage();
            await Task.Delay(cooldown);
        }
    }

    private void LoginToSketchfab()
    {
        var service = ServiceManager.GetService<OpenIDConnectService>();
        service.OidcProvider.ClientData = _clientDataObject.clientData;
        service.LoginCompleted += OnLoginCompleted;
        service.ServerListener.ListeningUri = SketchfabManager.URL;
        service.OpenLoginPage();
    }

    private void ShowDirectLoginPopup()
    {
        var clientId = _clientDirectLoginDataObject.clientData.ClientId;
        var clientSecret = _clientDirectLoginDataObject.clientData.ClientSecret;
        Action<bool, string> onPopupClose = OnDirectLoginCompleted; 
        PopupsViewer.Instance.Show(_directLoginPopupPrefab, onPopupClose, clientId, clientSecret);
    }

    private void OnDirectLoginCompleted(bool result, string json)
    {
        if (result)
        {
            var response = JsonConvert.DeserializeObject<MirageXR.Sketchfab.SketchfabResponse>(json);
            _token = response.access_token;
            _renewToken = response.refresh_token;
            LocalFiles.SaveLoginDetails("sketchfab", _renewToken, _token);
            Toast.Instance.Show("Authorization was successful");
            DBManager.sketchfabLastTokenRenewDate = DateTime.Now;
            return;
        }
        Close();
    }
    
    private void OnLoginCompleted(object sender, EventArgs e)
    {
        var service = ServiceManager.GetService<OpenIDConnectService>();
        service.LoginCompleted -= OnLoginCompleted;
        _token = service.AccessToken;
        LocalFiles.SaveLoginDetails("sketchfab", string.Empty, _token);
        Toast.Instance.Show("Authorization was successful");
        DBManager.sketchfabLastTokenRenewDate = DateTime.Now;
    }

    private async void Accept(ModelListItem item)
    {
        _previewItem = item.previewItem;
        item.interactable = false;
        await MirageXR.Sketchfab.LoadModelAsync(_previewItem);
        item.interactable = true;
        OnAccept();
    }
    
    private async void DownloadItem(ModelListItem item)
    {
        var (result, downloadInfo) = await MirageXR.Sketchfab.GetDownloadInfoAsync(_token, item.previewItem);
        if (result)
        {
            item.OnBeginDownload();
            if (await MirageXR.Sketchfab.DownloadModelAndExtractAsync(downloadInfo.gltf.url, item.previewItem, item.OnDownload))
            {
                Toast.Instance.Show("Download successfully.");
                item.isDownloaded = true;
                item.OnEndDownload();
                return;
            }
            item.OnEndDownload();
        }
        
        Toast.Instance.Show("Download error. Please, try again.");
    }

    protected override void OnAccept()
    {
        var predicate = $"3d:{_previewItem.name}";
        if (_content != null)
        {
            EventManager.DeactivateObject(_content);
        }
        else
        {
            _content = ActivityManager.Instance.AddAnnotation(_step, GetOffset());
            _content.option = _previewItem.name;
            _content.predicate = predicate;
            _content.url = predicate;
        }

        _content.predicate = predicate;
        EventManager.ActivateObject(_content);
        EventManager.NotifyActionModified(_step);

        Close();
    }
}