using KimLIb.EventSystem;
using UnityEngine;

public static class DialogEvent
{
    public static StartDialogEvent StartDialogEvent = new StartDialogEvent();
    public static SetDialogLineEvent SetDialogLineEvent = new SetDialogLineEvent();
    public static SkipDialogLineEvent SkipDialogLineEvent = new SkipDialogLineEvent();
    public static EndDialogEvent EndDialogEvent = new EndDialogEvent();
}

public class EndDialogEvent: GameEvent
{

}

public class SkipDialogLineEvent : GameEvent
{

}

public class SetDialogLineEvent : GameEvent
{
    public int Id;
    public string SpeakerName;
    public Sprite SpeakeIcon;
    public string Description;
    public System.Action TalkEndAction;
    public SetDialogLineEvent InitData(int id, string speakerName, Sprite speakeIcon, string description, System.Action talkEndAction = null)
    {
        Id = id;
        SpeakerName = speakerName;
        SpeakeIcon = speakeIcon;
        Description = description;
        TalkEndAction = talkEndAction;
        return this;
    }

    public SetDialogLineEvent InitData(string speakerName, Sprite speakeIcon, string description, System.Action talkEndAction = null)
    {
        return InitData(0, speakerName, speakeIcon, description, talkEndAction);
    }
}
public class StartDialogEvent : GameEvent
{
    public DialogDataSO dialogData;
    public StartDialogEvent InitData(DialogDataSO dialogData)
    {
        this.dialogData = dialogData;
        return this;
    }
}
