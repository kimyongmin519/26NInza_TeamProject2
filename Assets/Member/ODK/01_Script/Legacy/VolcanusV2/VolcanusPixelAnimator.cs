using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public sealed class VolcanusPixelAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] skillOneFrames;
        [SerializeField] private Sprite[] skillTwoFrames;
        [SerializeField, Min(1f)] private float idleFrameRate = 8f;
        [SerializeField, Min(1f)] private float attackFrameRate = 12f;

        private Sprite[] currentFrames;
        private float frameDuration;
        private float elapsed;
        private int frameIndex;
        private bool loop;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            PlayDefault();
        }

        private void OnEnable() => PlayDefault();

        private void Update()
        {
            if (spriteRenderer == null || currentFrames == null || currentFrames.Length <= 1) return;

            elapsed += Time.deltaTime;
            if (elapsed < frameDuration) return;

            int step = Mathf.FloorToInt(elapsed / frameDuration);
            elapsed -= step * frameDuration;
            frameIndex += step;

            if (loop)
                frameIndex %= currentFrames.Length;
            else
                frameIndex = Mathf.Min(frameIndex, currentFrames.Length - 1);

            spriteRenderer.sprite = currentFrames[frameIndex];
        }

        public bool Play(string stateName)
        {
            Sprite[] frames = stateName switch
            {
                "skill_1" => skillOneFrames,
                "skill_2" => skillTwoFrames,
                "idle_2" => skillTwoFrames,
                "IDLE" => idleFrames,
                _ => null
            };

            if (frames == null || frames.Length == 0) return false;
            Begin(frames, stateName == "IDLE", stateName == "IDLE" ? idleFrameRate : attackFrameRate);
            return true;
        }

        public void PlayDefault()
        {
            if (idleFrames == null || idleFrames.Length == 0) return;
            Begin(idleFrames, true, idleFrameRate);
        }

        public void Stop()
        {
            currentFrames = null;
            elapsed = 0f;
        }

        private void Begin(Sprite[] frames, bool shouldLoop, float frameRate)
        {
            currentFrames = frames;
            loop = shouldLoop;
            frameDuration = 1f / Mathf.Max(1f, frameRate);
            frameIndex = 0;
            elapsed = 0f;
            if (spriteRenderer != null) spriteRenderer.sprite = frames[0];
        }
    }
}
