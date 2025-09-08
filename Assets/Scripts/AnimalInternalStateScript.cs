using UnityEngine;

public class AnimalInternalState : MonoBehaviour
{
    [Header("Other Scripts")]
    public CreatureMovementBounded CreatureMovementScript;
    public BrainScript BrainScript;
    public EvolutionSimController EvolutionSimController;

    [Header("Decrease Rates (per second)")]
    public float hungerDecreaseRate = 0.01f;
    public float thirstDecreaseRate = 0.01f;

    // Internal state variables
    public float age = 0f;
    public float hunger = 1f;
    public float thirst = 1f;

    [Header("Layer Masks")]
    public LayerMask foodLayers; // Can include multiple layers
    public LayerMask waterLayer; // Water triggers should be on this layer

    [Header("Water Detection")]
    public float waterRayLength = 1f; // Distance to check below the animal
    public int rayCount = 4; 

    private bool isInWater = false;

    void Awake()
    {
        if (EvolutionSimController == null)
            EvolutionSimController = FindObjectOfType<EvolutionSimController>();
    }

    void Update()
    {
        // Scale changes by simulation speed
        float scaledDelta = Time.deltaTime * Time.timeScale;

        // Increase age
        age += scaledDelta;

        // FOOD
        // Decrease hunger and thirst
        hunger -= hungerDecreaseRate * scaledDelta;
        thirst -= thirstDecreaseRate * scaledDelta;

        // WATER
        // Check for water using raycasts
        isInWater = IsInWater();

        // Restore thirst if in water
        if (isInWater)
        {
            Drink(1f * scaledDelta);
        }

        // Clamp values between 0 and 1
        hunger = Mathf.Clamp01(hunger);
        thirst = Mathf.Clamp01(thirst);

        // DEATH
        // Destroy animal if hunger or thirst reaches 0
        if (thirst <= 0f)
        {
            if (!CreatureMovementScript.controlledByHuman)
            {
                EvolutionSimController.CreatureDied(age, BrainScript.brain.GetWeights(), "thirst");
                Destroy(gameObject);
            }
        }
        else if (hunger <= 0f)
        {
            if (!CreatureMovementScript.controlledByHuman)
            {
                EvolutionSimController.CreatureDied(age, BrainScript.brain.GetWeights(), "hunger");
                Destroy(gameObject);
            }
        }
    }

    // Raycast water detection
    private bool IsInWater()
    {
        Vector3 origin = transform.position;
        float offset = 0.5f; // spread for multiple rays
        for (int i = 0; i < rayCount; i++)
        {
            // Slightly offset each ray along x-axis (or z-axis) to cover more area
            Vector3 rayOrigin = origin + transform.right * ((i - rayCount / 2f) * offset);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, waterRayLength, waterLayer))
            {
                return true;
            }
        }
        return false;
    }

    // Public getters
    public float GetAge() => age;
    public float GetHunger() => hunger;
    public float GetThirst() => thirst;

    public void Eat(float amount) {
        hunger = Mathf.Clamp01(hunger + amount);
        age += 5f;
    }
    public void Drink(float amount) => thirst = Mathf.Clamp01(thirst + amount);

    // Trigger detection for food and water
    private void OnTriggerEnter(Collider other)
    {
        // Check if object is in any of the food layers
        if (((1 << other.gameObject.layer) & foodLayers) != 0)
        {
            PlantLife food = other.GetComponentInParent<PlantLife>();
            if (food != null)
            {
                float nutrition = food.GetNutritionalScore();
                Eat(nutrition * 0.1f); // Increase hunger by 10% of nutritional value
                Destroy(other.gameObject); // Remove food object
            }
        }

        // Check if object is a water trigger
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            isInWater = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            isInWater = false;
        }
    }
}
