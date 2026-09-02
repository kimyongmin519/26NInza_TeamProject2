using KimLIb.EventSystem;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public enum ScreenFadeType
{
    FadeIn,
    FadeOut
}
public class FadeScreenManager : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInValue = 2.5f;
    [SerializeField] private EventChannelSO UiEventChannel;
    private readonly int _valueHash = Shader.PropertyToID("_CircleSize");

    private void Awake()
    {
        fadeImage.material = new Material(fadeImage.material);
        UiEventChannel.AddListener<SceneChangeEvent>(HandleFadeScreenEvent);

    }
    private void OnDestroy()
    {
        UiEventChannel.RemoveListener<SceneChangeEvent>(HandleFadeScreenEvent);
    }

    private void HandleFadeScreenEvent(SceneChangeEvent evt)
    {
        float fadeValue = 0, startValue = 0;
        if (evt.FadeType == ScreenFadeType.FadeIn)
        {
            startValue = 0;
            fadeValue = fadeInValue;
        }
        if (evt.FadeType == ScreenFadeType.FadeOut)
        {
            startValue = fadeInValue;
            fadeValue = 0;
        }

        StartCoroutine(FadeCorotine(startValue, fadeValue, evt.FadeDuration, evt.AfterFadeCallback));
    }

    private IEnumerator FadeCorotine(float start, float end, float Duration, Action callback)
    {
        fadeImage.material.SetFloat(_valueHash, start);
        float currentTime = 0;
        while (currentTime < Duration)
        {
            currentTime += Time.deltaTime;
            fadeImage.material.SetFloat(_valueHash, Mathf.Lerp(start, end, currentTime / Duration));
            yield return null;
        }
        callback?.Invoke();
    }
}
