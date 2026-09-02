using KimLIb.EventSystem;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEngine;

public class FristLoader : MonoBehaviour
{
    [SerializeField] private EventChannelSO UiEventChannel;
    private void Start()
    {
        UiEventChannel.RaiseEvent(UiEvent.SceneChangeEvent.InitData(ScreenFadeType.FadeIn, 0.75f, default));
    }
}
