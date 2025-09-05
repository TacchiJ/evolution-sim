using UnityEngine;

public class TemperatureControllerScript : MonoBehaviour
{
    [Header("Temperature Settings")]
    public float temperature = 0f;
    public bool movingTemperature = false;
    [Range(0f, 1f)]
    public float temperatureSpeed = 0.1f; 

    private void Update()
    {
        if (!movingTemperature) return;

        temperature += temperatureSpeed * Time.deltaTime;
        if (temperature > 1f)
            temperature -= 1f;

    }

    public float GetTemperature()
    {
        return temperature;
    }
}
