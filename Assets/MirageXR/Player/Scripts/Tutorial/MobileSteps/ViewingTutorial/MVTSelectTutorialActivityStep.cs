using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MirageXR
{

    public class MVTSelectTutorialActivityStep : TutorialStep
    {
        protected override void SecuredEnterStep()
        {
            ActivityListView_v2 alv = RootView_v2.Instance.activityListView;
            ActivityListItem_v2 tmp = alv.GetComponent<ActivityListItem_v2>();

            TutorialItem titem = new TutorialItem();

            var queue = new Queue<TutorialModel>();
            queue.Enqueue(new TutorialModel { id = "", message = "Click the first activity.", position = TutorialModel.MessagePosition.Bottom });
            this.manager.MobileTutorial.Show(queue);
        }

        protected override void SecuredExitStep()
        {
            this.manager.NextStep();
        }
        protected override void SecuredCloseStep()
        {
            this.manager.MobileTutorial.Hide();
            //nothing
        }
    }
}
