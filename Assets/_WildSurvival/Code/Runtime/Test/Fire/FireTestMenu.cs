using UnityEngine;

public class FireTestMenu : MonoBehaviour
{
    private bool showMenu = false;
    private FireInstance selectedFire;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showMenu = !showMenu;
        }
    }

    void OnGUI()
    {
        if (!showMenu) return;

        GUI.Box(new Rect(Screen.width - 310, 10, 300, 400), "Fire System Test Menu");

        int y = 40;

        // Get nearest fire
        FireInstance[] fires = FindObjectsOfType<FireInstance>();
        if (fires.Length > 0)
        {
            selectedFire = fires[0];

            GUI.Label(new Rect(Screen.width - 300, y, 280, 20),
                $"Selected: {selectedFire.name}");
            y += 30;

            // Quick actions
            if (GUI.Button(new Rect(Screen.width - 300, y, 130, 25), "Ignite"))
            {
                selectedFire.TryIgnite(new IgnitionSource
                {
                    name = "Debug",
                    successRate = 1f
                });
            }

            if (GUI.Button(new Rect(Screen.width - 160, y, 130, 25), "Extinguish"))
            {
                selectedFire.Extinguish();
            }
            y += 30;

            if (GUI.Button(new Rect(Screen.width - 300, y, 130, 25), "Add Fuel"))
            {
                selectedFire.DebugAddFuel(50f);
            }

            if (GUI.Button(new Rect(Screen.width - 160, y, 130, 25), "Max Fuel"))
            {
                selectedFire.DebugAddFuel(100f);
            }
            y += 30;

            // Temperature control
            GUI.Label(new Rect(Screen.width - 300, y, 280, 20), "Temperature:");
            y += 20;
            float temp = GUI.HorizontalSlider(
                new Rect(Screen.width - 300, y, 280, 20),
                selectedFire.GetCurrentTemperature(),
                0f, 800f
            );
            selectedFire.SetTemperature(temp);
            y += 30;
        }

        // Spawn controls
        GUI.Label(new Rect(Screen.width - 300, y, 280, 20), "Spawn Fire:");
        y += 25;

        if (GUI.Button(new Rect(Screen.width - 300, y, 90, 25), "Campfire"))
        {
            SpawnFire(FireInstance.FireType.Campfire);
        }

        if (GUI.Button(new Rect(Screen.width - 205, y, 90, 25), "Torch"))
        {
            SpawnFire(FireInstance.FireType.Torch);
        }

        if (GUI.Button(new Rect(Screen.width - 110, y, 90, 25), "Forge"))
        {
            SpawnFire(FireInstance.FireType.Forge);
        }
        y += 30;

        // Weather control
        GUI.Label(new Rect(Screen.width - 300, y, 280, 20), "Weather:");
        y += 25;

        if (GUI.Button(new Rect(Screen.width - 300, y, 90, 25), "Clear"))
        {
            SetWeather(WeatherSystem.WeatherType.Clear);
        }

        if (GUI.Button(new Rect(Screen.width - 205, y, 90, 25), "Rain"))
        {
            SetWeather(WeatherSystem.WeatherType.LightRain);
        }

        if (GUI.Button(new Rect(Screen.width - 110, y, 90, 25), "Storm"))
        {
            SetWeather(WeatherSystem.WeatherType.Storm);
        }
    }

    void SpawnFire(FireInstance.FireType type)
    {
        Transform player = GameObject.FindWithTag("Player").transform;
        Vector3 spawnPos = player.position + player.forward * 3f;
        spawnPos.y = 0;

        FirePrefabFactory factory = FindObjectOfType<FirePrefabFactory>();
        if (factory != null)
        {
            factory.CreateFire(type, spawnPos);
        }
    }

    void SetWeather(WeatherSystem.WeatherType weather)
    {
        WeatherSystem system = FindObjectOfType<WeatherSystem>();
        if (system != null)
        {
            system.CurrentWeather = weather;
        }
    }
}