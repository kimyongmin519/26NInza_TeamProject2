using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogUIContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ActorText;
    [SerializeField] private ShadowPixelText DialogText;
    [SerializeField] private TextMeshProUGUI FallbackDialogText;
    [SerializeField] private DialogTextAnimator DialogTextAnimator;
    [SerializeField] private List<Image> ActorIcons;
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
                if (texts[i] != ActorText)
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

    public void Set(int actorIndex, string actorText, string dialogText, Sprite actorBackGround, Action talkEndAction = null)
    {
        SetActorText(actorText);
        SetDialogText(dialogText, talkEndAction);
        SetActorIcon(actorIndex, actorBackGround);
    }

    public void Set(string actorText, string dialogText, Sprite actorBackGround, Action talkEndAction = null) => Set(0, actorText, dialogText, actorBackGround, talkEndAction);
    public void Set(DiglogData dialogData) => Set(dialogData.Id, dialogData.SpeakerName, dialogData.Description, dialogData.SpeakeIcon);
    public void SetActorIcon(int index, Sprite sprite)
    {
        if (ActorIcons == null || ActorIcons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < ActorIcons.Count; i++)
        {
            bool isCurrentActor = i == index;
            ActorIcons[i].gameObject.SetActive(isCurrentActor);

            if (isCurrentActor)
            {
                ActorIcons[i].sprite = sprite;
            }
        }
    }

    public void SetActorText(string text)
    {
        if (ActorText != null)
        {
            ActorText.text = text;
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
        SetActorText(string.Empty);
        SetText(string.Empty);
        SetVisibleCharacters(0);
        SetAllActorIconActive(false);
    }

    private void SetAllActorIconActive(bool isActive)
    {
        if (ActorIcons == null)
        {
            return;
        }

        for (int i = 0; i < ActorIcons.Count; i++)
        {
            ActorIcons[i].gameObject.SetActive(isActive);
        }
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
