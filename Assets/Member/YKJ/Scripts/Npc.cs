using KimLIb.EventSystem;
using Member.KYM.Scripts.Agents;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Npc : MonoBehaviour, Interactable
{
    [SerializeField] private EventChannelSO UiEventChannel;
    [SerializeField] private EventChannelSO DialogEventChannel;
    [SerializeField] private DialogDataSO dialogData;
    public void Interaction(Agent player)
    {
        Debug.Log("상호 작용 완료");
        //  UiEventChannel.RaiseEvent(UiEvent.SceneChangeEvent.InitData(ScreenFadeType.FadeOut, 0.75f, () => SceneManager.LoadScene("TestScene2")));
        if (DialogManager.Talking == false)
            DialogEventChannel.RaiseEvent(DialogEvent.StartDialogEvent.InitData(dialogData));
    }
}
