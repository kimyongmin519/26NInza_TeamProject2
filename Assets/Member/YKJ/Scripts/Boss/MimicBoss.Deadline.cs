using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.Players;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Member.YKJ.Bosses
{
    public sealed partial class MimicBoss
    {
        [Header("Phase Two Deadline")]
        [SerializeField, Min(1f)] private float encounterTimeLimit = 60f;
        [SerializeField, Min(0.1f)] private float explosionChargeDuration = 3f;
        [SerializeField, Min(1f)] private float explosionChestScale = 2.5f;
        [SerializeField, Min(1f)] private float timeoutExplosionScale = 6f;
        [SerializeField, Range(0f, 1f)] private float deadlineVignetteIntensity = 0.55f;

        private float _encounterElapsed;
        private float _explosionElapsed;
        private bool _timeoutStarted;
        private bool _timeoutExploded;
        private PlayerController _deadlinePlayer;
        private HealthModule _deadlineHealth;
        private PlayerInputSO _lockedInput;
        private bool[] _previousInputLocks;
        private IMover _lockedMover;
        private bool _previousManualMovement;
        private Volume _deadlineVolume;
        private VolumeProfile _deadlineProfile;
        private Vignette _deadlineVignette;
        private UniversalAdditionalCameraData _deadlineCamera;
        private bool _previousPostProcessing;

        public float EncounterTimeRemaining => Mathf.Max(0f, encounterTimeLimit - _encounterElapsed);
        public bool IsTimeoutSequenceActive => _timeoutStarted;

        private void BeginDeadline()
        {
            ClearDeadline();
            _encounterElapsed = 0f;
            _explosionElapsed = 0f;
            _timeoutStarted = false;
            _timeoutExploded = false;
            _deadlinePlayer = target != null ? target.GetComponentInParent<PlayerController>() : null;
            _deadlineHealth = _deadlinePlayer != null ? _deadlinePlayer.HealthModule :
                target != null ? target.GetComponentInParent<HealthModule>() : null;

            Camera view = boomCamera != null ? boomCamera : Camera.main;
            if (view == null) return;
            _deadlineCamera = view.GetUniversalAdditionalCameraData();
            _previousPostProcessing = _deadlineCamera.renderPostProcessing;
            _deadlineCamera.renderPostProcessing = true;
            int mask = _deadlineCamera.volumeLayerMask.value;
            if (mask == 0)
            {
                Debug.LogWarning("Mimic deadline vignette needs a camera Volume Layer Mask.", this);
                return;
            }
            int layer = 0;
            while ((mask & (1 << layer)) == 0) layer++;
            var volumeObject = new GameObject("Mimic Deadline Vignette");
            volumeObject.transform.SetParent(transform, false);
            volumeObject.layer = layer;
            _deadlineVolume = volumeObject.AddComponent<Volume>();
            _deadlineVolume.isGlobal = true;
            _deadlineVolume.priority = 10000f;
            _deadlineVolume.weight = 0f;
            _deadlineProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _deadlineVignette = _deadlineProfile.Add<Vignette>(true);
            _deadlineVignette.color.Override(new Color(0.85f, 0f, 0f, 1f));
            _deadlineVignette.intensity.Override(deadlineVignetteIntensity);
            _deadlineVignette.smoothness.Override(0.65f);
            _deadlineVolume.sharedProfile = _deadlineProfile;
        }

        // Starts after the coin transition. Pausing or reading dialogue does not consume the deadline.
        private bool TickDeadline(float deltaTime)
        {
            if (!_encounterActive || _timeoutExploded) return true;
            if (Phase != EncounterPhase.PhaseTwo) return false;
            if (_deadlineHealth != null && _deadlineHealth.IsDead)
            {
                StopEncounter();
                return true;
            }
            if (_timeoutStarted)
            {
                if (_timeoutExploded) return true;
                _lockedInput?.AllInputLock(true);
                _lockedMover?.StopImmediately(true, true);
                _explosionElapsed += deltaTime;
                float progress = Mathf.Clamp01(_explosionElapsed / Mathf.Max(0.1f, explosionChargeDuration));
                bodyAnimator?.SetExplosionCharge(progress, explosionChestScale);
                if (progress >= 1f) ExplodeDeadline();
                return true;
            }
            if (!DialogManager.Talking && !BubbleDialogManager.Talking)
                _encounterElapsed += Mathf.Max(0f, deltaTime);
            if (_deadlineVolume != null)
                _deadlineVolume.weight = Mathf.Clamp01(_encounterElapsed / Mathf.Max(1f, encounterTimeLimit));
            if (_encounterElapsed < encounterTimeLimit) return false;

            _timeoutStarted = true;
            Patterns.Cancel();
            ClearBooms();
            ClearWeapons();
            combatVfx?.Clear();
            bodyAnimator?.ResetPose();
            if (_deadlinePlayer != null)
            {
                var skill = _deadlinePlayer.SkillModule?.GetCurrentSkill();
                if (skill is { IsUsing: true }) skill.StopSkill();
                _deadlinePlayer.GetComponentInChildren<Member.KYM.Scripts.Players.RobotArm.RobotArmGrappler>()?.StopGrapple();
                _lockedInput = _deadlinePlayer.PlayerInput;
                if (_lockedInput != null)
                {
                    _previousInputLocks = new bool[(int)global::LockKey.END];
                    for (int i = 0; i < _previousInputLocks.Length; i++)
                        _previousInputLocks[i] = _lockedInput.IsInputLocked((global::LockKey)i);
                    _lockedInput.AllInputLock(true);
                }
                _lockedMover = _deadlinePlayer.GetModule<IMover>();
                if (_lockedMover != null)
                {
                    _previousManualMovement = _lockedMover.CanManualMovement;
                    _lockedMover.CanManualMovement = false;
                    _lockedMover.StopImmediately(true, true);
                }
            }
            return true;
        }

        private void ExplodeDeadline()
        {
            // Latch before damage: player death listeners may disable this encounter synchronously.
            _timeoutExploded = true;
            _encounterActive = false;
            StopEncounterAudio();
            PlaySound(explosionSound);
            bodyAnimator?.HideAfterExplosion();
            if (boomPrefab != null)
            {
                ParticleSystem explosion = Instantiate(boomPrefab, MouthPosition, boomPrefab.transform.rotation);
                explosion.transform.localScale *= timeoutExplosionScale;
                foreach (ParticleSystemRenderer renderer in explosion.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    renderer.sortingLayerName = "Weapon";
                    renderer.sortingOrder = 100;
                }
                explosion.Play(true);
                Destroy(explosion.gameObject, 10f);
            }
            else combatVfx?.DeathBurst();

            if (_deadlineHealth != null && !_deadlineHealth.IsDead)
            {
                // Timeout is a scripted loss, bypassing hit/dash invulnerability and damage conversion.
                _deadlineHealth.SettingInvisibleTime(0f);
                _deadlineHealth.ApplyDamage(new DamageData(100f, DamageType.Special));
                if (!_deadlineHealth.IsDead) _deadlineHealth.ActiveDeath();
            }
        }

        private void ClearDeadline()
        {
            bool playerDead = _deadlineHealth != null && _deadlineHealth.IsDead;
            if (!playerDead)
            {
                if (_lockedInput != null && _previousInputLocks != null)
                    for (int i = 0; i < _previousInputLocks.Length; i++)
                        _lockedInput.LockInput((global::LockKey)i, _previousInputLocks[i]);
                if (_lockedMover != null) _lockedMover.CanManualMovement = _previousManualMovement;
            }
            _lockedInput = null;
            _previousInputLocks = null;
            _lockedMover = null;
            if (_deadlineVolume != null)
            {
                _deadlineVolume.enabled = false;
                Destroy(_deadlineVolume.gameObject);
            }
            if (_deadlineVignette != null) Destroy(_deadlineVignette);
            if (_deadlineProfile != null) Destroy(_deadlineProfile);
            if (_deadlineCamera != null) _deadlineCamera.renderPostProcessing = _previousPostProcessing;
            _deadlineVolume = null;
            _deadlineVignette = null;
            _deadlineProfile = null;
            _deadlineCamera = null;
        }
    }
}
