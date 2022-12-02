using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MirageXR
{
    public class HelpStepCopyStep : HelpStep
    {
        protected override void Init()
        {
            this._instructionText = "To make a copy of a step and all its contents, tap on Edit step and then tap on the info tab.";
            GameObject Edit = GameObject.Find("EditButton");

            Button button = Edit.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(SecuredExitStep);
            }

            this.highlightedObject = Edit;
            EventManager.NewActivityCreationButtonPressed += DefaultExitEventListener;
        }

        protected override void Detach()
        {
            EventManager.NewActivityCreationButtonPressed -= DefaultExitEventListener;
        }

        protected override void SecuredExitStep()
        {
            Detach();
            RemoveHighlight();
            RemoveInstruction();
        }
    }
}
