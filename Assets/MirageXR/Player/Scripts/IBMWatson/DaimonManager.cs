using i5.Toolkit.Core.VerboseLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DaimonManager : MonoBehaviour
{
    private static MirageXR.ActivityManager activityManager => MirageXR.RootObject.Instance.activityManager;

    public SpeechInputService mySpeechInputMgr { get; private set; }
    private SpeechOutputService mySpeechOutputMgr;

    public GameObject myCharacter { get; set; }
    private Animator myAnimator;

    public GameObject[] lookTargets { get; private set; }

    private Dictionary<string, object> _context = null;
    private bool _waitingForResponse = true;

    public float wait = 0.0f; //currently no wait, may require a fix
    public bool check = false;
    public bool play = false;
    public bool triggerNext = false;



    // Start is called before the first frame update
    void Start()
    {
        mySpeechInputMgr = GetComponent<SpeechInputService>();
        mySpeechOutputMgr = GetComponent<SpeechOutputService>();
        myAnimator = myCharacter.GetComponent<Animator>();
    }


    IEnumerator Look(float preDelay, float duration, GameObject lookTarget)
    {
        yield return new WaitForSeconds(preDelay);

        AppLog.LogTrace("Look=" + "LEFT/RIGHT");
        yield return new WaitForSeconds(duration);

    }


    // Update is called once per frame
    void Update()
    {


        if (check)
        {
            wait -= Time.deltaTime; //reverse count
        }

        if ((wait < 0f) && (check))
        {

            //check that clip is not playing
            AppLog.LogInfo("-------------------- Speech Output has finished playing, now reactivating SpeechInput.");
            check = false;

            if (triggerNext)
            {
                triggerNext = false;
                activityManager.ActivateNextAction();
            }

            //Now let's start listening again.....
            mySpeechInputMgr.Active = true;
            mySpeechInputMgr.StartRecording();

        }
    }


    // check for exercise name (from ExerciseController.cs)
    // and run the according animation (in myAnimator)
    public void Animate(string exercise)
    {

        switch (exercise)
        {
            case "B12":
                myAnimator.Play("BreathingIdle");
                break;
            case "waving":
                myAnimator.Play("waving");
                break;
            default:
                break;
        }

    }

}
