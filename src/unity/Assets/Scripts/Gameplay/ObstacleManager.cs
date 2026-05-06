using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public float spawnDistance = 60f;
    public float destroyDistance = 15f;
    public float lateralSpread = 15f;
    public float verticalSpread = 7f;
    public int maxActiveObstacles = 30;
    public float baseSpawnInterval = 2.0f;
    public float minSpawnInterval = 0.5f;

    public float obstacleSpeed = 3f;
    public AnimationCurve attentionIntervalCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);

    private float spawnTimer = 0f;
    private float currentInterval;
    private Transform playerTransform;
    private BCIManager bciManager;
    private List<GameObject> activeObstacles = new List<GameObject>();

    void Start()
    {
        playerTransform = FindObjectOfType<PlayerController>()?.transform;
        bciManager = BCIManager.Instance;
        currentInterval = baseSpawnInterval;
    }

    void Update()
    {
        if (!playerTransform) return;

        CleanupFarObstacles();
        UpdateSpawnInterval();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentInterval && activeObstacles.Count < maxActiveObstacles)
        {
            SpawnObstacle();
            spawnTimer = 0f;
        }
    }

    private void UpdateSpawnInterval()
    {
        float att = bciManager ? bciManager.GetAttention() : 50f;
        float ratio = Mathf.Clamp01(att / 100f);
        float curveVal = attentionIntervalCurve.Evaluate(ratio);
        currentInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, 1f - curveVal);
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Vector3 spawnPos = playerTransform.position + playerTransform.forward * spawnDistance;
        spawnPos += new Vector3(
            Random.Range(-lateralSpread, lateralSpread),
            Random.Range(-verticalSpread, verticalSpread),
            0
        );

        GameObject obstacle = Instantiate(prefab, spawnPos, Random.rotation);
        ObstacleMovement movement = obstacle.AddComponent<ObstacleMovement>();
        movement.speed = obstacleSpeed;
        movement.playerTransform = playerTransform;
        activeObstacles.Add(obstacle);
    }

    private void CleanupFarObstacles()
    {
        activeObstacles.RemoveAll(o => o == null);
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null) { activeObstacles.RemoveAt(i); continue; }
            float dist = Vector3.Distance(activeObstacles[i].transform.position, playerTransform.position);
            if (dist > spawnDistance + destroyDistance)
            {
                Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }
    }

    public void SetDifficultyMultiplier(float speedMul, float intervalMul)
    {
        obstacleSpeed = 3f * speedMul;
        baseSpawnInterval = 2.0f * intervalMul;
    }

    void OnDestroy()
    {
        foreach (var obs in activeObstacles)
        {
            if (obs) Destroy(obs);
        }
        activeObstacles.Clear();
    }
}

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 3f;
    public Transform playerTransform;

    void Update()
    {
        if (playerTransform)
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0;
            transform.position += -playerTransform.forward * speed * Time.deltaTime;
            transform.Rotate(Vector3.up, speed * 30f * Time.deltaTime);
        }
    }
}
