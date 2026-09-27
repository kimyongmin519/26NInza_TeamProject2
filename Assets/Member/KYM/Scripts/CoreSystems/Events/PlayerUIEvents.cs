using KimLIb.EventSystem;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public class PlayerUIStateRequest : GameEvent { }

    public class PlayerUIStateEvent : GameEvent
    {
        public float Health;
        public float MaxHealth;
        public int RemainingJumps;
        public int MaxJumps;
        public float DashCharge;
        public bool DashReady;
        public bool DashRecharged;
        public bool IsDead;
        public ProjectileDataSO HeldProjectile;
    }

    public class PlayerUIPositionEvent : GameEvent
    {
        public Vector3 Position;
    }
}
