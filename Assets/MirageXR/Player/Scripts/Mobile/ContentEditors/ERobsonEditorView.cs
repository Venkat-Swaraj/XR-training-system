using System;

public class ERobsonEditorView : PopupEditorBase
{
    public override ContentType editorForType => ContentType.EROBSON;

    public override void Init(Action<PopupBase> onClose, params object[] args)
    {
        base.Init(onClose, args);
        UpdateView();
    }

    private void UpdateView()
    {
    }

    protected override void OnAccept()
    {
    }
}
