﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
//using Microsoft.MixedReality.Toolkit.UI;

namespace MirageXR
{
    public class CharacterController : MirageXRPrefab
    {


        [SerializeField] private Transform rightHandBone;

        [Tooltip("You need to play with this to set the correct start rotation of the image container")]
        [SerializeField] private Vector3 handRotationOffset;

        private Animator _anim;
        private NavMeshAgent _agent;
        private ToggleObject _myObj;
        private Color _pathNodesColor;
        private GameObject _myImageFrame;
        private List<StepSettings> stepsSettings;
        private GameObject imageContainer;
        private float animationLength;
        private float triggerDuration;

        //Animation variables
        private bool animationClipPlaying;
        private bool animationPlayedOnce;
        public bool characterParsed {
            get; private set;
        }

        //Watson assistant
        private GameObject watsonService;
        public bool AIActivated { get; private set; }

        //character settings
        private CharacterSettings _characterSetting;
        private MovementManager movementManger;
        private Dropdown animationMenu;

        private string characterDataFolder;

        #region Image Display publics

        private bool isImageAssignModeActive;

        public bool IsImageAssignModeActive
        {
            get
            {
                return isImageAssignModeActive;
            }
            set
            {
                if (isImageAssignModeActive)
                {
                    _characterSetting.AssignImageButton().GetComponent<Image>().color = Color.white;
                    isImageAssignModeActive = false;
                    return;
                }

                isImageAssignModeActive = value;
                if (value)
                {
                    //turn of all other assign button if is on
                    foreach (var character in FindObjectsOfType<CharacterController>())
                        if (character != this && character.IsImageAssignModeActive)
                            character.IsImageAssignModeActive = false;

                    _characterSetting.AssignImageButton().GetComponent<Image>().color = Color.red;
                }
                else _characterSetting.AssignImageButton().GetComponent<Image>().color = Color.white;
            }
        }

        //Check the character gender
        private List<string> _maleNames = new List<string>{ "Boy_A", "Boy_B", "Boy_C", "Fridolin", "Man_A", "Man_B", "Man_C", "Alien" };
        private bool IAmMale
        {
            get
            {
                if (_maleNames.Find(n => n.Contains(name.Replace("char:", "").Replace("(Clone)", ""))) != null)
                        return true;
                else
                        return false;
            }
        }



        public ToggleObject MyImageAnnotation { get; set; }
        #endregion

        public ToggleObject MyToggleObject()
        {
            return _myObj;
        }

        public DialogRecorder DialogRecorder { get; private set; }

        public AudioEditor MyAudioEditor { get; set; }

        public Action MyAction { get; set; }

        public string MovementType { get; set; }

        public string AnimationType { get; set; }

        public bool AnimationLoop { get; set; }

        public GameObject Character { get; set; }

        public bool AgentReturnAtTheEnd { get; set; }

        public void AnimationClipPlaying(bool value)
        {
            animationClipPlaying = value;
        }

        public NavMeshAgent Agent => _agent;

        public bool AnyNodeMoving
        {
            set;get;
        }

        public List<GameObject> Destinations { get; set; }


        public override bool Init(ToggleObject obj)
        {
            _myObj = obj;

            // Try to set the parent and if it fails, terminate initialization.
            if (!SetParent(obj))
            {
                Debug.Log("Couldn't set the parent.");
                return false;
            }

            name = obj.predicate;
            obj.text = name;
            transform.parent.GetComponent<PoiEditor>().ICanRotate = true;

            _agent.enabled = false;

            // Set scaling if defined in action configuration.
            if (!obj.scale.Equals(0))
            {
                transform.localScale = new Vector3(obj.scale, obj.scale, obj.scale);
            }

            // If everything was ok, return base result.
            return base.Init(obj);
        }



        private void Start()
        {
            //The folder where character data will be saved in
            characterDataFolder = Path.Combine(ActivityManager.Instance.Path, "characterinfo");

            //generate a random color
            _pathNodesColor = new Color(
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f)
            );

            var ObjectManipulater = transform.parent.GetComponent<ObjectManipulator>();
            if (ObjectManipulater)
            {
                //disable fully one hand manipulatin
                ObjectManipulater.ManipulationType = Microsoft.MixedReality.Toolkit.Utilities.ManipulationHandFlags.TwoHanded;
                //only scale on two hand manupulation
                ObjectManipulater.TwoHandedManipulationType = Microsoft.MixedReality.Toolkit.Utilities.TransformFlags.Scale;

                ObjectManipulater.OnManipulationEnded.AddListener(delegate { FitSpeedToSize(); });
            }

            //add NavMeshObstacle to the camera to avoid collision between the camera and the character
            var cam = Camera.main;
            if (cam.GetComponentInChildren<NavMeshObstacle>() == null)
            {
                var NavmeshObstacleObj = Instantiate(Resources.Load<GameObject>("Prefabs/Characters/NavmeshObstacle"), cam.transform.position, cam.transform.rotation);
                NavmeshObstacleObj.transform.SetParent(cam.transform);
            }

            _characterSetting.AssignImageButton().onClick.AddListener(delegate { IsImageAssignModeActive = !IsImageAssignModeActive; });
            _characterSetting.ResetImageButton().onClick.AddListener(ResetImage);

            _characterSetting.Trigger().onValueChanged.AddListener(delegate { OnTriggerValueChanged(); }) ;
        }



        private async void OnEnable()
        {
            EventManager.OnEditModeChanged += SetEditModeState;

            stepsSettings = new List<StepSettings>();

            _anim = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            //create the character setting
            var characterSettingPrefab = Resources.Load<GameObject>("Prefabs/Characters/CharacterSettingPanel");
            var spawnPoint = transform.Find("UISpawnPoint");
            _characterSetting = Instantiate(characterSettingPrefab, spawnPoint.position, spawnPoint.rotation).GetComponent<CharacterSettings>(); 
            _characterSetting.transform.SetParent(transform);
            _characterSetting.AnimationLoopToggle().onValueChanged.AddListener(delegate { OnAnimatinLoopToggle(); });

            movementManger = _characterSetting.MovementManager();
            movementManger.PathLoop.onValueChanged.AddListener(delegate { LoopPath(); });
            movementManger.FollowPlayer.onValueChanged.AddListener(delegate { FollowPlayer(); });

            animationMenu = _characterSetting.AnimationMenu();
            animationMenu.onValueChanged.AddListener(delegate { OnAnimationClipChanged(); });

            //create image container
            var imageContainerPrefab = Resources.Load<GameObject>("Prefabs/Characters/ImageDisplayFiles/PictureContainer");
            imageContainer = Instantiate(imageContainerPrefab, Vector3.zero, Quaternion.identity);
            imageContainer.transform.SetParent(transform.Find("CharacterGroup").transform);

            //create the audio source which will be used by Watson and dialogplayer
            CreateAudioSource();

            MyAction = ActivityManager.Instance.ActiveAction;

            DialogRecorder = _characterSetting.DialogRecorder();
            if (DialogRecorder)
                DialogRecorder.MyCharacter = this;

            _characterSetting.AIToggle.onValueChanged.AddListener(delegate { AddWatsonAssistant(); });
            _characterSetting.PreRecordToggle.onValueChanged.AddListener(delegate { DeactivateAI(); });

            //Do not remove this
            await Task.Delay(200);

            //sometimes the character be destroyed by keep alive during the delay above
            if (this == null) return;

            //wait for the character be setup by using JSON data
            await ActivateCharacterOnEnable();
            characterParsed = true;

            //Remove animation clips which not exist for this character from the drobdown list
            for (int i = animationMenu.options.Count - 1 ; i > 0 ; i--)
                if (_anim.parameters.ToList().Find(c => c.name == animationMenu.options[i].text) == null)
                    animationMenu.options.RemoveAt(i);


            //if the movementtype was not found in the json file use followpath as default(with one node)
            if (MovementType == string.Empty)
                MovementType = "followPath";

            //sometime after enabling the character some script try to disable it again for any reason, we need to checkit here to avoid getting Coroutine exception
            if (gameObject.activeInHierarchy)
                StartCoroutine(WaitForMovementType());

            //If animation clip is display image, move the image annotation to the character poster
            MoveImageToHandPos();

            SetEditModeState(ActivityManager.Instance.EditModeActive);

            //if movement is inplace play the audio at the start
            if (Destinations.Count == 1)
                DialogRecorder.PlayDialog();
        }



        private void OnDisable()
        {
            EventManager.OnEditModeChanged -= SetEditModeState;
            SaveJson();

            DestroyDestinationsOnDisable();
        }

        /// <summary>
        /// Check that there is an audio editor present and, if not, create one.
        /// </summary>
        /// <param name="forceRemove">Set to true to remove an existing audio editor.</param>
        public void AudioEditorCheck(bool forceRemove = false)
        {
            bool hasAudioEditor = GetComponentInChildren<AudioEditor>();

            if (forceRemove && hasAudioEditor)
            {
                MyAudioEditor = null;
                Destroy(GetComponentInChildren<AudioEditor>().gameObject);
                return;
            }

            if (hasAudioEditor)
            {
                MyAudioEditor = GetComponentInChildren<AudioEditor>();
            }
            else
            {
                GameObject audioEditorObject = Instantiate(Resources.Load("Prefabs/UI/Hololens/EditorPanels/AudioEditor") as GameObject);
                audioEditorObject.transform.SetParent(this.transform);
                MyAudioEditor = audioEditorObject.GetComponent<AudioEditor>();

            }
        }



        private void FixedUpdate()
        {
            if (this == null) {
                StopAllCoroutines();
                return;
             }

             triggerDuration = DialogRecorder.DialogLength() >= animationLength ? DialogRecorder.DialogLength() : animationLength;

            if (!_anim || !_agent || MovementType == string.Empty || !characterParsed ) return;

            //Deactive the animation selector if movement is follow path and loop is on
            animationMenu.interactable = !(AgentReturnAtTheEnd && !movementManger.FollowPlayer.isOn);

            if (_agent.isActiveAndEnabled)
            {
                var charPosXZ = new Vector3(_agent.transform.position.x, 0, _agent.transform.position.z);
                var destinationPosXZ = new Vector3(_agent.destination.x, 0, _agent.destination.z);

                if (Vector3.Distance(charPosXZ, destinationPosXZ) > _agent.stoppingDistance && (Destinations.Count > 1 || MovementType == "followplayer"))
                {
                    _agent.updateRotation = true;
                    _agent.updatePosition = true;

                    //start walking animation a bit after moving
                    if(Vector3.Distance(charPosXZ, destinationPosXZ) > _agent.stoppingDistance + 0.1f)
                        PlayClip("Walk");

                    //if image display is playing stop the particles and hide the image
                    if(animationClipPlaying)
                        StartCoroutine(OnImageDisplayIntro(false));
                }
                else
                {
                    _agent.updateRotation = false;
                    _agent.updatePosition = false;
                    if (animationPlayedOnce)
                        SelectClip();

                }
            }

        }

        private void AddWatsonAssistant()
        {
            if (AIActivated && characterParsed) return;

            //stop dialog recording before activating AI
            if(DialogRecorder.isRecording) 
                DialogRecorder.StopDialogRecording();

            DialogRecorder.StopDialog();

            //destroy all Watson services in the scene
            foreach (var dialogService in FindObjectsOfType<DialogueService>())
                Destroy(dialogService.transform.parent.gameObject);

            //Set back the audio mode for all characters to pre-record, except for me
            foreach (var character in FindObjectsOfType<CharacterController>())
                if (character != this)
                    character._characterSetting.PreRecordToggle.isOn = true;


            //Create a new Watson assistant for this character
            var watsonServicePrefab = Resources.Load<GameObject>("Prefabs/Characters/IBMWatsonAssistant");
            watsonService = Instantiate(watsonServicePrefab, Vector3.zero, Quaternion.Euler(0, 180, 0));
            watsonService.transform.SetParent(transform);
            watsonService.transform.localPosition = new Vector3(0, 2, 0);
            watsonService.transform.Find("WatsonServices").GetComponent<DaimonManager>().myCharacter = _anim.gameObject;
            var speechOutputService = watsonService.transform.Find("WatsonServices").GetComponent<SpeechOutputService>();
            speechOutputService.myCharacter = _anim.gameObject;
            speechOutputService.myVoice = IAmMale ? "en-US_KevinV3Voice" : "en-US_AllisonVoice";
            AIActivated = true;
        }


        private void CreateAudioSource()
        {
            var audioSource = _anim.gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 7;
        }


        private void DeactivateAI()
        {
            if (!AIActivated) return;

            if (watsonService)
            {
                watsonService.transform.Find("WatsonServices").GetComponent<SpeechInputService>().StopRecording();
                Destroy(watsonService);
            }
            AIActivated = false;
        }


        private void OnTriggerValueChanged()
        {
            if (!characterParsed) return;

            if (_characterSetting.Trigger().isOn)
            {
                if (ActivityManager.Instance.ActionsOfTypeAction.IndexOf(MyAction) != ActivityManager.Instance.ActionsOfTypeAction.Count - 1)
                {
                    MyAction.AddArlemTrigger("character", _myObj.predicate, _myObj.poi, triggerDuration);
                }
                else
                { 
                    // give the info and close
                    DialogWindow.Instance.Show("Info!",
                    "This is the last step. The trigger is disabled!\n Add a new step and try again.",
                    new DialogButtonContent("Ok"));

                    _characterSetting.Trigger().isOn = false;
                }
            }
            else
                MyAction.RemoveArlemTrigger(_myObj);
        }


        private IEnumerator WaitForMovementType()
        {
            //wait until the movement type is set
            while(MovementType == "")
            {
                yield return null;
            }

            //wait until the destination is set for followpath movement
            if (MovementType == "followpath")
            {
                while (Destinations == null || Destinations.Count == 0)
                {
                    yield return null;
                }
            }

            //set the movement type
            switch (MovementType)
            {
                case "followpath":
                    _agent.enabled = true;
                    _agent.stoppingDistance = 0.05f;
                    FollowThePath(0, false);
                    break;
                case "followplayer":
                    _agent.enabled = true;
                    _agent.stoppingDistance = 1.5f;
                    _agent.autoBraking = true;
                    StartCoroutine(FollowThePlayer());
                    break;
                default:
                    break;
            }

            //set the color of my destinations nodes
            if(Destinations != null && Destinations.Count > 0)
                foreach (var node in Destinations)
                    foreach (var mesh in node.GetComponentsInChildren<MeshRenderer>())
                        mesh.material.color = _pathNodesColor;

        }


        public void LoopPath()
        {
            AgentReturnAtTheEnd = movementManger.PathLoop.isOn;
            _agent.autoBraking = !movementManger.PathLoop.isOn;
            //sometime after enabling the character some script try to disable it again for any reason, we need to checkit here to avoid getting Coroutine exception
            if (gameObject.activeInHierarchy)
                StartCoroutine(WaitForMovementType());
        }



        public void FollowPlayer()
        {

            //if after loading movement set to follow path but there is no nodes then create one
            if (Destinations == null || Destinations.Count == 0)
            {
                Destinations = new List<GameObject>();
                var des = Instantiate(Resources.Load<GameObject>("Prefabs/characters/CharacterDestination"), transform.position, Quaternion.identity);
                des.GetComponent<Destination>().MyCharacter = this;
                des.transform.SetParent(transform.parent);
                des.SetActive(false);
                Destinations.Add(des);
            }

            foreach (var node in Destinations)
                    node.SetActive(!movementManger.FollowPlayer.isOn);

            MovementType = movementManger.FollowPlayer.isOn ? "followplayer" : "followpath";
            //sometime after enabling the character some script try to disable it again for any reason, we need to checkit here to avoid getting Coroutine exception
            if (gameObject.activeInHierarchy)
                StartCoroutine(WaitForMovementType());
        }



        Tuple<DestinationPoints, string> PrepareNodesToSave()
        {
            var destinationPoints = new DestinationPoints();

            //prepare the movementType and the destination nodes
            switch (MovementType)
            {
                case "followpath":
                    var points = new Point[Destinations.Count];

                    for (int i = 0; i < Destinations.Count; i++)
                    {
                        var desNode = Destinations[i].transform;
                        //create a point from this des point 
                        points[i] = new Point(i, desNode.localPosition, desNode.localRotation, Destinations[i].name);
                    }

                    destinationPoints.points = points;
                    destinationPoints.returnPath = AgentReturnAtTheEnd;

                    break;
                case "followplayer":
                    break;
                default:
                    //This is because we only add moveType in the augmentationEditor at the beggining and might be null in other step
                    //TODO: Later should in each step movementType be set seperately
                    var characterOriginalStep = stepsSettings.Find(a => a.actionId != string.Empty);
                    if (characterOriginalStep != null)
                    {
                        MovementType = characterOriginalStep.movementType;
                        destinationPoints = characterOriginalStep.destinations;
                    }
                    else
                        MovementType = "followplayer";

                    break;
            }

            return Tuple.Create(destinationPoints, MovementType);
        }


        private void FitSpeedToSize()
        {
            if (!_agent) return;
            var CharacterSize = transform.parent.localScale.x;
            _agent.speed = CharacterSize;
            _agent.stoppingDistance = CharacterSize * _agent.stoppingDistance;

        }


        public void SaveJson()
        {
            if (_myObj == null || _myObj.poi == "" || !characterParsed) return; //only if the character is instantiated not the prefab

            var character = new Character();

            //add settings of all steps(which contains me) in json file
            foreach (var action in ActivityManager.Instance.ActionsOfTypeAction)
            {
                if (action.enter.activates.Find(p => p.poi == _myObj.poi) != null)
                {
                    //create a new step to keep current step settings for this character
                    var step = new StepSettings
                    {
                        actionId = action.id
                    };

                    //Each time we call this method the json file will be regenrated with all steps settings the:
                    // save the active step settings for the character
                    if(step.actionId == ActivityManager.Instance.ActiveActionId)
                    {
                        var(destinationPoints, movementType) = PrepareNodesToSave();
                        step.movementType = movementType;
                        step.destinations = destinationPoints; //null if not followpath
                        step.animationType = animationMenu.options[animationMenu.value].text;
                        step.animationLoop = _characterSetting.AnimationLoopToggle().isOn;

                        //if character has a dialog recorder
                        if (DialogRecorder)
                        {
                            step.dialogLoop = DialogRecorder.LoopToggle.isOn;
                            step.dialSaveName = ActivityManager.Instance.ActiveActionId + "_" + _myObj.poi + ".wav";
                        }

                        //if character has an image and animation type is image display
                        step.imagePoiId = MyImageAnnotation != null ? MyImageAnnotation.poi : string.Empty;
                    }
                    //save the setting on other steps
                    else
                    {
                        var otherStep = stepsSettings.Find(a => a.actionId == action.id);
                        if (otherStep == null) continue;

                        step.movementType = otherStep.movementType;
                        step.destinations = otherStep.destinations;
                        step.animationType = otherStep.animationType;
                        step.animationLoop = otherStep.animationLoop;

                        //if character has a dialog recorder
                        if (DialogRecorder)
                        {
                            step.dialogLoop = otherStep.dialogLoop;
                            step.dialSaveName = otherStep.actionId + "_" + _myObj.poi + ".wav";
                        }

                        //if character has an image and animation type is image display
                        step.imagePoiId = otherStep.imagePoiId;
                    }

                    //Replace the step if exists, otherwise add
                    if (stepsSettings.Find(a => a.actionId == step.actionId) != null)
                        stepsSettings[stepsSettings.FindIndex(a => a.actionId == step.actionId)] = step;
                    else
                        stepsSettings.Add(step);
                }
            }

            //add our saved stepsSettings list to this character
            character.steps = stepsSettings;

            //save the size of the character (x,y and z is same)
            character.scale = transform.localScale.x;

            ///save AI activation status
            //remove AI for all characters from jsons
            foreach (var ch in FindObjectsOfType<CharacterController>())
            {
                //Save all other characters AIActive to false
                if (ch == this) continue;
                var jsonpath = $"{characterDataFolder}/{ch.MyToggleObject().poi}.json";

                if (!File.Exists(jsonpath)) continue;

                var anotherCharacter = JsonUtility.FromJson<Character>(File.ReadAllText(jsonpath));
                anotherCharacter.AIActive = false;
                string anotherCharacterNewJson = JsonUtility.ToJson(anotherCharacter);
                File.WriteAllText(jsonpath, anotherCharacterNewJson);
            }

            //then save it only for this char if it is set to true
            character.AIActive = AIActivated;


            //create characterinfo folder if not exist
            string characterJson = JsonUtility.ToJson(character);
            if (!Directory.Exists(characterDataFolder))
                Directory.CreateDirectory(characterDataFolder);

            string jsonPath = $"{characterDataFolder}/{_myObj.poi}.json";

            //delete the exsiting file first
            if (File.Exists(jsonPath))
                File.Delete(jsonPath);

            var assetBundlePath = $"{characterDataFolder}/{_myObj.option}";
            if (!File.Exists(assetBundlePath))
            {
                var manifestFilePath = $"{Application.dataPath}/MirageXR/Common/AssetBundles/{_myObj.option}.manifest";
                var bundleFilePath = $"{Application.dataPath}/MirageXR/Common/AssetBundles/{_myObj.option}";
                if (File.Exists(manifestFilePath))
                    File.Copy(manifestFilePath , assetBundlePath + ".manifest");
                if (File.Exists(bundleFilePath))
                    File.Copy(bundleFilePath, assetBundlePath);
            }

            //write the json file
            File.WriteAllText(jsonPath, characterJson);
        }



        IEnumerator FollowThePlayer()
        {
            SelectClip();
            while (_agent && _agent.gameObject.activeInHierarchy && Camera.main && _agent.enabled && MovementType == "followplayer")
            {
                yield return new WaitForSeconds(2f);

                var player = Camera.main.transform;
                _agent.SetDestination(player.position);

                var charPosXZ = new Vector3(_agent.transform.position.x, 0, _agent.transform.position.z);
                var destinationPosXZ = new Vector3(player.position.x, 0, player.position.z);
                if (Vector3.Distance(charPosXZ, destinationPosXZ) <= _agent.stoppingDistance)
                {
                    animationClipPlaying = false;
                    SelectClip();
                }

            }
        }



        public async void FollowThePath(int index, bool backNow)
        {
            if (!_agent || MovementType == "followplayer") return;

            int nextIndex = await CheckIndex(index, backNow);
            bool nextBackNowState = nextIndex <= index;

            await MoveToNextDestination(nextIndex);

            if (index == 0) nextBackNowState = false;

            //return the path if toggle is selected
            if (AgentReturnAtTheEnd)
            {
                //only repeat the path on play mode
                if (!ActivityManager.Instance.EditModeActive)
                {
                    if (index == Destinations.Count - 1)
                        nextBackNowState = true;
                }
                else
                {
                    nextBackNowState = false;
                    if (this != null && index == Destinations.Count - 1 && !animationPlayedOnce)
                    {
                        StartCoroutine(DoOnEndOFPath());
                    }
                }

                FollowThePath(nextIndex, nextBackNowState);

            }
            else
            {
                if (index < Destinations.Count)
                {
                    FollowThePath(nextIndex, false);
                    if (this != null && index == Destinations.Count - 1 && !animationPlayedOnce)
                    {
                        StartCoroutine(DoOnEndOFPath());
                    }
                }
            }
        }


        IEnumerator DoOnEndOFPath()
        {
            var charPosXZ = new Vector3(_agent.transform.position.x, 0, _agent.transform.position.z);
            var destinationPosXZ = new Vector3(_agent.destination.x, 0, _agent.destination.z);

            while (this != null && Vector3.Distance(charPosXZ, destinationPosXZ) > _agent.stoppingDistance)
                yield return null;

            SelectClip();
            DialogRecorder.PlayDialog();
            var yRot = Destinations.LastOrDefault().transform.localRotation.y;
            transform.parent.localRotation = Quaternion.Euler(0, yRot , 0);
        }



        private async Task<int> CheckIndex(int index, bool backNow)
        {
            if (backNow)
                index--;
            else
                index++;

            await Task.Delay(1);

            if (index <= 0) index = 0;
            if (index >= Destinations.Count) index = Destinations.Count - 1;

            return index;
        }



        private async Task<bool> MoveToNextDestination(int index)
        {
            if (!_agent || !_agent.isActiveAndEnabled || Destinations == null) return false;

            if (index < 0) return true; //go to the next point

            _agent.SetDestination(Destinations[index].transform.position);
            while (_agent && _agent.remainingDistance > _agent.stoppingDistance)
            {
                await Task.Delay(10);
            }

            return true;
        }



        private void PlayClip(string clipName)
        {
            if (!_anim) return;

            foreach (var param in _anim.parameters) _anim.SetBool(param.name, param.name == clipName);
        }


        private void OnAnimatinLoopToggle()
        {
            if (!characterParsed) return;

            if (_characterSetting.AnimationLoopToggle().isOn) animationClipPlaying = false; 
            AnimationLoop = _characterSetting.AnimationLoopToggle().isOn;
        }


        private void OnAnimationClipChanged()
        {
            if (!characterParsed) return;

            StartCoroutine(OnImageDisplayIntro(true)); 
            animationClipPlaying = false;
        }


        /// <summary>
        /// this method is called in update, be carefull what you add here. also on droplist changed value
        /// </summary>
        public async void SelectClip(string clip = null)
        {
            //return if an animation clip is already playing
            if (!_anim || this == null || animationClipPlaying) return;

            //return if the character is following the player (on walking)
            var charPosXZ = new Vector3(_agent.transform.position.x, 0, _agent.transform.position.z);
            var destinationPosXZ = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            if (Vector3.Distance(charPosXZ, destinationPosXZ) > _agent.stoppingDistance && MovementType == "followplayer") return;

            var clipName = animationMenu.options[animationMenu.value].text;

            if (!string.IsNullOrEmpty(AnimationType) && !ActivityManager.Instance.EditModeActive)
            {
                var animationIntValue = await GetAnimationMenuValue(AnimationType);

                if (animationIntValue != 9999) //TODO: remove magic number
                    animationMenu.value = animationIntValue;
            }
            else
            {
                AnimationType = clipName;
            }

            //show the image if it is not displayed yet
            if (AnimationType == _characterSetting.defaultImageDisplayAnimationName && !imageContainer.transform.Find("ImagePlane").gameObject.activeInHierarchy)
                StartCoroutine(OnImageDisplayIntro(true));

            PlayClip(AnimationType);

            animationPlayedOnce = true;

            //Do on Idle
            if (AnimationType == "Idle" || clip != null)
            {
                if (imageContainer)
                {
                    imageContainer.transform.Find("ImagePlane").gameObject.SetActive(false);
                    imageContainer.transform.Find("Magic Particles").GetComponent<ParticleSystem>().Stop();
                }
            }

            animationClipPlaying = true;

            var time = 0f;
            if(_anim != null)
                foreach (AnimationClip ac in _anim.runtimeAnimatorController.animationClips)
                {
                    if (ac.name == AnimationType)
                    {
                        time = ac.length;
                        animationLength = ac.length;
                    }

                }

            //Do not wait for idle because the idle is default one anyway
            if (AnimationType != "Idle")
                await Task.Delay((int)(time * 1000));

            if (!_characterSetting.AnimationLoopToggle().isOn && AnimationType != _characterSetting.defaultImageDisplayAnimationName)
            {
                PlayClip("Idle");
                return;
            }

            animationClipPlaying = false;
        }


        private async Task<int> GetAnimationMenuValue(string clipName)
        {
            int num = 9999;
            for (int i = 0; i < animationMenu.options.Count;  i++)
            {
                var option = animationMenu.options[i].text;
                if (option == clipName)
                    num = i;
            }

            await Task.Delay(1);

            return num;
        }



         void DestroyDestinationsOnDisable()
        {
            if (Destinations == null) return;

            foreach (GameObject des in Destinations)
            {
                Destroy(des);
            }
        }



         public async Task<bool> ActivateCharacterOnEnable()
        {
            var characterLoaded = false;
            //if any character path has been found parse it

            var jsonpath = $"{characterDataFolder}/{_myObj.poi}.json";
            if (File.Exists(jsonpath))
                characterLoaded = await ParseCharacters(jsonpath);

            if (characterLoaded)
                return true;
            else
                return false;
        }



        void SetEditModeState(bool editModeActive)
        {
            //hilde all nodes(the root object of node, Destination.cs,  will not be deactivated)
            foreach (var des in Destinations)
                if (des)
                    des.transform.GetChild(0).gameObject.SetActive(editModeActive);

            //seperate the dialog player and hide other settings
            if (!editModeActive)
            {
                //seprate the play and stop button and let them be displayed on playmode too
                if (File.Exists($"{characterDataFolder}/{ActivityManager.Instance.ActiveAction.id}_{_myObj.poi}.wav") && !AIActivated)
                {
                    DialogRecorder.transform.SetParent(_characterSetting.transform);
                    DialogRecorder.CloseDialogRecorder();
                }

                _characterSetting.transform.GetChild(0).gameObject.SetActive(false); //child 0 is the background gameobject (the parent of all other settings)

                //disable all bound box
                Destinations.ForEach(d => d.GetComponent<BoundsControl>().Active = false);
            }
            else 
            {
                if (DialogRecorder)
                {
                    DialogRecorder.transform.SetParent(_characterSetting.transform.GetChild(0));
                    _characterSetting.transform.GetChild(0).gameObject.SetActive(true);
                    DialogRecorder.OpenDialogRecorder();
                }

                //enable boundbox of the last node
                Destinations[Destinations.Count-1].GetComponent<BoundsControl> ().Active = true;
            }
        }

    

        /// <summary>
        /// parse the destination nodes that character should follow
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="jsonPath"></param>
        public async Task<bool> ParseCharacters(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return false;

            List<GameObject> destinations = new List<GameObject>();
            Character character = JsonUtility.FromJson<Character>(File.ReadAllText(jsonPath));

            foreach (var step in character.steps)
            {
                StepSettings s = new StepSettings
                {
                    destinations = step.destinations,
                    movementType = step.movementType,
                    animationType = step.animationType,
                    animationLoop = step.animationLoop,
                    dialSaveName = step.dialSaveName,
                    dialogLoop = step.dialogLoop,
                    imagePoiId = step.imagePoiId,
                    actionId = step.actionId,
                };

                stepsSettings.Add(s);
            }

            //set the size of the character
            if(character.scale != 0)
                transform.localScale = new Vector3(character.scale, character.scale, character.scale);


            //Adjust navmesh setting
            FitSpeedToSize();

            //if the loaded character cannot be found in any step (the json format is changed maybe)
            if (character.steps.Count == 0)
            {
                gameObject.SetActive(false);
                Debug.LogError("The character augmentation has had a major change. Please recreate the existing characters.");
                return false;
            }

            string MyActionID = string.Empty;
            if (stepsSettings.Find(a => a.actionId == ActivityManager.Instance.ActiveActionId) != null )
                MyActionID = ActivityManager.Instance.ActiveActionId;
            else
                MyActionID = stepsSettings.FirstOrDefault().actionId;

            //get the info for the active step
            var movementType = stepsSettings.Find(a => a.actionId == MyActionID).movementType;
            var destinationPoint = stepsSettings.Find(a => a.actionId == MyActionID).destinations;
            var animationType = stepsSettings.Find(a => a.actionId == MyActionID).animationType;
            var animationLoop = stepsSettings.Find(a => a.actionId == MyActionID).animationLoop;
            var imagePoi = stepsSettings.Find(a => a.actionId == MyActionID).imagePoiId;
            var dialogLoop = stepsSettings.Find(a => a.actionId == MyActionID).dialogLoop;
            var dialogSaveName = stepsSettings.Find(a => a.actionId == MyActionID).dialSaveName;

            //set the movement panel toggles
            movementManger.PathLoop.isOn = destinationPoint.returnPath;
            movementManger.FollowPlayer.isOn = movementType == "followplayer";

            //and if the destinations are still not created 
            if (movementType == "followpath")
            {
                var nodePrefab = Resources.Load("Prefabs/characters/CharacterDestination") as GameObject;
                foreach (Point desPoint in destinationPoint.points)
                {
                    //instantiate points from json file
                    GameObject des = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity);
                    des.name = desPoint.name;
                    des.GetComponentInChildren<TextMesh>().text = (desPoint.index + 1).ToString();
                    des.transform.SetParent(transform.parent.transform);
                    des.transform.localPosition = desPoint.position != null ? desPoint.position : Vector3.zero;
                    des.transform.localRotation = desPoint.rotation != null ? desPoint.rotation : Quaternion.Euler(0,180,0);
                    destinations.Add(des);
                }

                Destinations = destinations;
                transform.localPosition = Destinations[0].transform.localPosition; //move immediately to the first destination at the start
            }

            MovementType = movementType;

            //default animation if not found
            if (animationType == null || animationMenu.options.Find(a => a.text == animationType) == null)
                animationType = "Idle";
            else if(animationType == _characterSetting.defaultImageDisplayAnimationName)
                StartCoroutine(OnImageDisplayIntro(destinations.Count == 1)); //show immidiatly if there is no path to follow


            AnimationType = animationType;
            AnimationLoop = animationLoop;
            _characterSetting.AnimationLoopToggle().isOn = AnimationLoop;
            animationMenu.value = await GetAnimationMenuValue(animationType);
            if(AnimationType != _characterSetting.defaultImageDisplayAnimationName) //stop display image if the animation is not Display Image
                StartCoroutine(OnImageDisplayIntro(false));

            //find the image annotation which is assigned to this character
            var imageViewer = FindObjectsOfType<FloatingImageViewer>().ToList().Find(x => x.MyToggleObject().poi == imagePoi);
            if (imageViewer)
            {
                MyImageAnnotation = imageViewer.MyToggleObject();
                StartCoroutine(MoveMyImage(GameObject.Find(MyImageAnnotation.poi), MyImageAnnotation));
            }

            AgentReturnAtTheEnd = destinationPoint.returnPath;

            //hide the loop toggle on loading an activity and set the looping of the dialog player
            if (DialogRecorder)
            {
                DialogRecorder.LoopToggle.isOn = dialogLoop;
                DialogRecorder.DialogSaveName = "characterinfo/" + dialogSaveName;
                DialogRecorder.MyCharacter = this;
            }


            //Activate AI if is activated for this character
            if (character.AIActive)
                _characterSetting.AIToggle.isOn = true;


            //inplace is added here only for the old character in the repo. There is no inplace mode longer
            if (movementType == "followpath" || movementType == "inplace")
                foreach (GameObject des in destinations)
                    des.GetComponent<Destination>().MyCharacter = this;

            var myTrigger = ActivityManager.Instance.ActiveAction.triggers.Find(t => t.id == _myObj.poi);
            if (myTrigger != null)
            {
                _characterSetting.Trigger().isOn = true;
                StartCoroutine(ActivateTrigger());
            }

            await Task.Delay(1);

            return true;
        }
        

        IEnumerator ActivateTrigger()
        {
            while (!animationPlayedOnce)
                yield return null;

            var myTrigger = ActivityManager.Instance.ActiveAction.triggers.Find(t => t.id == _myObj.poi);
            if (_characterSetting.Trigger().isOn && !ActivityManager.Instance.EditModeActive && myTrigger != null)
            {
                var triggerDuration = myTrigger.duration;
                yield return new WaitForSeconds(triggerDuration);
                ActionListMenu.Instance.NextAction();
            }
        }




        void MoveImageToHandPos()
        {
            if (!imageContainer || rightHandBone == null) return;

            imageContainer.transform.position = rightHandBone.position;
            imageContainer.transform.rotation = rightHandBone.rotation * Quaternion.Euler(handRotationOffset);
            imageContainer.transform.localScale = Vector3.zero;
            imageContainer.transform.SetParent(rightHandBone);
        }



         IEnumerator MoveMyImage(GameObject img, ToggleObject annotation)
        {
            while (img != null && MyImageAnnotation == annotation)
            {
                img.transform.position = imageContainer.transform.GetChild(0).position;
                img.transform.rotation = imageContainer.transform.GetChild(0).rotation * Quaternion.Euler(90,0,0);
                img.transform.localScale = imageContainer.transform.localScale;
                yield return null;
            }
        }



        public void ResetImage()
        {
            if (MyImageAnnotation != null)
            {
                Vector3 resetPos = Vector3.zero;
                Quaternion resetRot = Quaternion.identity;
                var annotationSpawnPoint = GameObject.Find("AnnotationSpawnPoint");
                if (annotationSpawnPoint)
                {
                    resetPos = annotationSpawnPoint.transform.position;
                    resetRot = annotationSpawnPoint.transform.rotation;
                }
                GameObject.Find(MyImageAnnotation.poi).transform.position = resetPos;
                GameObject.Find(MyImageAnnotation.poi).transform.rotation = resetRot;
                GameObject.Find(MyImageAnnotation.poi).transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                StopCoroutine(MoveMyImage(GameObject.Find(MyImageAnnotation.poi), MyImageAnnotation));
                MyImageAnnotation = null;
                IsImageAssignModeActive = false;
            }
        }



        public void SetImage(ToggleObject annotation)
        {
            foreach (var character in FindObjectsOfType<CharacterController>())
            {
                if (character != this && character.MyImageAnnotation == annotation)
                    character.ResetImage();
            }

            if (MyImageAnnotation != null)
                ResetImage();

            MyImageAnnotation = annotation;
            IsImageAssignModeActive = false;

            var poi = GameObject.Find(annotation.poi);
            StopCoroutine(MoveMyImage(poi, annotation));
            StartCoroutine(MoveMyImage(poi, annotation));
        }



        IEnumerator OnImageDisplayIntro(bool visibility)
        {
            yield return new WaitForSeconds(0.1f);
            //only if animation type is "Image Display"
            try
            {
                //show the image at the end of the path
                var charShowsAnImage = AnimationType == _characterSetting.defaultImageDisplayAnimationName && visibility;

                if (_myImageFrame) _myImageFrame.SetActive(charShowsAnImage);
                if (MyImageAnnotation != null) GameObject.Find(MyImageAnnotation.poi).transform.GetChild(0).gameObject.SetActive(charShowsAnImage); //hide the floating image viewer
                if (imageContainer)
                {
                    imageContainer.transform.Find("ImagePlane").gameObject.SetActive(charShowsAnImage);
                    if (charShowsAnImage)
                    {
                        StartCoroutine(ImageDisplayIntroSize());
                        imageContainer.transform.Find("Magic Particles").GetComponent<ParticleSystem>().Play();
                    }
                    else
                        imageContainer.transform.Find("Magic Particles").GetComponent<ParticleSystem>().Stop();
                }
            }
            catch (NullReferenceException e)
            {
                Debug.LogError(
                    "Some references are missing on display image animation part. f.exp imageContainer,MyImageAnnotation ,etc. " +
                    e);
            }

        }



        IEnumerator ImageDisplayIntroSize()
        {
            imageContainer.transform.localScale = Vector3.zero;
            yield return new WaitForSeconds(1f);
            while (imageContainer.transform.localScale.x < 1)
            {
                var temp = imageContainer.transform.localScale;
                imageContainer.transform.localScale = temp + new Vector3(5f, 5f, 5f) * Time.deltaTime;
                yield return new WaitForSeconds(0.01f);
            }
            imageContainer.transform.localScale = new Vector3(1, 1, 1);
        }
    }


    [Serializable]
    public class Character
    {
        public List<StepSettings> steps;
        public float scale;
        public bool AIActive;
    }


    [Serializable]
    public class StepSettings
    {
        public string actionId;
        public string movementType;
        public DestinationPoints destinations;
        public string animationType;
        public bool animationLoop;
        public string dialSaveName;
        public bool dialogLoop;
        public string imagePoiId;
    }


    [Serializable]
    public class DestinationPoints
    {
        public Point[] points = null;
        public bool returnPath = false;
    }


    [Serializable]
    public class Point
    {
        public int index;
        public Vector3 position;
        public Quaternion rotation;
        public string name;

        public Point(int index, Vector3 pos,Quaternion rot, string name)
        {
            this.index = index;
            this.position = pos;
            this.rotation = rot;
            this.name = name;
        }
    }
}
