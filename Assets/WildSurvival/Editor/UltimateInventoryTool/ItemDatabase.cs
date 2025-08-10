using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WildSurvival.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildSurvival.Database
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "WildSurvival/Databases/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        public void AddItem(ItemDefinition item)
        {
            if (item != null && !items.Contains(item))
            {
                items.Add(item);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void RemoveItem(ItemDefinition item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public List<ItemDefinition> GetAllItems()
        {
            return new List<ItemDefinition>(items);
        }

        public ItemDefinition GetItem(string itemID)
        {
            return items.FirstOrDefault(i => i.itemID == itemID);
        }

        public void Clear()
        {
            items.Clear();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public List<ItemDefinition> GetItemsByCategory(ItemCategory category)
        {
            return items.Where(i => i.primaryCategory == category).ToList();
        }

        public List<ItemDefinition> GetItemsWithTag(ItemTag tag)
        {
            return items.Where(i => i.tags != null && i.tags.Contains(tag)).ToList();
        }
    }
}