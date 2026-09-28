using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusMissileAttack : VolcanusSkill
    {
        [SerializeField] private VolcanusMissile missilePrefab;
        [SerializeField] private Transform[] spawnPoints = new Transform[4];
        [SerializeField] private Vector2[] spawnOffsets =
        {
            new Vector2(-2.5f, 1.2f), new Vector2(-0.8f, 2f),
            new Vector2(0.8f, 2f), new Vector2(2.5f, 1.2f)
        };
        [SerializeField] private float readyDuration = 0.55f;
        [SerializeField] private float spawnInterval = 0.18f;
        [SerializeField] private float startSpeed = 2f;
        [SerializeField] private float acceleration = 7.5f;
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float damage = 1f;

        protected override string DefaultActionState => Volcanus.ComboState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
            Boss.AnimateMissileCast(readyDuration / ActionSpeed,
                spawnInterval * 3f / ActionSpeed);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, Boss.HeadPosition);
            yield return new WaitForSeconds(readyDuration / ActionSpeed);

            for (int i = 0; i < 4 && !Boss.IsDead; i++)
            {
                Vector3 position = GetSpawnPosition(i);
                if (missilePrefab == null) yield break;
                VolcanusMissile missile = Instantiate(missilePrefab, position, Quaternion.identity);
                missile.Launch(Boss.Target, startSpeed, acceleration, maxSpeed, lifeTime, damage,
                    Boss.PlayerLayer);
                missile.OnExplode += Boss.AttackImpact;
                Boss.MissileSpawn(position);
                Boss.PlayFeedback(VolcanusFeedbackType.Boulder, position);
                if (i < 3) yield return new WaitForSeconds(spawnInterval / ActionSpeed);
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
                return spawnPoints[index].position;
            Vector2 offset = spawnOffsets != null && index < spawnOffsets.Length
                ? spawnOffsets[index] * Scale
                : Vector2.zero;
            return Boss.transform.TransformPoint(offset);
        }
    }
}
