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
        [SerializeField] private float dashDamage = 16f;
        [SerializeField] private float dashKnockback = 2f;
        [SerializeField] private Vector2 dashHitbox = new Vector2(1.4f, 2.6f);
        [SerializeField] private float swordSpeed = 11f;
        [SerializeField] private float swordInterval = 0.2f;
        [SerializeField, Min(0)] private int volleyCount = 1;
        [SerializeField] private float dashDistanceScale = 1.3f;
        [SerializeField] private float swordFlightTime = 1.6f;

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

            Vector2 direction = (Boss.Target.position - Boss.transform.position).normalized;
            Vector3 dashEnd = Boss.Arena != null
                ? Boss.Arena.Clamp(Boss.transform.position + (Vector3)direction * Boss.ArenaHalfWidth * dashDistanceScale, 1f)
                : Boss.transform.position + (Vector3)direction * 16f;
            dashTelegraph = Boss.SpawnTelegraph();
            Vector3 lineOffset = Vector3.up * Boss.GetHitCenter().y - Vector3.up * Boss.transform.position.y;
            dashTelegraph?.Show(Boss.transform.position + lineOffset, dashEnd + lineOffset, readyDuration / DurationScale);
            yield return new WaitForSeconds(readyDuration / DurationScale);
            Boss.ReleaseTelegraph(dashTelegraph);
            dashTelegraph = null;

            List<EnchantedSword> volley = volleyCount > 0 ? Boss.TakeSwords(volleyCount) : new List<EnchantedSword>();

            dashCaster.EnableCasting(
                new DamageData(dashDamage, DamageType.Melee, knockbackForce: direction * dashKnockback),
                dashDuration / DurationScale
            );
            Boss.PlayAnimation(Swordmaster.RunState);
            Boss.Cue(SwordmasterCue.DashStart, Boss.transform.position);
            float progress = 0f;
            Vector3 start = Boss.transform.position;
            dashTween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Boss.transform.position = Vector3.Lerp(start, dashEnd, value);
                }, 1f, dashDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .SetTarget(Boss.transform);

            foreach (EnchantedSword sword in volley)
            {
                if (sword == null) continue;
                Vector2 aim = (Boss.Target.position - sword.transform.position).normalized;
                sword.FireMagic(aim, swordSpeed, swordFlightTime);
                yield return new WaitForSeconds(swordInterval / DurationScale);
            }

            yield return dashTween.WaitForCompletion();
            dashCaster.DisableCasting();
            Boss.AttackImpact(Boss.transform.position);
            Boss.PlayAnimation(Swordmaster.Attack1State);
            Boss.Cue(SwordmasterCue.DashEnd, Boss.transform.position);
        }

        protected override void OnSwordmasterCancel()
        {
            dashTween?.Kill();
            dashCaster?.DisableCasting();
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
