using System.Collections.Generic;
using UnityEngine;

public class EvolutionSimController : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int epochs = 10;
    public int creaturesPerEpoch = 20;
    public GameObject creaturePrefab;     // Prefab with BrainScript
    public Transform creaturesParent;

    [Header("Evolution Settings")]
    [Range(0f, 1f)]
    public float mutationRate = 0.05f;   // small mutation
    [Range(0f, 1f)]
    public float mutationStrength = 0.1f; // how much weights change

    private int currentEpoch = 0;
    private int deathsThisEpoch = 0;
    private List<float> agesThisEpoch = new List<float>();

    // Store all neural networks for selection
    private List<float[]> previousGenWeights = new List<float[]>();

    private void Start()
    {
        StartEpoch();
    }

    private void StartEpoch()
    {
        deathsThisEpoch = 0;
        agesThisEpoch.Clear();

        // Spawn creatures
        for (int i = 0; i < creaturesPerEpoch; i++)
        {
            GameObject c = Instantiate(creaturePrefab, creaturesParent);
            BrainScript brainScript = c.GetComponent<BrainScript>();

            // Assign random or inherited weights
            if (previousGenWeights.Count == 0)
            {
                // First generation, random weights already in BrainScript
            }
            else
            {
                // Select parent probabilistically by age
                float[] parentWeights = SelectParent();
                float[] childWeights = MutateWeights(parentWeights);
                brainScript.brain.SetWeights(childWeights);
            }
        }
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

    /// <summary>
    /// Select a parent weight set probabilistically, weighted by age (longer living = more likely)
    /// </summary>
    private float[] SelectParent()
    {
        // Compute cumulative sum of ages
        float totalAge = 0f;
        foreach (float a in agesThisEpoch)
            totalAge += a;

        // Pick a random value
        float r = Random.Range(0f, totalAge);
        float sum = 0f;
        int index = 0;
        for (int i = 0; i < agesThisEpoch.Count; i++)
        {
            sum += agesThisEpoch[i];
            if (r <= sum)
            {
                index = i;
                break;
            }
        }

        // Return weights from chosen parent
        return previousGenWeights[index];
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
