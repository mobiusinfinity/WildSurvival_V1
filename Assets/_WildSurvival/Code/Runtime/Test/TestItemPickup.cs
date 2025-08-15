using UnityEngine;

/// <summary>
/// Simple item pickup for testing
/// </summary>
public class TestItemPickup : MonoBehaviour
{
    [Header("Item Configuration")]
    public string itemID = "stone";
    public int quantity = 1;
    public bool destroyOnPickup = true;

    [Header("Visual")]
    public bool rotateItem = true;
    public float rotationSpeed = 30f;
    public bool floatItem = true;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 1f;

    private float startY;
    private bool canPickup = true;

    private void Start()
    {
        startY = transform.position.y;

        // Add visual indicator
        if (GetComponent<Collider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.radius = 1f;
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        // Floating animation
        if (floatItem)
        {
            Vector3 pos = transform.position;
            pos.y = startY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = pos;
        }

        // Rotation animation
        if (rotateItem)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canPickup) return;

        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                // Try to add to inventory
                if (TryAddToInventory(inventory))
                {
                    OnPickupSuccess();
                }
                else
                {
                    OnPickupFailed();
                }
            }
        }
    }

    private bool TryAddToInventory(PlayerInventory inventory)
    {
        // Get item definition
        ItemDefinition itemDef = GetItemDefinition();
        if (itemDef != null)
        {
            return inventory.TryAddItem(itemDef, quantity);
        }

        // Fallback - just log for testing
        Debug.Log($"[TEST] Picked up {quantity}x {itemID}");
        return true;
    }

    private ItemDefinition GetItemDefinition()
    {
        // Try to load from Resources
        ItemDefinition item = Resources.Load<ItemDefinition>($"Items/{itemID}");

        // Create temporary for testing
        if (item == null)
        {
            Debug.LogWarning($"Creating temporary ItemDefinition for {itemID}");
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemID = itemID;
            item.displayName = itemID;
            item.weight = 0.1f;
            item.maxStackSize = 99;
        }

        return item;
    }

    private void OnPickupSuccess()
    {
        // Show notification
        NotificationSystem notifications = FindObjectOfType<NotificationSystem>();
        if (notifications != null)
        {
            notifications.ShowNotification(
                $"Picked up {quantity}x {itemID}",
                NotificationSystem.NotificationType.Item
            );
        }

        // Play sound
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }

        // Destroy or disable
        if (destroyOnPickup)
        {
            Destroy(gameObject, 0.1f);
        }
        else
        {
            canPickup = false;
            gameObject.SetActive(false);
        }
    }

    private void OnPickupFailed()
    {
        Debug.Log($"Inventory full - cannot pick up {itemID}");

        NotificationSystem notifications = FindObjectOfType<NotificationSystem>();
        if (notifications != null)
        {
            notifications.ShowNotification(
                "Inventory full!",
                NotificationSystem.NotificationType.Warning
            );
        }
    }
}