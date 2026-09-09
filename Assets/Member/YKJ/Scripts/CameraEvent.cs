using KimLIb.EventSystem;
using UnityEngine;

public static class CameraEvent
{
    public static FocusCameraTargetEvent FocusCameraTargetEvent = new FocusCameraTargetEvent();
    public static ReturnDefaultCameraTargetEvent ReturnDefaultCameraTargetEvent = new ReturnDefaultCameraTargetEvent();
}

public class FocusCameraTargetEvent : GameEvent
{
    public Transform Target;

    public FocusCameraTargetEvent Init(Transform target)
    {
        Target = target;
        return this;
    }
}

public class ReturnDefaultCameraTargetEvent : GameEvent
{
}
