using UnityEngine;
using System.Collections.Generic;

public class PlantSpawnerOnTiles : MonoBehaviour
{
    [Header("References")]
    public Transform tileParent;
    public GameObject prefabToSpawn;

    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float spawnLikelihood = 0.3f; 
    public float spawnTimeInterval = 2f;
    public float minDistance = 2f;

    [Header("Spawn Counts")]
    public int initialSpawnCount = 10;
    public int spawnPerInterval = 2;

    [Header("Runtime")]
    public List<GameObject> spawnedObjects = new List<GameObject>();

    private float timer;

    void Start()
    {
        // Spawn initial objects
        for (int i = 0; i < initialSpawnCount; i++)
        {
            TrySpawn();
        }
    }

    void Update()
    {
        if (prefabToSpawn == null || tileParent == null)
            return;

        // Clean up null entries (destroyed plants)
        spawnedObjects.RemoveAll(obj => obj == null);

        timer += Time.deltaTime;
        if (timer >= spawnTimeInterval)
        {
            timer = 0f;

            // Try spawning multiple objects this tick
            for (int i = 0; i < spawnPerInterval; i++)
            {
                TrySpawn();
            }
        }
    }

    void TrySpawn()
    {
        if (Random.value > spawnLikelihood)
            return; // Skip this attempt

        // Collect candidate vertices from all child tiles
        List<Vector3> allVertices = new List<Vector3>();
        foreach (Transform child in tileParent)
        {
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Vector3[] vertices = mf.sharedMesh.vertices;
            foreach (var v in vertices)
            {
                allVertices.Add(child.TransformPoint(v));
            }
        }

        if (allVertices.Count == 0) return;

        // Pick a random vertex from the combined pool
        Vector3 worldPoint = allVertices[Random.Range(0, allVertices.Count)];

        // Check distance against existing spawned objects
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;
            if (Vector3.Distance(worldPoint, obj.transform.position) < minDistance)
                return; // Too close, skip spawn
        }

        // Spawn the prefab
        GameObject instance = Instantiate(prefabToSpawn, worldPoint, Quaternion.identity, transform);
        spawnedObjects.Add(instance);
    }
}
