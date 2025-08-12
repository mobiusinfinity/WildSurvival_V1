using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Database for storing and managing ItemData assets
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Wild Survival/Databases/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("Database")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public void AddItem(ItemData item)
    {
        if (item != null && !items.Contains(item))
        {
            items.Add(item);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(items);
    }

    public ItemData GetItem(string itemID)
    {
        return items.FirstOrDefault(i => i.itemID == itemID);
    }

    public void Clear()
    {
        items.Clear();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}