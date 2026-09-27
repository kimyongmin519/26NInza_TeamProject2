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
        [SerializeField, Min(0.01f)] private float anticipationTime = 0.14f;
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
        private bool _preparing;
        private bool _pendingTongue;

        public int EmittedCount => _progress?.Count ?? 0;
        public override bool CanStart() => Boss != null && Boss.CanEmitWeapons;

        public override void OnStart()
        {
            RestoreChest();
            _progress = new MimicEmissionProgress(Mathf.Max(1, weaponCount), Mathf.Max(0.01f, interval));
            _heights = new MimicTreasureHeightCycle();
            _finishRemaining = 0f;
            _preparing = _pendingTongue = false;
        }

        public override void OnPause()
        {
            RestoreChest();
            Boss.BodyAnimator?.ResetPose();
        }
        public override void OnResume()
        {
            RestoreChest();
            _preparing = false;
        }
        public override void OnEnd()
        {
            RestoreChest();
            Boss.BodyAnimator?.ResetPose();
        }
        private void OnDisable()
        {
            RestoreChest();
            Boss?.BodyAnimator?.ResetPose();
        }

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
            Boss?.BodyAnimator?.ResetPose();
        }

        public override void OnUpdate(float deltaTime)
        {
            if (_progress.Finished || _pendingTongue)
            {
                _finishRemaining -= deltaTime;
                if (_finishRemaining <= 0f)
                {
                    if (_pendingTongue)
                    {
                        _pendingTongue = false;
                        InterruptWith(Boss.Tongue);
                    }
                    else EndPattern();
                }
                return;
            }
            _finishRemaining -= deltaTime;
            if (_finishRemaining <= 0f) RestoreChest();
            float preparation = Mathf.Min(anticipationTime, interval * 0.45f);
            if (!_preparing && _progress.TimeUntilEmission <= preparation + deltaTime)
            {
                _preparing = true;
                RestoreChest();
                Boss.BodyAnimator?.PrepareWeaponSpit(Mathf.Min(preparation, _progress.TimeUntilEmission));
            }
            if (!_progress.Advance(deltaTime))
                return;

            OpenChest();
            MimicWeapon weapon = Boss.EmitTreasureWeapon(_heights.Next(), arcHeight);
            _preparing = false;
            _finishRemaining = Boss.BodyAnimator != null ? Boss.BodyAnimator.SpitDuration(interval) : 0.18f;
            if (weapon != null)
            {
                Boss.BodyAnimator?.SpitWeapon(weapon.GetComponent<Rigidbody2D>().linearVelocity, interval);
                if (feedbackChannel != null && spitFeedback != null)
                    feedbackChannel.RaiseEvent(new PlayFeedBack().Init(spitFeedback.FeedBackId));
                if (Boss.Patterns.Current != this)
                    return;
            }
            if (_progress.Finished)
            {
                if (_finishRemaining <= 0f)
                    EndPattern();
                return;
            }

            if (_progress.IsTongueCheckpoint)
                _pendingTongue = true;
        }
    }
}
