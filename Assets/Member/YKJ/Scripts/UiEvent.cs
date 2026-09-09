using KimLIb.EventSystem;
using KimLIb.SoundSystem;
using System;
using UnityEngine;

public class UiEvent
{
    public static readonly SceneChangeEvent SceneChangeEvent = new SceneChangeEvent();
    public static readonly HighlightEvent HighlightEvent = new HighlightEvent();
}

public class HighlightEvent : GameEvent
{
    public bool IsShow;

    public HighlightEvent Init(bool isShow)
    {
        IsShow = isShow;
        return this;
    }
}

public class SceneChangeEvent : GameEvent
{

    public ScreenFadeType FadeType;
    public float FadeDuration;
    public Action AfterFadeCallback;

    public SceneChangeEvent InitData(ScreenFadeType screenFadeType, float fadeDuration, Action afterFadeCallback)
    {
        FadeType = screenFadeType;
        FadeDuration = fadeDuration;
        AfterFadeCallback = afterFadeCallback;

        return this;
    }
}
