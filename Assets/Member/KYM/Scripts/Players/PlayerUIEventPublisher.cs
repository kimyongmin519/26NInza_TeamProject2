using KimLIb.EventSystem;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.Players.RobotArm;
using Member.KYM.Scripts.Players.Skills;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    // 플레이어 정보 수집은 여기서만 담당하고 UI에는 값만 전달한다.
    public sealed class PlayerUIEventPublisher
    {
        private readonly PlayerController _player;
        private readonly EventChannelSO _channel;
        private readonly PlayerDashSkill _dash;
        private readonly RobotArmGrabber _grabber;
        private PlayerUIStateEvent _previous;
        private PlayerUIStateEvent _sample = new PlayerUIStateEvent();
        private readonly PlayerUIPositionEvent _position = new PlayerUIPositionEvent();

        public PlayerUIEventPublisher(PlayerController player)
        {
            _player = player;
            _channel = player.UIChannel;
            _dash = player.GetComponentInChildren<PlayerDashSkill>(true);
            _grabber = player.GetComponentInChildren<RobotArmGrabber>(true);
            if (_channel != null)
                _channel.AddListener<PlayerUIStateRequest>(HandleRequest);
        }

        public void Publish()
        {
            if (_channel == null)
                return;

            PlayerUIStateEvent state = ReadState(_sample);
            bool changed = _previous == null ||
                state.Health != _previous.Health || state.MaxHealth != _previous.MaxHealth ||
                state.RemainingJumps != _previous.RemainingJumps || state.MaxJumps != _previous.MaxJumps ||
                state.DashCharge != _previous.DashCharge || state.DashReady != _previous.DashReady ||
                state.IsDead != _previous.IsDead || !ReferenceEquals(state.HeldObject, _previous.HeldObject) ||
                state.HeldProjectile != _previous.HeldProjectile || state.HeldIcon != _previous.HeldIcon ||
                state.HeldName != _previous.HeldName;
            if (changed)
            {
                state.DashRecharged = _previous != null && !_previous.DashReady && state.DashReady;
                _sample = _previous ?? new PlayerUIStateEvent();
                _previous = state;
                _channel.RaiseEvent(state);
            }

            _position.Position = _player.transform.position;
            _channel.RaiseEvent(_position);
        }

        private PlayerUIStateEvent ReadState(PlayerUIStateEvent state)
        {
            IGrabbable held = _grabber != null ? _grabber.HeldObject : null;
            Component heldComponent = held as Component;
            ProjectileDataSO heldData = (held as AbstractProjectile)?.ProjectileData;
            if (heldData == null && heldComponent != null)
                heldData = heldComponent.GetComponent<IGrabbableDisplayData>()?.DisplayData;
            SpriteRenderer heldRenderer = heldComponent != null &&
                                          (heldData == null || heldData.Icon == null)
                ? heldComponent.GetComponentInChildren<SpriteRenderer>(true)
                : null;
            float charge = _dash != null
                ? Mathf.Min(_dash.NormalizedRecharge, _dash.NormalizedCooldown) : 0f;
            state.Health = _player.HealthModule != null ? _player.HealthModule.CurrentHealth : 0f;
            state.MaxHealth = _player.HealthModule != null ? _player.HealthModule.MaxHealth : 0f;
            state.IsDead = _player.HealthModule != null && _player.HealthModule.IsDead;
            state.RemainingJumps = _player.RemainingJumpCount;
            state.MaxJumps = _player.MaxJumpCount;
            state.DashCharge = charge;
            state.DashReady = _dash != null && charge >= 1f;
            state.DashRecharged = false;
            state.HeldObject = held;
            state.HeldProjectile = heldData;
            state.HeldIcon = heldData != null && heldData.Icon != null
                ? heldData.Icon : heldRenderer != null ? heldRenderer.sprite : null;
            state.HeldName = heldData != null && !string.IsNullOrWhiteSpace(heldData.Name)
                ? heldData.Name : heldComponent != null
                ? heldComponent.gameObject.name.Replace("(Clone)", string.Empty).Trim()
                : null;
            return state;
        }

        private void HandleRequest(PlayerUIStateRequest request)
        {
            // 동기화 응답은 재충전 연출을 다시 발생시키지 않는다.
            _channel.RaiseEvent(ReadState(new PlayerUIStateEvent()));
            _position.Position = _player.transform.position;
            _channel.RaiseEvent(_position);
        }

        public void Dispose()
        {
            if (_channel != null)
                _channel.RemoveListener<PlayerUIStateRequest>(HandleRequest);
        }
    }
}
