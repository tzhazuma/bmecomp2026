using UnityEngine;
using System.Collections.Generic;

public class CollectibleManager : MonoBehaviour
{
    public GameObject collectiblePrefab;
    public float spawnDistance = 55f;
    public float lateralSpread = 18f;
    public float verticalSpread = 8f;
    public float spawnInterval = 1.5f;
    public int maxActiveCollectibles = 20;
    public int pointsValue = 10;
    public int comboBonus = 30;

    public GameObject comboBonusPrefab;

    private float spawnTimer = 0f;
    private Transform playerTransform;
    private List<GameObject> activeCollectibles = new List<GameObject>();

    void Start()
    {
        playerTransform = FindObjectOfType<PlayerController>()?.transform;
    }

    void Update()
    {
        if (!playerTransform) return;
        CleanupFarCollectibles();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeCollectibles.Count < maxActiveCollectibles)
        {
            SpawnCollectible();
            spawnTimer = 0f;
        }
    }

    private void SpawnCollectible()
    {
        if (!collectiblePrefab) return;

        Vector3 spawnPos = playerTransform.position + playerTransform.forward * spawnDistance;
        spawnPos += new Vector3(
            Random.Range(-lateralSpread, lateralSpread),
            Random.Range(-verticalSpread, verticalSpread),
            0
        );

        GameObject collectible = Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);
        CollectibleMovement movement = collectible.AddComponent<CollectibleMovement>();
        movement.playerTransform = playerTransform;

        float freq = Random.Range(1f, 3f);
        float amp = Random.Range(0.5f, 2f);
        movement.floatFreq = freq;
        movement.floatAmp = amp;

        activeCollectibles.Add(collectible);

        float att = BCIManager.Instance?.GetAttention() ?? 50f;
        if (att > 70f)
        {
            var renderer = collectible.GetComponent<Renderer>();
            if (renderer)
                renderer.material.color = Color.Lerp(Color.white, Color.gold, (att - 70f) / 30f);
        }
    }

    private void CleanupFarCollectibles()
    {
        activeCollectibles.RemoveAll(o => o == null);
        for (int i = activeCollectibles.Count - 1; i >= 0; i--)
        {
            if (activeCollectibles[i] == null) { activeCollectibles.RemoveAt(i); continue; }
            float dist = Vector3.Distance(
                activeCollectibles[i].transform.position,
                playerTransform.position
            );
            if (dist > spawnDistance + 20f)
            {
                Destroy(activeCollectibles[i]);
                activeCollectibles.RemoveAt(i);
            }
        }
    }

    public void SpawnComboEffect(Vector3 position)
    {
        if (comboBonusPrefab)
        {
            GameObject fx = Instantiate(comboBonusPrefab, position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    public void SetDifficultyMultiplier(float intervalMul)
    {
        spawnInterval = 1.5f * intervalMul;
    }

    void OnDestroy()
    {
        foreach (var c in activeCollectibles)
        {
            if (c) Destroy(c);
        }
        activeCollectibles.Clear();
    }
}

public class CollectibleMovement : MonoBehaviour
{
    public Transform playerTransform;
    public float floatFreq = 2f;
    public float floatAmp = 1f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (playerTransform)
        {
            transform.position += -playerTransform.forward * 2f * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y = startPos.y + Mathf.Sin(Time.time * floatFreq) * floatAmp;
            transform.position = pos;
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
        }
    }
}
