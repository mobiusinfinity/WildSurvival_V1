using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WildSurvival.Editor.Tools;

namespace WildSurvival.Editor.DatabaseGeneration
{
    /// <summary>
    /// Comprehensive database generator for Wild Survival inventory system
    /// Creates a realistic, balanced set of items and recipes with proper progression
    /// </summary>
    public class WildSurvivalDatabaseGenerator : EditorWindow
    {
        // Database references
        private ItemDatabase itemDatabase;
        private RecipeDatabase recipeDatabase;

        // Generation stats
        private int itemsGenerated = 0;
        private int recipesGenerated = 0;
        private List<string> generationLog = new List<string>();

        [MenuItem("Tools/Wild Survival/Generate Complete Database")]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalDatabaseGenerator>("Database Generator");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Wild Survival Database Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Database references
            itemDatabase = (ItemDatabase)EditorGUILayout.ObjectField("Item Database", itemDatabase, typeof(ItemDatabase), false);
            recipeDatabase = (RecipeDatabase)EditorGUILayout.ObjectField("Recipe Database", recipeDatabase, typeof(RecipeDatabase), false);

            EditorGUILayout.Space(10);

            if (itemDatabase == null || recipeDatabase == null)
            {
                EditorGUILayout.HelpBox("Please assign both databases before generating", MessageType.Warning);
                return;
            }

            // Generation options
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All Items", GUILayout.Height(30)))
            {
                GenerateAllItems();
            }

            if (GUILayout.Button("Generate All Recipes", GUILayout.Height(30)))
            {
                GenerateAllRecipes();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("GENERATE COMPLETE DATABASE", GUILayout.Height(40)))
            {
                GenerateCompleteDatabase();
            }

            EditorGUILayout.Space(10);

            // Stats
            EditorGUILayout.LabelField($"Items Generated: {itemsGenerated}");
            EditorGUILayout.LabelField($"Recipes Generated: {recipesGenerated}");

            // Log
            if (generationLog.Count > 0)
            {
                EditorGUILayout.LabelField("Generation Log:", EditorStyles.boldLabel);
                var logStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUILayout.TextArea(string.Join("\n", generationLog.TakeLast(10)), logStyle, GUILayout.Height(100));
            }
        }

        private void GenerateCompleteDatabase()
        {
            generationLog.Clear();
            itemsGenerated = 0;
            recipesGenerated = 0;

            GenerateAllItems();
            GenerateAllRecipes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            generationLog.Add($"✓ Complete database generated: {itemsGenerated} items, {recipesGenerated} recipes");
            Debug.Log($"Database generation complete! Created {itemsGenerated} items and {recipesGenerated} recipes");
        }

        private void GenerateAllItems()
        {
            // Clear existing items (optional - comment out to append)
            // itemDatabase.Clear();

            // Generate items by category
            GenerateResourceItems();
            GenerateToolItems();
            GenerateWeaponItems();
            GenerateFoodItems();
            GenerateMedicineItems();
            GenerateClothingItems();
            GenerateBuildingItems();
            GenerateContainerItems();
            GenerateFuelItems();
            GenerateMiscItems();

            EditorUtility.SetDirty(itemDatabase);
        }

        private void GenerateResourceItems()
        {
            // === WOOD RESOURCES ===
            CreateItem("wood_log", "Log", "A sturdy log from a tree", ItemCategory.Resource,
                weight: 5f, stackSize: 5, gridSize: new Vector2Int(1, 3));

            CreateItem("wood_stick", "Stick", "A small wooden stick", ItemCategory.Resource,
                weight: 0.2f, stackSize: 20, gridSize: new Vector2Int(1, 2));

            CreateItem("wood_plank", "Wooden Plank", "Processed wooden plank", ItemCategory.Resource,
                weight: 2f, stackSize: 10, gridSize: new Vector2Int(2, 1));

            CreateItem("bark", "Tree Bark", "Rough tree bark", ItemCategory.Resource,
                weight: 0.1f, stackSize: 30, gridSize: new Vector2Int(1, 1));

            CreateItem("resin", "Tree Resin", "Sticky tree resin", ItemCategory.Resource,
                weight: 0.05f, stackSize: 50, gridSize: new Vector2Int(1, 1));

            // === STONE RESOURCES ===
            CreateItem("stone_small", "Small Stone", "A small stone", ItemCategory.Resource,
                weight: 0.5f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("stone_large", "Large Stone", "A heavy stone", ItemCategory.Resource,
                weight: 3f, stackSize: 5, gridSize: new Vector2Int(2, 2));

            CreateItem("flint", "Flint", "Sharp flint stone", ItemCategory.Resource,
                weight: 0.3f, stackSize: 15, gridSize: new Vector2Int(1, 1));

            CreateItem("clay", "Clay", "Moldable clay", ItemCategory.Resource,
                weight: 1f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("sand", "Sand", "Fine sand", ItemCategory.Resource,
                weight: 1.5f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            // === METAL RESOURCES ===
            CreateItem("ore_copper", "Copper Ore", "Raw copper ore", ItemCategory.Resource,
                weight: 2f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("ore_iron", "Iron Ore", "Raw iron ore", ItemCategory.Resource,
                weight: 2.5f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("ore_tin", "Tin Ore", "Raw tin ore", ItemCategory.Resource,
                weight: 2f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("ingot_copper", "Copper Ingot", "Refined copper ingot", ItemCategory.Resource,
                weight: 1f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("ingot_iron", "Iron Ingot", "Refined iron ingot", ItemCategory.Resource,
                weight: 1.2f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("ingot_bronze", "Bronze Ingot", "Bronze alloy ingot", ItemCategory.Resource,
                weight: 1.1f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("ingot_steel", "Steel Ingot", "High-quality steel ingot", ItemCategory.Resource,
                weight: 1.3f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            // === FIBER RESOURCES ===
            CreateItem("fiber_plant", "Plant Fiber", "Basic plant fibers", ItemCategory.Resource,
                weight: 0.05f, stackSize: 50, gridSize: new Vector2Int(1, 1));

            CreateItem("rope", "Rope", "Woven rope", ItemCategory.Resource,
                weight: 0.3f, stackSize: 10, gridSize: new Vector2Int(1, 2));

            CreateItem("cloth", "Cloth", "Basic woven cloth", ItemCategory.Resource,
                weight: 0.2f, stackSize: 20, gridSize: new Vector2Int(2, 2));

            CreateItem("leather", "Leather", "Tanned animal hide", ItemCategory.Resource,
                weight: 0.5f, stackSize: 10, gridSize: new Vector2Int(2, 2));

            CreateItem("fur", "Animal Fur", "Warm animal fur", ItemCategory.Resource,
                weight: 0.3f, stackSize: 15, gridSize: new Vector2Int(2, 2));

            // === ANIMAL RESOURCES ===
            CreateItem("bone", "Bone", "Animal bone", ItemCategory.Resource,
                weight: 0.3f, stackSize: 20, gridSize: new Vector2Int(1, 2));

            CreateItem("antler", "Antler", "Deer antler", ItemCategory.Resource,
                weight: 0.5f, stackSize: 5, gridSize: new Vector2Int(2, 1));

            CreateItem("feather", "Feather", "Bird feather", ItemCategory.Resource,
                weight: 0.01f, stackSize: 100, gridSize: new Vector2Int(1, 1));

            CreateItem("animal_fat", "Animal Fat", "Rendered animal fat", ItemCategory.Resource,
                weight: 0.2f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            LogGeneration($"Generated {29} resource items");
        }

        private void GenerateToolItems()
        {
            // === BASIC TOOLS ===
            CreateItem("tool_stone_knife", "Stone Knife", "Basic cutting tool", ItemCategory.Tool,
                weight: 0.5f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 50f);

            CreateItem("tool_stone_axe", "Stone Axe", "Basic wood chopping tool", ItemCategory.Tool,
                weight: 2f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 75f);

            CreateItem("tool_stone_pickaxe", "Stone Pickaxe", "Basic mining tool", ItemCategory.Tool,
                weight: 2.5f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 75f);

            CreateItem("tool_stone_hammer", "Stone Hammer", "Basic crafting tool", ItemCategory.Tool,
                weight: 1.5f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 100f);

            CreateItem("tool_stone_shovel", "Stone Shovel", "Basic digging tool", ItemCategory.Tool,
                weight: 1.8f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 60f);

            // === BRONZE TOOLS ===
            CreateItem("tool_bronze_knife", "Bronze Knife", "Sharp cutting tool", ItemCategory.Tool,
                weight: 0.6f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 150f);

            CreateItem("tool_bronze_axe", "Bronze Axe", "Efficient wood chopping tool", ItemCategory.Tool,
                weight: 2.2f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 200f);

            CreateItem("tool_bronze_pickaxe", "Bronze Pickaxe", "Durable mining tool", ItemCategory.Tool,
                weight: 2.7f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 200f);

            CreateItem("tool_bronze_hammer", "Bronze Hammer", "Quality crafting tool", ItemCategory.Tool,
                weight: 1.7f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 250f);

            // === IRON TOOLS ===
            CreateItem("tool_iron_knife", "Iron Knife", "Professional cutting tool", ItemCategory.Tool,
                weight: 0.7f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 300f);

            CreateItem("tool_iron_axe", "Iron Axe", "Professional wood chopping tool", ItemCategory.Tool,
                weight: 2.5f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 350f);

            CreateItem("tool_iron_pickaxe", "Iron Pickaxe", "Professional mining tool", ItemCategory.Tool,
                weight: 3f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 350f);

            CreateItem("tool_iron_hammer", "Iron Hammer", "Professional crafting tool", ItemCategory.Tool,
                weight: 2f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 400f);

            CreateItem("tool_iron_shovel", "Iron Shovel", "Professional digging tool", ItemCategory.Tool,
                weight: 2.2f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 300f);

            // === STEEL TOOLS ===
            CreateItem("tool_steel_knife", "Steel Knife", "Master cutting tool", ItemCategory.Tool,
                weight: 0.8f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 500f);

            CreateItem("tool_steel_axe", "Steel Axe", "Master wood chopping tool", ItemCategory.Tool,
                weight: 2.8f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 600f);

            CreateItem("tool_steel_pickaxe", "Steel Pickaxe", "Master mining tool", ItemCategory.Tool,
                weight: 3.3f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 600f);

            // === SPECIAL TOOLS ===
            CreateItem("tool_fishing_rod", "Fishing Rod", "Tool for catching fish", ItemCategory.Tool,
                weight: 0.5f, stackSize: 1, gridSize: new Vector2Int(1, 4), durability: 100f);

            CreateItem("tool_saw", "Saw", "Tool for precise wood cutting", ItemCategory.Tool,
                weight: 1f, stackSize: 1, gridSize: new Vector2Int(2, 1), durability: 200f);

            CreateItem("tool_chisel", "Chisel", "Tool for detailed carving", ItemCategory.Tool,
                weight: 0.3f, stackSize: 1, gridSize: new Vector2Int(1, 1), durability: 150f);

            CreateItem("tool_tongs", "Tongs", "Tool for handling hot materials", ItemCategory.Tool,
                weight: 0.8f, stackSize: 1, gridSize: new Vector2Int(1, 2), durability: 300f);

            CreateItem("tool_mortar_pestle", "Mortar & Pestle", "Tool for grinding", ItemCategory.Tool,
                weight: 2f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 500f);

            LogGeneration($"Generated {22} tool items");
        }

        private void GenerateWeaponItems()
        {
            // === MELEE WEAPONS ===
            CreateItem("weapon_wooden_club", "Wooden Club", "Basic bludgeoning weapon", ItemCategory.Weapon,
                weight: 1.5f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 50f);

            CreateItem("weapon_stone_spear", "Stone Spear", "Basic thrusting weapon", ItemCategory.Weapon,
                weight: 2f, stackSize: 1, gridSize: new Vector2Int(1, 4), durability: 75f);

            CreateItem("weapon_bronze_sword", "Bronze Sword", "Balanced cutting weapon", ItemCategory.Weapon,
                weight: 1.8f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 200f);

            CreateItem("weapon_iron_sword", "Iron Sword", "Professional combat weapon", ItemCategory.Weapon,
                weight: 2.2f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 350f);

            CreateItem("weapon_steel_sword", "Steel Sword", "Master combat weapon", ItemCategory.Weapon,
                weight: 2.5f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 600f);

            CreateItem("weapon_battle_axe", "Battle Axe", "Heavy combat weapon", ItemCategory.Weapon,
                weight: 3.5f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 400f);

            // === RANGED WEAPONS ===
            CreateItem("weapon_sling", "Sling", "Basic ranged weapon", ItemCategory.Weapon,
                weight: 0.2f, stackSize: 1, gridSize: new Vector2Int(1, 1), durability: 100f);

            CreateItem("weapon_bow_simple", "Simple Bow", "Basic archery weapon", ItemCategory.Weapon,
                weight: 0.8f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 100f);

            CreateItem("weapon_bow_recurve", "Recurve Bow", "Advanced archery weapon", ItemCategory.Weapon,
                weight: 1f, stackSize: 1, gridSize: new Vector2Int(1, 3), durability: 200f);

            CreateItem("weapon_crossbow", "Crossbow", "Mechanical ranged weapon", ItemCategory.Weapon,
                weight: 3f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 300f);

            // === AMMUNITION ===
            CreateItem("ammo_arrow_stone", "Stone Arrow", "Basic arrow", ItemCategory.Weapon,
                weight: 0.05f, stackSize: 50, gridSize: new Vector2Int(1, 2));

            CreateItem("ammo_arrow_bronze", "Bronze Arrow", "Sharp arrow", ItemCategory.Weapon,
                weight: 0.06f, stackSize: 50, gridSize: new Vector2Int(1, 2));

            CreateItem("ammo_arrow_iron", "Iron Arrow", "Professional arrow", ItemCategory.Weapon,
                weight: 0.07f, stackSize: 50, gridSize: new Vector2Int(1, 2));

            CreateItem("ammo_bolt", "Crossbow Bolt", "Heavy projectile", ItemCategory.Weapon,
                weight: 0.1f, stackSize: 30, gridSize: new Vector2Int(1, 2));

            LogGeneration($"Generated {14} weapon items");
        }

        private void GenerateFoodItems()
        {
            // === RAW FOODS ===
            CreateItem("food_meat_raw", "Raw Meat", "Uncooked animal meat", ItemCategory.Food,
                weight: 0.5f, stackSize: 10, gridSize: new Vector2Int(2, 1));

            CreateItem("food_fish_raw", "Raw Fish", "Freshly caught fish", ItemCategory.Food,
                weight: 0.3f, stackSize: 10, gridSize: new Vector2Int(2, 1));

            CreateItem("food_berries", "Wild Berries", "Foraged berries", ItemCategory.Food,
                weight: 0.1f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("food_mushroom", "Wild Mushroom", "Foraged mushroom", ItemCategory.Food,
                weight: 0.05f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("food_roots", "Edible Roots", "Nutritious roots", ItemCategory.Food,
                weight: 0.2f, stackSize: 15, gridSize: new Vector2Int(1, 1));

            CreateItem("food_nuts", "Wild Nuts", "Protein-rich nuts", ItemCategory.Food,
                weight: 0.1f, stackSize: 30, gridSize: new Vector2Int(1, 1));

            CreateItem("food_honey", "Wild Honey", "Natural sweetener", ItemCategory.Food,
                weight: 0.3f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            CreateItem("food_eggs", "Bird Eggs", "Fresh eggs", ItemCategory.Food,
                weight: 0.1f, stackSize: 12, gridSize: new Vector2Int(1, 1));

            // === COOKED FOODS ===
            CreateItem("food_meat_cooked", "Cooked Meat", "Grilled meat", ItemCategory.Food,
                weight: 0.4f, stackSize: 10, gridSize: new Vector2Int(2, 1));

            CreateItem("food_fish_cooked", "Cooked Fish", "Grilled fish", ItemCategory.Food,
                weight: 0.25f, stackSize: 10, gridSize: new Vector2Int(2, 1));

            CreateItem("food_meat_smoked", "Smoked Meat", "Preserved meat", ItemCategory.Food,
                weight: 0.3f, stackSize: 15, gridSize: new Vector2Int(2, 1));

            CreateItem("food_fish_smoked", "Smoked Fish", "Preserved fish", ItemCategory.Food,
                weight: 0.2f, stackSize: 15, gridSize: new Vector2Int(2, 1));

            CreateItem("food_stew", "Hearty Stew", "Nutritious meal", ItemCategory.Food,
                weight: 0.8f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            CreateItem("food_bread", "Bread", "Baked bread", ItemCategory.Food,
                weight: 0.3f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("food_dried_fruit", "Dried Fruit", "Preserved fruit", ItemCategory.Food,
                weight: 0.05f, stackSize: 30, gridSize: new Vector2Int(1, 1));

            CreateItem("food_jerky", "Meat Jerky", "Long-lasting protein", ItemCategory.Food,
                weight: 0.1f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            // === DRINKS ===
            CreateItem("drink_water_dirty", "Dirty Water", "Unsafe water", ItemCategory.Food,
                weight: 1f, stackSize: 1, gridSize: new Vector2Int(1, 2));

            CreateItem("drink_water_clean", "Clean Water", "Purified water", ItemCategory.Food,
                weight: 1f, stackSize: 1, gridSize: new Vector2Int(1, 2));

            CreateItem("drink_tea", "Herbal Tea", "Soothing beverage", ItemCategory.Food,
                weight: 0.5f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            CreateItem("drink_juice", "Fruit Juice", "Refreshing drink", ItemCategory.Food,
                weight: 0.5f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            LogGeneration($"Generated {20} food items");
        }

        private void GenerateMedicineItems()
        {
            CreateItem("medicine_bandage", "Bandage", "Basic wound dressing", ItemCategory.Medicine,
                weight: 0.05f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_herbs", "Medicinal Herbs", "Natural healing herbs", ItemCategory.Medicine,
                weight: 0.02f, stackSize: 30, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_poultice", "Herbal Poultice", "Healing paste", ItemCategory.Medicine,
                weight: 0.1f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_antidote", "Antidote", "Poison remedy", ItemCategory.Medicine,
                weight: 0.1f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_painkiller", "Pain Relief", "Natural painkiller", ItemCategory.Medicine,
                weight: 0.05f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_antiseptic", "Antiseptic", "Infection prevention", ItemCategory.Medicine,
                weight: 0.2f, stackSize: 5, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_salve", "Healing Salve", "Skin treatment", ItemCategory.Medicine,
                weight: 0.1f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("medicine_tonic", "Health Tonic", "Vitality booster", ItemCategory.Medicine,
                weight: 0.3f, stackSize: 5, gridSize: new Vector2Int(1, 2));

            LogGeneration($"Generated {8} medicine items");
        }

        private void GenerateClothingItems()
        {
            // === HEAD ===
            CreateItem("clothing_hat_cloth", "Cloth Hat", "Basic head protection", ItemCategory.Clothing,
                weight: 0.1f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 100f);

            CreateItem("clothing_hat_leather", "Leather Hat", "Durable head protection", ItemCategory.Clothing,
                weight: 0.2f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 200f);

            CreateItem("clothing_hat_fur", "Fur Hat", "Warm head protection", ItemCategory.Clothing,
                weight: 0.3f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 150f);

            // === CHEST ===
            CreateItem("clothing_shirt_cloth", "Cloth Shirt", "Basic torso clothing", ItemCategory.Clothing,
                weight: 0.3f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 100f);

            CreateItem("clothing_shirt_leather", "Leather Vest", "Protective torso clothing", ItemCategory.Clothing,
                weight: 0.8f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 250f);

            CreateItem("clothing_coat_fur", "Fur Coat", "Warm torso clothing", ItemCategory.Clothing,
                weight: 1.5f, stackSize: 1, gridSize: new Vector2Int(3, 3), durability: 200f);

            // === LEGS ===
            CreateItem("clothing_pants_cloth", "Cloth Pants", "Basic leg clothing", ItemCategory.Clothing,
                weight: 0.4f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 100f);

            CreateItem("clothing_pants_leather", "Leather Pants", "Durable leg clothing", ItemCategory.Clothing,
                weight: 0.6f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 200f);

            // === FEET ===
            CreateItem("clothing_shoes_cloth", "Cloth Shoes", "Basic footwear", ItemCategory.Clothing,
                weight: 0.2f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 75f);

            CreateItem("clothing_boots_leather", "Leather Boots", "Durable footwear", ItemCategory.Clothing,
                weight: 0.5f, stackSize: 1, gridSize: new Vector2Int(2, 2), durability: 200f);

            // === HANDS ===
            CreateItem("clothing_gloves_cloth", "Cloth Gloves", "Basic hand protection", ItemCategory.Clothing,
                weight: 0.05f, stackSize: 1, gridSize: new Vector2Int(1, 1), durability: 50f);

            CreateItem("clothing_gloves_leather", "Leather Gloves", "Work gloves", ItemCategory.Clothing,
                weight: 0.1f, stackSize: 1, gridSize: new Vector2Int(1, 1), durability: 150f);

            // === ACCESSORIES ===
            CreateItem("clothing_backpack_small", "Small Backpack", "Increases carry capacity", ItemCategory.Clothing,
                weight: 0.5f, stackSize: 1, gridSize: new Vector2Int(2, 3), durability: 200f);

            CreateItem("clothing_backpack_large", "Large Backpack", "Greatly increases carry capacity", ItemCategory.Clothing,
                weight: 1f, stackSize: 1, gridSize: new Vector2Int(3, 3), durability: 300f);

            CreateItem("clothing_belt", "Tool Belt", "Additional quick slots", ItemCategory.Clothing,
                weight: 0.3f, stackSize: 1, gridSize: new Vector2Int(3, 1), durability: 250f);

            LogGeneration($"Generated {15} clothing items");
        }

        private void GenerateBuildingItems()
        {
            // === WALLS ===
            CreateItem("building_wall_wood", "Wooden Wall", "Basic wall structure", ItemCategory.Building,
                weight: 10f, stackSize: 5, gridSize: new Vector2Int(3, 3));

            CreateItem("building_wall_stone", "Stone Wall", "Sturdy wall structure", ItemCategory.Building,
                weight: 20f, stackSize: 3, gridSize: new Vector2Int(3, 3));

            // === ROOFS ===
            CreateItem("building_roof_thatch", "Thatch Roof", "Basic roof covering", ItemCategory.Building,
                weight: 5f, stackSize: 5, gridSize: new Vector2Int(3, 2));

            CreateItem("building_roof_wood", "Wooden Roof", "Solid roof covering", ItemCategory.Building,
                weight: 8f, stackSize: 5, gridSize: new Vector2Int(3, 2));

            // === DOORS & WINDOWS ===
            CreateItem("building_door_wood", "Wooden Door", "Basic entrance", ItemCategory.Building,
                weight: 5f, stackSize: 3, gridSize: new Vector2Int(2, 3));

            CreateItem("building_window", "Window", "Light and ventilation", ItemCategory.Building,
                weight: 3f, stackSize: 5, gridSize: new Vector2Int(2, 2));

            // === FOUNDATIONS ===
            CreateItem("building_foundation_wood", "Wooden Foundation", "Building base", ItemCategory.Building,
                weight: 15f, stackSize: 3, gridSize: new Vector2Int(3, 3));

            CreateItem("building_foundation_stone", "Stone Foundation", "Sturdy building base", ItemCategory.Building,
                weight: 25f, stackSize: 2, gridSize: new Vector2Int(3, 3));

            // === FURNITURE ===
            CreateItem("furniture_bed", "Bed", "Sleeping furniture", ItemCategory.Building,
                weight: 8f, stackSize: 1, gridSize: new Vector2Int(2, 3));

            CreateItem("furniture_chest", "Storage Chest", "Item storage", ItemCategory.Building,
                weight: 5f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("furniture_table", "Table", "Work surface", ItemCategory.Building,
                weight: 6f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("furniture_chair", "Chair", "Seating", ItemCategory.Building,
                weight: 3f, stackSize: 2, gridSize: new Vector2Int(1, 1));

            // === WORKSTATIONS ===
            CreateItem("station_campfire", "Campfire", "Basic cooking station", ItemCategory.Building,
                weight: 5f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("station_workbench", "Workbench", "Crafting station", ItemCategory.Building,
                weight: 10f, stackSize: 1, gridSize: new Vector2Int(3, 2));

            CreateItem("station_forge", "Forge", "Metal working station", ItemCategory.Building,
                weight: 30f, stackSize: 1, gridSize: new Vector2Int(3, 3));

            CreateItem("station_anvil", "Anvil", "Metal shaping station", ItemCategory.Building,
                weight: 50f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("station_tanning_rack", "Tanning Rack", "Leather processing", ItemCategory.Building,
                weight: 8f, stackSize: 1, gridSize: new Vector2Int(2, 3));

            CreateItem("station_cooking_pot", "Cooking Pot", "Advanced cooking", ItemCategory.Building,
                weight: 3f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            LogGeneration($"Generated {18} building items");
        }

        private void GenerateContainerItems()
        {
            CreateItem("container_pouch", "Small Pouch", "Small storage container", ItemCategory.Container,
                weight: 0.1f, stackSize: 1, gridSize: new Vector2Int(1, 1));

            CreateItem("container_bag", "Bag", "Medium storage container", ItemCategory.Container,
                weight: 0.3f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("container_crate", "Wooden Crate", "Large storage container", ItemCategory.Container,
                weight: 2f, stackSize: 1, gridSize: new Vector2Int(3, 3));

            CreateItem("container_barrel", "Barrel", "Liquid storage container", ItemCategory.Container,
                weight: 3f, stackSize: 1, gridSize: new Vector2Int(2, 3));

            CreateItem("container_bottle", "Glass Bottle", "Liquid container", ItemCategory.Container,
                weight: 0.2f, stackSize: 5, gridSize: new Vector2Int(1, 2));

            CreateItem("container_waterskin", "Waterskin", "Portable water container", ItemCategory.Container,
                weight: 0.1f, stackSize: 1, gridSize: new Vector2Int(1, 2));

            CreateItem("container_quiver", "Quiver", "Arrow container", ItemCategory.Container,
                weight: 0.2f, stackSize: 1, gridSize: new Vector2Int(1, 3));

            LogGeneration($"Generated {7} container items");
        }

        private void GenerateFuelItems()
        {
            CreateItem("fuel_tinder", "Tinder", "Fire starting material", ItemCategory.Fuel,
                weight: 0.01f, stackSize: 100, gridSize: new Vector2Int(1, 1));

            CreateItem("fuel_kindling", "Kindling", "Small burning material", ItemCategory.Fuel,
                weight: 0.1f, stackSize: 50, gridSize: new Vector2Int(1, 1));

            CreateItem("fuel_firewood", "Firewood", "Standard fuel", ItemCategory.Fuel,
                weight: 2f, stackSize: 10, gridSize: new Vector2Int(1, 2));

            CreateItem("fuel_charcoal", "Charcoal", "High-heat fuel", ItemCategory.Fuel,
                weight: 0.5f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("fuel_coal", "Coal", "Long-burning fuel", ItemCategory.Fuel,
                weight: 1f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("fuel_oil", "Oil", "Liquid fuel", ItemCategory.Fuel,
                weight: 0.8f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("fuel_torch", "Torch", "Portable light source", ItemCategory.Fuel,
                weight: 0.5f, stackSize: 5, gridSize: new Vector2Int(1, 2), durability: 60f);

            CreateItem("fuel_candle", "Candle", "Long-lasting light", ItemCategory.Fuel,
                weight: 0.1f, stackSize: 10, gridSize: new Vector2Int(1, 1), durability: 120f);

            LogGeneration($"Generated {8} fuel items");
        }

        private void GenerateMiscItems()
        {
            CreateItem("misc_map", "Map", "Shows explored areas", ItemCategory.Misc,
                weight: 0.05f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("misc_compass", "Compass", "Shows direction", ItemCategory.Misc,
                weight: 0.1f, stackSize: 1, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_book", "Skill Book", "Teaches new recipes", ItemCategory.Misc,
                weight: 0.5f, stackSize: 1, gridSize: new Vector2Int(2, 2));

            CreateItem("misc_key", "Key", "Opens locked doors", ItemCategory.Misc,
                weight: 0.01f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_coin", "Coin", "Currency", ItemCategory.Misc,
                weight: 0.01f, stackSize: 100, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_gem", "Gemstone", "Valuable gem", ItemCategory.Misc,
                weight: 0.05f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_paper", "Paper", "Writing material", ItemCategory.Misc,
                weight: 0.01f, stackSize: 50, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_ink", "Ink", "Writing fluid", ItemCategory.Misc,
                weight: 0.1f, stackSize: 10, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_needle", "Needle", "Sewing tool", ItemCategory.Misc,
                weight: 0.01f, stackSize: 20, gridSize: new Vector2Int(1, 1));

            CreateItem("misc_hook", "Fish Hook", "Fishing equipment", ItemCategory.Misc,
                weight: 0.01f, stackSize: 30, gridSize: new Vector2Int(1, 1));

            LogGeneration($"Generated {10} misc items");
        }

        private ItemDefinition CreateItem(string id, string name, string desc, ItemCategory category,
            float weight = 1f, int stackSize = 1, Vector2Int? gridSize = null, float durability = 0f)
        {
            // Check if item already exists
            var existingItem = itemDatabase.GetItem(id);
            if (existingItem != null)
            {
                return existingItem;
            }

            // Create directories if needed
            EnsureDirectoryExists("Assets/_Project/Data/Items");

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemID = id;
            item.displayName = name;
            item.description = desc;
            item.primaryCategory = category;
            item.weight = weight;
            item.maxStackSize = stackSize;
            item.gridSize = gridSize ?? Vector2Int.one;
            item.hasDurability = durability > 0;
            item.maxDurability = durability;

            // Initialize shape grid
            item.shapeGrid = new bool[item.gridSize.x, item.gridSize.y];
            for (int x = 0; x < item.gridSize.x; x++)
            {
                for (int y = 0; y < item.gridSize.y; y++)
                {
                    item.shapeGrid[x, y] = true; // Default to full shape
                }
            }

            // Set appropriate tags
            SetItemTags(item);

            string path = $"Assets/_Project/Data/Items/{id}.asset";
            AssetDatabase.CreateAsset(item, path);

            itemDatabase.AddItem(item);
            itemsGenerated++;

            return item;
        }

        private void SetItemTags(ItemDefinition item)
        {
            var tags = new List<ItemTag>();

            // Add category-based tags
            switch (item.primaryCategory)
            {
                case ItemCategory.Tool:
                    tags.Add(ItemTag.Tool);
                    if (item.itemID.Contains("axe")) tags.Add(ItemTag.Sharp);
                    if (item.itemID.Contains("hammer")) tags.Add(ItemTag.Heavy);
                    break;

                case ItemCategory.Weapon:
                    tags.Add(ItemTag.Weapon);
                    if (item.itemID.Contains("sword") || item.itemID.Contains("knife")) tags.Add(ItemTag.Sharp);
                    if (item.itemID.Contains("club") || item.itemID.Contains("hammer")) tags.Add(ItemTag.Heavy);
                    break;

                case ItemCategory.Food:
                    tags.Add(ItemTag.Consumable);
                    if (item.itemID.Contains("meat") || item.itemID.Contains("fish")) tags.Add(ItemTag.Organic);
                    break;

                case ItemCategory.Resource:
                    tags.Add(ItemTag.CraftingMaterial);
                    if (item.itemID.Contains("wood")) tags.Add(ItemTag.Wood);
                    if (item.itemID.Contains("stone") || item.itemID.Contains("ore")) tags.Add(ItemTag.Stone);
                    if (item.itemID.Contains("metal") || item.itemID.Contains("ingot")) tags.Add(ItemTag.Metal);
                    break;

                case ItemCategory.Fuel:
                    tags.Add(ItemTag.Fuel);
                    if (item.itemID.Contains("wood")) tags.Add(ItemTag.Wood);
                    break;
            }

            // Add stackable tag
            if (item.maxStackSize > 1)
                tags.Add(ItemTag.Stackable);

            item.tags = tags.ToArray();
        }

        private void GenerateAllRecipes()
        {
            // Clear existing recipes (optional)
            // recipeDatabase.Clear();

            GenerateToolRecipes();
            GenerateWeaponRecipes();
            GenerateFoodRecipes();
            GenerateMedicineRecipes();
            GenerateClothingRecipes();
            GenerateBuildingRecipes();
            GenerateProcessingRecipes();
            GenerateUpgradeRecipes();

            EditorUtility.SetDirty(recipeDatabase);
        }

        private void GenerateToolRecipes()
        {
            // === STONE TOOLS ===
            CreateRecipe("recipe_stone_knife", "Craft Stone Knife", CraftingCategory.Tools,
                WorkstationType.None,
                new[] { ("stone_small", 1), ("wood_stick", 1), ("fiber_plant", 2) },
                new[] { "tool_stone_knife" },
                craftTime: 5f);

            CreateRecipe("recipe_stone_axe", "Craft Stone Axe", CraftingCategory.Tools,
                WorkstationType.None,
                new[] { ("stone_large", 1), ("wood_stick", 2), ("rope", 1) },
                new[] { "tool_stone_axe" },
                craftTime: 8f);

            CreateRecipe("recipe_stone_pickaxe", "Craft Stone Pickaxe", CraftingCategory.Tools,
                WorkstationType.None,
                new[] { ("stone_large", 2), ("wood_stick", 2), ("rope", 1) },
                new[] { "tool_stone_pickaxe" },
                craftTime: 10f);

            CreateRecipe("recipe_stone_hammer", "Craft Stone Hammer", CraftingCategory.Tools,
                WorkstationType.Workbench,
                new[] { ("stone_large", 1), ("wood_stick", 1), ("rope", 1) },
                new[] { "tool_stone_hammer" },
                craftTime: 6f);

            // === BRONZE TOOLS ===
            CreateRecipe("recipe_bronze_knife", "Forge Bronze Knife", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_bronze", 1), ("wood_stick", 1), ("leather", 1) },
                new[] { "tool_bronze_knife" },
                craftTime: 15f);

            CreateRecipe("recipe_bronze_axe", "Forge Bronze Axe", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_bronze", 2), ("wood_stick", 2), ("leather", 1) },
                new[] { "tool_bronze_axe" },
                craftTime: 20f);

            CreateRecipe("recipe_bronze_pickaxe", "Forge Bronze Pickaxe", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_bronze", 3), ("wood_stick", 2), ("leather", 1) },
                new[] { "tool_bronze_pickaxe" },
                craftTime: 25f);

            // === IRON TOOLS ===
            CreateRecipe("recipe_iron_knife", "Forge Iron Knife", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_iron", 1), ("wood_stick", 1), ("leather", 1) },
                new[] { "tool_iron_knife" },
                craftTime: 20f);

            CreateRecipe("recipe_iron_axe", "Forge Iron Axe", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_iron", 2), ("wood_plank", 2), ("leather", 2) },
                new[] { "tool_iron_axe" },
                craftTime: 30f);

            CreateRecipe("recipe_iron_pickaxe", "Forge Iron Pickaxe", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_iron", 3), ("wood_plank", 2), ("leather", 2) },
                new[] { "tool_iron_pickaxe" },
                craftTime: 35f);

            // === SPECIAL TOOLS ===
            CreateRecipe("recipe_fishing_rod", "Craft Fishing Rod", CraftingCategory.Tools,
                WorkstationType.Workbench,
                new[] { ("wood_stick", 3), ("rope", 2), ("misc_hook", 1) },
                new[] { "tool_fishing_rod" },
                craftTime: 10f);

            CreateRecipe("recipe_saw", "Craft Saw", CraftingCategory.Tools,
                WorkstationType.Workbench,
                new[] { ("ingot_iron", 1), ("wood_plank", 1), ("tool_iron_hammer", 0) }, // 0 means not consumed
                new[] { "tool_saw" },
                craftTime: 15f);

            LogGeneration($"Generated {12} tool recipes");
        }

        private void GenerateWeaponRecipes()
        {
            // === MELEE WEAPONS ===
            CreateRecipe("recipe_wooden_club", "Craft Wooden Club", CraftingCategory.Weapons,
                WorkstationType.None,
                new[] { ("wood_log", 1), ("rope", 1) },
                new[] { "weapon_wooden_club" },
                craftTime: 5f);

            CreateRecipe("recipe_stone_spear", "Craft Stone Spear", CraftingCategory.Weapons,
                WorkstationType.None,
                new[] { ("wood_stick", 2), ("stone_small", 1), ("rope", 1) },
                new[] { "weapon_stone_spear" },
                craftTime: 8f);

            CreateRecipe("recipe_bronze_sword", "Forge Bronze Sword", CraftingCategory.Weapons,
                WorkstationType.Anvil,
                new[] { ("ingot_bronze", 3), ("leather", 2), ("wood_stick", 1) },
                new[] { "weapon_bronze_sword" },
                craftTime: 30f);

            CreateRecipe("recipe_iron_sword", "Forge Iron Sword", CraftingCategory.Weapons,
                WorkstationType.Anvil,
                new[] { ("ingot_iron", 3), ("leather", 2), ("wood_plank", 1) },
                new[] { "weapon_iron_sword" },
                craftTime: 40f);

            // === RANGED WEAPONS ===
            CreateRecipe("recipe_sling", "Craft Sling", CraftingCategory.Weapons,
                WorkstationType.None,
                new[] { ("leather", 1), ("rope", 1) },
                new[] { "weapon_sling" },
                craftTime: 5f);

            CreateRecipe("recipe_bow_simple", "Craft Simple Bow", CraftingCategory.Weapons,
                WorkstationType.Workbench,
                new[] { ("wood_stick", 3), ("rope", 2) },
                new[] { "weapon_bow_simple" },
                craftTime: 15f);

            CreateRecipe("recipe_bow_recurve", "Craft Recurve Bow", CraftingCategory.Weapons,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 2), ("rope", 3), ("antler", 1) },
                new[] { "weapon_bow_recurve" },
                craftTime: 25f);

            // === AMMUNITION ===
            CreateRecipe("recipe_arrow_stone", "Craft Stone Arrows", CraftingCategory.Weapons,
                WorkstationType.Workbench,
                new[] { ("wood_stick", 5), ("stone_small", 5), ("feather", 5) },
                new[] { ("ammo_arrow_stone", 10) },
                craftTime: 10f);

            CreateRecipe("recipe_arrow_bronze", "Craft Bronze Arrows", CraftingCategory.Weapons,
                WorkstationType.Workbench,
                new[] { ("wood_stick", 5), ("ingot_bronze", 1), ("feather", 5) },
                new[] { ("ammo_arrow_bronze", 10) },
                craftTime: 15f);

            CreateRecipe("recipe_arrow_iron", "Craft Iron Arrows", CraftingCategory.Weapons,
                WorkstationType.Workbench,
                new[] { ("wood_stick", 5), ("ingot_iron", 1), ("feather", 5) },
                new[] { ("ammo_arrow_iron", 10) },
                craftTime: 15f);

            LogGeneration($"Generated {10} weapon recipes");
        }

        private void GenerateFoodRecipes()
        {
            // === COOKING ===
            CreateRecipe("recipe_cook_meat", "Cook Meat", CraftingCategory.Cooking,
                WorkstationType.Campfire,
                new[] { ("food_meat_raw", 1) },
                new[] { "food_meat_cooked" },
                craftTime: 30f);

            CreateRecipe("recipe_cook_fish", "Cook Fish", CraftingCategory.Cooking,
                WorkstationType.Campfire,
                new[] { ("food_fish_raw", 1) },
                new[] { "food_fish_cooked" },
                craftTime: 20f);

            CreateRecipe("recipe_smoke_meat", "Smoke Meat", CraftingCategory.Cooking,
                WorkstationType.Campfire,
                new[] { ("food_meat_raw", 5), ("fuel_firewood", 2) },
                new[] { ("food_meat_smoked", 5) },
                craftTime: 60f);

            CreateRecipe("recipe_smoke_fish", "Smoke Fish", CraftingCategory.Cooking,
                WorkstationType.Campfire,
                new[] { ("food_fish_raw", 5), ("fuel_firewood", 1) },
                new[] { ("food_fish_smoked", 5) },
                craftTime: 45f);

            CreateRecipe("recipe_stew", "Cook Hearty Stew", CraftingCategory.Cooking,
                WorkstationType.CookingPot,
                new[] { ("food_meat_raw", 1), ("food_roots", 2), ("food_mushroom", 2), ("drink_water_clean", 1) },
                new[] { ("food_stew", 2) },
                craftTime: 45f);

            CreateRecipe("recipe_dried_fruit", "Dry Fruit", CraftingCategory.Cooking,
                WorkstationType.None,
                new[] { ("food_berries", 5) },
                new[] { ("food_dried_fruit", 3) },
                craftTime: 120f);

            CreateRecipe("recipe_jerky", "Make Jerky", CraftingCategory.Cooking,
                WorkstationType.None,
                new[] { ("food_meat_raw", 2), ("misc_salt", 1) },
                new[] { ("food_jerky", 3) },
                craftTime: 180f);

            // === DRINKS ===
            CreateRecipe("recipe_purify_water", "Purify Water", CraftingCategory.Cooking,
                WorkstationType.Campfire,
                new[] { ("drink_water_dirty", 1) },
                new[] { "drink_water_clean" },
                craftTime: 15f);

            CreateRecipe("recipe_herbal_tea", "Brew Herbal Tea", CraftingCategory.Cooking,
                WorkstationType.CookingPot,
                new[] { ("medicine_herbs", 2), ("drink_water_clean", 1) },
                new[] { "drink_tea" },
                craftTime: 20f);

            CreateRecipe("recipe_fruit_juice", "Make Fruit Juice", CraftingCategory.Cooking,
                WorkstationType.None,
                new[] { ("food_berries", 5), ("drink_water_clean", 1) },
                new[] { ("drink_juice", 2) },
                craftTime: 10f);

            LogGeneration($"Generated {10} food recipes");
        }

        private void GenerateMedicineRecipes()
        {
            CreateRecipe("recipe_bandage", "Craft Bandage", CraftingCategory.Medicine,
                WorkstationType.None,
                new[] { ("cloth", 1) },
                new[] { ("medicine_bandage", 3) },
                craftTime: 5f);

            CreateRecipe("recipe_poultice", "Make Herbal Poultice", CraftingCategory.Medicine,
                WorkstationType.None,
                new[] { ("medicine_herbs", 3), ("drink_water_clean", 1) },
                new[] { ("medicine_poultice", 2) },
                craftTime: 10f);

            CreateRecipe("recipe_antidote", "Brew Antidote", CraftingCategory.Medicine,
                WorkstationType.CookingPot,
                new[] { ("medicine_herbs", 2), ("food_mushroom", 1), ("drink_water_clean", 1) },
                new[] { "medicine_antidote" },
                craftTime: 20f);

            CreateRecipe("recipe_painkiller", "Make Pain Relief", CraftingCategory.Medicine,
                WorkstationType.None,
                new[] { ("bark", 3), ("medicine_herbs", 1) },
                new[] { ("medicine_painkiller", 2) },
                craftTime: 15f);

            CreateRecipe("recipe_antiseptic", "Make Antiseptic", CraftingCategory.Medicine,
                WorkstationType.CookingPot,
                new[] { ("resin", 2), ("medicine_herbs", 2), ("drink_water_clean", 1) },
                new[] { "medicine_antiseptic" },
                craftTime: 25f);

            CreateRecipe("recipe_salve", "Make Healing Salve", CraftingCategory.Medicine,
                WorkstationType.None,
                new[] { ("animal_fat", 1), ("medicine_herbs", 2), ("resin", 1) },
                new[] { ("medicine_salve", 2) },
                craftTime: 15f);

            LogGeneration($"Generated {6} medicine recipes");
        }

        private void GenerateClothingRecipes()
        {
            // === BASIC CLOTHING ===
            CreateRecipe("recipe_cloth_hat", "Craft Cloth Hat", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("cloth", 2), ("rope", 1) },
                new[] { "clothing_hat_cloth" },
                craftTime: 15f);

            CreateRecipe("recipe_cloth_shirt", "Craft Cloth Shirt", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("cloth", 3), ("rope", 2) },
                new[] { "clothing_shirt_cloth" },
                craftTime: 20f);

            CreateRecipe("recipe_cloth_pants", "Craft Cloth Pants", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("cloth", 4), ("rope", 2) },
                new[] { "clothing_pants_cloth" },
                craftTime: 25f);

            // === LEATHER CLOTHING ===
            CreateRecipe("recipe_leather_hat", "Craft Leather Hat", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 2), ("rope", 1) },
                new[] { "clothing_hat_leather" },
                craftTime: 20f);

            CreateRecipe("recipe_leather_vest", "Craft Leather Vest", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 4), ("rope", 2) },
                new[] { "clothing_shirt_leather" },
                craftTime: 30f);

            CreateRecipe("recipe_leather_pants", "Craft Leather Pants", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 5), ("rope", 2) },
                new[] { "clothing_pants_leather" },
                craftTime: 35f);

            CreateRecipe("recipe_leather_boots", "Craft Leather Boots", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 3), ("rope", 2) },
                new[] { "clothing_boots_leather" },
                craftTime: 25f);

            // === FUR CLOTHING ===
            CreateRecipe("recipe_fur_hat", "Craft Fur Hat", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("fur", 2), ("leather", 1), ("rope", 1) },
                new[] { "clothing_hat_fur" },
                craftTime: 25f);

            CreateRecipe("recipe_fur_coat", "Craft Fur Coat", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("fur", 5), ("leather", 2), ("rope", 3) },
                new[] { "clothing_coat_fur" },
                craftTime: 45f);

            // === ACCESSORIES ===
            CreateRecipe("recipe_small_backpack", "Craft Small Backpack", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 3), ("cloth", 2), ("rope", 2) },
                new[] { "clothing_backpack_small" },
                craftTime: 30f);

            CreateRecipe("recipe_large_backpack", "Craft Large Backpack", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 5), ("cloth", 3), ("rope", 3), ("ingot_bronze", 2) },
                new[] { "clothing_backpack_large" },
                craftTime: 45f);

            LogGeneration($"Generated {11} clothing recipes");
        }

        private void GenerateBuildingRecipes()
        {
            // === WORKSTATIONS ===
            CreateRecipe("recipe_campfire", "Build Campfire", CraftingCategory.Building,
                WorkstationType.None,
                new[] { ("stone_small", 5), ("wood_stick", 3), ("fuel_tinder", 5) },
                new[] { "station_campfire" },
                craftTime: 10f);

            CreateRecipe("recipe_workbench", "Build Workbench", CraftingCategory.Building,
                WorkstationType.None,
                new[] { ("wood_plank", 4), ("wood_log", 2), ("stone_small", 4) },
                new[] { "station_workbench" },
                craftTime: 30f);

            CreateRecipe("recipe_forge", "Build Forge", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("stone_large", 10), ("clay", 5), ("ingot_iron", 2) },
                new[] { "station_forge" },
                craftTime: 60f);

            CreateRecipe("recipe_anvil", "Build Anvil", CraftingCategory.Building,
                WorkstationType.Forge,
                new[] { ("ingot_iron", 10), ("wood_log", 2) },
                new[] { "station_anvil" },
                craftTime: 45f);

            CreateRecipe("recipe_tanning_rack", "Build Tanning Rack", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 4), ("rope", 3) },
                new[] { "station_tanning_rack" },
                craftTime: 20f);

            CreateRecipe("recipe_cooking_pot", "Craft Cooking Pot", CraftingCategory.Building,
                WorkstationType.Forge,
                new[] { ("ingot_iron", 3), ("wood_stick", 1) },
                new[] { "station_cooking_pot" },
                craftTime: 25f);

            // === STRUCTURES ===
            CreateRecipe("recipe_wall_wood", "Build Wooden Wall", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 5), ("wood_log", 2) },
                new[] { "building_wall_wood" },
                craftTime: 20f);

            CreateRecipe("recipe_wall_stone", "Build Stone Wall", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("stone_large", 8), ("clay", 3) },
                new[] { "building_wall_stone" },
                craftTime: 40f);

            CreateRecipe("recipe_door_wood", "Build Wooden Door", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 4), ("ingot_iron", 1) },
                new[] { "building_door_wood" },
                craftTime: 25f);

            CreateRecipe("recipe_roof_thatch", "Build Thatch Roof", CraftingCategory.Building,
                WorkstationType.None,
                new[] { ("fiber_plant", 20), ("wood_stick", 5) },
                new[] { "building_roof_thatch" },
                craftTime: 15f);

            // === FURNITURE ===
            CreateRecipe("recipe_bed", "Build Bed", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 4), ("cloth", 3), ("fur", 2) },
                new[] { "furniture_bed" },
                craftTime: 30f);

            CreateRecipe("recipe_chest", "Build Storage Chest", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 6), ("ingot_iron", 2) },
                new[] { "furniture_chest" },
                craftTime: 25f);

            CreateRecipe("recipe_table", "Build Table", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 4), ("wood_log", 2) },
                new[] { "furniture_table" },
                craftTime: 20f);

            LogGeneration($"Generated {13} building recipes");
        }

        private void GenerateProcessingRecipes()
        {
            // === MATERIAL PROCESSING ===
            CreateRecipe("recipe_planks", "Cut Planks", CraftingCategory.Processing,
                WorkstationType.Workbench,
                new[] { ("wood_log", 1), ("tool_saw", 0) }, // Tool not consumed
                new[] { ("wood_plank", 3) },
                craftTime: 10f);

            CreateRecipe("recipe_rope", "Weave Rope", CraftingCategory.Processing,
                WorkstationType.None,
                new[] { ("fiber_plant", 3) },
                new[] { "rope" },
                craftTime: 5f);

            CreateRecipe("recipe_cloth", "Weave Cloth", CraftingCategory.Processing,
                WorkstationType.Workbench,
                new[] { ("fiber_plant", 5) },
                new[] { "cloth" },
                craftTime: 15f);

            CreateRecipe("recipe_leather", "Tan Leather", CraftingCategory.Processing,
                WorkstationType.TanningRack,
                new[] { ("fur", 2), ("bark", 3) },
                new[] { ("leather", 2) },
                craftTime: 60f);

            CreateRecipe("recipe_charcoal", "Make Charcoal", CraftingCategory.Processing,
                WorkstationType.Campfire,
                new[] { ("wood_log", 3) },
                new[] { ("fuel_charcoal", 5) },
                craftTime: 45f);

            // === METAL PROCESSING ===
            CreateRecipe("recipe_smelt_copper", "Smelt Copper", CraftingCategory.Processing,
                WorkstationType.Forge,
                new[] { ("ore_copper", 2), ("fuel_charcoal", 1) },
                new[] { "ingot_copper" },
                craftTime: 30f);

            CreateRecipe("recipe_smelt_iron", "Smelt Iron", CraftingCategory.Processing,
                WorkstationType.Forge,
                new[] { ("ore_iron", 2), ("fuel_charcoal", 2) },
                new[] { "ingot_iron" },
                craftTime: 45f);

            CreateRecipe("recipe_smelt_tin", "Smelt Tin", CraftingCategory.Processing,
                WorkstationType.Forge,
                new[] { ("ore_tin", 2), ("fuel_charcoal", 1) },
                new[] { "ingot_tin" },
                craftTime: 30f);

            CreateRecipe("recipe_alloy_bronze", "Make Bronze", CraftingCategory.Processing,
                WorkstationType.Forge,
                new[] { ("ingot_copper", 3), ("ingot_tin", 1) },
                new[] { ("ingot_bronze", 4) },
                craftTime: 40f);

            CreateRecipe("recipe_alloy_steel", "Make Steel", CraftingCategory.Advanced,
                WorkstationType.Forge,
                new[] { ("ingot_iron", 2), ("fuel_charcoal", 3) },
                new[] { "ingot_steel" },
                craftTime: 60f);

            LogGeneration($"Generated {10} processing recipes");
        }

        private void GenerateUpgradeRecipes()
        {
            // === TOOL UPGRADES ===
            CreateRecipe("recipe_repair_stone_tool", "Repair Stone Tool", CraftingCategory.Tools,
                WorkstationType.Workbench,
                new[] { ("stone_small", 1), ("rope", 1) },
                new string[] { }, // No output, repairs existing
                craftTime: 10f);

            CreateRecipe("recipe_repair_bronze_tool", "Repair Bronze Tool", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_bronze", 1) },
                new string[] { }, // No output, repairs existing
                craftTime: 15f);

            CreateRecipe("recipe_repair_iron_tool", "Repair Iron Tool", CraftingCategory.Tools,
                WorkstationType.Anvil,
                new[] { ("ingot_iron", 1) },
                new string[] { }, // No output, repairs existing
                craftTime: 20f);

            // === CONTAINER RECIPES ===
            CreateRecipe("recipe_pouch", "Craft Pouch", CraftingCategory.Clothing,
                WorkstationType.None,
                new[] { ("leather", 1), ("rope", 1) },
                new[] { "container_pouch" },
                craftTime: 10f);

            CreateRecipe("recipe_bag", "Craft Bag", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("cloth", 2), ("rope", 2) },
                new[] { "container_bag" },
                craftTime: 15f);

            CreateRecipe("recipe_barrel", "Build Barrel", CraftingCategory.Building,
                WorkstationType.Workbench,
                new[] { ("wood_plank", 5), ("ingot_iron", 2) },
                new[] { "container_barrel" },
                craftTime: 30f);

            CreateRecipe("recipe_bottle", "Craft Bottle", CraftingCategory.Building,
                WorkstationType.Forge,
                new[] { ("sand", 2), ("fuel_charcoal", 1) },
                new[] { ("container_bottle", 2) },
                craftTime: 20f);

            CreateRecipe("recipe_waterskin", "Craft Waterskin", CraftingCategory.Clothing,
                WorkstationType.None,
                new[] { ("leather", 2), ("resin", 1) },
                new[] { "container_waterskin" },
                craftTime: 15f);

            CreateRecipe("recipe_quiver", "Craft Quiver", CraftingCategory.Clothing,
                WorkstationType.Workbench,
                new[] { ("leather", 2), ("rope", 1) },
                new[] { "container_quiver" },
                craftTime: 20f);

            // === FUEL RECIPES ===
            CreateRecipe("recipe_torch", "Craft Torch", CraftingCategory.Tools,
                WorkstationType.None,
                new[] { ("wood_stick", 1), ("cloth", 1), ("resin", 1) },
                new[] { ("fuel_torch", 2) },
                craftTime: 5f);

            CreateRecipe("recipe_candle", "Make Candle", CraftingCategory.Tools,
                WorkstationType.Campfire,
                new[] { ("animal_fat", 1), ("rope", 1) },
                new[] { ("fuel_candle", 3) },
                craftTime: 10f);

            LogGeneration($"Generated {11} upgrade/misc recipes");
        }

        private RecipeDefinition CreateRecipe(string id, string name, CraftingCategory category,
            WorkstationType workstation, (string, int)[] ingredients, string[] outputs, float craftTime = 5f)
        {
            // Check if recipe already exists
            var existingRecipe = recipeDatabase.GetRecipe(id);
            if (existingRecipe != null)
            {
                return existingRecipe;
            }

            // Create directories if needed
            EnsureDirectoryExists("Assets/_Project/Data/Recipes");

            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.recipeID = id;
            recipe.recipeName = name;
            recipe.category = category;
            recipe.requiredWorkstation = workstation;
            recipe.baseCraftTime = craftTime;

            // Set up ingredients
            var ingredientList = new List<RecipeIngredient>();
            foreach (var (itemId, quantity) in ingredients)
            {
                var item = itemDatabase.GetItem(itemId);
                if (item != null)
                {
                    ingredientList.Add(new RecipeIngredient
                    {
                        name = item.displayName,
                        specificItem = item,
                        category = item.primaryCategory,
                        quantity = quantity,
                        consumed = quantity > 0 // 0 means tool not consumed
                    });
                }
                else
                {
                    // Create placeholder if item doesn't exist yet
                    ingredientList.Add(new RecipeIngredient
                    {
                        name = itemId,
                        quantity = quantity,
                        consumed = quantity > 0
                    });
                }
            }
            recipe.ingredients = ingredientList.ToArray();

            // Set up outputs
            var outputList = new List<RecipeOutput>();
            foreach (var outputStr in outputs)
            {
                // Parse output string (can be "itemId" or ("itemId", quantity))
                string itemId = outputStr;
                int quantity = 1;

                // Check if it's a tuple format
                if (outputStr.StartsWith("(") && outputStr.EndsWith(")"))
                {
                    var parts = outputStr.Trim('(', ')').Split(',');
                    if (parts.Length == 2)
                    {
                        itemId = parts[0].Trim();
                        int.TryParse(parts[1].Trim(), out quantity);
                    }
                }

                var item = itemDatabase.GetItem(itemId);
                if (item != null)
                {
                    outputList.Add(new RecipeOutput
                    {
                        item = item,
                        quantityMin = quantity,
                        quantityMax = quantity,
                        chance = 1f
                    });
                }
            }
            recipe.outputs = outputList.ToArray();

            // Set description
            recipe.description = GenerateRecipeDescription(recipe);

            string path = $"Assets/_Project/Data/Recipes/{id}.asset";
            AssetDatabase.CreateAsset(recipe, path);

            recipeDatabase.AddRecipe(recipe);
            recipesGenerated++;

            return recipe;
        }



        private RecipeDefinition CreateRecipe(string id, string name, CraftingCategory category,
            WorkstationType workstation, (string, int)[] ingredients, (string, int)[] outputs, float craftTime = 5f)
        {
            // Convert output tuples to string array
            var outputStrings = outputs.Select(o => $"({o.Item1}, {o.Item2})").ToArray();
            return CreateRecipe(id, name, category, workstation, ingredients, outputStrings, craftTime);
        }

        private string GenerateRecipeDescription(RecipeDefinition recipe)
        {
            var desc = $"Craft {recipe.recipeName} ";

            if (recipe.requiredWorkstation != WorkstationType.None)
            {
                desc += $"at {recipe.requiredWorkstation}. ";
            }

            desc += "Requires: ";
            desc += string.Join(", ", recipe.ingredients.Select(i => $"{i.quantity}x {i.name}"));

            return desc;
        }

        private void EnsureDirectoryExists(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string parentPath = currentPath;
                currentPath = $"{currentPath}/{folders[i]}";

                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentPath, folders[i]);
                    Debug.Log($"Created folder: {currentPath}");
                }
            }

            // Refresh to ensure Unity recognizes the new folders
            AssetDatabase.Refresh();
        }
        public static class ProjectInitializer
        {
            [MenuItem("Tools/Wild Survival/Initialize Project", priority = 0)]
            public static void InitializeProject()
            {
                Debug.Log("=== Initializing Wild Survival Project ===");

                // 1. Create folder structure
                CreateFolderStructure();

                // 2. Create databases
                CreateDatabases();

                // 3. Refresh
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("=== Project initialization complete! ===");
                EditorUtility.DisplayDialog("Success", "Project structure initialized successfully!", "OK");
            }

            private static void CreateFolderStructure()
            {
                // Main folders
                CreateFolder("Assets", "_Project");
                CreateFolder("Assets/_Project", "Scripts");
                CreateFolder("Assets/_Project", "Data");
                CreateFolder("Assets/_Project", "Prefabs");
                CreateFolder("Assets/_Project", "Materials");
                CreateFolder("Assets/_Project", "Textures");

                // Script subfolders
                CreateFolder("Assets/_Project/Scripts", "Data");
                CreateFolder("Assets/_Project/Scripts", "Database");
                CreateFolder("Assets/_Project/Scripts", "Inventory");
                CreateFolder("Assets/_Project/Scripts", "Crafting");
                CreateFolder("Assets/_Project/Scripts", "Player");
                CreateFolder("Assets/_Project/Scripts", "Editor");

                // Data subfolders
                CreateFolder("Assets/_Project/Data", "Items");
                CreateFolder("Assets/_Project/Data", "Recipes");
                CreateFolder("Assets/_Project/Data", "Config");

                Debug.Log("Folder structure created");
            }

            private static void CreateFolder(string parent, string newFolder)
            {
                string path = $"{parent}/{newFolder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
            }

            private static void CreateDatabases()
            {
                // Item Database
                string itemDbPath = "Assets/_Project/Data/ItemDatabase.asset";
                if (!AssetDatabase.LoadAssetAtPath<ItemDatabase>(itemDbPath))
                {
                    var itemDb = ScriptableObject.CreateInstance<ItemDatabase>();
                    AssetDatabase.CreateAsset(itemDb, itemDbPath);
                    Debug.Log("Created ItemDatabase");
                }

                // Recipe Database
                string recipeDbPath = "Assets/_Project/Data/RecipeDatabase.asset";
                if (!AssetDatabase.LoadAssetAtPath<RecipeDatabase>(recipeDbPath))
                {
                    var recipeDb = ScriptableObject.CreateInstance<RecipeDatabase>();
                    AssetDatabase.CreateAsset(recipeDb, recipeDbPath);
                    Debug.Log("Created RecipeDatabase");
                }
            }

            [MenuItem("Tools/Wild Survival/Validate Project Setup")]
            public static void ValidateSetup()
            {
                bool isValid = true;
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== Project Validation Report ===");

                // Check folders
                string[] requiredFolders = {
            "Assets/_Project",
            "Assets/_Project/Data",
            "Assets/_Project/Data/Items",
            "Assets/_Project/Data/Recipes",
            "Assets/_Project/Scripts"
        };

                foreach (var folder in requiredFolders)
                {
                    if (AssetDatabase.IsValidFolder(folder))
                    {
                        report.AppendLine($"✓ {folder}");
                    }
                    else
                    {
                        report.AppendLine($"✗ {folder} - MISSING");
                        isValid = false;
                    }
                }

                // Check databases
                if (AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset"))
                {
                    report.AppendLine("✓ ItemDatabase exists");
                }
                else
                {
                    report.AppendLine("✗ ItemDatabase - MISSING");
                    isValid = false;
                }

                if (AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/_Project/Data/RecipeDatabase.asset"))
                {
                    report.AppendLine("✓ RecipeDatabase exists");
                }
                else
                {
                    report.AppendLine("✗ RecipeDatabase - MISSING");
                    isValid = false;
                }

                report.AppendLine($"\nValidation Result: {(isValid ? "PASSED" : "FAILED")}");

                Debug.Log(report.ToString());

                if (!isValid)
                {
                    if (EditorUtility.DisplayDialog("Validation Failed",
                        "Project setup is incomplete. Would you like to initialize now?",
                        "Yes", "No"))
                    {
                        InitializeProject();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Passed",
                        "Project setup is complete and valid!", "Great!");
                }
            }
        }


        private void LogGeneration(string message)
        {
            generationLog.Add(message);
            Debug.Log($"[Database Generator] {message}");
        }

        public static class DatabaseGeneratorExtensions
        {
            [MenuItem("Tools/Wild Survival/Add Missing Items")]
            public static void AddMissingItems()
            {
                // Ensure all directories exist first
                EnsureDirectoryStructure();

                var itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
                if (itemDB == null)
                {
                    Debug.LogError("ItemDatabase not found at Assets/_Project/Data/ItemDatabase.asset");
                    return;
                }

                // Create misc_salt if it doesn't exist
                if (itemDB.GetItem("misc_salt") == null)
                {
                    var salt = ScriptableObject.CreateInstance<ItemDefinition>();
                    salt.itemID = "misc_salt";
                    salt.displayName = "Salt";
                    salt.description = "Mineral salt for preservation";
                    salt.primaryCategory = ItemCategory.Misc;
                    salt.weight = 0.1f;
                    salt.maxStackSize = 50;
                    salt.gridSize = Vector2Int.one;

                    AssetDatabase.CreateAsset(salt, "Assets/_Project/Data/Items/misc_salt.asset");
                    itemDB.AddItem(salt);
                    Debug.Log("Created missing item: Salt");
                }

                // Create ingot_tin if missing
                if (itemDB.GetItem("ingot_tin") == null)
                {
                    var tin = ScriptableObject.CreateInstance<ItemDefinition>();
                    tin.itemID = "ingot_tin";
                    tin.displayName = "Tin Ingot";
                    tin.description = "Refined tin ingot";
                    tin.primaryCategory = ItemCategory.Resource;
                    tin.weight = 1f;
                    tin.maxStackSize = 20;
                    tin.gridSize = Vector2Int.one;

                    AssetDatabase.CreateAsset(tin, "Assets/_Project/Data/Items/ingot_tin.asset");
                    itemDB.AddItem(tin);
                    Debug.Log("Created missing item: Tin Ingot");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("Missing items check complete!");
            }

            private static void EnsureDirectoryStructure()
            {
                // Create main project folder
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Project");
                }

                // Create Data folder
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Data");
                }

                // Create Items folder
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Items"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Data", "Items");
                }

                // Create Recipes folder
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Recipes"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Data", "Recipes");
                }

                // Create database files if they don't exist
                CreateDatabasesIfMissing();
            }

            private static void CreateDatabasesIfMissing()
            {
                // Create ItemDatabase if missing
                string itemDbPath = "Assets/_Project/Data/ItemDatabase.asset";
                if (!AssetDatabase.LoadAssetAtPath<ItemDatabase>(itemDbPath))
                {
                    var itemDb = ScriptableObject.CreateInstance<ItemDatabase>();
                    AssetDatabase.CreateAsset(itemDb, itemDbPath);
                    Debug.Log("Created ItemDatabase");
                }

                // Create RecipeDatabase if missing
                string recipeDbPath = "Assets/_Project/Data/RecipeDatabase.asset";
                if (!AssetDatabase.LoadAssetAtPath<RecipeDatabase>(recipeDbPath))
                {
                    var recipeDb = ScriptableObject.CreateInstance<RecipeDatabase>();
                    AssetDatabase.CreateAsset(recipeDb, recipeDbPath);
                    Debug.Log("Created RecipeDatabase");
                }
            }

        }
    }

    // Add missing misc_salt item creation
    public static class DatabaseGeneratorExtensions
    {
        [MenuItem("Tools/Wild Survival/Add Missing Items")]
        public static void AddMissingItems()
        {
            var itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Data/ItemDatabase.asset");
            if (itemDB == null) return;

            // Create misc_salt if it doesn't exist
            if (itemDB.GetItem("misc_salt") == null)
            {
                var salt = ScriptableObject.CreateInstance<ItemDefinition>();
                salt.itemID = "misc_salt";
                salt.displayName = "Salt";
                salt.description = "Mineral salt for preservation";
                salt.primaryCategory = ItemCategory.Misc;
                salt.weight = 0.1f;
                salt.maxStackSize = 50;
                salt.gridSize = Vector2Int.one;

                AssetDatabase.CreateAsset(salt, "Assets/_Project/Data/Items/misc_salt.asset");
                itemDB.AddItem(salt);
            }

            // Create ingot_tin if missing
            if (itemDB.GetItem("ingot_tin") == null)
            {
                var tin = ScriptableObject.CreateInstance<ItemDefinition>();
                tin.itemID = "ingot_tin";
                tin.displayName = "Tin Ingot";
                tin.description = "Refined tin ingot";
                tin.primaryCategory = ItemCategory.Resource;
                tin.weight = 1f;
                tin.maxStackSize = 20;
                tin.gridSize = Vector2Int.one;

                AssetDatabase.CreateAsset(tin, "Assets/_Project/Data/Items/ingot_tin.asset");
                itemDB.AddItem(tin);


            }

            


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Missing items added successfully!");


        }


    }


}

