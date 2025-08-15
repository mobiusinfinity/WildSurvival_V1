// FireTestController.cs - Main test controller
using UnityEngine;

public class FireTestController : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== FIRE SYSTEM TEST CONTROLS ===");
        Debug.Log("Movement: WASD");
        Debug.Log("Look: Mouse");
        Debug.Log("Sprint: Left Shift");
        Debug.Log("Build Campfire: B");
        Debug.Log("Interact with Fire: F");
        Debug.Log("Open Inventory: I");
        Debug.Log("Quick Actions:");
        Debug.Log("  1 - Give test items");
        Debug.Log("  2 - Set player cold");
        Debug.Log("  3 - Set player hot");
        Debug.Log("  4 - Start rain");
        Debug.Log("  5 - Add wind");
        Debug.Log("  H - Show help");
    }

    void Update()
    {
        // Quick test actions
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GiveTestItems();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetPlayerCold();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetPlayerHot();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ToggleRain();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ToggleWind();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHelp();
        }
    }

    void GiveTestItems()
    {
        var inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.AddItem("stone", 10);
            inventory.AddItem("stick", 10);
            inventory.AddItem("tinder", 5);
            inventory.AddItem("matches", 3);
            inventory.AddItem("wood_log", 5);
            inventory.AddItem("meat_raw", 3);

            NotificationSystem.Instance?.ShowNotification(
                "Test items added to inventory!",
                NotificationSystem.NotificationType.Success);
        }
    }

    void SetPlayerCold()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var vitals = player.GetComponent<PlayerStats>();
            if (vitals != null)
            {
                // Set cold temperature
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.SetBodyTemperature(33f);
                    NotificationSystem.Instance?.ShowNotification(
                        "Player temperature set to COLD (33°C)",
                        NotificationSystem.NotificationType.Warning);
                }
            }
        }
    }

    void SetPlayerHot()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.SetBodyTemperature(40f);
                NotificationSystem.Instance?.ShowNotification(
                    "Player temperature set to HOT (40°C)",
                    NotificationSystem.NotificationType.Warning);
            }
        }
    }

    void ToggleRain()
    {
        // Set rain on all fires
        var fires = FindObjectsOfType<FireInstance>();
        foreach (var fire in fires)
        {
            // Would need to add rain intensity setter to FireInstance
            NotificationSystem.Instance?.ShowNotification(
                "Rain effect toggled (visual only in test)",
                NotificationSystem.NotificationType.Info);
        }
    }

    void ToggleWind()
    {
        NotificationSystem.Instance?.ShowNotification(
            "Wind effect toggled (visual only in test)",
            NotificationSystem.NotificationType.Info);
    }

    void ShowHelp()
    {
        string help = "FIRE SYSTEM TEST CONTROLS\n" +
                     "========================\n" +
                     "WASD - Move\n" +
                     "Mouse - Look\n" +
                     "Shift - Sprint\n" +
                     "B - Build Campfire\n" +
                     "F - Interact with Fire\n" +
                     "I - Open Inventory\n" +
                     "1 - Give Test Items\n" +
                     "2 - Make Player Cold\n" +
                     "3 - Make Player Hot\n" +
                     "4 - Toggle Rain\n" +
                     "5 - Toggle Wind";

        Debug.Log(help);
        NotificationSystem.Instance?.ShowNotification(
            "Help printed to console (press H)",
            NotificationSystem.NotificationType.Info);
    }
}