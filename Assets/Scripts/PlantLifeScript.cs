using UnityEngine;

public class PlantLife : MonoBehaviour
{
    [Header("Temperature")]
    public TemperatureControllerScript temperatureController;

    [Header("Plant Settings")]
    [Range(0f, 100f)] public float lifeExpectancy = 30f;   
    [Range(0f, 1f)] public float idealTemperatureMin = 10f;
    [Range(0f, 1f)] public float idealTemperatureMax = 25f;
    [Range(0f, 10f)] public float outsideTemperatureMortalityRate = 3f;
    [Range(0f, 10f)] public float nutritionalScore = 5f;    

    [Header("Runtime Info")]
    public float age = 0f;
    public float currentTemperature = 0f;
    public bool isInIdealRange = true;

    void Awake()
    {
        temperatureController = FindObjectOfType<TemperatureControllerScript>();
    }

    void Update()
    {
        // Get current temperature from controller
        currentTemperature = temperatureController.GetTemperature();

        // Check if temperature is within the ideal range
        isInIdealRange = currentTemperature >= idealTemperatureMin && currentTemperature <= idealTemperatureMax;

        // Plants age faster if outside their comfort zone
        float ageRate = isInIdealRange ? 1f : outsideTemperatureMortalityRate;
        age += Time.deltaTime * ageRate;

        // If plant exceeds life expectancy, destroy it
        if (age >= lifeExpectancy)
        {
            Destroy(gameObject);
        }
    }

    public float GetNutritionalScore()
    {
        age = lifeExpectancy; // Force plant to die after being eaten
        return nutritionalScore;
    }

}
