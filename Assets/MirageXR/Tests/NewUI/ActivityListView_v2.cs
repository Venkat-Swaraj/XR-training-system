using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using MirageXR;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ActivityListView_v2 : BaseView
{
    [SerializeField] private Button _btnFilter;
    [SerializeField] private Transform _listTransform;
    [SerializeField] private ActivityListItem_v2 _smallItemPrefab;
    [SerializeField] private ActivityListItem_v2 _bigItemPrefab;
    [SerializeField] private SortingView _sortingPrefab;

    [Space]
    [SerializeField] private Button _btnArrow;
    [SerializeField] private Button _btnBackToActivity;
    [SerializeField] private Button _btnRestartActivity;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private GameObject _arrowDown;
    [SerializeField] private GameObject _arrowUp;

    private List<SessionContainer> _content;
    private readonly List<ActivityListItem_v2> _items = new List<ActivityListItem_v2>();
    private bool _interactable = true;

    public List<SessionContainer> content => _content;

    public override void Initialization(BaseView parentView)
    {
        _btnFilter.onClick.AddListener(OnByDateClick);
        _btnBackToActivity.onClick.AddListener(OnBacktoActivityButton);
        _btnRestartActivity.onClick.AddListener(OnRestartActivityButton);

        _btnArrow.onClick.AddListener(ArrowBtnPressed);
        _arrowDown.SetActive(true);
        _arrowUp.SetActive(false);

        EventManager.OnActivitySaved += FetchAndUpdateView;

        FetchAndUpdateView();
    }

    private static async Task<List<SessionContainer>> FetchContent()
    {
        var dictionary = new Dictionary<string, SessionContainer>();

        var localList = await LocalFiles.GetDownloadedActivities();
        localList.ForEach(t =>
        {
            if (dictionary.ContainsKey(t.id))
            {
                dictionary[t.id].Activity = t;
            }
            else
            {
                dictionary.Add(t.id, new SessionContainer { Activity = t });
            }
        });

        var remoteList = await RootObject.Instance.moodleManager.GetArlemList();
        remoteList?.ForEach(t =>
        {
            if (dictionary.ContainsKey(t.sessionid))
            {
                dictionary[t.sessionid].Session = t;
            }
            else
            {
                dictionary.Add(t.sessionid, new SessionContainer { Session = t });
            }
        });

        return dictionary.Values.ToList();
    }

    public async void FetchAndUpdateView()
    {
        _content = await FetchContent();
        UpdateView();
    }

    public void UpdateView()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            return;
        }
#endif
        _items.ForEach(item => Destroy(item.gameObject));
        _items.Clear();

        var prefab = !DBManager.showBigCards ? _smallItemPrefab : _bigItemPrefab;
        _content.ForEach(content =>
        {
            var item = Instantiate(prefab, _listTransform);
            item.Init(content);
            _items.Add(item);
        });
    }

    private void OnByDateClick()
    {
        PopupsViewer.Instance.Show(_sortingPrefab, this);
    }

    private void OnBacktoActivityButton()
    {
        RootView_v2.Instance.OnActivityLoaded();
    }

    private void OnRestartActivityButton()
    {
        RestartActivityAsync().AsAsyncVoid();
    }

    private async Task RestartActivityAsync()
    {
        LoadView.Instance.Show();
        RootView_v2.Instance.OnActivityLoaded();
        await RootObject.Instance.activityManager.ActivateFirstAction();
        LoadView.Instance.Hide();
    }

    private async void OnNewActivityChanged()
    {
        LoadView.Instance.Show();
        await RootObject.Instance.editorSceneService.LoadEditorAsync();
        await RootObject.Instance.activityManager.CreateNewActivity();
        LoadView.Instance.Hide();
    }

    private void ArrowBtnPressed()
    {
        if (_arrowDown.activeSelf)
        {
            _panel.DOAnchorPos(new Vector2(0, -1100), 0.25f);
            _arrowDown.SetActive(false);
            _arrowUp.SetActive(true);
        }
        else
        {
            _panel.DOAnchorPos(new Vector2(0, -60), 0.25f);
            _arrowDown.SetActive(true);
            _arrowUp.SetActive(false);
        }
    }
}