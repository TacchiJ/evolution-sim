using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EvolutionSimController : MonoBehaviour
{
    [Header("Simulation Speed")]
    [Tooltip("Multiplier for simulation speed (1 = normal, 2 = double, etc.)")]
    [Range(0f, 10f)]
    public float simulationSpeed = 1f;

    [Header("Simulation Settings")]
    public int epochs = 10;
    public int creaturesPerEpoch = 20;
    public GameObject creaturePrefab;     // Prefab with BrainScript
    public Transform creaturesParent;
    public Transform meshParentObject;

    [Header("Initial Generation")]
    [Tooltip("If true, first epoch will be initialized from saved weights.")]
    public bool loadFirstEpochFromSave = false;
    public string saveFilePath = "SavedWeights.json";
    public string loadFilePath = "SavedWeights.json";

    [Header("Evolution Settings")]
    [Range(0f, 1f)]
    public float mutationRate = 0.05f;   // small mutation
    [Range(0f, 1f)]
    public float mutationStrength = 0.1f; // how much weights change

    private int currentEpoch = 0;
    private int deathsThisEpoch = 0;
    private List<float> agesThisEpoch = new List<float>();

    private List<Mesh> spawnMeshes = new List<Mesh>();
    private List<Matrix4x4> spawnMatrices = new List<Matrix4x4>();

    // Store all neural networks for selection
    private List<float[]> previousGenWeights = new List<float[]>();

    private void Start()
    {
        CacheSpawnMeshes();

        // Load weights if requested
        if (loadFirstEpochFromSave && File.Exists(saveFilePath))
        {
            LoadWeightsFromFile(loadFilePath);
        }

        StartEpoch();
    }

    private void Update()
    {
        Time.timeScale = simulationSpeed;
    }

    private void CacheSpawnMeshes()
    {
        spawnMeshes.Clear();
        spawnMatrices.Clear();

        MeshFilter[] filters = meshParentObject.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in filters)
        {
            spawnMeshes.Add(mf.sharedMesh);
            spawnMatrices.Add(mf.transform.localToWorldMatrix);
        }
    }

    private void StartEpoch()
    {
        deathsThisEpoch = 0;
        agesThisEpoch.Clear();

        foreach (Transform child in creaturesParent)
            Destroy(child.gameObject);

        List<Vector3> placedPositions = new List<Vector3>();

        // Spawn creatures
        for (int i = 0; i < creaturesPerEpoch; i++)
        {
            Vector3 pos = FindSpawnPosition(placedPositions);
            if (pos == Vector3.negativeInfinity)
            {
                Debug.LogWarning("Could not find valid spawn position for creature!");
                continue;
            }

            GameObject c = Instantiate(creaturePrefab, pos, Quaternion.identity, creaturesParent);
            BrainScript brainScript = c.GetComponent<BrainScript>();

            // Only mutate weights if previous generation exists AND this is NOT the first epoch loaded from file
            if (previousGenWeights.Count > 0 && agesThisEpoch.Count > 0)
            {
                float[] parentWeights = SelectParent();
                float[] childWeights = MutateWeights(parentWeights);
                brainScript.SetWeights(childWeights);
            }
            else if (previousGenWeights.Count > 0)
            {
                // Use the loaded weights directly, without selecting parents
                int idx = Random.Range(0, previousGenWeights.Count);
                brainScript.SetWeights(previousGenWeights[idx]);
            }

            placedPositions.Add(pos);
        }
    }

    private Vector3 FindSpawnPosition(List<Vector3> placed)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            // Pick random mesh
            int mIndex = Random.Range(0, spawnMeshes.Count);
            Mesh mesh = spawnMeshes[mIndex];
            Matrix4x4 matrix = spawnMatrices[mIndex];

            // Pick random triangle
            int[] tris = mesh.triangles;
            int t = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t]]);
            Vector3 v1 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t + 1]]);
            Vector3 v2 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t + 2]]);

            // Barycentric random point
            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            Vector3 point = (1 - r1) * v0 + (r1 * (1 - r2)) * v1 + (r1 * r2) * v2;

            // Y offset
            point += Vector3.up * 1f;

            // Check distance from already placed
            bool valid = true;
            foreach (Vector3 p in placed)
            {
                if (Vector3.Distance(p, point) < 2f)
                {
                    valid = false;
                    break;
                }
            }

            if (valid) return point;
        }

        return Vector3.negativeInfinity; // fail
    }

    /// <summary>
    /// Called by a creature when it dies
    /// </summary>
    /// <param name="age">Final age of creature</param>
    /// <param name="weights">Neural network weights of creature</param>
    public void CreatureDied(float age, float[] weights)
    {
        deathsThisEpoch++;
        agesThisEpoch.Add(age);

        // Save weights for next generation
        previousGenWeights.Add(weights);

        // Check if epoch is over
        if (deathsThisEpoch >= creaturesPerEpoch)
        {
            EndEpoch();
        }
    }

    private void EndEpoch()
    {
        float meanAge = 0f;
        float maxAge = 0f;

        foreach (float a in agesThisEpoch)
        {
            meanAge += a;
            if (a > maxAge) maxAge = a;
        }
        meanAge /= agesThisEpoch.Count;

        Debug.Log($"Epoch {currentEpoch + 1} ended. Max age: {maxAge:F2}, Mean age: {meanAge:F2}");

        SaveWeightsSortedByAge();

        currentEpoch++;
        if (currentEpoch < epochs)
        {
            // Clear previous generation weights list for next epoch
            previousGenWeights.Clear();

            // Start next epoch
            StartEpoch();
        }
        else
        {
            Debug.Log("Simulation complete!");
        }
    }

    private void SaveWeightsSortedByAge()
    {
        // Pair ages with weights
        List<(float age, float[] weights)> scored = new List<(float, float[])>();
        for (int i = 0; i < agesThisEpoch.Count; i++)
        {
            scored.Add((agesThisEpoch[i], previousGenWeights[i]));
        }

        // Sort descending by age
        scored.Sort((a, b) => b.age.CompareTo(a.age));

        // Convert to serializable format
        List<string> serialized = new List<string>();
        foreach (var s in scored)
        {
            string line = string.Join(",", s.weights);
            serialized.Add(line);
        }

        File.WriteAllLines(saveFilePath, serialized.ToArray());
        // Debug.Log($"Saved weights for epoch {currentEpoch + 1} to {saveFilePath}");
    }

    private void LoadWeightsFromFile(string path)
    {
        previousGenWeights.Clear();
        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(',');
            float[] weights = new float[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                weights[i] = float.Parse(tokens[i]);
            previousGenWeights.Add(weights);
        }

        Debug.Log($"Loaded {previousGenWeights.Count} weight sets from {path}");
    }

    /// <summary>
    /// Select a parent weight set from the top 50% of creatures,
    /// with a ~90% chance to pick from the top 10%.
    /// </summary>
    private float[] SelectParent()
    {
        // Pair up ages with weights
        List<(float age, float[] weights)> scored = new List<(float, float[])>();
        for (int i = 0; i < agesThisEpoch.Count; i++)
        {
            scored.Add((agesThisEpoch[i], previousGenWeights[i]));
        }

        // Sort descending by age (best first)
        scored.Sort((a, b) => b.age.CompareTo(a.age));

        int count = scored.Count;
        int cutoff = count / 2; // only top 50% survive
        int top10 = Mathf.Max(1, count / 10); // top 10%

        // 90% chance pick from top 10%
        if (Random.value < 0.9f)
        {
            int idx = Random.Range(0, top10);
            return scored[idx].weights;
        }
        else
        {
            // 10% chance pick randomly from rest of top 50% (excluding top 10% already handled)
            int idx = Random.Range(top10, cutoff);
            return scored[idx].weights;
        }
    }

    /// <summary>
    /// Apply small random mutations to weights
    /// </summary>
    private float[] MutateWeights(float[] parentWeights)
    {
        float[] childWeights = new float[parentWeights.Length];
        for (int i = 0; i < parentWeights.Length; i++)
        {
            childWeights[i] = parentWeights[i];
            if (Random.value < mutationRate)
            {
                childWeights[i] += Random.Range(-mutationStrength, mutationStrength);
            }
        }
        return childWeights;
    }
}
