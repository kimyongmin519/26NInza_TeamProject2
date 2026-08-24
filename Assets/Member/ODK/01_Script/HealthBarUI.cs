using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public interface IHealthModuleExtra
{
    
    public void Setting(HealthModule module);
}
public class HealthBarUI : MonoBehaviour, IHealthModuleExtra
{
    public Transform barObject;
    public Image barHightLight;
    public Image barFill;

    public Sequence currentSequence;
    HealthModule healthModule;
    public void ChangeHealthBar(float cur, float prev)
    {
        currentSequence?.Kill();
        float mag = Mathf.Abs(prev - cur);
        currentSequence.Append(barObject.DOShakePosition(0.4f, mag / 10, 10, 90, false, true, ShakeRandomnessMode.Full));
        currentSequence.Append(barObject.DOShakeRotation(0.4f,mag / 10));


    }

    public void Setting(HealthModule module)
    {
        healthModule = module;

    }
}
