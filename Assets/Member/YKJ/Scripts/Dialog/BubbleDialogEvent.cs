using KimLIb.EventSystem;
using UnityEngine;

public static class BubbleDialogEvent
{
    public static StartBubbleDialogEvent StartBubbleDialogEvent = new StartBubbleDialogEvent();
    public static SetBubbleDialogLineEvent SetBubbleDialogLineEvent = new SetBubbleDialogLineEvent();
    public static SkipBubbleDialogLineEvent SkipBubbleDialogLineEvent = new SkipBubbleDialogLineEvent();
    public static EndBubbleDialogEvent EndBubbleDialogEvent = new EndBubbleDialogEvent();
}

public class StartBubbleDialogEvent : GameEvent
{
    public DialogDataSO DialogData;
    public Transform Target;

    public StartBubbleDialogEvent InitData(DialogDataSO dialogData, Transform target)
    {
        DialogData = dialogData;
        Target = target;
        return this;
    }
}

public class SetBubbleDialogLineEvent : GameEvent
{
    public int Id;
    public string SpeakerName;
    public Sprite SpeakeIcon;
    public string Description;
    public Transform Target;
    public System.Action TalkEndAction;

    public SetBubbleDialogLineEvent InitData(int id, string speakerName, Sprite speakeIcon, string description, Transform target, System.Action talkEndAction = null)
    {
        Id = id;
        SpeakerName = speakerName;
        SpeakeIcon = speakeIcon;
        Description = description;
        Target = target;
        TalkEndAction = talkEndAction;
        return this;
    }
}

public class SkipBubbleDialogLineEvent : GameEvent
{
}

public class EndBubbleDialogEvent : GameEvent
{
}
