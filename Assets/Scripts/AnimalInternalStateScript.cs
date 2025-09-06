using UnityEngine;

public class AnimalInternalState : MonoBehaviour
{
    [Header("Decrease Rates (per second)")]
    public float hungerDecreaseRate = 0.01f;
    public float thirstDecreaseRate = 0.01f;

    // Internal state variables
    public float age = 0f;
    public float hunger = 1f;
    public float thirst = 1f;

    // Layer masks for food and water (assign in Inspector)
    public LayerMask foodLayers;
    public LayerMask waterLayer;

    private bool isInWater = false;

    void Update()
    {
        // Increase age
        age += Time.deltaTime;

        // Decrease hunger and thirst
        hunger -= hungerDecreaseRate * Time.deltaTime;
        thirst -= thirstDecreaseRate * Time.deltaTime;

        // If in water, restore thirst
        if (isInWater)
        {
            thirst += 0.1f * Time.deltaTime;
        }

        // Clamp values between 0 and 1
        hunger = Mathf.Clamp01(hunger);
        thirst = Mathf.Clamp01(thirst);

        // Destroy animal if hunger or thirst reaches 0
        if (hunger <= 0f || thirst <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // Public getters
    public float GetAge() => age;
    public float GetHunger() => hunger;
    public float GetThirst() => thirst;

    // Optional: public methods to modify hunger/thirst
    public void Eat(float amount) => hunger = Mathf.Clamp01(hunger + amount);
    public void Drink(float amount) => thirst = Mathf.Clamp01(thirst + amount);

    // Trigger detection for food and water
    private void OnTriggerEnter(Collider other)
    {
        // Check for food
        if (((1 << other.gameObject.layer) & foodLayers) != 0)
        {
            // Assume food has PlantLife component with GetNutritionalScore()
            PlantLife food = other.GetComponent<PlantLife>();
            if (food != null)
            {
                float nutrition = food.GetNutritionalScore();
                Eat(nutrition * 0.1f); // Increase hunger by 10% of nutritional value
                Destroy(other.gameObject); // Remove food
            }
        }

        // Check for water
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
