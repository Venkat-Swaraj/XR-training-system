﻿using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using TiltBrush;
using UnityEngine;
using UnityEngine.UI;

namespace MirageXR
{
    public class UiManager : MonoBehaviour
    {
        private static ActivityManager activityManager => RootObject.Instance.activityManager;
        private static CalibrationManager calibrationManager => RootObject.Instance.calibrationManager;

        [SerializeField] private bool IsMenuVisible;
        private bool _inAction;
        public bool IsFindActive;

        // Task list location is attached to Hololens main camera.

        [Tooltip("Drag and drop DebugConsole game object here.")]
        public GameObject DebugConsole;

        [Tooltip("Drag and drop Menu game object here.")]
        public GameObject ActionList;

        public string WelcomeMessage = "";

        public static UiManager Instance { get; private set; }

        public bool IsCalibrated;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            EventManager.OnPlayerReset += PlayerReset;
            EventManager.OnMoveActivityList += MoveActivityList;
            EventManager.OnMoveActionList += MoveActionList;
            EventManager.OnToggleGuides += ToggleFind;
            EventManager.OnToggleMenu += ToggleMenu;
            EventManager.OnToggleLock += ToggleLockTouch;
            EventManager.OnWorkplaceLoaded += ShowActivityStart;
            EventManager.OnActivityStarted += ActivityStarted;
            EventManager.OnWorkplaceCalibrated += WorkplaceCalibrated;
            EventManager.OnDeleteActionByVoice += DeleteActionVoice;
            EventManager.OnAddActionByVoice += AddActionVoice;
            EventManager.OnLoginByVoice += LoginVoice;
            EventManager.OnRegisterActionByVoice += RegisterVoice;
            EventManager.OnSaveActivityActionByVoice += SaveActivityVoice;
            EventManager.OnUploadActivityActionByVoice += UploadActivityVoice;
            EventManager.OnOpenAnnotationByVoice += OpenAnnotationVoice;

            EventManager.OnActionlistToggleByVoice += ActionListToggleVoice;
            EventManager.OnNextByVoice += NextVoice;
            EventManager.OnBackByVoice += BackVoice;
            EventManager.OnLockMenuByVoice += LockMenuVoice;
            EventManager.OnReleaseMenuByVoice += ReleaseMenuVoice;
            EventManager.OnStartByVoice += StartActivityVoice;
        }

        private void OnDisable()
        {
            EventManager.OnPlayerReset -= PlayerReset;
            EventManager.OnMoveActivityList -= MoveActivityList;
            EventManager.OnMoveActionList -= MoveActionList;
            EventManager.OnToggleGuides -= ToggleFind;
            EventManager.OnToggleMenu -= ToggleMenu;
            EventManager.OnToggleLock -= ToggleLockTouch;
            EventManager.OnWorkplaceLoaded -= ShowActivityStart;
            EventManager.OnActivityStarted -= ActivityStarted;
            EventManager.OnWorkplaceCalibrated -= WorkplaceCalibrated;
            EventManager.OnDeleteActionByVoice -= DeleteActionVoice;
            EventManager.OnAddActionByVoice -= AddActionVoice;
            EventManager.OnLoginByVoice -= LoginVoice;
            EventManager.OnRegisterActionByVoice -= RegisterVoice;
            EventManager.OnSaveActivityActionByVoice -= SaveActivityVoice;
            EventManager.OnUploadActivityActionByVoice -= UploadActivityVoice;
            EventManager.OnOpenAnnotationByVoice -= OpenAnnotationVoice;

            EventManager.OnActionlistToggleByVoice -= ActionListToggleVoice;
            EventManager.OnNextByVoice -= NextVoice;
            EventManager.OnBackByVoice -= BackVoice;
            EventManager.OnLockMenuByVoice -= LockMenuVoice;
            EventManager.OnReleaseMenuByVoice -= ReleaseMenuVoice;
            EventManager.OnStartByVoice -= StartActivityVoice;
        }

        private void PlayerReset()
        {
            ClearDebug();
            HideDebug();
            HideMenu();
            WelcomeMessage = string.Empty;
            RootObject.Instance.calibrationManager.DisableCalibration();
        }

        private void MoveActivityList()
        {
            var activityList = FindObjectOfType<ActivitySelectionMenu>().gameObject;
            if (ActionList == null || !ActionList.activeInHierarchy) return;

            SetPosition(activityList);
            LookAtCamera(activityList);
        }



        private void MoveActionList()
        {
            if (ActionList == null || !ActionList.activeInHierarchy) return;

            SetPosition(ActionList);
            LookAtCamera(ActionList);
        }


        public void SetPosition(GameObject x)
        {
            var userViewport = GameObject.FindGameObjectWithTag("UserViewport").transform;
            if (userViewport == null) return;

            x.transform.position = userViewport.position;

            LookAtCamera(x);
        }


        public void LookAtCamera(GameObject obj)
        {
            obj.transform.LookAt(2 * obj.transform.position - Camera.main.transform.position);
        }


        private void ShowLoading(string activity)
        {
            HideSelectionPanel();
            HideMenu();
        }

        private void Start()
        {
            // Set default visibility to hidden.
            DebugConsole.SetActive(false);
            //Menu.GetComponent<CanvasGroup> ().alpha = 0;
            //Menu.GetComponent<GraphicRaycaster> ().enabled = false;
            ActionList.gameObject.SetActive(false);
        }

        /// <summary>
        /// Clear debug console. Called from Hololens keyword manager.
        /// </summary>
        public void ClearDebug()
        {
            DebugConsole.transform.parent.SendMessage("ClearDebug", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Show debug console. Called from Hololens keyword manager.
        /// </summary>
        public void ShowDebug()
        {
            DebugConsole.SetActive(true);
        }

        /// <summary>
        /// Hide debug console. Called from Hololens keyword manager.
        /// </summary>
        public void HideDebug()
        {
            DebugConsole.SetActive(false);
        }

        /// <summary>
        /// Show selection panel.
        /// </summary>
        public void ShowSelectionPanel()
        {
            // TODO: is this still needed? Selection is now done in the main menu
            //ActivitySelectionPanel.SetActive(true);
            HideMenu();
        }


        /// <summary>
        /// Touch activated show selection panel.
        /// </summary>
        public void ShowSelectionPanelTouch()
        {
            ShowSelectionPanel();
            EventManager.Click();
        }

        /// <summary>
        /// Hide selection panel.
        /// </summary>
        public void HideSelectionPanel()
        {
            var ActivitySelectionPanel = FindObjectOfType<ActivitySelectionMenu>();
            if (ActivitySelectionPanel != null)
                ActivitySelectionPanel.gameObject.SetActive(false);
        }


        /// <summary>
        /// Touch activated hide selection panel.
        /// </summary>
        public void HideSelectionPanelTouch()
        {
            HideSelectionPanel();
            EventManager.Click();
        }

        /// <summary>
        /// Open the annotaiton list
        /// </summary>
        private void OpenAnnotationVoice()
        {
            ActionEditor.Instance.OnAddButtonToggle();
        }

        private void CreateCalibrationGuide()
        {
            // do not create if it is exist already
            if (!PlatformManager.Instance.WorldSpaceUi || GameObject.Find("CalibrationGuide(Clone)") || GameObject.Find("CalibrationGuide"))
                return;

            if (IsCalibrated) // create the guild if activity is not calibrated.
            {
                var prefab = Resources.Load<GameObject>("Prefabs/Calibration/CalibrationGuide");
                var position = PlatformManager.Instance.GetTaskStationPosition() - Vector3.forward * 0.1f;
                var guildeObject = Instantiate(prefab, position, Camera.main.transform.rotation);
                guildeObject.name = "CalibrationGuide";
                var okButton = guildeObject.transform.FindDeepChild("OKButton");
                okButton.GetComponent<Button>().onClick.AddListener(() => { Destroy(guildeObject, 0.1f); });
                okButton.GetComponent<PressableButton>().ButtonPressed.AddListener(() => { Destroy(guildeObject, 0.1f); });

                guildeObject.transform.FindDeepChild("OKButton").GetComponent<Button>().onClick.AddListener(() => { Destroy(guildeObject, 0.1f); });
            }
        }

        private async void ShowActivityStart()
        {
            HideSelectionPanel();
            EventManager.HideGuides();
            IsFindActive = false;
            ShowMenu();
            ShowTasklist(false);

            _inAction = false;

            // Stop any message so that the activity start message is surely played.
            Maggie.Stop();

            // Add a small delay just be sure that the message is stopped.
            await Task.Delay(250);

            if (PlatformManager.Instance.WorldSpaceUi)
            {
                RootObject.Instance.calibrationManager.DisableCalibration();
            }

            if (IsCalibrated)
            {
                Maggie.Speak(!string.IsNullOrEmpty(WelcomeMessage) ? WelcomeMessage : "Activity loaded and ready to be started.");
            }
            else
            {
                // Nag.
                // Maggie.Speak("Workplace anchors have not been calibrated. Please run the calibration before starting the activity.");
                CreateCalibrationGuide();

                if (PlatformManager.Instance.WorldSpaceUi)
                {
                    RootObject.Instance.calibrationManager.EnableCalibration();
                }

                // Hile loading text
                Loading.Instance.LoadingVisibility(false);
            }

            // EventManager.ActivityLoadedStamp(SystemInfo.deviceUniqueIdentifier, activityManager.Activity.id, System.DateTime.UtcNow.ToUniversalTime().ToString());
        }

        private void ActivityStarted()
        {
            //WelcomeMessage = activityManager.Activity;

            switch (PlayerPrefs.GetString("uistyle"))
            {
                case "tasklist":
                    ShowTasklist();
                    break;
                default:
                    ShowActivityCards();
                    break;
            }

            _inAction = true;
        }

        private void WorkplaceCalibrated()
        {
            IsCalibrated = true;
        }

        /// <summary>
        /// Show tasklist. Called from Hololens keyword manager.
        /// </summary>
        public void ShowMenu()
        {
            ActionList.gameObject.SetActive(true);

            IsMenuVisible = true;
        }

        /// <summary>
        /// Show tasklist with voice command. Includes tts feedback.
        /// </summary>
        private void ActionListToggleVoice(bool activated)
        {
            if (!_inAction) return;

            if (activated)
                ShowMenu();
            else
                HideMenu();
        }

        /// <summary>
        /// Delete the active action
        /// </summary>
        private void DeleteActionVoice()
        {
            if (activityManager.ActiveAction == null) return;

            DialogWindow.Instance.Show("Warning!", "Are you sure you want to delete this step?",
                new DialogButtonContent("Yes", () => activityManager.DeleteAction(activityManager.ActiveActionId)),
                new DialogButtonContent("No"));
        }

        /// <summary>
        /// Add a new action step
        /// </summary>
        private void AddActionVoice()
        {
            if (activityManager.ActiveAction == null) return;

            ActionListMenu.Instance.AddAction();
        }

        /// <summary>
        /// Open login panel
        /// </summary>
        private void LoginVoice()
        {
            var loginButton = GameObject.Find("LoginButton");
            if (loginButton != null)
            {
                loginButton.GetComponent<Button>().onClick.Invoke();
            }
        }

        /// <summary>
        /// Open Moodle register page on browser
        /// </summary>
        private void RegisterVoice()
        {
            Application.OpenURL(DBManager.registerPage);
        }

        /// <summary>
        /// Save the current activity
        /// </summary>
        private void SaveActivityVoice()
        {
            if (activityManager.ActiveAction == null) return;

            activityManager.SaveData();
            if (PlatformManager.Instance.WorldSpaceUi)
                Maggie.Speak("Save completed");
        }

        /// <summary>
        /// Upload the current activity
        /// </summary>
        private void UploadActivityVoice()
        {
            if (activityManager.ActiveAction == null) return;

            ActivityEditor.Instance.OnUploadButtonClicked(0);
        }

        /// <summary>
        /// Hide tasklist.
        /// </summary>
        public void HideMenu()
        {
            ActionList.gameObject.SetActive(false);
            IsMenuVisible = false;
        }

        /// <summary>
        /// Touch activated hide tasklist.
        /// </summary>
        public void HideMenuTouch()
        {
            HideMenu();
            EventManager.Click();
        }

        /// <summary>
        /// Changes task list visibility. Used with ui button.
        /// </summary>
        public void ToggleMenu()
        {
            if (!IsMenuVisible)
                ShowMenu();
            else
                HideMenu();
            EventManager.Click();
        }

        public void ShowSensors()
        {
            if (!IsMenuVisible)
            {
                ShowMenu();
            }
        }

        public void ShowSensorsByVoice()
        {
            //if (_inAction)
            //{
            //    if (!HumEnvButton.activeSelf)
            //    {
            //        Maggie.Speak("Nothing to show on the sensor panel.");
            //    }

            //    else
            //    {
            //        Maggie.Ok();
            //        ShowSensors();
            //    }
            //}
            //else
            //    Maggie.Speak("Please start the activity first.");
        }

        public void ToggleSensors()
        {
            //if (SensorPanel.localScale == Vector3.one)
            //    ReturnToActivity();
            //else
            //    ShowSensors();
        }

        public void ToggleSensorsTouch()
        {
            if (_inAction)
            {
                EventManager.Click();
                ToggleSensors();
            }
        }

        public void ShowActivityCards()
        {
            if (!IsMenuVisible)
                ShowMenu();

            PlayerPrefs.SetString("uistyle", "cards");
            PlayerPrefs.Save();
        }

        public void ShowActivityCardsTouch()
        {
            EventManager.Click();
            ShowActivityCards();
        }

        public void ShowTasklist(bool storeState = true)
        {
            if (!IsMenuVisible)
                ShowMenu();

            if (storeState)
            {
                PlayerPrefs.SetString("uistyle", "tasklist");
                PlayerPrefs.Save();
            }
        }

        public void ToggleLockTouch()
        {
            //EventManager.Click();
        }

        public void LockMenuVoice()
        {
            var SpriteToggle = ActionList.GetComponent<WindowMovement>().SpriteToggle;
            SpriteToggle.IsSelected = true;
            SpriteToggle.ToggleValue();
        }

        public void ReleaseMenuVoice()
        {
            var SpriteToggle = ActionList.GetComponent<WindowMovement>().SpriteToggle;
            SpriteToggle.IsSelected = false;
            SpriteToggle.ToggleValue();
        }

        public void ToggleFind()
        {
            EventManager.Click();
            //if (IsFindActive)
            //{
            //    MenuLine.SetActive(false);
            //    EventManager.HideGuides();
            //}
            //else
            //{
            //    MenuLine.SetActive(true);
            //    EventManager.ShowGuides();
            //}

            IsFindActive = !IsFindActive;
        }

        public void ShowGuidesVoice()
        {
            if (activityManager.IsReady)
            {
                IsFindActive = true;
                EventManager.ShowGuides();
                Maggie.Ok();
            }
            else
                Maggie.Speak("Please start the activity first.");
        }

        public void HideGuidesVoice()
        {
            if (activityManager.IsReady)
            {
                IsFindActive = false;
                EventManager.HideGuides();
                Maggie.Ok();
            }
            else
                Maggie.Speak("Please start the activity first.");
        }

        public void RestartPlayerTouch()
        {
            EventManager.Click();
            RootObject.Instance.activityManager.PlayerReset().AsAsyncVoid();
        }

        public void RestartPlayerVoice()
        {
            Maggie.Ok();
            RootObject.Instance.activityManager.PlayerReset().AsAsyncVoid();
        }

        public void ClearAllVoice()
        {
            Maggie.Ok();
            EventManager.ClearAll();
        }

        public void StartActivityVoice()
        {
            var activitySelectionMenu = FindObjectOfType<ActivitySelectionMenu>();
            if (activitySelectionMenu != null && !_inAction) // this should work only if no other activity is loaded.
                activitySelectionMenu.OpenRecorder();
        }

        public void NextVoice()
        {
            if (_inAction)
            {
                ActionListMenu.Instance.NextAction();
            }
            else
            {
                if (activityManager.IsReady)
                    StartActivityVoice();
                else
                    Maggie.Speak("Please start the activity first.");
            }
        }

        public void BackVoice()
        {
            if (_inAction)
            {
                ActionListMenu.Instance.PreviousAction();
            }
        }

        private void ReturnToActivity()
        {
            if (_inAction)
            {
                switch (PlayerPrefs.GetString("uistyle"))
                {
                    case "tasklist":
                        ShowTasklist();
                        break;
                    default:
                        ShowActivityCards();
                        break;
                }
            }
            else
                Maggie.Speak("Please start the activity first.");
        }
    }
}
