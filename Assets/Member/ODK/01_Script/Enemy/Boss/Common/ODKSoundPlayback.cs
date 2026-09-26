using KimLIb.SoundSystem;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public static class ODKSoundPlayback
    {
        private static SoundManager soundManager;

        public static bool Play(SoundClipSO clip, Vector3 position)
        {
            if (clip == null || clip.audioClip == null) return false;

            if (soundManager == null)
                soundManager = Object.FindFirstObjectByType<SoundManager>();

            if (soundManager == null || soundManager.SoundChannel == null) return false;

            soundManager.SoundChannel.RaiseEvent(
                new PlaySoundEvent().InitData(position, clip)
            );
            return true;
        }

        public static void ClearCache()
        {
            soundManager = null;
        }
    }
}
