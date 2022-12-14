using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MirageXR;
using UnityEngine;

public class TutorialModel
{
    public enum MessagePosition
    {
        Top,
        Middle,
        Bottom
    }

    public bool hasId => !string.IsNullOrEmpty(id);

    public bool hasMessage => !string.IsNullOrEmpty(message);

    public string id;
    public string message;
    public MessagePosition position = MessagePosition.Middle;
}

public class Tutorial : MonoBehaviour
{
    private const int MAX_TRY_COUNT = 40;
    private const int WAIT_IN_MILLISECONDS = 250;

    [SerializeField] private CanvasGroup _backgroundCanvasGroup;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private TutorialMessageView _tutorialMessageViewPrefab;
    [SerializeField] private RectTransform[] _searchRoots;

    private Queue<TutorialModel> _queue;
    private TutorialMessageView _lastMessageView;
    private TutorialItem _lastCopy;

    protected void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        Hide();
    }

    public void Show(Queue<TutorialModel> queue)
    {
        _backgroundCanvasGroup.gameObject.SetActive(true);
        _panel.gameObject.SetActive(true);
        _queue = queue;
        Next();
    }

    public void Hide()
    {
        if (_lastMessageView)
        {
            Destroy(_lastMessageView.gameObject);
            _lastMessageView = null;
        }

        if (_lastCopy)
        {
            Destroy(_lastCopy.gameObject);
            _lastCopy = null;
        }

        _backgroundCanvasGroup.gameObject.SetActive(false);
        _panel.gameObject.SetActive(false);
    }

    private void Next()
    {
        NextAsync().AsAsyncVoid();
    }

    private async Task NextAsync()
    {
        if (_queue.Count != 0)
        {
            await ShowItem(_queue.Dequeue());
        }
        else
        {
            Hide();
        }
    }

    private async Task ShowItem(TutorialModel model, int tryCount = 0)
    {
        if (model.hasId)
        {
            var item = await FindTutorialItem(model.id);
            if (item)
            {
                _lastCopy = CopyTutorialItem(item);
                SetUpTargetCopy(item, _lastCopy);
            }
            else
            {
                Debug.LogError($"Can't find TutorialModel with id = '{model.id}'");
                model.id = null;
            }
        }

        if (model.hasMessage)
        {
            _lastMessageView = ShowMessage(model);
        }
    }

    private TutorialMessageView ShowMessage(TutorialModel model)
    {
        var tutorialMessageView = Instantiate(_tutorialMessageViewPrefab, _panel);
        tutorialMessageView.Initialization(model, OnMessageViewButtonClicked);
        return tutorialMessageView;
    }

    private void OnMessageViewButtonClicked(TutorialModel model)
    {
        Hide();
    }

    private async Task<TutorialItem> FindTutorialItem(string id)
    {
        for (int i = 0; i < MAX_TRY_COUNT; i++)
        {
            foreach (var searchRoot in _searchRoots)
            {
                var items = searchRoot.GetComponentsInChildren<TutorialItem>();
                if (items == null)
                {
                    continue;
                }

                var item = items.FirstOrDefault(t => t.id == id);
                if (item)
                {
                    return item;
                }
            }

            await Task.Delay(WAIT_IN_MILLISECONDS);
        }

        return null;
    }

    private TutorialItem CopyTutorialItem(TutorialItem item)
    {
        if (item.isPartOfScrollView)
        {
            item.ScrollToTop();
        }

        if (item.inputField)
        {
            GameObject hbPrefab = Resources.Load("prefabs/UI/Mobile/Tutorial/HighlightButton", typeof(GameObject)) as GameObject;
            GameObject target = item.gameObject;
            var highlightButton = Object.Instantiate(hbPrefab, target.transform.position, target.transform.rotation);
            highlightButton.transform.SetParent(RootView_v2.Instance.transform);
            //highlightButton.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            highlightButton.transform.localScale = target.transform.localScale;

            var rectTransform = (RectTransform)target.transform;

            if (rectTransform)
            {
                var height = rectTransform.rect.height;
                var width = rectTransform.rect.width;

                var copyRectTransform = (RectTransform)highlightButton.transform;
                copyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                copyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                copyRectTransform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            }

            var copyItem = highlightButton.AddComponent<TutorialItem>();
            return copyItem;
        }
        else
        {
            var copyItem = Instantiate(item, _panel, true);
            return copyItem;
        }
    }

    private void SetUpTargetCopy(TutorialItem item, TutorialItem copy)
    {
        if (item.button)
        {
            copy.button.onClick.RemoveAllListeners();
            copy.button.onClick.AddListener(() => OnTargetCopyClicked(item, copy));
        }

        if (item.toggle)
        {
            copy.toggle.onValueChanged.RemoveAllListeners();
            copy.toggle.onValueChanged.AddListener(value => OnTargetCopyClicked(item, copy));
        }

        if (item.inputField)
        {
            HighlightingButton higBtn = copy.gameObject.GetComponent<HighlightingButton>();
            higBtn.SetTarget(item.inputField);
            higBtn.Btn.onClick.AddListener(() => OnTargetCopyClicked(item, copy));
        }

        copy.StartTracking(item.transform);
    }

    private void OnTargetCopyClicked(TutorialItem item, TutorialItem copy)
    {
        if (item.button)
        {
            item.button.onClick.Invoke();
        }
        if (item.toggle)
        {
            item.toggle.onValueChanged.Invoke(true);
        }
        copy.StopTracking();

        if (_lastCopy == copy)
        {
            _lastCopy = null;
        }

        Destroy(copy.gameObject);
        if (_lastMessageView)
        {
            Destroy(_lastMessageView.gameObject);
            _lastMessageView = null;
        }

        Next();
    }
}
