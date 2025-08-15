// Complete the FireManagementUI class that was cut off
using System.Collections.Generic;
using UnityEngine;


public class FireManagementUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject firePanel;
    [SerializeField] private UnityEngine.UI.Text temperatureText;
    [SerializeField] private UnityEngine.UI.Slider fuelBar;
    [SerializeField] private UnityEngine.UI.Text stateText;
    [SerializeField] private Transform fuelListContainer;
    [SerializeField] private GameObject fuelItemPrefab;
    [SerializeField] private UnityEngine.UI.Button igniteButton;
    [SerializeField] private UnityEngine.UI.Button extinguishButton;
    [SerializeField] private UnityEngine.UI.Button addFuelButton;

    private FireInstance currentFire;
    private FireInteractionController controller;
    private List<GameObject> fuelItemButtons = new List<GameObject>();

    public void Show(FireInstance fire)
    {
        currentFire = fire;
        firePanel.SetActive(true);
        UpdateDisplay();
    }

    public void Hide()
    {
        firePanel.SetActive(false);
        currentFire = null;
        ClearFuelList();
    }

    public void OpenManagementPanel(FireInstance fire, FireInteractionController ctrl)
    {
        currentFire = fire;
        controller = ctrl;
        firePanel.SetActive(true);

        RefreshFuelInventory();
        UpdateDisplay();
    }

    private void Update()
    {
        if (currentFire != null && firePanel.activeSelf)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (currentFire == null) return;

        // Temperature
        float temp = currentFire.GetCookingTemperature();
        temperatureText.text = $"Temperature: {temp:F0}°C";

        // Fuel level
        float fuelPercent = currentFire.GetFuelPercentage();
        fuelBar.value = fuelPercent;

        // State
        var state = currentFire.GetState();
        stateText.text = $"State: {state}";
        stateText.color = GetStateColor(state);

        // Button states
        igniteButton.interactable = state == FireInstance.FireState.Unlit;
        extinguishButton.interactable = state != FireInstance.FireState.Unlit &&
                                       state != FireInstance.FireState.Extinguished;
    }

    private Color GetStateColor(FireInstance.FireState state)
    {
        return state switch
        {
            FireInstance.FireState.Unlit => Color.gray,
            FireInstance.FireState.Igniting => Color.yellow,
            FireInstance.FireState.Smoldering => new Color(1f, 0.5f, 0f),
            FireInstance.FireState.Burning => Color.red,
            FireInstance.FireState.Blazing => new Color(1f, 0.2f, 0f),
            FireInstance.FireState.Dying => new Color(0.5f, 0.3f, 0.1f),
            FireInstance.FireState.Extinguished => Color.black,
            _ => Color.white
        };
    }

    private void RefreshFuelInventory()
    {
        ClearFuelList();

        if (controller == null) return;

        var availableFuels = controller.GetAvailableFuels();

        foreach (var fuel in availableFuels)
        {
            CreateFuelButton(fuel);
        }
    }

    private void CreateFuelButton(ItemDefinition fuel)
    {
        var btnObj = Instantiate(fuelItemPrefab, fuelListContainer);
        var btn = btnObj.GetComponent<UnityEngine.UI.Button>();

        // Set display
        var text = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
        text.text = $"{fuel.displayName} ({GetFuelBurnTime(fuel)}min)";

        // Set icon if available
        var image = btnObj.GetComponentInChildren<UnityEngine.UI.Image>();
        if (image != null && fuel.icon != null)
        {
            image.sprite = fuel.icon;
        }

        // Add click handler
        btn.onClick.AddListener(() => {
            controller.AddFuelToFire(currentFire, fuel.itemID, 1);
            RefreshFuelInventory();
        });

        fuelItemButtons.Add(btnObj);
    }

    private float GetFuelBurnTime(ItemDefinition fuel)
    {
        // Calculate based on fuel properties
        if (fuel.fuelProperties != null)
        {
            return fuel.fuelProperties.burnDuration;
        }

        // Default times based on item ID
        return fuel.itemID switch
        {
            "fuel_tinder" => 2f,
            "fuel_kindling" => 5f,
            "wood_log" => 20f,
            "fuel_charcoal" => 60f,
            _ => 10f
        };
    }

    private void ClearFuelList()
    {
        foreach (var btn in fuelItemButtons)
        {
            Destroy(btn);
        }
        fuelItemButtons.Clear();
    }

    // Button handlers
    public void OnIgniteButtonClicked()
    {
        controller?.TryIgniteFire(currentFire);
    }

    public void OnExtinguishButtonClicked()
    {
        currentFire?.ExtinguishFire("Manually extinguished");
        Hide();
    }

    public void OnAddFuelButtonClicked()
    {
        RefreshFuelInventory();
    }
}