using KimLIb.EventSystem;
using UnityEngine;

public static class InputEvent
{
    public static LockInputEvent LockInputEvent = new LockInputEvent();
    public static LockInputAllEvent LockInputAllEvent = new LockInputAllEvent();
}
public class LockInputEvent : GameEvent
{
    public LockKey LockKey { get; set; }
    public bool IsLocked { get; set; }

    public LockInputEvent Init(LockKey lockKey, bool isLocked)
    {
        LockKey = lockKey;
        IsLocked = isLocked;
        return this;
    }
}
public class LockInputAllEvent : GameEvent
{
    public bool IsLocked { get; set; }

    public LockInputAllEvent Init(bool isLocked)
    {
        IsLocked = isLocked;
        return this;
    }
}