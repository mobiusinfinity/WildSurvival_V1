using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Main inventory UI controller.
/// Manages the inventory window and slot displays.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform hotbarContainer;
    [SerializeField] private GameObject hotbarSlotPrefab;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private TextMeshProUGUI slotsText;
    [SerializeField] private Image weightBar;

    [Header("Item Info")]
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Image itemIcon;

    private InventoryManager inventory;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private List<InventorySlotUI> hotbarSlotUIs = new List<InventorySlotUI>();
    private bool isOpen = false;

    private void Start()
    {
        inventory = InventoryManager.Instance;

        // Check if inventory manager exists
        if (inventory == null)
        {
            Debug.LogError("[InventoryUI] InventoryManager.Instance is null! Creating one...");
            GameObject go = new GameObject("InventoryManager");
            inventory = go.AddComponent<InventoryManager>();
        }

        InitializeUI();
        SubscribeToEvents();

        // Start with inventory closed
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isOpen = false;
        }
        else
        {
            Debug.LogError("[InventoryUI] inventoryPanel is not assigned!");
        }
    }

    private void Update()
    {
        // Toggle inventory with I key
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[InventoryUI] I key pressed");
            ToggleInventory();
        }

        // Alternative: Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("[InventoryUI] Tab key pressed");
            ToggleInventory();
        }

        // Close inventory with Escape
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryUI] inventoryPanel is null! Creating basic UI...");
            CreateBasicUI();
            return;
        }

        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        Debug.Log("[InventoryUI] Opening inventory");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isOpen = true;
            UpdateUI();

            // Show cursor for inventory interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // The event system will handle firing OnInventoryOpened if needed
        }
    }

    public void CloseInventory()
    {
        Debug.Log("[InventoryUI] Closing inventory");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isOpen = false;
            HideItemInfo();

            // Hide cursor again
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // The event system will handle firing OnInventoryClosed if needed
        }
    }

    private void CreateBasicUI()
    {
        Debug.Log("[InventoryUI] Creating basic UI structure");

        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create inventory panel
        GameObject panel = new GameObject("InventoryPanel");
        panel.transform.SetParent(canvas.transform, false);
        inventoryPanel = panel;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.2f);
        rect.anchorMax = new Vector2(0.8f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Create title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY (Basic)";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        inventoryPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        // Skip if no prefabs are assigned
        if (slotPrefab == null || slotsContainer == null)
        {
            Debug.LogWarning("[InventoryUI] Slot prefab or container not assigned. Running in basic mode.");
            return;
        }

        // Create inventory slot UIs
        for (int i = 0; i < inventory.InventorySize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = slotGO.AddComponent<InventorySlotUI>();

            slotUI.Initialize(i, false);
            slotUIs.Add(slotUI);
        }

        // Create hotbar slot UIs only if references exist
        if (hotbarSlotPrefab != null && hotbarContainer != null)
        {
            for (int i = 0; i < inventory.HotbarSize; i++)
            {
                GameObject hotbarGO = Instantiate(hotbarSlotPrefab, hotbarContainer);
                InventorySlotUI hotbarUI = hotbarGO.GetComponent<InventorySlotUI>();
                if (hotbarUI == null) hotbarUI = hotbarGO.AddComponent<InventorySlotUI>();

                hotbarUI.Initialize(i, true);
                hotbarSlotUIs.Add(hotbarUI);
            }
        }

        UpdateUI();
    }

    private void SubscribeToEvents()
    {
        // Remove the null checks - just subscribe directly
        InventoryEvents.OnInventoryOpened += OnInventoryOpened;
        InventoryEvents.OnInventoryClosed += OnInventoryClosed;
        InventoryEvents.OnSlotChanged += OnSlotChanged;
        InventoryEvents.OnWeightChanged += OnWeightChanged;
        InventoryEvents.OnHotbarSlotSelected += OnHotbarSlotSelected;
    }

    private void UnsubscribeFromEvents()
    {
        // Remove the null checks - just unsubscribe directly
        InventoryEvents.OnInventoryOpened -= OnInventoryOpened;
        InventoryEvents.OnInventoryClosed -= OnInventoryClosed;
        InventoryEvents.OnSlotChanged -= OnSlotChanged;
        InventoryEvents.OnWeightChanged -= OnWeightChanged;
        InventoryEvents.OnHotbarSlotSelected -= OnHotbarSlotSelected;
    }

    private void OnInventoryOpened()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        UpdateUI();
    }

    private void OnInventoryClosed()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        HideItemInfo();
    }

    private void OnSlotChanged(int slotIndex, ItemStack stack)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count)
        {
            slotUIs[slotIndex].UpdateSlot(inventory.GetSlot(slotIndex));
        }

        UpdateUI();
    }

    private void OnWeightChanged(float currentWeight)
    {
        UpdateWeightDisplay();
    }

    private void OnHotbarSlotSelected(int slotIndex)
    {
        for (int i = 0; i < hotbarSlotUIs.Count; i++)
        {
            hotbarSlotUIs[i].SetSelected(i == slotIndex);
        }
    }

    private void UpdateUI()
    {
        if (inventory == null) return;

        // Update all slot displays
        for (int i = 0; i < slotUIs.Count && i < inventory.InventorySize; i++)
        {
            slotUIs[i].UpdateSlot(inventory.GetSlot(i)); // Fixed: removed named parameter
        }

        // Update hotbar
        if (inventory.HotbarSlots != null)
        {
            for (int i = 0; i < hotbarSlotUIs.Count && i < inventory.HotbarSize; i++)
            {
                hotbarSlotUIs[i].UpdateSlot(inventory.HotbarSlots[i]);
            }
        }

        UpdateWeightDisplay();
        UpdateSlotsDisplay();
    }

    private void UpdateWeightDisplay()
    {
        if (inventory == null) return;

        if (weightText != null)
        {
            weightText.text = $"{inventory.CurrentWeight:F1} / {inventory.MaxWeight:F1} kg";

            if (inventory.IsOverWeight)
            {
                weightText.color = Color.red;
            }
            else if (inventory.WeightPercentage > 75f)
            {
                weightText.color = Color.yellow;
            }
            else
            {
                weightText.color = Color.white;
            }
        }

        if (weightBar != null)
        {
            weightBar.fillAmount = inventory.WeightPercentage / 100f;
        }
    }

    private void UpdateSlotsDisplay()
    {
        if (inventory == null) return;

        if (slotsText != null)
        {
            slotsText.text = $"Slots: {inventory.UsedSlotCount} / {inventory.InventorySize}";
        }
    }

    public void ShowItemInfo(ItemData item)
    {
        if (item == null || itemInfoPanel == null) return;

        itemInfoPanel.SetActive(true);

        if (itemNameText != null)
            itemNameText.text = item.ItemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.Description;

        if (itemIcon != null && item.Icon != null)
            itemIcon.sprite = item.Icon;
    }

    public void HideItemInfo()
    {
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
    }
}