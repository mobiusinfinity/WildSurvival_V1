using UnityEngine;

/// <summary>
/// Runtime debug overlay for fire system testing
/// </summary>
public class FireDebugOverlay : MonoBehaviour
{
    private bool showOverlay = true;
    private bool showFireList = true;
    private bool showPlayerStats = true;
    private bool showControls = true;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    private FireInstance[] allFires;
    private GameObject player;
    private float refreshTimer = 0.5f;
    private float nextRefresh;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        SetupStyles();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showOverlay = !showOverlay;
        }

        if (Time.time >= nextRefresh)
        {
            allFires = FindObjectsOfType<FireInstance>();
            nextRefresh = Time.time + refreshTimer;
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        if (showFireList)
            DrawFireList();

        if (showPlayerStats)
            DrawPlayerStats();

        if (showControls)
            DrawControls();

        DrawToggleButtons();
    }

    private void SetupStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 12;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 10;
    }

    private void DrawFireList()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 300), boxStyle);

        GUILayout.Label("🔥 ACTIVE FIRES", labelStyle);
        GUILayout.Space(5);

        if (allFires != null && allFires.Length > 0)
        {
            foreach (var fire in allFires)
            {
                if (fire == null) continue;

                GUILayout.BeginHorizontal();

                // State indicator
                string stateIcon = GetStateIcon(fire.GetState());
                GUILayout.Label(stateIcon, GUILayout.Width(20));

                // Fire info
                GUILayout.Label($"{fire.name}", labelStyle, GUILayout.Width(100));
                GUILayout.Label($"{fire.GetCookingTemperature():F0}°C", labelStyle, GUILayout.Width(50));
                GUILayout.Label($"{fire.GetFuelPercentage():F0}%", labelStyle, GUILayout.Width(40));

                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("No active fires", labelStyle);
        }

        GUILayout.EndArea();
    }

    private void DrawPlayerStats()
    {
        if (player == null) return;

        GUILayout.BeginArea(new Rect(10, 320, 250, 150), boxStyle);

        GUILayout.Label("👤 PLAYER STATS", labelStyle);
        GUILayout.Space(5);

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            GUILayout.Label($"Body Temp: {stats.BodyTemperature:F1}°C", labelStyle);
            GUILayout.Label($"Health: {stats.CurrentHealth:F0}/{stats.MaxHealth:F0}", labelStyle);
            GUILayout.Label($"Stamina: {stats.CurrentStamina:F0}/{stats.MaxStamina:F0}", labelStyle);
            GUILayout.Label($"Hunger: {stats.CurrentHunger:F0}", labelStyle);
            GUILayout.Label($"Thirst: {stats.CurrentThirst:F0}", labelStyle);
        }

        GUILayout.EndArea();
    }

    private void DrawControls()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 260, 10, 250, 200), boxStyle);

        GUILayout.Label("🎮 QUICK CONTROLS", labelStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("Give Fire Materials", buttonStyle))
        {
            GiveFireMaterials();
        }

        if (GUILayout.Button("Spawn Campfire Here", buttonStyle))
        {
            SpawnCampfire();
        }

        if (GUILayout.Button("Ignite All Fires", buttonStyle))
        {
            IgniteAllFires();
        }

        if (GUILayout.Button("Extinguish All", buttonStyle))
        {
            ExtinguishAllFires();
        }

        if (GUILayout.Button("Make Player Cold", buttonStyle))
        {
            MakePlayerCold();
        }

        if (GUILayout.Button("Toggle Rain", buttonStyle))
        {
            ToggleRain();
        }

        GUILayout.EndArea();
    }

    private void DrawToggleButtons()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 260, 220, 250, 100), boxStyle);

        GUILayout.Label("Display Options", labelStyle);

        showFireList = GUILayout.Toggle(showFireList, "Show Fire List");
        showPlayerStats = GUILayout.Toggle(showPlayerStats, "Show Player Stats");
        showControls = GUILayout.Toggle(showControls, "Show Controls");

        GUILayout.Label("Press F1 to toggle overlay", labelStyle);

        GUILayout.EndArea();
    }

    private string GetStateIcon(FireInstance.FireState state)
    {
        return state switch
        {
            FireInstance.FireState.Unlit => "⚫",
            FireInstance.FireState.Igniting => "🟡",
            FireInstance.FireState.Smoldering => "🟠",
            FireInstance.FireState.Burning => "🔴",
            FireInstance.FireState.Blazing => "🔥",
            FireInstance.FireState.Dying => "🟤",
            FireInstance.FireState.Extinguished => "⚪",
            _ => "❓"
        };
    }

    private void GiveFireMaterials()
    {
        var inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.AddItem("stone", 10);
            inventory.AddItem("stick", 10);
            inventory.AddItem("tinder", 5);
            inventory.AddItem("matches", 5);
            inventory.AddItem("wood_log", 10);
        }
    }

    private void SpawnCampfire()
    {
        if (player == null) return;

        var prefab = Resources.Load<GameObject>("Prefabs/Fire/Campfire");
        if (prefab != null)
        {
            var pos = player.transform.position + player.transform.forward * 3f;
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    private void IgniteAllFires()
    {
        foreach (var fire in allFires)
        {
            if (fire != null)
            {
                fire.TryIgnite(IgnitionSource.Matches);
            }
        }
    }

    private void ExtinguishAllFires()
    {
        foreach (var fire in allFires)
        {
            if (fire != null)
            {
                fire.ExtinguishFire("Debug command");
            }
        }
    }

    private void MakePlayerCold()
    {
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.SetBodyTemperature(32f);
            }
        }
    }

    private void ToggleRain()
    {
        // Would integrate with weather system
        Debug.Log("Rain toggled (requires weather system)");
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}