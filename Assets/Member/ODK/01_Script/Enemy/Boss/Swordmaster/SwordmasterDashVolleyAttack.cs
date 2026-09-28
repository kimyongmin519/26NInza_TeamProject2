using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterDashVolleyAttack : SwordmasterSkill
    {
        [SerializeField] private float teleportEdgePadding = 1.4f;
        [SerializeField] private float readyDuration = 0.8f;
        [SerializeField] private float dashDuration = 1.25f;
        [SerializeField] private float dashDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float dashKnockback = 2f;
        [SerializeField] private Vector2 dashHitbox = new Vector2(1.4f, 2.6f);
        [SerializeField] private float swordSpeed = 11f;
        [SerializeField] private float swordInterval = 0.2f;
        [SerializeField, Min(0)] private int volleyCount = 3;
        [SerializeField] private float dashDistanceScale = 1.3f;
        [SerializeField] private float swordFlightTime = 1.6f;

        [Header("Warning")]
        [SerializeField, Min(0.01f)] private float warningLineWidth = 0.16f;

        private DamageCaster dashCaster;
        private Tween dashTween;
        private SwordmasterTelegraph dashTelegraph;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override void OnSwordmasterInitialize()
        {
            GameObject casterObject = new GameObject("Dash Damage Caster");
            casterObject.transform.SetParent(transform, false);
            dashCaster = casterObject.AddComponent<DamageCaster>();
            dashCaster.ConfigureBox(dashHitbox, Boss.PlayerLayer);
        }

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            bool fromLeft = Random.value < 0.5f;
            float x = Boss.ArenaCenter.x + (fromLeft ? -1f : 1f) *
                Mathf.Max(1f, Boss.ArenaHalfWidth - teleportEdgePadding);
            Vector3 teleportPoint = Boss.GetGroundPoint(x);
            teleportPoint.z = Boss.transform.position.z;
            Boss.Teleport(teleportPoint);
            Boss.AttackReady(teleportPoint);
            Boss.PlayAnimation(Swordmaster.JumpState);
            Boss.Cue(SwordmasterCue.DashReady, teleportPoint);

            yield return new WaitForSeconds(readyDuration / DurationScale);

            Vector2 direction = (Boss.Target.position - Boss.transform.position).normalized;
            Vector3 moveEnd = Boss.Arena != null
                ? Boss.Arena.Clamp(Boss.transform.position + (Vector3)direction * Boss.ArenaHalfWidth * dashDistanceScale, 1f)
                : Boss.transform.position + (Vector3)direction * 16f;
            moveEnd.z = Boss.transform.position.z;
            List<EnchantedSword> volley = volleyCount > 0 ? Boss.TakeSwords(volleyCount) : new List<EnchantedSword>();

            Boss.PlayAnimation(Swordmaster.RunState);
            Boss.Cue(SwordmasterCue.DashStart, Boss.transform.position);
            dashTween = Boss.transform.DOMove(moveEnd, dashDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .SetTarget(Boss.transform);

            yield return new WaitForSeconds(dashDuration * 0.25f / DurationScale);
            Boss.PlayAnimation(Swordmaster.Attack1State);
            foreach (EnchantedSword sword in volley)
            {
                if (sword == null || Boss.Target == null) continue;
                Vector2 aim = (Boss.Target.position - sword.transform.position).normalized;
                sword.FireMagic(aim, swordSpeed, swordFlightTime);
                yield return new WaitForSeconds(swordInterval / DurationScale);
            }

            if (dashTween != null && dashTween.IsActive()) yield return dashTween.WaitForCompletion();
            Boss.PlayAnimation(Swordmaster.IdleState);
            Boss.Cue(SwordmasterCue.DashEnd, Boss.transform.position);
        }

        protected override void OnSwordmasterCancel()
        {
            dashTween?.Kill();
            dashCaster?.DisableCasting();
            dashCaster?.ClearWorldPose();
            Boss?.ReleaseTelegraph(dashTelegraph);
            dashTelegraph = null;
        }

        private void OnDrawGizmosSelected()
        {
            Swordmaster boss = GetComponentInParent<Swordmaster>();
            if (boss == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.25f, 0.85f);
            Gizmos.DrawWireCube(boss.transform.position, dashHitbox);
            Gizmos.DrawLine(
                boss.ArenaCenter + Vector3.left * (boss.ArenaHalfWidth - teleportEdgePadding),
                boss.ArenaCenter + Vector3.right * (boss.ArenaHalfWidth - teleportEdgePadding)
            );
        }
    }
}
