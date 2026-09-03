using UnityEngine;

public class VolcanusFeedback : MonoBehaviour
{
    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip readyClip;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private AudioClip laserClip;
    [SerializeField] private AudioClip missileClip;
    [SerializeField] private AudioClip phaseClip;
    [SerializeField] private AudioClip damageClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

    public void PlayReady() => Play(readyClip);
    public void PlayImpact() => Play(impactClip);
    public void PlayLaser() => Play(laserClip);
    public void PlayMissile() => Play(missileClip);
    public void PlayPhase() => Play(phaseClip);
    public void PlayDamage() => Play(damageClip);

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
