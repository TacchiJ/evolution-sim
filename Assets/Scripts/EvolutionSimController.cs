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
    private int hungerDeathsThisEpoch = 0;
    private int thirstDeathsThisEpoch = 0;

    private List<Mesh> spawnMeshes = new List<Mesh>();
    private List<Matrix4x4> spawnMatrices = new List<Matrix4x4>();

    // Store the last finished generation (parents for next epoch)
    private List<float[]> lastGenWeights = new List<float[]>();
    private List<float> lastGenAges = new List<float>();

    // Store the current generation as it dies
    private List<float[]> currentGenWeights = new List<float[]>();
    private List<float> currentGenAges = new List<float>();


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
        hungerDeathsThisEpoch = 0;
        thirstDeathsThisEpoch = 0;  
        currentGenAges.Clear();
        currentGenWeights.Clear();

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

            if (lastGenWeights.Count > 0 && lastGenAges.Count > 0)
            {
                if (i < Mathf.CeilToInt(creaturesPerEpoch * 0.02f))
                {
                    // Top 2% elites
                    brainScript.SetWeights(GetBestWeights());
                }
                else if (i < Mathf.CeilToInt(creaturesPerEpoch * 0.05f))
                {
                    // Next 3% mutated elites
                    brainScript.SetWeights(MutateWeights(GetBestWeights()));
                }
                else
                {
                    // General population selection with mutation
                    float[] parentWeights = SelectParent(tournamentSize: 3);
                    brainScript.SetWeights(MutateWeights(parentWeights));
                }
            }
            else if (lastGenWeights.Count > 0)
            {
                // If loading first epoch from file
                int idx = Random.Range(0, lastGenWeights.Count);
                brainScript.SetWeights(lastGenWeights[idx]);
            }

            placedPositions.Add(pos);
        }
    }

    private Vector3 FindSpawnPosition(List<Vector3> placed)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int mIndex = Random.Range(0, spawnMeshes.Count);
            Mesh mesh = spawnMeshes[mIndex];
            Matrix4x4 matrix = spawnMatrices[mIndex];

            int[] tris = mesh.triangles;
            int t = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t]]);
            Vector3 v1 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t + 1]]);
            Vector3 v2 = matrix.MultiplyPoint3x4(mesh.vertices[tris[t + 2]]);

            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            Vector3 point = (1 - r1) * v0 + (r1 * (1 - r2)) * v1 + (r1 * r2) * v2;

            point += Vector3.up * 1f;

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

        return Vector3.negativeInfinity;
    }

    /// <summary>
    /// Called by a creature when it dies
    /// </summary>
    public void CreatureDied(float age, float[] weights, string causeOfDeath)
    {
        deathsThisEpoch++;
        currentGenAges.Add(age);
        currentGenWeights.Add(weights);

        if (causeOfDeath == "hunger")
            hungerDeathsThisEpoch++;
        else if (causeOfDeath == "thirst")
            thirstDeathsThisEpoch++;

        if (deathsThisEpoch >= creaturesPerEpoch)
        {
            EndEpoch();
        }
    }

    private void EndEpoch()
    {
        float meanAge = 0f;
        float maxAge = 0f;

        foreach (float a in currentGenAges)
        {
            meanAge += a;
            if (a > maxAge) maxAge = a;
        }
        meanAge /= currentGenAges.Count;

        Debug.Log($"Epoch {currentEpoch + 1} ended.\nMax age: {maxAge:F2}\nMean age: {meanAge:F2}\nDeaths by hunger: {hungerDeathsThisEpoch}\nDeaths by thirst: {thirstDeathsThisEpoch}");

        SaveWeightsSortedByAge(currentGenWeights, currentGenAges);

        // Promote current gen → last gen
        lastGenWeights = new List<float[]>(currentGenWeights);
        lastGenAges = new List<float>(currentGenAges);

        currentGenWeights.Clear();
        currentGenAges.Clear();

        currentEpoch++;
        if (currentEpoch < epochs)
        {
            StartEpoch();
        }
        else
        {
            Debug.Log("Simulation complete!");
        }
    }

    private void SaveWeightsSortedByAge(List<float[]> weights, List<float> ages)
    {
        List<(float age, float[] weights)> scored = new List<(float, float[])>();
        for (int i = 0; i < ages.Count; i++)
        {
            scored.Add((ages[i], weights[i]));
        }

        scored.Sort((a, b) => b.age.CompareTo(a.age));

        List<string> serialized = new List<string>();
        foreach (var s in scored)
        {
            string line = string.Join(",", s.weights);
            serialized.Add(line);
        }

        File.WriteAllLines(saveFilePath, serialized.ToArray());
    }

    private void LoadWeightsFromFile(string path)
    {
        lastGenWeights.Clear();
        lastGenAges.Clear();

        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(',');
            float[] weights = new float[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                weights[i] = float.Parse(tokens[i]);
            lastGenWeights.Add(weights);
            lastGenAges.Add(0f); // Unknown fitness when loading
        }

        Debug.Log($"Loaded {lastGenWeights.Count} weight sets from {path}");
    }

    private float[] SelectParent(int tournamentSize = 3)
    {
        List<(float age, float[] weights)> scored = new List<(float, float[])>();
        for (int i = 0; i < lastGenAges.Count; i++)
        {
            scored.Add((lastGenAges[i], lastGenWeights[i]));
        }

        int count = scored.Count;
        if (count == 0)
        {
            Debug.LogWarning("No candidates available for parent selection!");
            return null;
        }

        List<(float age, float[] weights)> candidates = new List<(float, float[])>();
        for (int i = 0; i < tournamentSize; i++)
        {
            int idx = Random.Range(0, count);
            candidates.Add(scored[idx]);
        }

        candidates.Sort((a, b) => b.age.CompareTo(a.age));
        return candidates[0].weights;
    }

    private float[] GetBestWeights()
    {
        List<(float age, float[] weights)> scored = new List<(float, float[])>();
        for (int i = 0; i < lastGenAges.Count; i++)
        {
            scored.Add((lastGenAges[i], lastGenWeights[i]));
        }

        scored.Sort((a, b) => b.age.CompareTo(a.age));
        return scored[0].weights;
    }

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

            childWeights[i] = Mathf.Clamp(childWeights[i], -3f, 3f);
        }
        return childWeights;
    }
}
