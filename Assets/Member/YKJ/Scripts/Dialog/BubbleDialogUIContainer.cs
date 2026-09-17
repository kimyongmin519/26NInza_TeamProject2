using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BubbleDialogUIContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI SpeakerText;
    [SerializeField] private ShadowPixelText DialogText;
    [SerializeField] private TextMeshProUGUI FallbackDialogText;
    [SerializeField] private DialogTextAnimator DialogTextAnimator;
    [SerializeField] private float PerCharTime = 0.05f;

    private Coroutine _typingCoroutine;
    private string _currentDialogText;
    private Action _talkEndAction;
    private bool _isTyping;

    private void Awake()
    {
        if (DialogText == null)
        {
            DialogText = GetComponentInChildren<ShadowPixelText>(true);
        }

        if (FallbackDialogText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != SpeakerText)
                {
                    FallbackDialogText = texts[i];
                    break;
                }
            }
        }

        if (DialogTextAnimator == null && DialogText != null)
        {
            DialogTextAnimator = DialogText.GetComponent<DialogTextAnimator>();
        }

        if (DialogTextAnimator == null && FallbackDialogText != null)
        {
            DialogTextAnimator = FallbackDialogText.GetComponent<DialogTextAnimator>();
        }

        if (DialogTextAnimator == null && DialogText != null)
        {
            DialogTextAnimator = DialogText.gameObject.AddComponent<DialogTextAnimator>();
        }

        if (DialogTextAnimator == null && FallbackDialogText != null)
        {
            DialogTextAnimator = FallbackDialogText.gameObject.AddComponent<DialogTextAnimator>();
        }
    }

    public void Set(string speakerText, string dialogText, Action talkEndAction = null)
    {
        SetSpeakerText(speakerText);
        SetDialogText(dialogText, talkEndAction);
    }

    public void Set(DiglogData dialogData, Action talkEndAction = null)
    {
        Set(dialogData.SpeakerName, dialogData.Description, talkEndAction);
    }

    public void SetSpeakerText(string text)
    {
        if (SpeakerText != null)
        {
            SpeakerText.text = text;
        }
    }

    public void SetDialogText(string text, Action talkEndAction = null)
    {
        _currentDialogText = DialogTextAnimator != null ? DialogTextAnimator.SetText(text) : text;
        _talkEndAction = talkEndAction;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _typingCoroutine = StartCoroutine(TypingCoroutine());
    }

    public void SkipText()
    {
        if (_isTyping == false)
        {
            return;
        }

        CompleteTyping();
    }

    public void Clear()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _currentDialogText = string.Empty;
        _talkEndAction = null;
        _isTyping = false;
        SetSpeakerText(string.Empty);
        SetText(string.Empty);
        SetVisibleCharacters(0);
    }

    private IEnumerator TypingCoroutine()
    {
        _isTyping = true;
        SetText(_currentDialogText);
        SetVisibleCharacters(0);

        for (int i = 0; i < _currentDialogText.Length; i++)
        {
            float waitTime = DialogTextAnimator != null ? DialogTextAnimator.GetWait(i) : 0f;
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            SetVisibleCharacters(i + 1);

            float delay = DialogTextAnimator != null ? DialogTextAnimator.GetDelay(i, PerCharTime) : PerCharTime;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        _typingCoroutine = null;
        CompleteTyping();
    }

    private void CompleteTyping()
    {
        if (_isTyping == false)
        {
            return;
        }

        Coroutine typingCoroutine = _typingCoroutine;
        _typingCoroutine = null;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        SetText(_currentDialogText);
        SetVisibleCharacters(_currentDialogText.Length);
        _isTyping = false;
        _talkEndAction?.Invoke();
        _talkEndAction = null;
    }

    private void SetText(string text)
    {
        if (DialogText != null)
        {
            DialogText.SetText(text);
            return;
        }

        if (FallbackDialogText != null)
        {
            FallbackDialogText.text = text;
        }
    }

    private void SetVisibleCharacters(int count)
    {
        if (DialogTextAnimator != null)
        {
            DialogTextAnimator.SetMaxVisibleCharacters(count);
            return;
        }

        if (DialogText != null)
        {
            DialogText.SetMaxVisibleCharacters(count);
            return;
        }

        if (FallbackDialogText != null)
        {
            FallbackDialogText.maxVisibleCharacters = count;
        }
    }
}
