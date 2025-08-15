using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CookingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private Transform cookingSlotContainer;
    [SerializeField] private GameObject cookingSlotPrefab;
    [SerializeField] private Slider temperatureGauge;
    [SerializeField] private Text temperatureText;
    [SerializeField] private Button closeButton;

    private FireInstance currentFire;
    private CookingSystem cookingSystem;
    private List<CookingSlotUI> cookingSlots = new List<CookingSlotUI>();

    private void Awake()
    {
        if (cookingSystem == null)
            cookingSystem = GetComponent<CookingSystem>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // Start hidden
        if (cookingPanel != null)
            cookingPanel.SetActive(false);
    }

    public void Open(FireInstance fire)
    {
        currentFire = fire;
        if (cookingPanel != null)
            cookingPanel.SetActive(true);
        UpdateDisplay();
    }

    public void Close()
    {
        if (cookingPanel != null)
            cookingPanel.SetActive(false);
        currentFire = null;
    }

    public void StartCooking(FireInstance fire, ItemDefinition foodItem)
    {
        if (cookingSystem != null)
        {
            cookingSystem.StartCooking(fire, foodItem);
        }
        UpdateDisplay();
    }

    private void Update()
    {
        if (currentFire != null && cookingPanel != null && cookingPanel.activeSelf)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (currentFire == null) return;

        // Update temperature
        float temp = currentFire.GetCookingTemperature();
        if (temperatureGauge != null)
        {
            temperatureGauge.value = temp / 1000f; // Normalize to 0-1
        }

        if (temperatureText != null)
        {
            temperatureText.text = $"{temp:F0}°C";

            // Color code the temperature
            if (temp < 100f)
                temperatureText.color = Color.blue;
            else if (temp < 400f)
                temperatureText.color = Color.yellow;
            else if (temp < 600f)
                temperatureText.color = new Color(1f, 0.5f, 0f); // Orange
            else
                temperatureText.color = Color.red;
        }
    }

    [System.Serializable]
    public class CookingSlotUI
    {
        public GameObject slotObject;
        public Image itemIcon;
        public Slider progressBar;
        public Text statusText;
        public Button removeButton;
        public ItemDefinition cookingItem;
        public float progress;
    }
}