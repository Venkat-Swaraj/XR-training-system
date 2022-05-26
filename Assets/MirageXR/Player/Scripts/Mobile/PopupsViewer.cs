﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupsViewer : MonoBehaviour
{
    public static PopupsViewer Instance { get; private set; }

    [SerializeField] private Button _btnBackground;

    private readonly Stack<PopupBase> _stack = new Stack<PopupBase>();
    
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"{Instance.GetType().FullName} must only be a single copy!");
            return;
        }
        
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void Start()
    {
        _btnBackground.onClick.AddListener(OnOutTap);
    }

    public void Show(PopupBase popupPrefab, params object[] args)
    {
        var popup = Instantiate(popupPrefab, transform);
        _stack.Push(popup);
        popup.gameObject.SetActive(false);
        popup.Init(OnClose, args);
        UpdateView();
    }

    private void UpdateView()
    {
        if (_stack.Count == 0)
        {
            _btnBackground.gameObject.SetActive(false);
            return;
        }

        foreach (var popupBase in _stack)
        {
            popupBase.gameObject.SetActive(false);
        }
        
        var popup = _stack.Peek();
        if (popup.isMarkedToDelete)
        {
            popup.Close();
        }
        else
        {
            popup.gameObject.SetActive(true);
            popup.transform.SetAsLastSibling();
            _btnBackground.gameObject.SetActive(true);
        }
    }

    private void OnOutTap()
    {
        if (_stack.Count == 0) return;
        var popup = _stack.Peek();
        if (popup.canBeClosedByOutTap)
        {
            popup.Close();
        }
    }
    
    private void OnClose(PopupBase popup)
    {
        if (_stack.Peek() == popup)
        {
            Destroy(_stack.Pop().gameObject);
            UpdateView();
        }
    }
}
