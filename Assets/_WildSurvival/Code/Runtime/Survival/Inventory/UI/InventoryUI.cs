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

    private void Start()
    {
        inventory = InventoryManager.Instance;
        InitializeUI();
        SubscribeToEvents();

        // Start with inventory closed
        inventoryPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        // Create inventory slot UIs
        for (int i = 0; i < inventory.InventorySize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = slotGO.AddComponent<InventorySlotUI>();

            slotUI.Initialize(i, false);
            slotUIs.Add(slotUI);
        }

        // Create hotbar slot UIs
        for (int i = 0; i < inventory.HotbarSize; i++)
        {
            GameObject hotbarGO = Instantiate(hotbarSlotPrefab, hotbarContainer);
            InventorySlotUI hotbarUI = hotbarGO.GetComponent<InventorySlotUI>();
            if (hotbarUI == null) hotbarUI = hotbarGO.AddComponent<InventorySlotUI>();

            hotbarUI.Initialize(i, true);
            hotbarSlotUIs.Add(hotbarUI);
        }

        UpdateUI();
    }

    private void SubscribeToEvents()
    {
        InventoryEvents.OnInventoryOpened += OnInventoryOpened;
        InventoryEvents.OnInventoryClosed += OnInventoryClosed;
        InventoryEvents.OnSlotChanged += OnSlotChanged;
        InventoryEvents.OnWeightChanged += OnWeightChanged;
        InventoryEvents.OnHotbarSlotSelected += OnHotbarSlotSelected;
    }

    private void UnsubscribeFromEvents()
    {
        InventoryEvents.OnInventoryOpened -= OnInventoryOpened;
        InventoryEvents.OnInventoryClosed -= OnInventoryClosed;
        InventoryEvents.OnSlotChanged -= OnSlotChanged;
        InventoryEvents.OnWeightChanged -= OnWeightChanged;
        InventoryEvents.OnHotbarSlotSelected -= OnHotbarSlotSelected;
    }

    private void OnInventoryOpened()
    {
        inventoryPanel.SetActive(true);
        UpdateUI();
    }

    private void OnInventoryClosed()
    {
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
        // Update all slot displays
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateSlot(inventory.GetSlot(i));
        }

        // Update hotbar
        for (int i = 0; i < hotbarSlotUIs.Count; i++)
        {
            hotbarSlotUIs[i].UpdateSlot(inventory.HotbarSlots[i]);
        }

        UpdateWeightDisplay();
        UpdateSlotsDisplay();
    }

    private void UpdateWeightDisplay()
    {
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