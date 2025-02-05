using i5.Toolkit.Core.VerboseLogging;
using UnityEngine;
using UnityEngine.UI;

public class DialogTest : MonoBehaviour
{
    [SerializeField] private Dialog _dialog;
    [SerializeField] private Button _buttonMiddle;
    [SerializeField] private Button _buttonMiddleMultiline;
    [SerializeField] private Button _buttonBottomMultiline;
    [SerializeField] private Button _buttonBottomInputField;
    [SerializeField] private Toggle _toggleCanBeClosedByOutTap;

    private void Start()
    {
        _buttonMiddle.onClick.AddListener(ShowMiddleDialog);
        _buttonMiddleMultiline.onClick.AddListener(ShowMiddleMultilineDialog);
        _buttonBottomMultiline.onClick.AddListener(ShowBottomMultilineDialog);
        _buttonBottomInputField.onClick.AddListener(ShowBottomInputFieldDialog);
    }

    private void ShowMiddleDialog()
    {
        _dialog.ShowMiddle(
            "Middle Dialog Test!",
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
            "Left", () => AppLog.LogTrace("Left - click!"),
            "Right", () => AppLog.LogTrace("Right - click!"),
            _toggleCanBeClosedByOutTap.isOn);
    }

    private void ShowMiddleMultilineDialog()
    {
        _dialog.ShowMiddleMultiline(
            "Middle Multiline Dialog Test!",
            _toggleCanBeClosedByOutTap.isOn,
            ("Item 1", () => AppLog.LogTrace("Item 1 - click!"), false),
            ("Item 2", () => AppLog.LogTrace("Item 2 - click!"), false),
            ("Item 3", () => AppLog.LogTrace("Item 3 - click!"), true));
    }

    private void ShowBottomMultilineDialog()
    {
        _dialog.ShowBottomMultiline(
            "Bottom Multiline Dialog Test!",
            _toggleCanBeClosedByOutTap.isOn,
            ("Item 1", () => AppLog.LogTrace("Item 1 - click!"), false),
            ("Item 2", () => AppLog.LogTrace("Item 2 - click!"), false),
            ("Item 3", () => AppLog.LogTrace("Item 3 - click!"), true),
            ("Item 4", () => AppLog.LogTrace("Item 4 - click!"), true));
    }

    private void ShowBottomInputFieldDialog()
    {
        _dialog.ShowBottomInputField(
            "Bottom Multiline Dialog Test!",
            "Description",
            "Left", t => AppLog.LogTrace($"Item Left - click! Text: {t}"),
            "Right", t => AppLog.LogTrace($"Item Right - click! Text: {t}"));
    }
}
