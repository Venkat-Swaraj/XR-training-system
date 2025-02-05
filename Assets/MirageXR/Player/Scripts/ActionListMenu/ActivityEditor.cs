﻿using i5.Toolkit.Core.VerboseLogging;
using MirageXR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActivityEditor : MonoBehaviour
{
    [SerializeField] private Toggle editCheckbox;
    [SerializeField] private InputField activityTitleField;
    [SerializeField] private Button addButton;
    [SerializeField] private Button saveButton;
    public Button SaveButton => saveButton;

    [SerializeField] private Button uploadButon;
    public Button UploadButton => uploadButon;

    [SerializeField] private Text loginNeedText;

    [SerializeField] private GameObject updateConfirmPanel;
    [SerializeField] private Text ConfirmPanelText;
    [SerializeField] private Button ConfirmPanelYesButton;
    [SerializeField] private Dropdown optionsDropDown;

    public static ActivityEditor Instance { get; private set; }

    private void Awake()
    {
        EventManager.OnWorkplaceLoaded += CheckEditState;
    }

    private void Start()
    {
        if (!Instance)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        EventManager.OnEditModeChanged += SetEditorState;

        if (activityTitleField.text == string.Empty)
            activityTitleField.text = "New Activity";
        activityTitleField.onValueChanged.AddListener(OnActivityTitleChanged);

        if (RootObject.Instance.activityManager != null)
        {
            SetEditorState(RootObject.Instance.activityManager.EditModeActive);
        }
    }

    private void OnDisable()
    {
        activityTitleField.onValueChanged.RemoveListener(OnActivityTitleChanged);
        EventManager.OnEditModeChanged -= SetEditorState;
    }

    private void OnDestroy()
    {
        EventManager.OnWorkplaceLoaded -= CheckEditState;
    }

    private void CheckEditState()
    {
        if (string.IsNullOrEmpty(RootObject.Instance.activityManager.Activity.id))
        {
            RootObject.Instance.activityManager.EditModeActive = true;
        }
        else
        {
            SetEditorState(RootObject.Instance.activityManager.EditModeActive);
        }
        editCheckbox.isOn = RootObject.Instance.activityManager.EditModeActive;
    }

    public void SetEditorState(bool editModeActive)
    {
        activityTitleField.interactable = editModeActive;
        activityTitleField.GetComponent<Image>().enabled = editModeActive;
        addButton.gameObject.SetActive(editModeActive);
        saveButton.gameObject.SetActive(editModeActive);
        uploadButon.gameObject.SetActive(editModeActive);
        loginNeedText.text = string.Empty;
    }

    public void ShowCloneWarningPanel()
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>()
        {
            new Dropdown.OptionData("Clone"),
            new Dropdown.OptionData("Cancel"),
        };

        optionsDropDown.AddOptions(options);
        ConfirmPanelText.text = "You are not the original author of this file! Please select an option:";
        updateConfirmPanel.SetActive(true);
    }

    public void ShowUploadWarningPanel()
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>()
        {
            new Dropdown.OptionData("Update"),
            new Dropdown.OptionData("Clone"),
            new Dropdown.OptionData("Cancel"),
        };

        optionsDropDown.AddOptions(options);
        ConfirmPanelText.text = "This file is exist! Please select an option:";
        updateConfirmPanel.SetActive(true);
    }


    public void DoUploadProcess()
    {
        var option = optionsDropDown.options[optionsDropDown.value].text;   //TODO: use optionsDropDown.value instead of string

        switch (option)
        {
            case "Update":
                OnUploadButtonClicked(1);
                break;
            case "Clone":
                RootObject.Instance.activityManager.CloneActivity();
                OnUploadButtonClicked(2);
                break;
            case "Cancel":
            default:
                updateConfirmPanel.SetActive(false);
                RootObject.Instance.moodleManager.GetProgressText = "Upload";
                optionsDropDown.options.Clear();
                break;
        }
    }


    public void OnEditToggleChanged(bool value)
    {
        AppLog.LogDebug("Toggle changed " + value);
        if (RootObject.Instance.activityManager != null)
        {
            RootObject.Instance.activityManager.EditModeActive = value;
            transform.GetComponentInChildren<Toggle>().isOn = value;
        }
    }

    public void ToggleEditMode()
    {
        bool newValue = !RootObject.Instance.activityManager.EditModeActive;
        OnEditToggleChanged(newValue);
        transform.GetComponentInChildren<Toggle>().isOn = newValue;
    }

    private void OnActivityTitleChanged(string text)
    {
        RootObject.Instance.activityManager.Activity.name = text;
    }

    public void OnSaveButtonClicked()
    {
        EventManager.NotifyOnActivitySaveButtonClicked();
        SaveActivity();
    }

    private void SaveActivity()
    {
        EventManager.ActivitySaved();
        RootObject.Instance.activityManager.SaveData();
    }

    public void OpenScreenShot()
    {
        var actionEditor = FindObjectOfType<ActionEditor>();
        var ie = (ImageEditor)actionEditor.CreateEditorView(ContentType.IMAGE);
        var adv = actionEditor.DetailView;
        ie.IsThumbnail = true;
        ie.Open(adv.DisplayedAction, null);
    }

    public async void OnUploadButtonClicked(int updateMode)
    {
        EventManager.NotifyOnActivityUploadButtonClicked();

        SaveActivity();

        // clear optionDropDown options if is not empty
        optionsDropDown.options.Clear();

        // hide confirm panel
        updateConfirmPanel.SetActive(false);

        ////Thumbnail is mandatory
        //string thumbnailPath = Path.Combine(RootModel.Instance.activityManager.Path, "thumbnail.jpg");
        //if (!File.Exists(thumbnailPath))
        //{
        //    loginNeedText.text = "Thumbnail not exist!";
        //    return;
        //}else
        //{
        //    loginNeedText.text = "";
        //}

        // login needed for uploading
        if (DBManager.LoggedIn)
        {
            loginNeedText.text = string.Empty;
            await RootObject.Instance.moodleManager.UploadFile(RootObject.Instance.activityManager.ActivityPath, RootObject.Instance.activityManager.Activity.name, updateMode);
        }
        else
        {
            loginNeedText.text = "Login needed!";
        }
    }
}
