using System.Collections.Generic;
using UnityEngine;

public class BrainScript : MonoBehaviour
{
    [Header("References")]
    public AnimalVision AnimalVisionScript;
    public AnimalInternalState AnimalInternalStateScript;
    public CreatureMovementBounded CreatureMovementScript;

    [Header("Neural Network")]
    public NeuralNetwork brain;

    // Number of object types in your environment
    private int objectTypes = 6;

    // Max distance for normalization
    private float maxDistance = 15f;

    // Mapping from layer ID to one-hot index
    private Dictionary<int, int> layerToIndex = new Dictionary<int, int>()
    {
        { 3, 0 },   // Land
        { 4, 1 },   // Water
        { 7, 2 },   // Sheep
        { 8, 3 },   // Food1
        { 9, 4 },   // Food2
        { -1, 5 }   // Nothing
    };

    private void Awake()
    {
        // Initialize the neural network
        brain = new NeuralNetwork();
    }

    private void Update()
    {
        // 1. Get vision values
        List<float> visionValues = AnimalVisionScript.GetVisionValues(); // [dist, layer, dist, layer, ...]

        // Convert vision list to float array with one-hot encoding
        float[] visionInput = ConvertVisionToInputArray(visionValues);

        // 2. Get internal state
        float hunger = AnimalInternalStateScript.GetHunger();
        float thirst = AnimalInternalStateScript.GetThirst();
        float[] internalState = new float[] { hunger, thirst };

        // 3. Forward pass through the neural network
        float[] outputs = brain.Forward(visionInput, internalState);

        // 4. Apply outputs to movement
        float movementX = outputs[0]; // [-1, 1]
        float movementY = outputs[1]; // [-1, 1]

        CreatureMovementScript.SetHorizontal(movementX);
        CreatureMovementScript.SetVertical(movementY);
    }

    /// <summary>
    /// Converts a raw vision list [dist1, layer1, dist2, layer2, ...] 
    /// to the input array expected by the neural network (dist + one-hot layer)
    /// </summary>
    private float[] ConvertVisionToInputArray(List<float> visionValues)
    {
        int numRays = visionValues.Count / 2;
        float[] inputArray = new float[numRays * (1 + objectTypes)]; // 1 distance + 6 one-hot

        for (int i = 0; i < numRays; i++)
        {
            float distance = visionValues[i * 2];
            int layer = Mathf.RoundToInt(visionValues[i * 2 + 1]);

            // Normalize distance to 0..1
            distance = Mathf.Clamp01(distance / maxDistance);

            // Set distance
            inputArray[i * (1 + objectTypes)] = distance;

            // Initialize one-hot to all zeros
            for (int j = 0; j < objectTypes; j++)
                inputArray[i * (1 + objectTypes) + 1 + j] = 0f;

            // Set the correct one-hot index if layer exists
            if (layerToIndex.ContainsKey(layer))
            {
                int index = layerToIndex[layer];
                inputArray[i * (1 + objectTypes) + 1 + index] = 1f;
            }
        }

        return inputArray;
    }
}
