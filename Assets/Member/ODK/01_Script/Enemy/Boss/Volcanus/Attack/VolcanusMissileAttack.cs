using System.Collections;
using UnityEngine;

public class VolcanusMissileAttack : VolcanusSkill
{
    [SerializeField] private VolcanusMissile missilePrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];
    [SerializeField] private Vector2[] spawnOffsets =
    {
        new Vector2(-2.5f, 1.2f), new Vector2(-0.8f, 2f),
        new Vector2(0.8f, 2f), new Vector2(2.5f, 1.2f)
    };
    [SerializeField] private float spawnInterval = 0.18f;
    [SerializeField] private float startSpeed = 2f;
    [SerializeField] private float acceleration = 7.5f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float damage = 65f;

    public override bool CanUseSkill(GameObject target = null)
    {
        return Boss != null && Boss.Target != null;
    }

    protected override IEnumerator Execute(GameObject target)
    {
        for (int i = 0; i < 4; i++)
        {
            SpawnMissile(i);
            if (i < 3)
                yield return new WaitForSeconds(spawnInterval * DurationScale);
        }
    }

    private void SpawnMissile(int index)
    {
        Vector3 spawnPosition = GetSpawnPosition(index);
        VolcanusMissile missile = missilePrefab != null
            ? Instantiate(missilePrefab, spawnPosition, Quaternion.identity)
            : new GameObject($"Volcanus Missile {index + 1}").AddComponent<VolcanusMissile>();
        missile.transform.position = spawnPosition;
        missile.Launch(Boss.Target, startSpeed, acceleration, maxSpeed, lifeTime, damage);
        missile.OnExplode += Boss.AttackImpact;
        Boss.MissileSpawn(spawnPosition);
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
            return spawnPoints[index].position;
        Vector2 offset = spawnOffsets != null && index < spawnOffsets.Length ? spawnOffsets[index] : Vector2.zero;
        return Boss.Truso != null ? Boss.Truso.transform.TransformPoint(offset) : Boss.transform.TransformPoint(offset);
    }

    private void OnDrawGizmosSelected()
    {
        Volcanus boss = Boss != null ? Boss : GetComponentInParent<Volcanus>();
        if (boss == null) return;

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.9f);
        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPosition;
            if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
                spawnPosition = spawnPoints[i].position;
            else
            {
                Vector2 offset = spawnOffsets != null && i < spawnOffsets.Length ? spawnOffsets[i] : Vector2.zero;
                spawnPosition = boss.Truso != null
                    ? boss.Truso.transform.TransformPoint(offset)
                    : boss.transform.TransformPoint(offset);
            }

            Gizmos.DrawWireSphere(spawnPosition, 0.35f);
            if (boss.Target != null) Gizmos.DrawLine(spawnPosition, boss.Target.position);
        }
    }
}
