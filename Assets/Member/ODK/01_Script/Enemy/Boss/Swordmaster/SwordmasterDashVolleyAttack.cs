using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterDashVolleyAttack : SwordmasterSkill
    {
        [SerializeField] private float teleportEdgePadding = 2.2f;
        [SerializeField] private float readyDuration = 0.35f;
        [SerializeField] private float dashDuration = 0.8f;
        [SerializeField] private float dashDamage = 38f;
        [SerializeField] private Vector2 dashHitbox = new Vector2(2.2f, 3.2f);
        [SerializeField] private float swordSpeed = 18f;
        [SerializeField] private float swordInterval = 0.09f;

        private DamageCaster dashCaster;
        private Tween dashTween;

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
            yield return new WaitForSeconds(readyDuration / DurationScale);

            Vector2 direction = (Boss.Target.position - Boss.transform.position).normalized;
            Vector3 dashEnd = Boss.Arena != null
                ? Boss.Arena.Clamp(Boss.transform.position + (Vector3)direction * Boss.ArenaHalfWidth * 1.7f, 1f)
                : Boss.transform.position + (Vector3)direction * 16f;
            List<EnchantedSword> volley = Boss.TakeSwords(3);

            dashCaster.EnableCasting(
                new DamageData(dashDamage, DamageType.Melee, knockbackForce: direction * 5f),
                dashDuration / DurationScale
            );
            float progress = 0f;
            Vector3 start = Boss.transform.position;
            dashTween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Boss.transform.position = Vector3.Lerp(start, dashEnd, value * value);
                }, 1f, dashDuration / DurationScale)
                .SetEase(Ease.InQuad)
                .SetTarget(Boss.transform);

            foreach (EnchantedSword sword in volley)
            {
                if (sword == null) continue;
                Vector2 aim = (Boss.Target.position - sword.transform.position).normalized;
                sword.FireMagic(aim, swordSpeed, dashDuration + 0.35f);
                yield return new WaitForSeconds(swordInterval / DurationScale);
            }

            yield return dashTween.WaitForCompletion();
            dashCaster.DisableCasting();
            Boss.AttackImpact(Boss.transform.position);
            Boss.ShakeCamera(0.75f);
        }

        protected override void OnSwordmasterCancel()
        {
            dashTween?.Kill();
            dashCaster?.DisableCasting();
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
