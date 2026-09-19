using System.Collections.Generic;
using UnityEngine;

public class RandomObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private int spawnCount = 30;

    [SerializeField] private Vector2 areaMin = new Vector2(-8, -4);
    [SerializeField] private Vector2 areaMax = new Vector2(8, 4);

    [SerializeField] private float minDistance = 1.5f;

    private List<Vector2> spawnPositions = new List<Vector2>();

    void Start()
    {
        int attempts = 0;
        int maxAttempts = spawnCount * 100;

        while (spawnPositions.Count < spawnCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 position = new Vector2(
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y)
            );

            bool canSpawn = true;

            foreach (Vector2 existingPosition in spawnPositions)
            {
                if (Vector2.Distance(position, existingPosition) < minDistance)
                {
                    canSpawn = false;
                    break;
                }
            }

            if (canSpawn)
            {
                spawnPositions.Add(position);
                Instantiate(objectPrefab, position, Quaternion.identity);
            }
        }
    }
}