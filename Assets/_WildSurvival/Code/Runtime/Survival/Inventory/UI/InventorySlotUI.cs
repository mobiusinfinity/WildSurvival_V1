using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
    /// <summary>
    /// UI component for individual inventory slots.
    /// Handles display and interaction for a single slot.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionBorder;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.3f);

        private int slotIndex;
        private bool isHotbarSlot;
        private InventorySlot inventorySlot;
        private bool isSelected;

        private static GameObject draggedIcon;
        private static InventorySlotUI draggedSlot;

        public int SlotIndex => slotIndex;
        public bool IsEmpty => inventorySlot == null || inventorySlot.IsEmpty;

        public void Initialize(int index, bool hotbar = false)
        {
            slotIndex = index;
            isHotbarSlot = hotbar;

            if (selectionBorder != null)
                selectionBorder.gameObject.SetActive(false);

            UpdateDisplay();
        }

        public void UpdateSlot(InventorySlot slot)
        {
            inventorySlot = slot;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (inventorySlot == null || inventorySlot.IsEmpty)
            {
                // Empty slot
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.color = emptyColor;
                }

                if (quantityText != null)
                    quantityText.text = "";

                if (durabilityBar != null)
                    durabilityBar.gameObject.SetActive(false);
            }
            else
            {
                // Filled slot
                var stack = inventorySlot.Stack;

                if (iconImage != null && stack.Item.Icon != null)
                {
                    iconImage.sprite = stack.Item.Icon;
                    iconImage.color = Color.white;
                }

                if (quantityText != null)
                {
                    if (stack.Quantity > 1)
                        quantityText.text = stack.Quantity.ToString();
                    else
                        quantityText.text = "";
                }

                if (durabilityBar != null)
                {
                    if (stack.Item.HasDurability)
                    {
                        durabilityBar.gameObject.SetActive(true);
                        durabilityBar.fillAmount = stack.Durability / 100f;

                        // Color based on durability
                        if (stack.Durability > 50f)
                            durabilityBar.color = Color.green;
                        else if (stack.Durability > 25f)
                            durabilityBar.color = Color.yellow;
                        else
                            durabilityBar.color = Color.red;
                    }
                    else
                    {
                        durabilityBar.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectionBorder != null)
                selectionBorder.gameObject.SetActive(selected);

            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : normalColor;
        }

        #region Pointer Events
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    // Quick move to/from hotbar
                    if (isHotbarSlot)
                    {
                        // Move from hotbar to inventory
                        InventoryManager.Instance.SwapSlots(slotIndex, InventoryManager.Instance.GetFirstEmptySlot().SlotIndex);
                    }
                    else
                    {
                        // Move to first empty hotbar slot
                        // Implementation depends on hotbar system
                    }
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    // Split stack
                    if (!IsEmpty && inventorySlot.Quantity > 1)
                    {
                        InventoryManager.Instance.SplitStack(slotIndex, inventorySlot.Quantity / 2);
                    }
                }
                else
                {
                    // Select slot
                    if (isHotbarSlot)
                    {
                        InventoryEvents.HotbarSlotSelected(slotIndex);
                    }
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Use item
                if (!IsEmpty)
                {
                    InventoryManager.Instance.UseItem(slotIndex);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                // Drop item
                if (!IsEmpty)
                {
                    InventoryManager.Instance.DropItem(slotIndex, Input.GetKey(KeyCode.LeftControl) ? inventorySlot.Quantity : 1);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && !isSelected)
                backgroundImage.color = highlightColor;

            // Show item tooltip
            if (!IsEmpty && inventorySlot.Stack != null)
            {
                var ui = FindObjectOfType<InventoryUI>();
                if (ui != null)
                    ui.ShowItemInfo(inventorySlot.Stack.Item);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null && !isSelected)
                backgroundImage.color = normalColor;

            // Hide tooltip
            var ui = FindObjectOfType<InventoryUI>();
            if (ui != null)
                ui.HideItemInfo();
        }
        #endregion

        #region Drag & Drop
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;

            draggedSlot = this;
            InventoryManager.Instance.StartDragging(slotIndex);

            // Create drag visual
            if (draggedIcon == null)
            {
                draggedIcon = new GameObject("DraggedIcon");
                draggedIcon.transform.SetParent(transform.root);
                var image = draggedIcon.AddComponent<Image>();
                image.raycastTarget = false;
            }

            var dragImage = draggedIcon.GetComponent<Image>();
            dragImage.sprite = inventorySlot.Stack.Item.Icon;
            dragImage.color = new Color(1, 1, 1, 0.6f);

            draggedIcon.transform.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedIcon != null)
                draggedIcon.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            InventoryManager.Instance.EndDragging(-1);

            if (draggedIcon != null)
                Destroy(draggedIcon);

            draggedSlot = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (draggedSlot != null && draggedSlot != this)
            {
                InventoryManager.Instance.EndDragging(slotIndex);
            }
        }
        #endregion
    }