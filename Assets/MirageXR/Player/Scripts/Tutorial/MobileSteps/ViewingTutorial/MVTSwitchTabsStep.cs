using System.Collections.Generic;
using UnityEngine.UI;

namespace MirageXR
{
    public class MVTSwitchTabsStep : TutorialStep
    {
        protected override void SecuredEnterStep()
        {
            RootView_v2.Instance.activityView.BtnArrow.onClick.AddListener(this.DefaultExitEventListener);

            var queue = new Queue<TutorialModel>();
            queue.Enqueue(new TutorialModel { id = "activity_steps", message = "Great, calibration is now complete. Let's move on to the activity's content. First, switch to the Steps tab.", position = TutorialModel.MessagePosition.Middle });
            queue.Enqueue(new TutorialModel { id = "ui_toggle", message = "This screen shows all the Steps, the content of an Activity. It serves as an indicator of your progress, which Step you are on. For now let's begin interacting with the first Step's content by lowering the UI.", position = TutorialModel.MessagePosition.Bottom });
            this.manager.MobileTutorial.Show(queue);
        }

        protected override void SecuredExitStep()
        {
            RootView_v2.Instance.activityView.BtnArrow.onClick.RemoveListener(this.DefaultExitEventListener);
            this.manager.NextStep();
        }

        protected override void SecuredCloseStep()
        {
            RootView_v2.Instance.activityView.BtnArrow.onClick.RemoveListener(this.DefaultExitEventListener);
            this.manager.MobileTutorial.Hide();
        }
    }
}
