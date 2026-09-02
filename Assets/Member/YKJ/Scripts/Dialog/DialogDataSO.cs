using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "DialogSO", menuName = "Dialog/DialogSO")]
public class DialogDataSO : ScriptableObject
{
    public List<InitDialogData> DialogInitList;
    public List<DiglogData> DialogList;
}
[Serializable]
public class InitDialogData
{
    public int Id;
    public string SpeakerName;
    public Sprite SpeakeIcon;
}
[Serializable]
public class DiglogData
{
    [HideInInspector] public int Id;
    public string SpeakerName;
    public Sprite SpeakeIcon;
    public string Description;
    //추후 이벤트 SO가 생길수도 있음
}
