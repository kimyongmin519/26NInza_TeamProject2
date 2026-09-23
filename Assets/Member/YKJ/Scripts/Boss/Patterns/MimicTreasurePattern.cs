using UnityEngine;
using KimLIb.EventSystem;
using YKJ_Script.Feedbacks;

namespace Member.YKJ.Bosses
{
    public sealed class MimicTreasurePattern : MimicPattern
    {
        protected override int DefaultSkillId => 1;
        [SerializeField, Min(1)] private int weaponCount = 15;
        [SerializeField, Min(0.01f)] private float interval = 0.5f;
        [Tooltip("Extra arc height above the higher endpoint, not the landing height.")]
        [SerializeField, Min(0.05f)] private float arcHeight = 0.75f;
        [Header("Chest Appearance")]
        [SerializeField] private SpriteRenderer chestRenderer;
        [SerializeField] private Sprite openChestSprite;
        [Header("Spit Feedback")]
        [SerializeField] private EventChannelSO feedbackChannel;
        [SerializeField] private FeedbackSO spitFeedback;
        private Sprite _previousSprite;
        private bool _spriteChanged;
        private MimicEmissionProgress _progress;
        private MimicTreasureHeightCycle _heights;
        private float _finishRemaining;

        public int EmittedCount => _progress?.Count ?? 0;
        public override bool CanStart() => Boss != null && Boss.CanEmitWeapons;

        public override void OnStart()
        {
            Boss.BodyAnimator?.PrepareTreasure(interval);
            OpenChest();
            _progress = new MimicEmissionProgress(Mathf.Max(1, weaponCount), Mathf.Max(0.01f, interval));
            _heights = new MimicTreasureHeightCycle();
            _finishRemaining = 0f;
        }

        public override void OnPause()
        {
            RestoreChest();
            Boss.BodyAnimator?.ResetPose();
        }
        public override void OnResume()
        {
            OpenChest();
            Boss.BodyAnimator?.PrepareTreasure(interval);
        }
        public override void OnEnd()
        {
            RestoreChest();
            Boss.BodyAnimator?.ResetPose();
        }
        private void OnDisable() => RestoreChest();

        private void OpenChest()
        {
            if (_spriteChanged || chestRenderer == null || openChestSprite == null)
                return;
            _previousSprite = chestRenderer.sprite;
            _spriteChanged = true;
            chestRenderer.sprite = openChestSprite;
        }

        private void RestoreChest()
        {
            if (!_spriteChanged)
                return;
            if (chestRenderer != null)
                chestRenderer.sprite = _previousSprite;
            _previousSprite = null;
            _spriteChanged = false;
        }

        public override void OnUpdate(float deltaTime)
        {
            if (_progress.Finished)
            {
                _finishRemaining -= deltaTime;
                if (_finishRemaining <= 0f)
                    EndPattern();
                return;
            }
            if (!_progress.Advance(deltaTime))
                return;

            if (Boss.EmitTreasureWeapon(_heights.Next(), arcHeight) != null)
            {
                Boss.BodyAnimator?.Spit(interval);
                if (feedbackChannel != null && spitFeedback != null)
                    feedbackChannel.RaiseEvent(new PlayFeedBack().Init(spitFeedback.FeedBackId));
                if (Boss.Patterns.Current != this)
                    return;
            }
            if (_progress.Finished)
            {
                _finishRemaining = Boss.BodyAnimator != null ? Boss.BodyAnimator.SpitDuration(interval) : 0f;
                if (_finishRemaining <= 0f)
                    EndPattern();
                return;
            }

            if (_progress.IsTongueCheckpoint)
                InterruptWith(Boss.Tongue);
        }
    }
}
