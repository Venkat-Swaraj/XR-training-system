﻿using System;
using MirageXR;
using TMPro;
using UnityEngine;

public class PickAndPlaceEditorView : PopupEditorBase
{
    public override ContentType editorForType => ContentType.PICKANDPLACE;
    
    [SerializeField] private TMP_InputField _inputField;
    
    public override void Init(Action<PopupBase> onClose, params object[] args)
    {
        base.Init(onClose, args);
        UpdateView();
    }

    private void UpdateView()
    {
        _inputField.text = _content != null ? _content.text : string.Empty;
    }

    protected override void OnAccept()
    {
        if (string.IsNullOrEmpty(_inputField.text))
        {
            Toast.Instance.Show("Input field is empty.");
            return;
        }
        
        if (_content != null)
        {
            EventManager.DeactivateObject(_content);
        }
        else
        {
            _content = ActivityManager.Instance.AddAnnotation(_step, GetOffset());
            _content.predicate = editorForType.GetPredicate();
        }
        _content.text = _inputField.text;

        EventManager.ActivateObject(_content);
        EventManager.NotifyActionModified(_step);
        Close();
    }
}
