using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;

public class FristLoader : MonoBehaviour
{
    [SerializeField] private EventChannelSO UiEventChannel;
    private IEnumerator Start()
    {
        UiEventChannel.RaiseEvent(UiEvent.SceneChangeEvent.InitData(ScreenFadeType.FadeIn, 0.75f, default));
        yield return null;
        UiEventChannel.RaiseEvent(QuestEvent.GameStarted);
    }
}
