//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using WildSurvival.Data;

//namespace WildSurvival.Systems.Inventory
//{
//    public class InventoryManager : MonoBehaviour
//    {
//        [Header("Database References")]
//        [SerializeField] private ItemDatabase itemDatabase;
//        [SerializeField] private RecipeDatabase recipeDatabase;

//        [Header("Configuration")]
//        [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 6);
//        [SerializeField] private float maxWeight = 50f;

//        // Runtime data
//        private GridCell[,] grid;
//        private List<ItemInstance> items;
//        private float currentWeight;

//        // Singleton pattern for easy access
//        private static InventoryManager _instance;
//        public static InventoryManager Instance
//        {
//            get
//            {
//                if (_instance == null)
//                {
//                    _instance = FindObjectOfType<InventoryManager>();
//                    if (_instance == null)
//                    {
//                        GameObject go = new GameObject("InventoryManager");
//                        _instance = go.AddComponent<InventoryManager>();
//                    }
//                }
//                return _instance;
//            }
//        }

//        private void Awake()
//        {
//            if (_instance == null)
//            {
//                _instance = this;
//                DontDestroyOnLoad(gameObject);
//                Initialize();
//            }
//            else if (_instance != this)
//            {
//                Destroy(gameObject);
//            }
//        }

//        private void Initialize()
//        {
//            // Load databases
//            if (itemDatabase == null)
//            {
//                itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
//                if (itemDatabase == null)
//                {
//                    // Try loading from specific path
//                    itemDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(
//                        "Assets/_Project/Data/ItemDatabase.asset");
//                }
//            }

//            if (recipeDatabase == null)
//            {
//                recipeDatabase = Resources.Load<RecipeDatabase>("RecipeDatabase");
//                if (recipeDatabase == null)
//                {
//                    recipeDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeDatabase>(
//                        "Assets/_Project/Data/RecipeDatabase.asset");
//                }
//            }

//            // Initialize grid
//            grid = new GridCell[gridSize.x, gridSize.y];
//            for (int x = 0; x < gridSize.x; x++)
//            {
//                for (int y = 0; y < gridSize.y; y++)
//                {
//                    grid[x, y] = new GridCell(x, y);
//                }
//            }

//            items = new List<ItemInstance>();
//            currentWeight = 0f;

//            Debug.Log($"✓ Inventory initialized: {gridSize.x}x{gridSize.y} grid, {maxWeight}kg max weight");
//        }

//        // === CORE METHODS ===

//        public bool TryAddItem(string itemID, int quantity = 1)
//        {
//            if (itemDatabase == null)
//            {
//                Debug.LogError("ItemDatabase not loaded!");
//                return false;
//            }

//            var itemDef = itemDatabase.GetItem(itemID);
//            if (itemDef == null)
//            {
//                Debug.LogError($"Item '{itemID}' not found in database!");
//                return false;
//            }

//            // Check weight
//            float newWeight = currentWeight + (itemDef.weight * quantity);
//            if (newWeight > maxWeight)
//            {
//                Debug.LogWarning($"Too heavy! Current: {currentWeight}kg, Item would add: {itemDef.weight * quantity}kg");
//                return false;
//            }

//            // Try to stack with existing items
//            if (itemDef.stackable)
//            {
//                foreach (var existingItem in items.Where(i => i.itemID == itemID))
//                {
//                    int spaceInStack = itemDef.maxStackSize - existingItem.stackSize;
//                    if (spaceInStack > 0)
//                    {
//                        int toAdd = Mathf.Min(spaceInStack, quantity);
//                        existingItem.stackSize += toAdd;
//                        quantity -= toAdd;
//                        currentWeight += itemDef.weight * toAdd;

//                        Debug.Log($"✓ Stacked {toAdd}x {itemDef.displayName} (total: {existingItem.stackSize})");

//                        if (quantity <= 0)
//                        {
//                            OnInventoryChanged?.Invoke();
//                            return true;
//                        }
//                    }
//                }
//            }

//            // Find empty space for new item
//            var emptySpot = FindEmptySpace(itemDef.gridSize);
//            if (emptySpot.HasValue)
//            {
//                var newItem = new ItemInstance(itemDef)
//                {
//                    gridPosition = emptySpot.Value,
//                    stackSize = Mathf.Min(quantity, itemDef.maxStackSize)
//                };

//                PlaceItem(newItem);
//                currentWeight += itemDef.weight * newItem.stackSize;

//                Debug.Log($"✓ Added {newItem.stackSize}x {itemDef.displayName} at position {emptySpot.Value}");

//                OnInventoryChanged?.Invoke();
//                return true;
//            }

//            Debug.LogWarning($"No space for {itemDef.displayName}!");
//            return false;
//        }

//        private Vector2Int? FindEmptySpace(Vector2Int size)
//        {
//            for (int y = 0; y <= gridSize.y - size.y; y++)
//            {
//                for (int x = 0; x <= gridSize.x - size.x; x++)
//                {
//                    if (CanPlaceAt(x, y, size))
//                    {
//                        return new Vector2Int(x, y);
//                    }
//                }
//            }
//            return null;
//        }

//        private bool CanPlaceAt(int x, int y, Vector2Int size)
//        {
//            for (int dy = 0; dy < size.y; dy++)
//            {
//                for (int dx = 0; dx < size.x; dx++)
//                {
//                    if (grid[x + dx, y + dy].isOccupied)
//                        return false;
//                }
//            }
//            return true;
//        }

//        private void PlaceItem(ItemInstance item)
//        {
//            items.Add(item);

//            // Mark grid cells as occupied
//            for (int dy = 0; dy < item.definition.gridSize.y; dy++)
//            {
//                for (int dx = 0; dx < item.definition.gridSize.x; dx++)
//                {
//                    grid[item.gridPosition.x + dx, item.gridPosition.y + dy].isOccupied = true;
//                    grid[item.gridPosition.x + dx, item.gridPosition.y + dy].item = item;
//                }
//            }
//        }

//        public bool HasItem(string itemID, int quantity = 1)
//        {
//            int total = items.Where(i => i.itemID == itemID).Sum(i => i.stackSize);
//            return total >= quantity;
//        }

//        public void RemoveItem(string itemID, int quantity = 1)
//        {
//            int remaining = quantity;
//            var itemsToRemove = new List<ItemInstance>();

//            foreach (var item in items.Where(i => i.itemID == itemID))
//            {
//                if (item.stackSize <= remaining)
//                {
//                    remaining -= item.stackSize;
//                    itemsToRemove.Add(item);

//                    // Clear grid cells
//                    for (int dy = 0; dy < item.definition.gridSize.y; dy++)
//                    {
//                        for (int dx = 0; dx < item.definition.gridSize.x; dx++)
//                        {
//                            grid[item.gridPosition.x + dx, item.gridPosition.y + dy].isOccupied = false;
//                            grid[item.gridPosition.x + dx, item.gridPosition.y + dy].item = null;
//                        }
//                    }
//                }
//                else
//                {
//                    item.stackSize -= remaining;
//                    remaining = 0;
//                }

//                if (remaining <= 0) break;
//            }

//            foreach (var item in itemsToRemove)
//            {
//                items.Remove(item);
//                currentWeight -= item.definition.weight * item.stackSize;
//            }

//            OnInventoryChanged?.Invoke();
//        }

//        // === CRAFTING INTEGRATION ===

//        public bool CanCraft(string recipeID)
//        {
//            if (recipeDatabase == null) return false;

//            var recipe = recipeDatabase.GetRecipe(recipeID);
//            if (recipe == null) return false;

//            // Check ingredients
//            foreach (var ingredient in recipe.ingredients)
//            {
//                if (ingredient.specificItem != null)
//                {
//                    if (!HasItem(ingredient.specificItem.itemID, ingredient.quantity))
//                        return false;
//                }
//            }

//            return true;
//        }

//        public void CraftItem(string recipeID)
//        {
//            if (!CanCraft(recipeID)) return;

//            var recipe = recipeDatabase.GetRecipe(recipeID);

//            // Consume ingredients
//            foreach (var ingredient in recipe.ingredients)
//            {
//                if (ingredient.consumed && ingredient.specificItem != null)
//                {
//                    RemoveItem(ingredient.specificItem.itemID, ingredient.quantity);
//                }
//            }

//            // Add outputs
//            foreach (var output in recipe.outputs)
//            {
//                if (output.item != null)
//                {
//                    int quantity = UnityEngine.Random.Range(output.quantityMin, output.quantityMax + 1);
//                    TryAddItem(output.item.itemID, quantity);
//                }
//            }

//            Debug.Log($"✓ Crafted: {recipe.recipeName}");
//            OnItemCrafted?.Invoke(recipe);
//        }

//        // === EVENTS ===
//        public event Action OnInventoryChanged;
//        public event Action<RecipeDefinition> OnItemCrafted;

//        // === HELPERS ===

//        [Serializable]
//        private class GridCell
//        {
//            public int x, y;
//            public bool isOccupied;
//            public ItemInstance item;

//            public GridCell(int x, int y)
//            {
//                this.x = x;
//                this.y = y;
//                this.isOccupied = false;
//                this.item = null;
//            }
//        }

//        public List<ItemInstance> GetAllItems() => new List<ItemInstance>(items);
//        public float GetCurrentWeight() => currentWeight;
//        public float GetMaxWeight() => maxWeight;
//        public Vector2Int GetGridSize() => gridSize;
//    }
//}