using UnityEngine;
using TMPro;

public class FireDebugDisplay : MonoBehaviour
{
    private TextMeshProUGUI text;
    private GameObject player;
    private float updateInterval = 0.2f;
    private float timer;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        player = GameObject.FindWithTag("Player");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0;
            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        if (player == null || text == null) return;

        var stats = player.GetComponent<PlayerStats>();
        //var vitals = player.GetComponent<PlayerVitals>();

        string info = "FIRE SYSTEM DEBUG\n";
        info += "================\n";

        if (stats != null)
        {
            info += $"Body Temp: {stats.BodyTemperature:F1}°C\n";
            info += $"Health: {stats.CurrentHealth:F0}/{stats.MaxHealth:F0}\n";
            info += $"Stamina: {stats.CurrentStamina:F0}/{stats.MaxStamina:F0}\n";
        }

        // Find nearest fire
        var fires = FindObjectsOfType<FireInstance>();
        FireInstance nearestFire = null;
        float nearestDist = float.MaxValue;

        foreach (var fire in fires)
        {
            float dist = Vector3.Distance(player.transform.position, fire.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestFire = fire;
            }
        }

        if (nearestFire != null && nearestDist < 10f)
        {
            info += $"\nNearby Fire: {nearestDist:F1}m\n";
            info += $"Fire Temp: {nearestFire.GetCookingTemperature():F0}°C\n";
            info += $"Fire State: {nearestFire.GetState()}\n";
            info += $"Fuel: {nearestFire.GetFuelPercentage():F0}%\n";
        }
        else
        {
            info += "\nNo fire nearby\n";
        }

        info += $"\nFPS: {(1f / Time.deltaTime):F0}";

        text.text = info;
    }
}