using UnityEngine;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.Projectiles;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public interface IGrabbable
    {
        bool CanBeGrabbed { get; }
        Transform GrabTransform { get; }

        void Grab(Transform grabPoint, GameObject grabber);
        void Release();
        void Throw(ThrowData throwData);
    }

    // 잡기 자체와 별개로, 플레이어의 패리 연출에 해당하는 적 공격인지 알려준다.
    public interface IEnemyAttackGrabbable
    {
        bool IsEnemyAttackFrom(ModuleOwner grabber);
    }

    // 손에 든 물체 UI에 표시할 선택적 데이터다.
    public interface IGrabbableDisplayData
    {
        ProjectileDataSO DisplayData { get; }
    }
}
