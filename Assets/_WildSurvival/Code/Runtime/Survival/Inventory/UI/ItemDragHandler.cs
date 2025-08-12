using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Helper component for handling drag and drop operations.
/// Manages visual feedback during dragging.
/// </summary>
public class ItemDragHandler : MonoBehaviour
{
    private static ItemDragHandler instance;
    public static ItemDragHandler Instance => instance;

    [Header("Drag Visual")]
    [SerializeField] private GameObject dragVisualPrefab;
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private float dragAlpha = 0.6f;

    private GameObject currentDragVisual;
    private Image dragImage;
    private RectTransform dragRect;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Find or create drag canvas
        if (dragCanvas == null)
        {
            dragCanvas = GetComponentInParent<Canvas>();
            if (dragCanvas == null)
            {
                GameObject canvasObj = new GameObject("DragCanvas");
                dragCanvas = canvasObj.AddComponent<Canvas>();
                dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dragCanvas.sortingOrder = 100;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
    }

    public void StartDrag(ItemData item, int quantity)
    {
        if (item == null || item.Icon == null) return;

        // Create drag visual
        if (dragVisualPrefab != null)
        {
            currentDragVisual = Instantiate(dragVisualPrefab, dragCanvas.transform);
        }
        else
        {
            currentDragVisual = new GameObject("DragVisual");
            currentDragVisual.transform.SetParent(dragCanvas.transform);
            dragImage = currentDragVisual.AddComponent<Image>();
        }

        // Setup visual
        if (dragImage == null)
            dragImage = currentDragVisual.GetComponent<Image>();

        dragImage.sprite = item.Icon;
        dragImage.raycastTarget = false;

        Color color = dragImage.color;
        color.a = dragAlpha;
        dragImage.color = color;

        dragRect = currentDragVisual.GetComponent<RectTransform>();
        if (dragRect == null)
            dragRect = currentDragVisual.AddComponent<RectTransform>();

        dragRect.sizeDelta = new Vector2(64, 64); // Standard icon size

        // Add quantity text if needed
        if (quantity > 1)
        {
            GameObject textObj = new GameObject("QuantityText");
            textObj.transform.SetParent(currentDragVisual.transform);

            Text quantityText = textObj.AddComponent<Text>();
            quantityText.text = quantity.ToString();
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.fontSize = 14;
            quantityText.fontStyle = FontStyle.Bold;
            quantityText.color = Color.white;
            quantityText.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = new Vector2(-2, 2);
        }

        UpdateDragPosition(Input.mousePosition);
    }

    public void UpdateDragPosition(Vector3 position)
    {
        if (currentDragVisual != null && dragRect != null)
        {
            dragRect.position = position;
        }
    }

    public void EndDrag()
    {
        if (currentDragVisual != null)
        {
            Destroy(currentDragVisual);
            currentDragVisual = null;
            dragImage = null;
            dragRect = null;
        }
    }

    public bool IsDragging()
    {
        return currentDragVisual != null;
    }

    private void Update()
    {
        if (IsDragging())
        {
            UpdateDragPosition(Input.mousePosition);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}