// UltimateInventoryToolHub.cs

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static PlasticPipe.PlasticProtocol.Messages.NegotiationCommand;

/// <summary>
/// Ultimate Inventory Tool Hub - Elite Professional Edition
/// The most sophisticated inventory management system for Unity
/// Featuring advanced Tetris mechanics, AI integration, and stunning visuals
///
/// Ultimate Inventory Tool Hub - Complete Edition v5.0
/// Merged all features from previous tools, fixed all bugs
/// </summary>
public class UltimateInventoryToolHub : EditorWindow
{
    // ========== CONSTANTS ==========
    private const string WINDOW_TITLE = "🎮 Ultimate Inventory Hub - Complete";
    private const string VERSION = "5.0 Complete";
    private const float TAB_HEIGHT = 35f;
    private const float SIDEBAR_WIDTH = 260f;
    private const int GRID_CELL_SIZE = 45;
    private const int MAX_AI_BATCH_SIZE = 10000;

    // ========== VISUAL STYLE ==========
    private GUIStyle headerStyle;
    private GUIStyle tabButtonStyle;
    private GUIStyle activeTabStyle;
    private GUIStyle sidebarStyle;
    private GUIStyle cardStyle;
    private GUIStyle successStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    private GUIStyle glowBoxStyle;
    private GUIStyle tetrisGridStyle;
    private GUIStyle itemSlotStyle;
    private GUIStyle craftingSlotStyle;

    private Texture2D backgroundTexture;
    private Texture2D headerGradient;
    private Texture2D gridTexture;
    private Texture2D itemGlowTexture;
    private Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

    // At the top of UltimateInventoryToolHub class (around line 50-60):



    // ========== TAB SYSTEM ==========
    private enum TabMode
    {
        Dashboard,
        InventorySimulator,
        CraftingStudio,
        ItemCreator,
        RecipeBuilder,
        BatchProcessor,
        DatabaseManager,
        Analytics,
        Settings
    }

    private TabMode currentTab = TabMode.Dashboard;
    private TabMode previousTab = TabMode.Dashboard;
    private float tabTransition = 0f;

    // ========== DATABASE REFERENCES ==========
    private ItemDatabase itemDatabase;  // NOT UltimateInventoryToolHub.ItemDatabase
    private RecipeDatabase recipeDatabase;  // NOT UltimateInventoryToolHub.RecipeDatabase

    private bool databasesLoaded = false;
    private Dictionary<string, ItemDefinition> itemLookup = new Dictionary<string, ItemDefinition>();
    private Dictionary<string, RecipeDefinition> recipeLookup = new Dictionary<string, RecipeDefinition>();

    // ========== INVENTORY SIMULATOR STATE ==========
    private int gridWidth = 8;
    private int gridHeight = 10;
    private float maxWeight = 50f;
    private float currentWeight = 0f;
    private bool[,] gridOccupancy;
    private List<PlacedItem> placedItems = new List<PlacedItem>();
    private PlacedItem draggedItem;
    private Vector2Int dragOffset;
    private bool isDragging;
    private bool canPlaceAtDragPosition;
    private float gridZoom = 1f;
    private Vector2 gridPan = Vector2.zero;
    private Dictionary<Vector2Int, Color> heatmap = new Dictionary<Vector2Int, Color>();

    // ========== CRAFTING STUDIO STATE ==========
    private RecipeDefinition selectedRecipe;
    private Dictionary<string, int> availableMaterials = new Dictionary<string, int>();
    private float craftingProgress = 0f;
    private bool isCrafting = false;
    private List<CraftingQueue> craftingQueue = new List<CraftingQueue>();
    private WorkstationType currentWorkstation = WorkstationType.WorkBench;

    // ========== AI BATCH PROCESSOR STATE ==========
    private string aiPrompt = "";
    private string aiResponse = "";
    private List<ItemBatchData> pendingAIItems = new List<ItemBatchData>();
    private List<RecipeBatchData> pendingAIRecipes = new List<RecipeBatchData>();
    private bool isProcessingAI = false;
    private float aiProcessingProgress = 0f;
    private ValidationReport lastValidation;

    // ========== STATISTICS ==========
    private int totalItems = 0;
    private int totalRecipes = 0;
    private float lastRefreshTime = 0;
    private List<string> recentActivity = new List<string>();
    private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
    private List<AnalyticsDataPoint> analyticsHistory = new List<AnalyticsDataPoint>();

    // ========== PREFERENCES ==========
    private bool autoSaveEnabled = true;
    private bool showTooltips = true;
    private bool darkMode = true;
    private bool animationsEnabled = true;
    private bool hapticFeedback = true;
    private bool showGridNumbers = true;
    private bool autoArrangeItems = true;
    private float autoSaveInterval = 300f;
    private float lastAutoSave = 0;

    // ========== ANIMATION STATE ==========
    private float glowAnimation = 0;
    private float pulseAnimation = 0;
    private Dictionary<string, float> buttonAnimations = new Dictionary<string, float>();
    private Dictionary<PlacedItem, float> itemAnimations = new Dictionary<PlacedItem, float>();
    private ParticleEffect[] particles = new ParticleEffect[10];
    private int nextParticle = 0;

    // ========== UI STATE ==========
    private Vector2 mainScrollPosition;
    private Vector2 itemListScroll;
    private Vector2 recipeListScroll;
    private string searchQuery = "";
    private bool isSearching = false;
    private string statusMessage = "Ready";
    private Color statusColor = Color.white;
    private float statusTimer = 0;

    [MenuItem("Tools/Wild Survival/Debug/Check Database Types")]
    public static void CheckDatabaseTypes()
    {
        // Check ItemDatabase
        string[] itemDbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        foreach (string guid in itemDbGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
            if (db != null)
            {
                Debug.Log($"ItemDatabase at {path}: {db.GetAllItems().Count} items");
            }
        }

        // Check RecipeDatabase
        string[] recipeDbGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
        foreach (string guid in recipeDbGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
            if (db != null)
            {
                Debug.Log($"RecipeDatabase at {path}: {db.GetAllRecipes().Count} recipes");

                // Check actual types
                SerializedObject so = new SerializedObject(db);
                SerializedProperty recipesProp = so.FindProperty("recipes");
                if (recipesProp != null && recipesProp.arraySize > 0)
                {
                    for (int i = 0; i < recipesProp.arraySize; i++)
                    {
                        var element = recipesProp.GetArrayElementAtIndex(i);
                        if (element.objectReferenceValue != null)
                        {
                            Debug.Log($"  Recipe {i}: {element.objectReferenceValue.GetType()}");
                        }
                    }
                }
            }
        }
    }

    [MenuItem("Tools/Wild Survival/Maintenance/Clean Databases")]
    public static void CleanDatabases()
    {
        // Clean ItemDatabase
        var itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(
            "Assets/_WildSurvival/Code/Runtime/Data/Databases/ItemDatabase.asset");

        if (itemDb != null)
        {
            itemDb.CleanDatabase();
            EditorUtility.SetDirty(itemDb);
            Debug.Log("✅ Cleaned ItemDatabase");
        }

        // Clean RecipeDatabase
        var recipeDb = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(
            "Assets/_WildSurvival/Code/Runtime/Data/Databases/RecipeDatabase.asset");

        if (recipeDb != null)
        {
            recipeDb.CleanDatabase();
            EditorUtility.SetDirty(recipeDb);
            Debug.Log("✅ Cleaned RecipeDatabase");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    [MenuItem("Tools/Wild Survival/🎮 Ultimate Inventory Hub Elite", priority = 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<UltimateInventoryToolHub>();
        window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("d_Profiler.UIDetails").image);
        window.minSize = new Vector2(1400, 800);
        window.Show();

        // Center and maximize
        var position = window.position;
        position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
        window.position = position;
    }

    private void OnEnable()
    {
        InitializeStyles();
        LoadDatabases();
        InitializeGrid();
        LoadPreferences();
        SetupAnimations();

        // Load saved inventory grid
        LoadInventoryGrid();

        EditorApplication.update += OnEditorUpdate;

        ShowStatus("✨ Ultimate Inventory Hub Elite initialized!", Color.green);
        LogActivity("System initialized with all features");
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        SavePreferences();
        SaveDatabaseReferences(); // Add this line
        // Save inventory grid
        SaveInventoryGrid();

        if (autoSaveEnabled)
        {
            SaveAllDatabases();
        }
    }

    private void InitializeStyles()
    {
        // Create sophisticated gradients
        backgroundTexture = CreateGradientTexture(
            darkMode ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.96f, 0.96f, 0.97f),
            darkMode ? new Color(0.08f, 0.08f, 0.10f) : new Color(0.92f, 0.92f, 0.94f)
        );

        headerGradient = CreateGradientTexture(
            new Color(0.15f, 0.25f, 0.45f),
            new Color(0.10f, 0.15f, 0.35f)
        );

        gridTexture = CreateGridTexture();
        itemGlowTexture = CreateGlowTexture();
    }

    private void DrawSectionHeading(string icon, string title)
    {
        GUILayout.Space(10);

        Rect headingRect = GUILayoutUtility.GetRect(0, 45);

        // Draw background for heading
        EditorGUI.DrawRect(headingRect, new Color(0.1f, 0.1f, 0.12f, 0.3f));

        // Draw the heading text
        var style = new GUIStyle(headerStyle)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter
        };

        GUI.Label(headingRect, $"{icon} {title}", style);

        GUILayout.Space(10);
    }

    private Texture2D CreateGradientTexture(Color top, Color bottom)
    {
        var tex = new Texture2D(1, 128);
        for (int i = 0; i < 128; i++)
        {
            float t = i / 127f;
            tex.SetPixel(0, i, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CreateGridTexture()
    {
        var tex = new Texture2D(GRID_CELL_SIZE, GRID_CELL_SIZE);
        Color lineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Color fillColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);

        for (int x = 0; x < GRID_CELL_SIZE; x++)
        {
            for (int y = 0; y < GRID_CELL_SIZE; y++)
            {
                if (x == 0 || y == 0 || x == GRID_CELL_SIZE - 1 || y == GRID_CELL_SIZE - 1)
                    tex.SetPixel(x, y, lineColor);
                else
                    tex.SetPixel(x, y, fillColor);
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CreateGlowTexture()
    {
        var tex = new Texture2D(64, 64);
        Vector2 center = new Vector2(32, 32);

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / 32f;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2f); // Smooth falloff
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha * 0.5f));
            }
        }
        tex.Apply();
        return tex;
    }

    private void LoadDatabases()
    {
        // Try multiple approaches to ensure we find the databases

        // First, try to load from known paths
        string itemDbPath = "Assets/_WildSurvival/Data/Databases/ItemDatabase.asset";
        string recipeDbPath = "Assets/_WildSurvival/Data/Databases/RecipeDatabase.asset";

        itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(itemDbPath);
        recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(recipeDbPath);

        // If not found at expected path, search for them
        if (itemDatabase == null)
        {
            string[] itemDbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
            if (itemDbGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(itemDbGuids[0]);
                itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                Debug.Log($"Found ItemDatabase at: {path}");

                // Save the path for next time
                EditorPrefs.SetString("UIT_ItemDatabasePath", path);
            }
        }

        if (recipeDatabase == null)
        {
            string[] recipeDbGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
            if (recipeDbGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(recipeDbGuids[0]);
                recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
                Debug.Log($"Found RecipeDatabase at: {path}");

                // Save the path for next time
                EditorPrefs.SetString("UIT_RecipeDatabasePath", path);
            }
        }

        // Try loading from saved preferences if still not found
        if (itemDatabase == null)
        {
            string savedPath = EditorPrefs.GetString("UIT_ItemDatabasePath", "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(savedPath);
            }
        }

        if (recipeDatabase == null)
        {
            string savedPath = EditorPrefs.GetString("UIT_RecipeDatabasePath", "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(savedPath);
            }
        }

        // Build lookup dictionaries for performance
        if (itemDatabase != null)
        {
            itemLookup.Clear();
            foreach (var item in itemDatabase.GetAllItems())
            {
                if (item != null && !string.IsNullOrEmpty(item.itemID))
                {
                    itemLookup[item.itemID] = item;
                }
            }
            Debug.Log($"Loaded {itemDatabase.GetAllItems().Count} items from database");
        }

        if (recipeDatabase != null)
        {
            recipeLookup.Clear();
            foreach (var recipe in recipeDatabase.GetAllRecipes())
            {
                if (recipe != null && !string.IsNullOrEmpty(recipe.recipeID))
                {
                    recipeLookup[recipe.recipeID] = recipe;
                }
            }
            Debug.Log($"Loaded {recipeDatabase.GetAllRecipes().Count} recipes from database");
        }

        databasesLoaded = (itemDatabase != null && recipeDatabase != null);
        UpdateStatistics();
    }

    private void InitializeGrid(bool clearItems = true)
    {
        gridOccupancy = new bool[gridWidth, gridHeight];

        if (clearItems)
        {
            placedItems.Clear();
            currentWeight = 0f;
        }
        else
        {
            // Recalculate occupancy from existing items
            foreach (var item in placedItems)
            {
                Vector2Int size = item.GetSize();
                for (int x = item.gridPosition.x; x < item.gridPosition.x + size.x; x++)
                {
                    for (int y = item.gridPosition.y; y < item.gridPosition.y + size.y; y++)
                    {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                        {
                            gridOccupancy[x, y] = true;
                        }
                    }
                }
            }
        }

        UpdateHeatmap();
    }

    private void SetupAnimations()
    {
        glowAnimation = 0;
        pulseAnimation = 0;

        // Initialize particle effects
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i] = new ParticleEffect();
        }
    }

    private void OnGUI()
    {
        // Initialize styles if needed
        if (headerStyle == null)
        {
            SetupGUIStyles();
        }

        // Draw sophisticated background
        DrawAnimatedBackground();

        // Main layout
        EditorGUILayout.BeginVertical();

        // Premium header
        DrawPremiumHeader();

        // Content area
        EditorGUILayout.BeginHorizontal();

        // Enhanced sidebar
        DrawEnhancedSidebar();

        // Main content with transitions
        DrawMainContent();

        EditorGUILayout.EndHorizontal();

        // Professional status bar
        DrawProfessionalStatusBar();

        EditorGUILayout.EndVertical();

        // Handle shortcuts and input
        HandleAdvancedInput();

        // Draw overlay effects
        DrawOverlayEffects();

        // Force repaint for animations
        if (animationsEnabled && (glowAnimation > 0 || pulseAnimation > 0 || tabTransition > 0))
        {
            Repaint();
        }
    }

    private void SetupGUIStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };

        tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 13,
            fixedHeight = TAB_HEIGHT,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(25, 15, 8, 8),
            fontStyle = FontStyle.Normal
        };

        activeTabStyle = new GUIStyle(tabButtonStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.3f, 0.7f, 1f) }
        };

        cardStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(20, 20, 15, 15),
            margin = new RectOffset(5, 5, 5, 5)
        };

        tetrisGridStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = gridTexture }
        };

        itemSlotStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(2, 2, 2, 2),
            alignment = TextAnchor.MiddleCenter
        };

        craftingSlotStyle = new GUIStyle(itemSlotStyle)
        {
            normal = { background = Texture2D.whiteTexture }
        };

        successStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.2f, 0.9f, 0.3f) },
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        warningStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.9f, 0.7f, 0.2f) },
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        errorStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.9f, 0.2f, 0.2f) },
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
    }

    private void DrawAnimatedBackground()
    {
        if (backgroundTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, position.width, position.height), backgroundTexture, ScaleMode.StretchToFill);
        }

        // Animated particles in background
        if (animationsEnabled)
        {
            foreach (var particle in particles.Where(p => p.active))
            {
                float alpha = Mathf.Clamp01(particle.life);
                GUI.color = new Color(1, 1, 1, alpha * 0.1f);
                GUI.DrawTexture(new Rect(particle.position.x - 16, particle.position.y - 16, 32, 32), itemGlowTexture);
            }
            GUI.color = Color.white;
        }
    }

    private void DrawPremiumHeader()
    {
        Rect headerRect = GUILayoutUtility.GetRect(0, 70);

        // Gradient background with glow
        GUI.DrawTexture(headerRect, headerGradient, ScaleMode.StretchToFill);

        // Animated glow pulse
        //if (animationsEnabled)
        //{
            //float pulse = Mathf.Sin(pulseAnimation) * 0.5f + 0.5f;
            //GUI.color = new Color(1, 1, 1, pulse * 0.2f);
            //GUI.DrawTexture(headerRect, itemGlowTexture, ScaleMode.StretchToFill);
            //GUI.color = Color.white;
        //}

        // Title with shadow
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.Label(new Rect(headerRect.x + 2, headerRect.y + 12, headerRect.width, 35), "ULTIMATE INVENTORY TOOL HUB", headerStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(headerRect.x, headerRect.y + 10, headerRect.width, 35), "ULTIMATE INVENTORY TOOL HUB", headerStyle);

        // Subtitle
        GUI.Label(new Rect(headerRect.x, headerRect.y + 40, headerRect.width, 20),
            $"Elite Version {VERSION} | Wild Survival Professional Suite", EditorStyles.centeredGreyMiniLabel);

        // Quick action bar
        DrawQuickActionBar(headerRect);
    }

    private void DrawQuickActionBar(Rect headerRect)
    {
        Rect quickBarRect = new Rect(headerRect.width - 400, headerRect.y + 20, 390, 35);

        GUILayout.BeginArea(quickBarRect);
        GUILayout.BeginHorizontal();

        // Quick actions with animations
        if (DrawAnimatedButton("💾", "Quick Save All", 35))
        {
            QuickSave();
        }

        if (DrawAnimatedButton("🔄", "Refresh Databases", 35))
        {
            RefreshAll();
        }

        if (DrawAnimatedButton("⚡", "AI Batch Import", 35))
        {
            currentTab = TabMode.BatchProcessor;
        }

        if (DrawAnimatedButton("📊", "Generate Report", 35))
        {
            GenerateReport();
        }

        GUILayout.Space(10);

        // Global search with icon
        GUILayout.Label("🔍", GUILayout.Width(20));
        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        if (!string.IsNullOrEmpty(searchQuery))
        {
            isSearching = true;
            PerformGlobalSearch();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private bool DrawAnimatedButton(string icon, string tooltip, float width)
    {
        string key = icon + tooltip;
        if (!buttonAnimations.ContainsKey(key))
            buttonAnimations[key] = 0;

        // Hover animation
        Rect rect = GUILayoutUtility.GetRect(width, 30);
        bool hover = rect.Contains(Event.current.mousePosition);

        if (hover && animationsEnabled)
        {
            buttonAnimations[key] = Mathf.Lerp(buttonAnimations[key], 1f, Time.deltaTime * 10f);
            GUI.color = Color.Lerp(Color.white, new Color(0.3f, 0.7f, 1f), buttonAnimations[key]);
        }
        else
        {
            buttonAnimations[key] = Mathf.Lerp(buttonAnimations[key], 0f, Time.deltaTime * 10f);
            GUI.color = Color.white;
        }

        bool clicked = GUI.Button(rect, new GUIContent(icon, tooltip), EditorStyles.toolbarButton);

        GUI.color = Color.white;

        if (clicked && hapticFeedback)
        {
            SpawnParticle(rect.center);
        }

        return clicked;
    }

    private void DrawEnhancedSidebar()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(SIDEBAR_WIDTH));

        GUILayout.Space(10);

        // System health monitor
        DrawSystemHealthMonitor();

        GUILayout.Space(10);

        // Premium navigation
        DrawPremiumNavigation();

        GUILayout.Space(10);

        // Live statistics
        DrawLiveStatistics();

        GUILayout.FlexibleSpace();

        // Activity feed with animations
        DrawAnimatedActivityFeed();

        EditorGUILayout.EndVertical();
    }

    private void DrawSystemHealthMonitor()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("🏥 System Health", EditorStyles.boldLabel);
        //DrawSectionHeading("🏥", "System Health");


        // Database status with animated indicators
        DrawHealthIndicator("Item Database", itemDatabase != null, totalItems);
        DrawHealthIndicator("Recipe Database", recipeDatabase != null, totalRecipes);
        DrawHealthIndicator("Memory Usage", performanceMetrics.GetValueOrDefault("memory", 0) < 500,
            (int)(performanceMetrics.GetValueOrDefault("memory", 0)));
        DrawHealthIndicator("Performance", true, 60); // Always show as healthy, just monitoring

        EditorGUILayout.EndVertical();
    }

    private void DrawHealthIndicator(string label, bool isHealthy, int value)
    {
        EditorGUILayout.BeginHorizontal();

        // Animated status dot
        float pulse = Mathf.Sin(Time.realtimeSinceStartup * 3f) * 0.5f + 0.5f;
        Color dotColor = isHealthy ?
            Color.Lerp(Color.green, new Color(0.5f, 1f, 0.5f), pulse) :
            Color.Lerp(Color.red, new Color(1f, 0.5f, 0.5f), pulse);

        GUI.color = dotColor;
        EditorGUILayout.LabelField("●", GUILayout.Width(15));
        GUI.color = Color.white;

        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        EditorGUILayout.LabelField(value.ToString(), EditorStyles.boldLabel, GUILayout.Width(50));

        if (!isHealthy && label != "Performance")
        {
            if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                FixHealthIssue(label);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPremiumNavigation()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("🧭 Navigation", EditorStyles.boldLabel);

        GUILayout.Space(5);

        // Main features with icons and descriptions
        DrawPremiumTab(TabMode.Dashboard, "📊", "Dashboard", "Command center");
        DrawPremiumTab(TabMode.InventorySimulator, "🎮", "Inventory Simulator", "Tetris-style testing");
        DrawPremiumTab(TabMode.CraftingStudio, "⚒️", "Crafting Studio", "Recipe workshop");

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Content Creation", EditorStyles.centeredGreyMiniLabel);

        DrawPremiumTab(TabMode.ItemCreator, "🎨", "Item Creator", "Visual designer");
        DrawPremiumTab(TabMode.RecipeBuilder, "🔨", "Recipe Builder", "Crafting chains");

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Automation", EditorStyles.centeredGreyMiniLabel);

        DrawPremiumTab(TabMode.BatchProcessor, "🤖", "AI Batch Processor", "Mass generation");
        DrawPremiumTab(TabMode.DatabaseManager, "💾", "Database Manager", "Data control");

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Analysis", EditorStyles.centeredGreyMiniLabel);

        DrawPremiumTab(TabMode.Analytics, "📈", "Analytics", "Deep insights");
        DrawPremiumTab(TabMode.Settings, "⚙️", "Settings", "Preferences");

        EditorGUILayout.EndVertical();
    }

    private void DrawPremiumTab(TabMode tab, string icon, string label, string description)
    {
        bool isActive = (currentTab == tab);

        EditorGUILayout.BeginHorizontal();

        if (isActive)
        {
            // Animated selection indicator
            float pulse = Mathf.Sin(Time.realtimeSinceStartup * 2f) * 0.5f + 0.5f;
            GUI.color = Color.Lerp(new Color(0.3f, 0.7f, 1f), new Color(0.5f, 0.9f, 1f), pulse);
            EditorGUILayout.LabelField("▶", GUILayout.Width(15));
        }
        else
        {
            GUILayout.Space(15);
        }

        GUI.color = isActive ? new Color(0.3f, 0.7f, 1f) : Color.white;

        if (GUILayout.Button(new GUIContent($"{icon} {label}", description),
            isActive ? activeTabStyle : tabButtonStyle, GUILayout.Height(30)))
        {
            previousTab = currentTab;
            currentTab = tab;
            tabTransition = 1f;
            LogActivity($"Opened {label}");

            if (hapticFeedback)
            {
                glowAnimation = 1f;
            }
        }

        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLiveStatistics()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("📊 Live Statistics", EditorStyles.boldLabel);

        // Animated progress bars
        DrawAnimatedProgressBar("Items", totalItems, 1000, new Color(0.3f, 0.8f, 0.3f));
        DrawAnimatedProgressBar("Recipes", totalRecipes, 500, new Color(0.3f, 0.5f, 0.9f));
        DrawAnimatedProgressBar("Grid Usage", (int)(GetGridUsage() * 100), 100, new Color(0.9f, 0.7f, 0.2f));
        DrawAnimatedProgressBar("Weight", (int)currentWeight, (int)maxWeight, new Color(0.8f, 0.3f, 0.3f));

        EditorGUILayout.EndVertical();
    }

    private void DrawAnimatedProgressBar(string label, int current, int max, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(60));

        Rect rect = GUILayoutUtility.GetRect(150, 18);

        // Background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        // Animated fill
        //float targetProgress = Mathf.Clamp01((float)current / max);
        //float animatedProgress = Mathf.Lerp(0, targetProgress, Mathf.SmoothStep(0, 1, tabTransition));
        float targetProgress = max > 0 ? Mathf.Clamp01((float)current / max) : 0;

        float animatedProgress = targetProgress;



        if (animatedProgress > 0)
        {
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * animatedProgress, rect.height);

            // Gradient fill
            EditorGUI.DrawRect(fillRect, color);

            // Shine effect
            if (animationsEnabled && animatedProgress > 0.1f)
            {
                float shine = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
                Rect shineRect = new Rect(rect.x + (rect.width * animatedProgress * shine), rect.y, 2, rect.height);
                EditorGUI.DrawRect(shineRect, new Color(1, 1, 1, 0.5f));
            }
        }

        // Text overlay
        GUI.Label(rect, $"{current}/{max}", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimatedActivityFeed()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("📜 Activity Feed", EditorStyles.boldLabel);

        // Limit to 5 most recent activities
        int maxActivities = Mathf.Min(recentActivity.Count, 5);
        for (int i = 0; i < maxActivities; i++)
        {
            // Fade in animation for new entries
            float alpha = (i == 0 && tabTransition > 0) ? tabTransition : 1f;
            GUI.color = new Color(1, 1, 1, alpha);

            EditorGUILayout.LabelField($"• {recentActivity[i]}", EditorStyles.miniLabel);
        }

        GUI.color = Color.white;

        EditorGUILayout.EndVertical();
    }

    private void DrawMainContent()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        // Content with transition effects
        if (tabTransition > 0 && animationsEnabled)
        {
            float t = Mathf.SmoothStep(0, 1, 1f - tabTransition);
            GUI.color = new Color(1, 1, 1, t);
        }

        // Draw current tab content
        switch (currentTab)
        {
            case TabMode.Dashboard:
                DrawDashboard();
                break;
            case TabMode.InventorySimulator:
                DrawAdvancedInventorySimulator();
                break;
            case TabMode.CraftingStudio:
                DrawCraftingStudio();
                break;
            case TabMode.ItemCreator:
                DrawItemCreator();
                break;
            case TabMode.RecipeBuilder:
                DrawRecipeBuilder();
                break;
            case TabMode.BatchProcessor:
                DrawAIBatchProcessor();
                break;
            case TabMode.DatabaseManager:
                DrawDatabaseManager();
                break;
            case TabMode.Analytics:
                DrawAnalytics();
                break;
            case TabMode.Settings:
                DrawSettings();
                break;
        }

        GUI.color = Color.white;

        EditorGUILayout.EndVertical();
    }

    // ========== ADVANCED INVENTORY SIMULATOR ==========

    private void DrawAdvancedInventorySimulator()
    {
        try
        {
            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Space(10);

            Rect headingRect = GUILayoutUtility.GetRect(0, 40); // Increased from default
            DrawSectionHeading("🎮", "Inventory Simulator");

            EditorGUILayout.Space(10);

            // Toolbar
            DrawInventoryToolbar();

            EditorGUILayout.Space(10);

            // Main content area
            EditorGUILayout.BeginHorizontal();

            // Left panel - Item browser
            DrawItemBrowser();

            // Center - Grid
            DrawTetrisGrid();

            // Right panel - Properties
            if (placedItems != null) // Add null check
            {
                DrawItemProperties();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    catch (Exception e)
    {
        EditorGUILayout.EndVertical(); // Ensure we close the vertical group
        Debug.LogError($"Error in Inventory Simulator: {e.Message}");
    }
}

private void DrawInventoryToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Clear Grid", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("Clear Grid",
            "Are you sure you want to clear all items?", "Clear", "Cancel"))
            {
                ClearGrid();
            }
        }

        if (GUILayout.Button("Auto Arrange", EditorStyles.toolbarButton))
        {
            AutoArrangeItems();
        }

        if (GUILayout.Button("Generate Random", EditorStyles.toolbarButton))
        {
            GenerateRandomLoadout();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("💾 Save Grid", EditorStyles.toolbarButton))
        {
            SaveInventoryGrid();
            ShowStatus("Grid saved!", Color.green);
        }
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("📂 Load Grid", EditorStyles.toolbarButton))
        {
            LoadInventoryGrid();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // Grid size controls
        EditorGUILayout.LabelField("Grid:", GUILayout.Width(35));
        int newWidth = EditorGUILayout.IntField(gridWidth, GUILayout.Width(30));
        EditorGUILayout.LabelField("x", GUILayout.Width(15));
        int newHeight = EditorGUILayout.IntField(gridHeight, GUILayout.Width(30));

        if (newWidth != gridWidth || newHeight != gridHeight)
        {
            ResizeGrid(newWidth, newHeight);
        }

        GUILayout.Space(10);

        // Weight limit
        EditorGUILayout.LabelField("Max Weight:", GUILayout.Width(70));
        maxWeight = EditorGUILayout.Slider(maxWeight, 10f, 200f, GUILayout.Width(100));

        GUILayout.Space(20);

        // View options
        showGridNumbers = GUILayout.Toggle(showGridNumbers, "Numbers", EditorStyles.toolbarButton);
        autoArrangeItems = GUILayout.Toggle(autoArrangeItems, "Auto-Arrange", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        // Zoom controls
        EditorGUILayout.LabelField("Zoom:", GUILayout.Width(40));
        gridZoom = EditorGUILayout.Slider(gridZoom, 0.5f, 2f, GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemBrowser()
    {
        //EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(350));
        // Calculate available height based on window
        float availableHeight = position.height - 300; // Leave room for header, toolbar, and bottom items
        availableHeight = Mathf.Clamp(availableHeight, 200, 600); // Min 200, Max 600

        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(350), GUILayout.MinWidth(350));

        EditorGUILayout.LabelField("📦 Item Library", EditorStyles.boldLabel);

        // Category filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
        ItemCategory filterCategory = (ItemCategory)EditorGUILayout.EnumPopup(ItemCategory.Misc, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Item list with previews - PROPERLY START THE SCROLL VIEW
        //itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll, GUILayout.Height(400));
        itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll,
        GUILayout.Height(availableHeight),
        GUILayout.MaxWidth(320));


        if (itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems();

            foreach (var item in items)
            {
                if (filterCategory != ItemCategory.Misc && item.primaryCategory != filterCategory)
                    continue;

                DrawItemCard(item);
            }
        }
        else
        {
            // Create sample items for testing
            EditorGUILayout.HelpBox("No database loaded. Using sample items.", MessageType.Info);

            for (int i = 0; i < 10; i++)
            {
                var mockItem = CreateMockItem($"Item_{i}", UnityEngine.Random.Range(1, 4), UnityEngine.Random.Range(1, 4));
                DrawItemCard(mockItem);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawItemCard(ItemDefinition item)
    {
        //EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.MaxWidth(300));

        if (item.icon != null)
        {
            GUI.DrawTexture(GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32)),
                item.icon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUILayout.LabelField("📦", GUILayout.Width(32), GUILayout.Height(32));
        }

        // Info

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        // Truncate long names if needed
        string displayName = item.displayName;
        if (displayName.Length > 20)
            displayName = displayName.Substring(0, 17) + "...";

        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{item.gridSize.x}x{item.gridSize.y} | {item.weight:F1}kg",EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();


        // Add button
        if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(25)))
        {
            AddItemToGrid(item);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTetrisGrid()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField("Inventory Grid", EditorStyles.boldLabel);

        // Weight bar
        DrawWeightBar();

        EditorGUILayout.Space(5);

        // Calculate grid size with zoom
        float cellSize = GRID_CELL_SIZE * gridZoom;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth * cellSize, gridHeight * cellSize);

        // Grid background
        EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.18f));

        // Draw grid cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + x * cellSize,
                    gridRect.y + y * cellSize,
                    cellSize,
                    cellSize
                );

                // Cell background based on occupancy
                if (gridOccupancy[x, y])
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.3f, 0.2f, 0.2f, 0.5f));
                }
                else
                {
                    // Heatmap visualization
                    if (heatmap.ContainsKey(new Vector2Int(x, y)))
                    {
                        EditorGUI.DrawRect(cellRect, heatmap[new Vector2Int(x, y)] * 0.3f);
                    }
                }

                // Grid lines
                GUI.Box(cellRect, "", tetrisGridStyle);

                // Grid numbers
                if (showGridNumbers)
                {
                    GUI.Label(cellRect, $"{x},{y}", EditorStyles.miniLabel);
                }
            }
        }

        // Draw placed items
        foreach (var item in placedItems)
        {
            DrawPlacedItem(item, gridRect, cellSize);
        }

        // Draw dragged item
        if (isDragging && draggedItem != null)
        {
            DrawDraggedItem(gridRect, cellSize);
        }

        // Handle grid input
        HandleGridInput(gridRect, cellSize);

        EditorGUILayout.EndVertical();
    }

    private void DrawPlacedItem(PlacedItem item, Rect gridRect, float cellSize)
    {
        Vector2Int size = item.GetSize();

        Rect itemRect = new Rect(
            gridRect.x + item.gridPosition.x * cellSize,
            gridRect.y + item.gridPosition.y * cellSize,
            size.x * cellSize,
            size.y * cellSize
        );

        // Item background with gradient
        Color itemColor = item.displayColor;

        // Add animation if recently placed
        if (itemAnimations.ContainsKey(item) && itemAnimations[item] > 0)
        {
            float anim = itemAnimations[item];
            itemColor = Color.Lerp(itemColor, Color.white, anim * 0.5f);
            itemRect = new Rect(
                itemRect.x - anim * 2,
                itemRect.y - anim * 2,
                itemRect.width + anim * 4,
                itemRect.height + anim * 4
            );
        }

        EditorGUI.DrawRect(itemRect, itemColor);

        // Item border
        Handles.DrawSolidRectangleWithOutline(itemRect, Color.clear, Color.black);

        // Item icon
        if (item.itemDef.icon != null)
        {
            GUI.DrawTexture(new Rect(itemRect.x + 5, itemRect.y + 5, 32, 32), item.itemDef.icon.texture, ScaleMode.ScaleToFit);
        }

        // Item name and stack
        GUI.Label(itemRect, item.itemDef.displayName, EditorStyles.centeredGreyMiniLabel);

        if (item.stackSize > 1)
        {
            GUI.Label(new Rect(itemRect.xMax - 20, itemRect.yMax - 20, 20, 20),
                item.stackSize.ToString(), EditorStyles.whiteBoldLabel);
        }
    }

    private void DrawDraggedItem(Rect gridRect, float cellSize)
    {
        Vector2 mousePos = Event.current.mousePosition;
        Vector2Int gridPos = ScreenToGridPosition(mousePos, gridRect, cellSize);
        gridPos -= dragOffset;

        Vector2Int size = draggedItem.GetSize();

        // Check placement validity
        canPlaceAtDragPosition = CanPlaceItem(gridPos, size, draggedItem.uniqueId);

        Rect itemRect = new Rect(
            gridRect.x + gridPos.x * cellSize,
            gridRect.y + gridPos.y * cellSize,
            size.x * cellSize,
            size.y * cellSize
        );

        // Preview with transparency
        Color previewColor = canPlaceAtDragPosition ?
            new Color(0.2f, 0.8f, 0.2f, 0.5f) :
            new Color(0.8f, 0.2f, 0.2f, 0.5f);

        EditorGUI.DrawRect(itemRect, previewColor);
        Handles.DrawSolidRectangleWithOutline(itemRect, Color.clear, previewColor * 2f);

        // Show rotation hint
        if (draggedItem.itemDef.allowRotation)
        {
            GUI.Label(itemRect, "Press R to rotate", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawWeightBar()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Weight:", GUILayout.Width(50));

        Rect barRect = GUILayoutUtility.GetRect(300, 25);

        // Background
        //EditorGUI.DrawRect(barRect, Color.black);

        // Draw background - lighter gray so we can see it
        EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f, 1f));

        // Weight ratio with color gradient
        float ratio = maxWeight > 0 ? currentWeight / maxWeight : 0;
        Color barColor = Color.Lerp(Color.green, Color.red, ratio);

        if (currentWeight > 0)
        {
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height);
            EditorGUI.DrawRect(fillRect, barColor);
        }

        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        for (int i = 1; i < 10; i++)
        {
            float x = barRect.x + (barRect.width * i / 10f);
            EditorGUI.DrawRect(new Rect(x, barRect.y, 1, barRect.height), GUI.color);
        }
        GUI.color = Color.white;

        // Text overlay - make it more visible
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        // Text overlay
        GUI.Label(barRect, $"{currentWeight:F1} / {maxWeight:F1} kg ({ratio:P0})",
            EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemProperties()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(250));

        //EditorGUILayout.LabelField("📋 Properties", EditorStyles.boldLabel);
        DrawSectionHeading("📋", "Properties");

        if (draggedItem != null && draggedItem.itemDef != null)
        {
            EditorGUILayout.LabelField("Selected Item", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Name: {draggedItem.itemDef.displayName}");
            EditorGUILayout.LabelField($"Size: {draggedItem.GetSize().x}x{draggedItem.GetSize().y}");
            EditorGUILayout.LabelField($"Weight: {draggedItem.itemDef.weight:F1} kg");
            EditorGUILayout.LabelField($"Value: {draggedItem.itemDef.baseValue} gold");

            if (draggedItem.itemDef.allowRotation)
            {
                EditorGUILayout.LabelField($"Rotated: {draggedItem.isRotated}");
            }
        }

        EditorGUILayout.Space(10);

        // Statistics
        EditorGUILayout.LabelField("Grid Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Items: {placedItems.Count}");
        EditorGUILayout.LabelField($"Grid Usage: {GetGridUsage():P0}");
        EditorGUILayout.LabelField($"Total Value: {GetTotalValue()} gold");
        EditorGUILayout.LabelField($"Efficiency: {GetPackingEfficiency():P0}");

        EditorGUILayout.Space(10);

        // Quick actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Sort by Size"))
        {
            SortItemsBySize();
        }

        if (GUILayout.Button("Sort by Weight"))
        {
            SortItemsByWeight();
        }

        if (GUILayout.Button("Optimize Layout"))
        {
            OptimizeLayout();
        }

        EditorGUILayout.EndVertical();
    }

    // ========== CRAFTING STUDIO ==========

    private void DrawCraftingStudio()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        DrawSectionHeading("⚒️", "Crafting Studio");

        EditorGUILayout.Space(10);

        // Workstation selector
        DrawWorkstationSelector();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        // Recipe list
        DrawRecipeList();

        // Crafting interface
        DrawCraftingInterface();

        // Material inventory
        DrawMaterialInventory();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawWorkstationSelector()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUILayout.LabelField("Workstation:", GUILayout.Width(80));

        foreach (WorkstationType station in System.Enum.GetValues(typeof(WorkstationType)))
        {
            bool isSelected = (currentWorkstation == station);

            if (isSelected)
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);

            if (GUILayout.Button(station.ToString(), EditorStyles.toolbarButton))
            {
                currentWorkstation = station;
                LogActivity($"Selected {station} workstation");
            }

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRecipeList()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(250));

        EditorGUILayout.LabelField("📜 Available Recipes", EditorStyles.boldLabel);

        // Search
        string recipeSearch = EditorGUILayout.TextField("Search", "", EditorStyles.toolbarSearchField);

        EditorGUILayout.Space(5);

        recipeListScroll = EditorGUILayout.BeginScrollView(recipeListScroll, GUILayout.Height(400));

        if (recipeDatabase != null)
        {
            var recipes = recipeDatabase.GetAllRecipes()
                .Where(r => r.requiredWorkstation == currentWorkstation);

            foreach (var recipe in recipes)
            {
                if (!string.IsNullOrEmpty(recipeSearch) &&
                    !recipe.recipeName.ToLower().Contains(recipeSearch.ToLower()))
                    continue;

                DrawRecipeCard(recipe);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawRecipeCard(RecipeDefinition recipe)
    {
        bool isSelected = (selectedRecipe == recipe);

        if (isSelected)
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (GUILayout.Button(recipe.recipeName, EditorStyles.label))
        {
            selectedRecipe = recipe;
            LogActivity($"Selected recipe: {recipe.recipeName}");
        }

        // Show ingredients preview
        EditorGUILayout.LabelField($"Tier {recipe.tier} | {recipe.baseCraftTime:F1}s", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = Color.white;
    }

    private void DrawCraftingInterface()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField("Crafting", EditorStyles.boldLabel);

        if (selectedRecipe != null)
        {
            // Recipe name
            EditorGUILayout.LabelField(selectedRecipe.recipeName, headerStyle);

            EditorGUILayout.Space(10);

            // Ingredients
            EditorGUILayout.LabelField("Required Materials:", EditorStyles.boldLabel);

            bool canCraft = true;
            foreach (var ingredient in selectedRecipe.ingredients)
            {
                int available = availableMaterials.GetValueOrDefault(ingredient.name, 0);
                bool hasEnough = available >= ingredient.quantity;

                if (!hasEnough) canCraft = false;

                GUI.color = hasEnough ? Color.green : Color.red;
                EditorGUILayout.LabelField($"  • {ingredient.name}: {available}/{ingredient.quantity}",
                    hasEnough ? successStyle : errorStyle);
            }
            GUI.color = Color.white;

            EditorGUILayout.Space(10);

            // Outputs
            EditorGUILayout.LabelField("Produces:", EditorStyles.boldLabel);
            foreach (var output in selectedRecipe.outputs)
            {
                EditorGUILayout.LabelField($"  • {output.item?.displayName ?? "Unknown"} x{output.quantityMin}-{output.quantityMax}");
            }

            EditorGUILayout.Space(20);

            // Craft button
            GUI.enabled = canCraft && !isCrafting;

            if (GUILayout.Button(isCrafting ? $"Crafting... {craftingProgress:P0}" : "Craft",
                GUILayout.Height(40)))
            {
                StartCrafting(selectedRecipe);
            }

            GUI.enabled = true;

            // Progress bar
            if (isCrafting)
            {
                Rect progressRect = GUILayoutUtility.GetRect(300, 20);
                EditorGUI.DrawRect(progressRect, Color.black);
                EditorGUI.DrawRect(new Rect(progressRect.x, progressRect.y,
                    progressRect.width * craftingProgress, progressRect.height),
                    Color.green);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select a recipe to begin crafting", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialInventory()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(200));

        EditorGUILayout.LabelField("📦 Materials", EditorStyles.boldLabel);

        // Simulate material inventory
        if (availableMaterials.Count == 0)
        {
            // Generate sample materials
            availableMaterials["Wood"] = 50;
            availableMaterials["Stone"] = 30;
            availableMaterials["Iron"] = 10;
            availableMaterials["Fiber"] = 25;
        }

        foreach (var material in availableMaterials)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(material.Key);
            int newAmount = EditorGUILayout.IntField(material.Value, GUILayout.Width(50));
            if (newAmount != material.Value)
            {
                availableMaterials[material.Key] = newAmount;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Random Materials"))
        {
            AddRandomMaterials();
        }

        EditorGUILayout.EndVertical();
    }

    // ========== AI BATCH PROCESSOR ==========

    private void DrawAIBatchProcessor()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        //EditorGUILayout.LabelField("🤖 AI Batch Processor", headerStyle);
        DrawSectionHeading("🤖", "AI Batch Processor");

        EditorGUILayout.Space(10);

        // Mode selector
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Generate Items", EditorStyles.toolbarButton))
        {
            aiPrompt = GetItemGenerationPrompt();
        }

        if (GUILayout.Button("Generate Recipes", EditorStyles.toolbarButton))
        {
            aiPrompt = GetRecipeGenerationPrompt();
        }

        if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton))
        {
            ImportFromClipboard();
        }

        if (GUILayout.Button("Export Template", EditorStyles.toolbarButton))
        {
            ExportAITemplate();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // AI interaction area
        EditorGUILayout.BeginHorizontal();

        // Prompt area
        DrawAIPromptArea();

        // Processing area
        DrawAIProcessingArea();

        EditorGUILayout.EndHorizontal();

        // Validation area
        DrawAIValidationArea();

        EditorGUILayout.EndVertical();
    }

    private void DrawAIPromptArea()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(400));

        EditorGUILayout.LabelField("AI Prompt", EditorStyles.boldLabel);

        aiPrompt = EditorGUILayout.TextArea(aiPrompt, GUILayout.Height(200));

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Copy Prompt to Clipboard", GUILayout.Height(30)))
        {
            GUIUtility.systemCopyBuffer = aiPrompt;
            ShowStatus("Prompt copied! Send to AI agent", Color.green);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("AI Response", EditorStyles.boldLabel);

        aiResponse = EditorGUILayout.TextArea(aiResponse, GUILayout.Height(200));

        if (GUILayout.Button("Process AI Response", GUILayout.Height(35)))
        {
            ProcessAIResponse();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAIProcessingArea()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);

        if (isProcessingAI)
        {
            // Processing animation
            Rect rect = GUILayoutUtility.GetRect(300, 300);

            float rotation = Time.realtimeSinceStartup * 100f;
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rotation, rect.center);
            GUI.DrawTexture(rect, itemGlowTexture);
            GUI.matrix = matrix;

            EditorGUILayout.LabelField($"Processing... {aiProcessingProgress:P0}",
                EditorStyles.centeredGreyMiniLabel);
        }
        else if (pendingAIItems.Count > 0 || pendingAIRecipes.Count > 0)
        {
            EditorGUILayout.LabelField($"Ready to import:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  • {pendingAIItems.Count} items");
            EditorGUILayout.LabelField($"  • {pendingAIRecipes.Count} recipes");

            EditorGUILayout.Space(20);

            if (GUILayout.Button($"Import All ({pendingAIItems.Count + pendingAIRecipes.Count} items)",
                GUILayout.Height(40)))
            {
                ExecuteAIImport();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "1. Generate or paste a prompt\n" +
                "2. Send to your AI agent\n" +
                "3. Paste the JSON response\n" +
                "4. Click Process to validate and import",
                MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAIValidationArea()
    {
        if (lastValidation != null)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.LabelField("Validation Report", EditorStyles.boldLabel);

            // Show validation results
            GUI.color = lastValidation.hasErrors ? Color.red : Color.green;
            EditorGUILayout.LabelField(
                lastValidation.hasErrors ? "❌ Validation Failed" : "✅ Validation Passed",
                EditorStyles.boldLabel);
            GUI.color = Color.white;

            foreach (var message in lastValidation.messages)
            {
                EditorGUILayout.LabelField($"  • {message}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }

    // ========== HELPER METHODS ==========

    private void HandleGridInput(Rect gridRect, float cellSize)
    {
        Event e = Event.current;

        if (!gridRect.Contains(e.mousePosition))
            return;

        if (e.type == EventType.MouseDown)
        {
            Vector2Int gridPos = ScreenToGridPosition(e.mousePosition, gridRect, cellSize);
            PlacedItem itemAtPos = GetItemAt(gridPos);

            if (e.button == 0) // Left click - drag
            {
                if (itemAtPos != null)
                {
                    StartDragging(itemAtPos, gridPos);
                    e.Use();
                }
            }
            else if (e.button == 1) // Right click - remove
            {
                if (itemAtPos != null)
                {
                    RemoveItemFromGrid(itemAtPos);
                    ShowStatus($"Removed {itemAtPos.itemDef.displayName}", Color.yellow);
                    e.Use();
                    Repaint();
                }
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2Int gridPos = ScreenToGridPosition(e.mousePosition, gridRect, cellSize);
            PlacedItem itemAtPos = GetItemAt(gridPos);

            if (itemAtPos != null)
            {
                StartDragging(itemAtPos, gridPos);
                e.Use();
            }
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
        {
            PlaceDraggedItem(gridRect, cellSize);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && isDragging)
        {
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R && isDragging)
        {
            RotateDraggedItem();
            e.Use();
        }
    }

    private void HandleAdvancedInput()
    {
        Event e = Event.current;

        if (e.type == EventType.KeyDown)
        {
            // Global shortcuts
            if ((e.control || e.command) && e.keyCode == KeyCode.S)
            {
                QuickSave();
                e.Use();
            }
            else if ((e.control || e.command) && e.keyCode == KeyCode.R)
            {
                RefreshAll();
                e.Use();
            }
            else if ((e.control || e.command) && e.keyCode == KeyCode.I)
            {
                currentTab = TabMode.InventorySimulator;
                e.Use();
            }
            else if ((e.control || e.command) && e.keyCode == KeyCode.B)
            {
                currentTab = TabMode.BatchProcessor;
                e.Use();
            }

            // Tab navigation
            if (e.keyCode >= KeyCode.F1 && e.keyCode <= KeyCode.F9)
            {
                int index = e.keyCode - KeyCode.F1;
                if (index < System.Enum.GetValues(typeof(TabMode)).Length)
                {
                    currentTab = (TabMode)index;
                    tabTransition = 1f;
                    e.Use();
                }
            }
        }
    }

    private void DrawOverlayEffects()
    {
        // Draw floating particles
        if (animationsEnabled)
        {
            foreach (var particle in particles.Where(p => p != null && p.active))
            {
                float alpha = particle.life;
                GUI.color = new Color(1, 1, 1, alpha * 0.3f);

                Matrix4x4 matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(particle.rotation, particle.position);
                GUI.DrawTexture(new Rect(particle.position.x - 16, particle.position.y - 16, 32, 32), itemGlowTexture);
                GUI.matrix = matrix;
            }
            GUI.color = Color.white;
        }

        // Status message overlay
        if (statusTimer > 0)
        {
            Rect statusRect = new Rect(position.width / 2 - 200, 100, 400, 40);
            GUI.color = new Color(0, 0, 0, statusTimer * 0.8f);
            GUI.DrawTexture(statusRect, Texture2D.whiteTexture);

            GUI.color = new Color(statusColor.r, statusColor.g, statusColor.b, statusTimer);
            GUI.Label(statusRect, statusMessage, headerStyle);
            GUI.color = Color.white;
        }
    }

    private void DrawProfessionalStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // System status
        if (databasesLoaded)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("● ONLINE", successStyle, GUILayout.Width(70));
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("● OFFLINE", errorStyle, GUILayout.Width(70));
        }
        GUI.color = Color.white;

        // Current operation
        EditorGUILayout.LabelField($"Mode: {currentTab}", GUILayout.Width(150));

        GUILayout.FlexibleSpace();

        // Performance metrics
        EditorGUILayout.LabelField($"FPS: {performanceMetrics.GetValueOrDefault("fps", 60):F0}", GUILayout.Width(60));
        EditorGUILayout.LabelField($"Memory: {performanceMetrics.GetValueOrDefault("memory", 0):F0}MB", GUILayout.Width(100));

        // Auto-save status
        if (autoSaveEnabled)
        {
            float timeUntilSave = autoSaveInterval - (Time.realtimeSinceStartup - lastAutoSave);
            EditorGUILayout.LabelField($"Auto-save: {timeUntilSave:F0}s", GUILayout.Width(100));
        }

        // Clock
        EditorGUILayout.LabelField(System.DateTime.Now.ToString("HH:mm:ss"), GUILayout.Width(70));

        EditorGUILayout.EndHorizontal();
    }

    private void OnEditorUpdate()
    {
        // Update animations
        if (animationsEnabled)
        {
            glowAnimation = Mathf.Max(0, glowAnimation - Time.deltaTime * 2f);
            pulseAnimation += Time.deltaTime * 2f;
            tabTransition = Mathf.Max(0, tabTransition - Time.deltaTime * 3f);
            statusTimer = Mathf.Max(0, statusTimer - Time.deltaTime);

            // Update item animations
            var itemsToUpdate = itemAnimations.Keys.ToList();
            foreach (var item in itemsToUpdate)
            {
                itemAnimations[item] = Mathf.Max(0, itemAnimations[item] - Time.deltaTime * 2f);
            }

            // Update particles
            foreach (var particle in particles.Where(p => p != null))
            {
                particle.Update(Time.deltaTime);
            }
        }

        // Performance monitoring
        performanceMetrics["fps"] = 1f / Time.deltaTime;
        performanceMetrics["memory"] = System.GC.GetTotalMemory(false) / 1048576f; // MB

        // Auto-save
        if (autoSaveEnabled && Time.realtimeSinceStartup - lastAutoSave > autoSaveInterval)
        {
            AutoSave();
        }

        // Periodic refresh
        if (Time.realtimeSinceStartup - lastRefreshTime > 5f)
        {
            UpdateStatistics();
            lastRefreshTime = Time.realtimeSinceStartup;
        }
    }

    // ========== DASHBOARD ==========

    private void DrawDashboard()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        //EditorGUILayout.LabelField("📊 Command Center", headerStyle);
        DrawSectionHeading("📊", "Command Center");

        EditorGUILayout.Space(20);

        // Quick stats cards
        EditorGUILayout.BeginHorizontal();

        DrawStatCard("Total Items", totalItems.ToString(), Color.cyan);
        DrawStatCard("Total Recipes", totalRecipes.ToString(), Color.magenta);
        DrawStatCard("Grid Efficiency", $"{GetPackingEfficiency():P0}", Color.green);
        DrawStatCard("System Health", $"{GetSystemHealth():P0}", Color.yellow);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // Quick actions grid
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (DrawActionCard("🎲", "Generate 100 Items", Color.green))
        {
            GenerateBulkItems(100);
        }

        if (DrawActionCard("🔀", "Randomize Inventory", Color.blue))
        {
            GenerateRandomLoadout();
        }

        if (DrawActionCard("📤", "Export Database", Color.yellow))
        {
            ExportDatabase();
        }

        if (DrawActionCard("🧪", "Run Tests", Color.red))
        {
            RunSystemTests();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawStatCard(string label, string value, Color color)
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Height(80));

        GUI.color = color;
        EditorGUILayout.LabelField(value, headerStyle);
        GUI.color = Color.white;

        EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();
    }

    private bool DrawActionCard(string icon, string label, Color color)
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Height(100));

        GUI.backgroundColor = color * 0.3f;
        bool clicked = GUILayout.Button(icon, headerStyle, GUILayout.Height(50));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();

        return clicked;
    }

    // ========== RECIPE BUILDER ==========

    private class RecipeBuilderState
    {
        public RecipeDefinition currentRecipe;
        public bool isCreatingNew = true;
        public string recipeID = "";
        public string recipeName = "";
        public string description = "";
        public CraftingCategory category = CraftingCategory.Tools;
        public WorkstationType workstation = WorkstationType.None;
        public float craftTime = 5f;
        public int tier = 1;
        public bool isKnownByDefault = true;
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public List<RecipeOutput> outputs = new List<RecipeOutput>();

        public void Reset()
        {
            isCreatingNew = true;
            currentRecipe = null;
            recipeID = "recipe_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            recipeName = "New Recipe";
            description = "";
            category = CraftingCategory.Tools;
            workstation = WorkstationType.None;
            craftTime = 5f;
            tier = 1;
            isKnownByDefault = true;
            ingredients.Clear();
            outputs.Clear();
        }
    }

    private RecipeBuilderState recipeBuilderState = new RecipeBuilderState();
    //private Vector2 recipeListScroll;
    private string recipeSearchFilter = "";

    private void DrawRecipeBuilder()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        //EditorGUILayout.LabelField("🔨 Recipe Builder", headerStyle);
        DrawSectionHeading("🔨", "Recipe Builder");

        EditorGUILayout.BeginHorizontal();

        // Left Panel - Recipe List
        DrawRecipeBuilderList();

        // Right Panel - Recipe Editor
        DrawRecipeBuilderEditor();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawRecipeBuilderList()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));

        EditorGUILayout.LabelField("Recipes", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Create New Recipe", GUILayout.Height(30)))
        {
            recipeBuilderState.Reset();
        }

        EditorGUILayout.Space(5);

        // Search field
        recipeSearchFilter = EditorGUILayout.TextField("Search:", recipeSearchFilter);

        recipeListScroll = EditorGUILayout.BeginScrollView(recipeListScroll, GUILayout.Height(400));

        if (recipeDatabase != null)
        {
            var recipes = recipeDatabase.GetAllRecipes();
            foreach (var recipe in recipes)
            {
                if (!string.IsNullOrEmpty(recipeSearchFilter) &&
                    !recipe.recipeName.ToLower().Contains(recipeSearchFilter.ToLower()))
                    continue;

                if (GUILayout.Button(recipe.recipeName, EditorStyles.toolbarButton))
                {
                    LoadRecipeForEditing(recipe);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRecipeBuilderEditor()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField(
            recipeBuilderState.isCreatingNew ? "Create New Recipe" : $"Edit: {recipeBuilderState.recipeName}",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Basic Properties
        EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);
        recipeBuilderState.recipeID = EditorGUILayout.TextField("Recipe ID", recipeBuilderState.recipeID);
        recipeBuilderState.recipeName = EditorGUILayout.TextField("Recipe Name", recipeBuilderState.recipeName);

        EditorGUILayout.LabelField("Description");
        recipeBuilderState.description = EditorGUILayout.TextArea(recipeBuilderState.description, GUILayout.Height(60));

        EditorGUILayout.Space(10);

        // Crafting Properties
        EditorGUILayout.LabelField("Crafting Properties", EditorStyles.boldLabel);
        recipeBuilderState.category = (CraftingCategory)EditorGUILayout.EnumPopup("Category", recipeBuilderState.category);
        recipeBuilderState.workstation = (WorkstationType)EditorGUILayout.EnumPopup("Required Workstation", recipeBuilderState.workstation);
        recipeBuilderState.craftTime = EditorGUILayout.FloatField("Craft Time (seconds)", recipeBuilderState.craftTime);
        recipeBuilderState.tier = EditorGUILayout.IntField("Tier", recipeBuilderState.tier);
        recipeBuilderState.isKnownByDefault = EditorGUILayout.Toggle("Known by Default", recipeBuilderState.isKnownByDefault);

        EditorGUILayout.Space(10);

        // Ingredients
        DrawIngredientsList();

        EditorGUILayout.Space(10);

        // Outputs
        DrawOutputsList();

        EditorGUILayout.Space(20);

        // Action Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(recipeBuilderState.isCreatingNew ? "Create Recipe" : "Save Changes", GUILayout.Height(35)))
        {
            SaveRecipe();
        }
        GUI.backgroundColor = Color.white;

        if (!recipeBuilderState.isCreatingNew)
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Cancel", GUILayout.Height(35)))
            {
                LoadRecipeForEditing(recipeBuilderState.currentRecipe);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Delete Recipe",
                    $"Are you sure you want to delete {recipeBuilderState.recipeName}?",
                    "Delete", "Cancel"))
                {
                    DeleteCurrentRecipe();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawIngredientsList()
    {
        EditorGUILayout.LabelField("Ingredients", EditorStyles.boldLabel);

        for (int i = 0; i < recipeBuilderState.ingredients.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            var ingredient = recipeBuilderState.ingredients[i];

            // Item selection
            ingredient.specificItem = (ItemDefinition)EditorGUILayout.ObjectField(
                ingredient.specificItem, typeof(ItemDefinition), false, GUILayout.Width(150));

            // Or name-based
            if (ingredient.specificItem == null)
            {
                ingredient.name = EditorGUILayout.TextField(ingredient.name, GUILayout.Width(100));
            }

            // Quantity
            ingredient.quantity = EditorGUILayout.IntField(ingredient.quantity, GUILayout.Width(50));

            // Consumed
            ingredient.consumed = EditorGUILayout.Toggle(ingredient.consumed, GUILayout.Width(30));

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                recipeBuilderState.ingredients.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Ingredient"))
        {
            recipeBuilderState.ingredients.Add(new RecipeIngredient { quantity = 1, consumed = true });
        }
    }

    private void DrawOutputsList()
    {
        EditorGUILayout.LabelField("Outputs", EditorStyles.boldLabel);

        for (int i = 0; i < recipeBuilderState.outputs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            var output = recipeBuilderState.outputs[i];

            // Item selection
            output.item = (ItemDefinition)EditorGUILayout.ObjectField(
                output.item, typeof(ItemDefinition), false, GUILayout.Width(150));

            // Quantity range
            EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
            output.quantityMin = EditorGUILayout.IntField(output.quantityMin, GUILayout.Width(40));

            EditorGUILayout.LabelField("Max:", GUILayout.Width(30));
            output.quantityMax = EditorGUILayout.IntField(output.quantityMax, GUILayout.Width(40));

            // Chance
            EditorGUILayout.LabelField("Chance:", GUILayout.Width(50));
            output.chance = EditorGUILayout.Slider(output.chance, 0f, 1f, GUILayout.Width(60));

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                recipeBuilderState.outputs.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Output"))
        {
            recipeBuilderState.outputs.Add(new RecipeOutput { quantityMin = 1, quantityMax = 1, chance = 1f });
        }
    }

    private void LoadRecipeForEditing(RecipeDefinition recipe)
    {
        if (recipe == null) return;

        recipeBuilderState.isCreatingNew = false;
        recipeBuilderState.currentRecipe = recipe;
        recipeBuilderState.recipeID = recipe.recipeID;
        recipeBuilderState.recipeName = recipe.recipeName;
        recipeBuilderState.description = recipe.description;

        // Parse the category string to enum
        recipeBuilderState.category = ParseCraftingCategoryFromString(recipe.category);

        recipeBuilderState.workstation = recipe.requiredWorkstation;
        recipeBuilderState.craftTime = recipe.baseCraftTime;
        recipeBuilderState.tier = recipe.tier;
        recipeBuilderState.isKnownByDefault = recipe.isKnownByDefault;

        recipeBuilderState.ingredients = recipe.ingredients?.ToList() ?? new List<RecipeIngredient>();
        recipeBuilderState.outputs = recipe.outputs?.ToList() ?? new List<RecipeOutput>();
    }

    private void SaveRecipe()
    {
        if (string.IsNullOrEmpty(recipeBuilderState.recipeID) || string.IsNullOrEmpty(recipeBuilderState.recipeName))
        {
            EditorUtility.DisplayDialog("Error", "Recipe ID and Name are required", "OK");
            return;
        }

        // Check for duplicate ID when creating new
        if (recipeBuilderState.isCreatingNew && recipeDatabase != null)
        {
            if (recipeDatabase.HasRecipeWithID(recipeBuilderState.recipeID))
            {
                EditorUtility.DisplayDialog("Duplicate ID",
                    $"A recipe with ID '{recipeBuilderState.recipeID}' already exists!", "OK");
                return;
            }
        }

        RecipeDefinition recipe;

        if (recipeBuilderState.isCreatingNew)
        {
            recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            string path = $"Assets/_WildSurvival/Data/Recipes/{recipeBuilderState.recipeID}.asset";

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(recipe, path);
        }
        else
        {
            recipe = recipeBuilderState.currentRecipe;
        }

        // Update properties
        recipe.recipeID = recipeBuilderState.recipeID;
        recipe.recipeName = recipeBuilderState.recipeName;
        recipe.description = recipeBuilderState.description;
        recipe.category = recipeBuilderState.category.ToString();
        recipe.requiredWorkstation = recipeBuilderState.workstation;
        recipe.baseCraftTime = recipeBuilderState.craftTime;
        recipe.tier = recipeBuilderState.tier;
        recipe.isKnownByDefault = recipeBuilderState.isKnownByDefault;
        recipe.ingredients = recipeBuilderState.ingredients.ToArray();
        recipe.outputs = recipeBuilderState.outputs.ToArray();

        EditorUtility.SetDirty(recipe);

        if (recipeBuilderState.isCreatingNew)
        {
            if (recipeDatabase != null)
            {
                recipeDatabase.AddRecipe(recipe);
                EditorUtility.SetDirty(recipeDatabase);
            }
            recipeBuilderState.currentRecipe = recipe;
            recipeBuilderState.isCreatingNew = false;
        }

        AssetDatabase.SaveAssets();
        UpdateStatistics();

        ShowStatus($"✅ Recipe '{recipeBuilderState.recipeName}' saved!", Color.green);
        LogActivity($"Saved recipe: {recipeBuilderState.recipeName}");
    }

    private void DeleteCurrentRecipe()
    {
        if (recipeBuilderState.currentRecipe != null)
        {
            string name = recipeBuilderState.currentRecipe.recipeName;

            if (recipeDatabase != null)
            {
                recipeDatabase.RemoveRecipe(recipeBuilderState.currentRecipe);
                EditorUtility.SetDirty(recipeDatabase);
            }

            string path = AssetDatabase.GetAssetPath(recipeBuilderState.currentRecipe);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            recipeBuilderState.Reset();
            UpdateStatistics();

            ShowStatus($"Deleted recipe: {name}", Color.yellow);
            LogActivity($"Deleted recipe: {name}");
        }
    }


    private void DrawDatabaseManager()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        //EditorGUILayout.LabelField("💾 Database Manager", headerStyle);
        DrawSectionHeading("💾", "Database Manager");

        EditorGUILayout.BeginHorizontal();

        // Left panel - Database info
        DrawDatabaseInfo();

        // Right panel - Operations
        DrawDatabaseOperations();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void ConvertOrClearRecipes()
    {
        if (recipeDatabase == null) return;

        // Get the current recipes array via SerializedObject to see the actual type
        SerializedObject serializedDb = new SerializedObject(recipeDatabase);
        SerializedProperty recipesProp = serializedDb.FindProperty("recipes");

        if (recipesProp != null && recipesProp.arraySize > 0)
        {
            // Check if first element is valid
            var firstElement = recipesProp.GetArrayElementAtIndex(0);
            if (firstElement.objectReferenceValue == null ||
                !(firstElement.objectReferenceValue is RecipeDefinition))
            {
                // We have invalid recipes, need to clear them
                if (EditorUtility.DisplayDialog("Fix Recipe Database",
                    "The recipe database contains invalid recipes (type mismatch). " +
                    "Would you like to clear them and start fresh?",
                    "Yes, Clear", "Cancel"))
                {
                    recipeDatabase.ClearAllRecipes();
                    AssetDatabase.SaveAssets();
                    UpdateStatistics();
                    ShowStatus("Recipe database cleared!", Color.yellow);
                }
            }
        }
    }

    private void DrawDatabaseInfo()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(350));

        EditorGUILayout.LabelField("Database Status", EditorStyles.boldLabel);

        // Item Database
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Database:", GUILayout.Width(100));
        if (itemDatabase != null)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✅ {itemDatabase.GetAllItems().Count} items");
            GUI.color = Color.white;

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = itemDatabase;
            }
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("❌ Not loaded");
            GUI.color = Color.white;

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateOrLoadItemDatabase();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Recipe Database
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Recipe Database:", GUILayout.Width(100));
        if (recipeDatabase != null)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✅ {recipeDatabase.GetAllRecipes().Count} recipes");
            GUI.color = Color.white;

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = recipeDatabase;
            }
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("❌ Not loaded");
            GUI.color = Color.white;

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateOrLoadRecipeDatabase();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        if (itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems();
            var categories = items.GroupBy(i => i.primaryCategory);

            foreach (var cat in categories)
            {
                EditorGUILayout.LabelField($"{cat.Key}: {cat.Count()} items");
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDatabaseOperations()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

        // Add this new section for Recipe Fixes
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Recipe Database Fixes", EditorStyles.boldLabel);

        if (GUILayout.Button("Fix Recipe Type Mismatch", GUILayout.Height(30)))
        {
            ConvertOrClearRecipes();
        }

        if (GUILayout.Button("Clear All Recipes", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear All Recipes",
                "Are you sure you want to remove all recipes?", "Yes", "Cancel"))
            {
                if (recipeDatabase != null)
                {
                    recipeDatabase.ClearAllRecipes();
                    AssetDatabase.SaveAssets();
                    UpdateStatistics();
                    ShowStatus("All recipes cleared!", Color.yellow);
                }
            }
        }

        // Scan and Import
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Import Operations", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan for Orphaned Items", GUILayout.Height(30)))
        {
            ScanForOrphanedAssets();
        }

        if (GUILayout.Button("Import from ItemData Assets", GUILayout.Height(30)))
        {
            ImportFromItemDataAssets();
        }

        if (GUILayout.Button("Import from JSON", GUILayout.Height(30)))
        {
            ImportDatabaseFromJSON();
        }

        EditorGUILayout.Space(10);

        // Export
        EditorGUILayout.LabelField("Export Operations", EditorStyles.boldLabel);

        if (GUILayout.Button("Export to JSON", GUILayout.Height(30)))
        {
            ExportDatabaseToJSON();
        }

        if (GUILayout.Button("Export to CSV", GUILayout.Height(30)))
        {
            ExportDatabaseToCSV();
        }

        EditorGUILayout.Space(10);

        // Validation
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate All Items", GUILayout.Height(30)))
        {
            ValidateAllItems();
        }

        if (GUILayout.Button("Validate All Recipes", GUILayout.Height(30)))
        {
            ValidateAllRecipes();
        }

        if (GUILayout.Button("Check Recipe Dependencies", GUILayout.Height(30)))
        {
            CheckRecipeDependencies();
        }

        EditorGUILayout.Space(10);

        // Maintenance
        EditorGUILayout.LabelField("Maintenance", EditorStyles.boldLabel);

        if (GUILayout.Button("Clean Duplicate Entries", GUILayout.Height(30)))
        {
            CleanDuplicateEntries();
        }

        if (GUILayout.Button("Rebuild Databases", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Rebuild Databases",
                "This will scan all assets and rebuild databases. Continue?",
                "Rebuild", "Cancel"))
            {
                RebuildDatabases();
            }
        }

        EditorGUILayout.EndVertical();
    }


    // ========== ANALYTICS ==========

    private void DrawAnalytics()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        //EditorGUILayout.LabelField("📈 Analytics Dashboard", headerStyle);
        DrawSectionHeading("📈", "Analytics Dashboard");

        if (itemDatabase == null || recipeDatabase == null)
        {
            EditorGUILayout.HelpBox("Databases not loaded. Please load or create them first.", MessageType.Warning);
            return;
        }

        // Content Balance
        DrawContentBalance();

        EditorGUILayout.Space(20);

        // Recipe Complexity
        DrawRecipeComplexity();

        EditorGUILayout.Space(20);

        // Item Usage
        DrawItemUsage();

        EditorGUILayout.EndVertical();
    }

    private void DrawContentBalance()
    {
        EditorGUILayout.LabelField("Content Balance", EditorStyles.boldLabel);

        var items = itemDatabase.GetAllItems();
        var categories = System.Enum.GetValues(typeof(ItemCategory)).Cast<ItemCategory>();

        foreach (var category in categories)
        {
            var count = items.Count(i => i.primaryCategory == category);
            var percentage = items.Count > 0 ? (float)count / items.Count : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(category.ToString(), GUILayout.Width(100));

            // Progress bar
            Rect rect = GUILayoutUtility.GetRect(200, 20);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * percentage, rect.height),
                GetCategoryColor(category));

            EditorGUILayout.LabelField($"{count} ({percentage:P0})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawRecipeComplexity()
    {
        EditorGUILayout.LabelField("Recipe Complexity", EditorStyles.boldLabel);

        var recipes = recipeDatabase.GetAllRecipes();

        if (recipes.Count > 0)
        {
            var avgIngredients = recipes.Average(r => r.ingredients?.Length ?? 0);
            var avgOutputs = recipes.Average(r => r.outputs?.Length ?? 0);
            var avgCraftTime = recipes.Average(r => r.baseCraftTime);

            EditorGUILayout.LabelField($"Average Ingredients: {avgIngredients:F1}");
            EditorGUILayout.LabelField($"Average Outputs: {avgOutputs:F1}");
            EditorGUILayout.LabelField($"Average Craft Time: {avgCraftTime:F1}s");

            // Tier distribution
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Tier Distribution:");

            var tiers = recipes.GroupBy(r => r.tier).OrderBy(g => g.Key);
            foreach (var tier in tiers)
            {
                EditorGUILayout.LabelField($"  Tier {tier.Key}: {tier.Count()} recipes");
            }
        }
    }

    private void DrawItemUsage()
    {
        EditorGUILayout.LabelField("Item Usage in Recipes", EditorStyles.boldLabel);

        var items = itemDatabase.GetAllItems();
        var recipes = recipeDatabase.GetAllRecipes();

        Dictionary<string, int> itemUsage = new Dictionary<string, int>();

        foreach (var recipe in recipes)
        {
            if (recipe.ingredients != null)
            {
                foreach (var ingredient in recipe.ingredients)
                {
                    string key = ingredient.specificItem != null ?
                        ingredient.specificItem.itemID : ingredient.name;

                    if (!string.IsNullOrEmpty(key))
                    {
                        if (!itemUsage.ContainsKey(key))
                            itemUsage[key] = 0;
                        itemUsage[key]++;
                    }
                }
            }
        }

        // Show top 10 most used items
        var topItems = itemUsage.OrderByDescending(kvp => kvp.Value).Take(10);

        EditorGUILayout.LabelField("Most Used Items:");
        foreach (var kvp in topItems)
        {
            EditorGUILayout.LabelField($"  {kvp.Key}: Used in {kvp.Value} recipes");
        }

        // Show unused items
        var unusedItems = items.Where(i => !itemUsage.ContainsKey(i.itemID)).ToList();
        if (unusedItems.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Unused Items: {unusedItems.Count}");

            if (unusedItems.Count <= 5)
            {
                foreach (var item in unusedItems)
                {
                    EditorGUILayout.LabelField($"  - {item.displayName}");
                }
            }
        }
    }


    // ========== SETTINGS ==========

    private void DrawSettings()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        //EditorGUILayout.LabelField("⚙️ Settings", headerStyle);
        DrawSectionHeading("⚙️", "Settings");

        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("Preferences", EditorStyles.boldLabel);

        autoSaveEnabled = EditorGUILayout.Toggle("Auto-Save", autoSaveEnabled);
        if (autoSaveEnabled)
        {
            autoSaveInterval = EditorGUILayout.Slider("Save Interval (seconds)", autoSaveInterval, 60f, 600f);
        }

        EditorGUILayout.Space(10);

        showTooltips = EditorGUILayout.Toggle("Show Tooltips", showTooltips);
        darkMode = EditorGUILayout.Toggle("Dark Mode", darkMode);
        animationsEnabled = EditorGUILayout.Toggle("Enable Animations", animationsEnabled);
        hapticFeedback = EditorGUILayout.Toggle("Haptic Feedback", hapticFeedback);
        showGridNumbers = EditorGUILayout.Toggle("Show Grid Numbers", showGridNumbers);
        autoArrangeItems = EditorGUILayout.Toggle("Auto-Arrange Items", autoArrangeItems);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Save Preferences", GUILayout.Height(35)))
        {
            SavePreferences();
            ShowStatus("✅ Preferences saved!", Color.green);
        }

        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(25)))
        {
            ResetPreferences();
        }

        EditorGUILayout.EndVertical();
    }

    // ========== UTILITY METHODS ==========

    private Color GetCategoryColor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Resource: return Color.green;
            case ItemCategory.Tool: return Color.blue;
            case ItemCategory.Weapon: return Color.red;
            case ItemCategory.Food: return Color.yellow;
            case ItemCategory.Medicine: return Color.magenta;
            case ItemCategory.Clothing: return Color.cyan;
            case ItemCategory.Building: return new Color(0.5f, 0.3f, 0.1f);
            default: return Color.gray;
        }
    }

    private void OptimizePerformance()
    {
        // Clear caches
        itemLookup.Clear();
        recipeLookup.Clear();

        // Rebuild lookups
        if (itemDatabase != null)
        {
            foreach (var item in itemDatabase.GetAllItems())
            {
                itemLookup[item.itemID] = item;
            }
        }

        if (recipeDatabase != null)
        {
            foreach (var recipe in recipeDatabase.GetAllRecipes())
            {
                recipeLookup[recipe.recipeID] = recipe;
            }
        }

        // Force garbage collection
        System.GC.Collect();

        ShowStatus("Performance optimized", Color.green);
    }

    private void UpdateStatistics()
    {
        totalItems = itemDatabase?.GetAllItems().Count ?? 0;
        totalRecipes = recipeDatabase?.GetAllRecipes().Count ?? 0;

        // Update analytics
        analyticsHistory.Add(new AnalyticsDataPoint
        {
            timestamp = System.DateTime.Now,
            itemCount = totalItems,
            recipeCount = totalRecipes,
            gridEfficiency = GetPackingEfficiency()
        });

        // Keep only last 100 data points
        if (analyticsHistory.Count > 100)
        {
            analyticsHistory.RemoveAt(0);
        }
    }

    private void LogActivity(string message)
    {
        recentActivity.Insert(0, $"[{System.DateTime.Now:HH:mm:ss}] {message}");
        if (recentActivity.Count > 20)
        {
            recentActivity.RemoveRange(20, recentActivity.Count - 20);
        }
    }

    private void ShowStatus(string message, Color color)
    {
        statusMessage = message;
        statusColor = color;
        statusTimer = 3f;

        if (hapticFeedback)
        {
            glowAnimation = 1f;
        }
    }

    private void SpawnParticle(Vector2 position)
    {
        if (particles[nextParticle] == null)
            particles[nextParticle] = new ParticleEffect();

        particles[nextParticle].Spawn(position);
        nextParticle = (nextParticle + 1) % particles.Length;
    }

    private void QuickSave()
    {
        SaveAllDatabases();
        ShowStatus("✅ All databases saved successfully!", Color.green);
        LogActivity("Quick save completed");
    }

    private void AutoSave()
    {
        SaveAllDatabases();
        lastAutoSave = Time.realtimeSinceStartup;
        LogActivity("Auto-save completed");
    }

    private void SaveAllDatabases()
    {
        if (itemDatabase != null)
        {
            EditorUtility.SetDirty(itemDatabase);
        }

        if (recipeDatabase != null)
        {
            EditorUtility.SetDirty(recipeDatabase);
        }

        AssetDatabase.SaveAssets();
    }

    private void RefreshAll()
    {
        LoadDatabases();
        UpdateStatistics();
        ShowStatus("🔄 All data refreshed!", Color.cyan);
        LogActivity("System refresh completed");
    }

    private void GenerateReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("ULTIMATE INVENTORY TOOL HUB - SYSTEM REPORT");
        report.AppendLine("============================================");
        report.AppendLine($"Generated: {System.DateTime.Now}");
        report.AppendLine($"Version: {VERSION}");
        report.AppendLine();
        report.AppendLine("DATABASE STATISTICS");
        report.AppendLine($"  Total Items: {totalItems}");
        report.AppendLine($"  Total Recipes: {totalRecipes}");
        report.AppendLine($"  Grid Efficiency: {GetPackingEfficiency():P2}");
        report.AppendLine($"  System Health: {GetSystemHealth():P2}");
        report.AppendLine();
        report.AppendLine("PERFORMANCE METRICS");
        report.AppendLine($"  Average FPS: {performanceMetrics.GetValueOrDefault("fps", 60):F1}");
        report.AppendLine($"  Memory Usage: {performanceMetrics.GetValueOrDefault("memory", 0):F1} MB");

        string path = EditorUtility.SaveFilePanel("Save Report", Application.dataPath,
            $"inventory_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt", "txt");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, report.ToString());
            ShowStatus("📊 Report generated successfully!", Color.green);
            LogActivity("Report exported");
        }
    }

    private void PerformGlobalSearch()
    {
        // Search implementation
    }

    private void FixHealthIssue(string issue)
    {
        switch (issue)
        {
            case "Item Database":
                CreateOrLoadItemDatabase();
                break;
            case "Recipe Database":
                CreateOrLoadRecipeDatabase();
                break;
            case "Performance":
                OptimizePerformance();
                break;
        }

        // Refresh after fix
        LoadDatabases();
        UpdateStatistics();
        Repaint();
    }

    private void CreateOrLoadItemDatabase()
    {

        // First try to find existing at known location
        string expectedPath = "Assets/_WildSurvival/Data/Databases/ItemDatabase.asset";
        itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(expectedPath);

        if (itemDatabase != null)
        {
            Debug.Log($"Loaded existing ItemDatabase with {itemDatabase.GetAllItems().Count} items");
            EditorPrefs.SetString("UIT_ItemDatabasePath", expectedPath);
            UpdateStatistics();
            return;
        }

        // If not at expected location, search for it
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

            if (itemDatabase != null)
            {
                Debug.Log($"Found ItemDatabase at: {path} with {itemDatabase.GetAllItems().Count} items");
                EditorPrefs.SetString("UIT_ItemDatabasePath", path);
                UpdateStatistics();
                return;
            }
        }

        // Only create new if absolutely none exists
        if (EditorUtility.DisplayDialog("Create Item Database",
            "No Item Database found. Create a new one?",
            "Create", "Cancel"))
        {
            itemDatabase = ScriptableObject.CreateInstance<ItemDatabase>();

            // Ensure directory exists
            string dir = Path.GetDirectoryName(expectedPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(itemDatabase, expectedPath);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString("UIT_ItemDatabasePath", expectedPath);

            ShowStatus("✅ New Item Database created!", Color.green);
        }

        UpdateStatistics();
    }

    private void CreateOrLoadRecipeDatabase()
    {

        // First try to find existing at known location
        string expectedPath = "Assets/_WildSurvival/Data/Databases/RecipeDatabase.asset";
        recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(expectedPath);

        if (recipeDatabase != null)
        {
            Debug.Log($"Loaded existing RecipeDatabase with {recipeDatabase.GetAllRecipes().Count} recipes");
            EditorPrefs.SetString("UIT_RecipeDatabasePath", expectedPath);
            UpdateStatistics();
            return;
        }

        // If not at expected location, search for it
        string[] guids = AssetDatabase.FindAssets("t:RecipeDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);

            if (recipeDatabase != null)
            {
                Debug.Log($"Found RecipeDatabase at: {path} with {recipeDatabase.GetAllRecipes().Count} recipes");
                EditorPrefs.SetString("UIT_RecipeDatabasePath", path);
                UpdateStatistics();
                return;
            }
        }



        // Only create new if absolutely none exists
        if (EditorUtility.DisplayDialog("Create Recipe Database",
            "No Recipe Database found. Create a new one?",
            "Create", "Cancel"))
        {
            recipeDatabase = ScriptableObject.CreateInstance<RecipeDatabase>();

            // Ensure directory exists
            string dir = Path.GetDirectoryName(expectedPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(recipeDatabase, expectedPath);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString("UIT_RecipeDatabasePath", expectedPath);

            ShowStatus("✅ New Recipe Database created!", Color.green);
        }

        UpdateStatistics();
    }

    private void SaveDatabaseReferences()
    {
        if (itemDatabase != null)
        {
            string path = AssetDatabase.GetAssetPath(itemDatabase);
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString("UIT_ItemDatabasePath", path);
                Debug.Log($"Saved ItemDatabase path: {path}");
            }
        }

        if (recipeDatabase != null)
        {
            string path = AssetDatabase.GetAssetPath(recipeDatabase);
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString("UIT_RecipeDatabasePath", path);
                Debug.Log($"Saved RecipeDatabase path: {path}");
            }
        }
    }

    private void ScanAndPopulateItemDatabase()
    {
        if (itemDatabase == null) return;

        // Scan for ItemData assets (your existing items)
        string[] itemDataGuids = AssetDatabase.FindAssets("t:ItemData");
        int converted = 0;

        foreach (string guid in itemDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData oldItem = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (oldItem != null)
            {
                // Convert to ItemDefinition
                ItemDefinition newItem = ConvertItemDataToDefinition(oldItem);
                if (newItem != null)
                {
                    itemDatabase.AddItem(newItem);
                    converted++;
                }
            }
        }

        // Also scan for existing ItemDefinitions
        string[] itemDefGuids = AssetDatabase.FindAssets("t:ItemDefinition");
        foreach (string guid in itemDefGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
            {
                itemDatabase.AddItem(item);
            }
        }

        EditorUtility.SetDirty(itemDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log($"Populated database with {converted} converted items and {itemDefGuids.Length} existing definitions");
    }

    private void ScanAndPopulateRecipeDatabase()
    {
        if (recipeDatabase == null) return;

        // Scan for existing recipe assets
        string[] recipeGuids = AssetDatabase.FindAssets("t:RecipeDefinition");
        int added = 0;

        foreach (string guid in recipeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);

            if (recipe != null)
            {
                recipeDatabase.AddRecipe(recipe);
                added++;
            }
        }

        // Also check for CraftingRecipe type (if different)
        string[] craftingRecipeGuids = AssetDatabase.FindAssets("t:CraftingRecipe");
        foreach (string guid in craftingRecipeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CraftingRecipe oldRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);

            if (oldRecipe != null)
            {
                RecipeDefinition newRecipe = ConvertCraftingRecipeToDefinition(oldRecipe);
                if (newRecipe != null)
                {
                    recipeDatabase.AddRecipe(newRecipe);
                    added++;
                }
            }
        }

        EditorUtility.SetDirty(recipeDatabase);
        AssetDatabase.SaveAssets();

        Debug.Log($"Populated recipe database with {added} recipes");
    }

    private void CreateItemDatabase()
    {
        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        string path = "Assets/_WildSurvival/Data/Databases/ItemDatabase.asset";

        // Ensure directory exists
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        itemDatabase = db;

        ShowStatus("✅ Item database created!", Color.green);
        LogActivity("Item database created");
    }

    private void CreateRecipeDatabase()
    {
        var db = ScriptableObject.CreateInstance<RecipeDatabase>();
        string path = "Assets/_WildSurvival/Data/Databases/RecipeDatabase.asset";

        // Ensure directory exists
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        recipeDatabase = db;

        ShowStatus("✅ Recipe database created!", Color.green);
        LogActivity("Recipe database created");
    }

    private class ItemCreatorState
    {
        public ItemDefinition currentItem;
        public bool isCreatingNew = true;
        public string itemID = "";
        public string displayName = "";
        public string description = "";
        public Sprite icon;
        public GameObject worldModel;
        public ItemCategory category = ItemCategory.Misc;
        public float weight = 1f;
        public int maxStackSize = 1;
        public Vector2Int gridSize = Vector2Int.one;
        public bool[,] shapeGrid;
        public bool useCustomShape = false;
        public bool hasDurability = false;
        public float maxDurability = 100f;
        public int baseValue = 1;
        public bool allowRotation = true;

        // Fuel properties
        public bool isFuel;
        public FireInstance.FuelType fuelType;
        public float burnDuration = 10f;
        public float burnTemperature = 400f;
        public float heatOutput = 1f;
        public float smokeAmount = 1f;
        public float fuelValue = 10f;

        public void Reset()
        {
            isCreatingNew = true;
            currentItem = null;
            itemID = "item_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            displayName = "New Item";
            description = "";
            icon = null;
            worldModel = null;
            category = ItemCategory.Misc;
            weight = 1f;
            maxStackSize = 1;
            gridSize = Vector2Int.one;
            useCustomShape = false;
            hasDurability = false;
            maxDurability = 100f;
            baseValue = 1;
            allowRotation = true;
            InitializeShapeGrid();
        }

        public void InitializeShapeGrid()
        {
            shapeGrid = new bool[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    shapeGrid[x, y] = true; // Default to full rectangle
                }
            }
        }
    }

    private ItemCreatorState itemCreatorState = new ItemCreatorState();
    //private Vector2 itemListScroll;
    private string itemSearchFilter = "";

    // ========== CONVERSION METHODS ==========
    private ItemDefinition ConvertItemDataToDefinition(ItemData oldItem)
    {
        if (oldItem == null) return null;

        var newItem = ScriptableObject.CreateInstance<ItemDefinition>();
        newItem.itemID = oldItem.itemID;
        newItem.displayName = oldItem.itemName;
        newItem.description = oldItem.description;
        newItem.weight = oldItem.weight;
        newItem.maxStackSize = oldItem.maxStackSize;
        // Map other properties as needed

        // Save the new ItemDefinition
        string path = $"Assets/_WildSurvival/Data/Items/Converted_{oldItem.itemID}.asset";
        string dir = Path.GetDirectoryName(path);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        AssetDatabase.CreateAsset(newItem, path);
        return newItem;
    }

    private RecipeDefinition ConvertCraftingRecipeToDefinition(CraftingRecipe oldRecipe)
    {
        if (oldRecipe == null) return null;

        var newRecipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        // Map properties from old recipe type
        // This depends on your CraftingRecipe structure

        return newRecipe;
    }

    private void DrawItemCreator()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        DrawSectionHeading("🎨", "Item Creator");

        EditorGUILayout.BeginHorizontal();

        // Left Panel - Item List
        DrawItemCreatorList();

        // Right Panel - Item Editor
        DrawItemCreatorEditor();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawItemCreatorList()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));

        EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Create New Item", GUILayout.Height(30)))
        {
            itemCreatorState.Reset();
        }

        EditorGUILayout.Space(5);

        // Search field
        itemSearchFilter = EditorGUILayout.TextField("Search:", itemSearchFilter);

        itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll, GUILayout.Height(400));


        if (itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(itemSearchFilter) &&
                    !item.displayName.ToLower().Contains(itemSearchFilter.ToLower()))
                    continue;

                if (GUILayout.Button(item.displayName, EditorStyles.toolbarButton))
                {
                    LoadItemForEditing(item);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    private void DrawFuelProperties()
    {
        if (itemCreatorState.currentItem == null) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Fuel Properties", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            itemCreatorState.isFuel = EditorGUILayout.Toggle("Is Fuel", itemCreatorState.isFuel);

            if (itemCreatorState.isFuel)
            {
                EditorGUI.indentLevel++;

                itemCreatorState.fuelType = (FireInstance.FuelType)EditorGUILayout.EnumPopup(
                    "Fuel Type", itemCreatorState.fuelType);

                itemCreatorState.burnDuration = EditorGUILayout.FloatField(
                    "Burn Duration (min)", itemCreatorState.burnDuration);

                itemCreatorState.burnTemperature = EditorGUILayout.FloatField(
                    "Burn Temperature (°C)", itemCreatorState.burnTemperature);

                itemCreatorState.heatOutput = EditorGUILayout.Slider(
                    "Heat Output", itemCreatorState.heatOutput, 0.1f, 2f);

                itemCreatorState.smokeAmount = EditorGUILayout.Slider(
                    "Smoke Amount", itemCreatorState.smokeAmount, 0f, 3f);

                itemCreatorState.fuelValue = EditorGUILayout.FloatField(
                    "Fuel Value (units)", itemCreatorState.fuelValue);

                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawItemCreatorEditor()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField(
            itemCreatorState.isCreatingNew ? "Create New Item" : $"Edit: {itemCreatorState.displayName}",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Basic Properties
        EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);
        itemCreatorState.itemID = EditorGUILayout.TextField("Item ID", itemCreatorState.itemID);
        itemCreatorState.displayName = EditorGUILayout.TextField("Display Name", itemCreatorState.displayName);

        EditorGUILayout.LabelField("Description");
        itemCreatorState.description = EditorGUILayout.TextArea(itemCreatorState.description, GUILayout.Height(60));

        itemCreatorState.icon = (Sprite)EditorGUILayout.ObjectField("Icon", itemCreatorState.icon, typeof(Sprite), false);
        itemCreatorState.worldModel = (GameObject)EditorGUILayout.ObjectField("World Model", itemCreatorState.worldModel, typeof(GameObject), false);

        EditorGUILayout.Space(10);

        // Categories & Properties
        EditorGUILayout.LabelField("Categories & Properties", EditorStyles.boldLabel);
        itemCreatorState.category = (ItemCategory)EditorGUILayout.EnumPopup("Category", itemCreatorState.category);
        itemCreatorState.weight = EditorGUILayout.FloatField("Weight (kg)", itemCreatorState.weight);
        itemCreatorState.maxStackSize = EditorGUILayout.IntField("Max Stack Size", itemCreatorState.maxStackSize);
        itemCreatorState.baseValue = EditorGUILayout.IntField("Base Value", itemCreatorState.baseValue);

        EditorGUILayout.Space(10);

        // Grid & Shape
        EditorGUILayout.LabelField("Grid & Shape", EditorStyles.boldLabel);
        Vector2Int newGridSize = EditorGUILayout.Vector2IntField("Grid Size", itemCreatorState.gridSize);
        if (newGridSize != itemCreatorState.gridSize)
        {
            itemCreatorState.gridSize = newGridSize;
            itemCreatorState.InitializeShapeGrid();
        }

        itemCreatorState.allowRotation = EditorGUILayout.Toggle("Allow Rotation", itemCreatorState.allowRotation);
        itemCreatorState.useCustomShape = EditorGUILayout.Toggle("Use Custom Shape", itemCreatorState.useCustomShape);

        if (itemCreatorState.useCustomShape)
        {
            DrawShapeGridEditor();
        }

        EditorGUILayout.Space(10);

        // Durability
        itemCreatorState.hasDurability = EditorGUILayout.Toggle("Has Durability", itemCreatorState.hasDurability);
        if (itemCreatorState.hasDurability)
        {
            EditorGUI.indentLevel++;
            itemCreatorState.maxDurability = EditorGUILayout.FloatField("Max Durability", itemCreatorState.maxDurability);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(20);

        // Action Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(itemCreatorState.isCreatingNew ? "Create Item" : "Save Changes", GUILayout.Height(35)))
        {
            SaveItem();
        }
        GUI.backgroundColor = Color.white;

        if (!itemCreatorState.isCreatingNew)
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Cancel", GUILayout.Height(35)))
            {
                LoadItemForEditing(itemCreatorState.currentItem);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Delete Item",
                    $"Are you sure you want to delete {itemCreatorState.displayName}?",
                    "Delete", "Cancel"))
                {
                    DeleteCurrentItem();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawShapeGridEditor()
    {
        EditorGUILayout.LabelField("Shape Grid (Click to toggle cells):", EditorStyles.miniLabel);

        const int cellSize = 25;

        for (int y = 0; y < itemCreatorState.gridSize.y; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < itemCreatorState.gridSize.x; x++)
            {
                bool isActive = itemCreatorState.shapeGrid[x, y];

                GUI.backgroundColor = isActive ? Color.green : Color.red;
                if (GUILayout.Button(isActive ? "■" : "□", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    itemCreatorState.shapeGrid[x, y] = !isActive;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;

        // Preset shapes
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Preset Shapes:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Full")) SetShapePreset(ShapePreset.Full);
        if (GUILayout.Button("L-Shape")) SetShapePreset(ShapePreset.L);
        if (GUILayout.Button("T-Shape")) SetShapePreset(ShapePreset.T);
        if (GUILayout.Button("Line")) SetShapePreset(ShapePreset.Line);
        if (GUILayout.Button("Plus")) SetShapePreset(ShapePreset.Plus);
        EditorGUILayout.EndHorizontal();
    }

    private enum ShapePreset { Full, L, T, Line, Plus }

    private void SetShapePreset(ShapePreset preset)
    {
        // Clear grid first
        for (int x = 0; x < itemCreatorState.gridSize.x; x++)
            for (int y = 0; y < itemCreatorState.gridSize.y; y++)
                itemCreatorState.shapeGrid[x, y] = false;

        switch (preset)
        {
            case ShapePreset.Full:
                for (int x = 0; x < itemCreatorState.gridSize.x; x++)
                    for (int y = 0; y < itemCreatorState.gridSize.y; y++)
                        itemCreatorState.shapeGrid[x, y] = true;
                break;

            case ShapePreset.L:
                if (itemCreatorState.gridSize.x >= 2 && itemCreatorState.gridSize.y >= 3)
                {
                    itemCreatorState.shapeGrid[0, 0] = true;
                    itemCreatorState.shapeGrid[0, 1] = true;
                    itemCreatorState.shapeGrid[0, 2] = true;
                    itemCreatorState.shapeGrid[1, 2] = true;
                }
                break;

            case ShapePreset.T:
                if (itemCreatorState.gridSize.x >= 3 && itemCreatorState.gridSize.y >= 2)
                {
                    itemCreatorState.shapeGrid[0, 0] = true;
                    itemCreatorState.shapeGrid[1, 0] = true;
                    itemCreatorState.shapeGrid[2, 0] = true;
                    itemCreatorState.shapeGrid[1, 1] = true;
                }
                break;

            case ShapePreset.Line:
                for (int x = 0; x < itemCreatorState.gridSize.x; x++)
                    itemCreatorState.shapeGrid[x, 0] = true;
                break;

            case ShapePreset.Plus:
                int centerX = itemCreatorState.gridSize.x / 2;
                int centerY = itemCreatorState.gridSize.y / 2;

                for (int x = 0; x < itemCreatorState.gridSize.x; x++)
                    itemCreatorState.shapeGrid[x, centerY] = true;
                for (int y = 0; y < itemCreatorState.gridSize.y; y++)
                    itemCreatorState.shapeGrid[centerX, y] = true;
                break;
        }
    }

    private void LoadItemForEditing(ItemDefinition item)
    {
        if (item == null) return;

        itemCreatorState.isCreatingNew = false;
        itemCreatorState.currentItem = item;
        itemCreatorState.itemID = item.itemID;
        itemCreatorState.displayName = item.displayName;
        itemCreatorState.description = item.description;
        itemCreatorState.icon = item.icon;
        itemCreatorState.worldModel = item.worldModel;
        itemCreatorState.category = item.primaryCategory;
        itemCreatorState.weight = item.weight;
        itemCreatorState.maxStackSize = item.maxStackSize;
        itemCreatorState.gridSize = item.gridSize;
        itemCreatorState.baseValue = item.baseValue;
        itemCreatorState.allowRotation = item.allowRotation;
        itemCreatorState.hasDurability = item.hasDurability;
        itemCreatorState.maxDurability = item.maxDurability;
        itemCreatorState.useCustomShape = item.useCustomShape;

        // Load shape grid
        if (item.shapeGrid != null &&
            item.shapeGrid.GetLength(0) == item.gridSize.x &&
            item.shapeGrid.GetLength(1) == item.gridSize.y)
        {
            itemCreatorState.shapeGrid = item.shapeGrid;
        }
        else
        {
            itemCreatorState.InitializeShapeGrid();
        }
    }

    private void SaveItem()
    {
        if (string.IsNullOrEmpty(itemCreatorState.itemID) || string.IsNullOrEmpty(itemCreatorState.displayName))
        {
            EditorUtility.DisplayDialog("Error", "Item ID and Display Name are required", "OK");
            return;
        }

        // Check for duplicate ID when creating new item
        if (itemCreatorState.isCreatingNew && itemDatabase != null)
        {
            if (itemDatabase.HasItemWithID(itemCreatorState.itemID))
            {
                EditorUtility.DisplayDialog("Duplicate ID",
                    $"An item with ID '{itemCreatorState.itemID}' already exists!\n\n" +
                    "Please use a different ID or edit the existing item.", "OK");
                return;
            }

            // Warn about duplicate name
            if (itemDatabase.HasItemWithName(itemCreatorState.displayName))
            {
                if (!EditorUtility.DisplayDialog("Duplicate Name Warning",
                    $"An item with name '{itemCreatorState.displayName}' already exists.\n\n" +
                    "This might cause confusion. Continue anyway?", "Continue", "Cancel"))
                {
                    return;
                }
            }
        }

        ItemDefinition item;

        if (itemCreatorState.isCreatingNew)
        {
            // Check if asset already exists at path
            string path = $"Assets/_WildSurvival/Data/Items/{itemCreatorState.itemID}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("Overwrite Asset?",
                    $"An asset already exists at:\n{path}\n\nOverwrite it?", "Overwrite", "Cancel"))
                {
                    return;
                }

                // Delete the existing asset
                AssetDatabase.DeleteAsset(path);
            }

            item = ScriptableObject.CreateInstance<ItemDefinition>();

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(item, path);
        }
        else
        {
            item = itemCreatorState.currentItem;

            // Check if we're changing the ID
            if (item.itemID != itemCreatorState.itemID)
            {
                if (itemDatabase.HasItemWithID(itemCreatorState.itemID))
                {
                    EditorUtility.DisplayDialog("Duplicate ID",
                        $"Cannot change ID to '{itemCreatorState.itemID}' - it already exists!", "OK");
                    return;
                }
            }
        }

        // Update properties
        item.itemID = itemCreatorState.itemID;
        item.displayName = itemCreatorState.displayName;
        item.description = itemCreatorState.description;
        item.icon = itemCreatorState.icon;
        item.worldModel = itemCreatorState.worldModel;
        item.primaryCategory = itemCreatorState.category;
        item.weight = itemCreatorState.weight;
        item.maxStackSize = itemCreatorState.maxStackSize;
        item.gridSize = itemCreatorState.gridSize;
        item.baseValue = itemCreatorState.baseValue;
        item.allowRotation = itemCreatorState.gridSize.x != itemCreatorState.gridSize.y;
        item.hasDurability = itemCreatorState.hasDurability;
        item.maxDurability = itemCreatorState.maxDurability;
        item.useCustomShape = itemCreatorState.useCustomShape;
        item.shapeGrid = itemCreatorState.shapeGrid;

        EditorUtility.SetDirty(item);

        if (itemCreatorState.isCreatingNew)
        {
            if (itemDatabase != null)
            {
                bool added = itemDatabase.AddItem(item);
                if (added)
                {
                    itemCreatorState.currentItem = item;
                    itemCreatorState.isCreatingNew = false;

                    // Refresh the item list
                    RefreshItemList();
                }
                else
                {
                    // Failed to add - delete the asset
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item));
                    ShowStatus("❌ Failed to add item to database!", Color.red);
                    return;
                }
            }
        }

        AssetDatabase.SaveAssets();
        UpdateStatistics();

        ShowStatus($"✅ Item '{itemCreatorState.displayName}' saved!", Color.green);
        LogActivity($"Saved item: {itemCreatorState.displayName}");
    }

    private void RefreshItemList()
    {
        // Force refresh of the item list display
        if (itemDatabase != null)
        {
            itemLookup.Clear();
            foreach (var item in itemDatabase.GetAllItems())
            {
                if (item != null && !string.IsNullOrEmpty(item.itemID))
                {
                    itemLookup[item.itemID] = item;
                }
            }
        }

        // Force repaint
        Repaint();
    }

    private void DeleteCurrentItem()
    {
        if (itemCreatorState.currentItem != null)
        {
            string name = itemCreatorState.currentItem.displayName;

            if (itemDatabase != null)
            {
                itemDatabase.RemoveItem(itemCreatorState.currentItem);
                EditorUtility.SetDirty(itemDatabase);
            }

            string path = AssetDatabase.GetAssetPath(itemCreatorState.currentItem);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            itemCreatorState.Reset();
            UpdateStatistics();

            ShowStatus($"Deleted item: {name}", Color.yellow);
            LogActivity($"Deleted item: {name}");
        }
    }

    // ========== INVENTORY SIMULATOR METHODS ==========

    private void ClearGrid()
    {
        InitializeGrid();
        ShowStatus("Grid cleared", Color.yellow);
        LogActivity("Inventory grid cleared");
    }

    private void AutoArrangeItems()
    {
        var itemsToArrange = new List<PlacedItem>(placedItems);
        ClearGrid();

        // Sort by size (largest first)
        itemsToArrange.Sort((a, b) =>
        {
            int sizeA = a.GetSize().x * a.GetSize().y;
            int sizeB = b.GetSize().x * b.GetSize().y;
            return sizeB.CompareTo(sizeA);
        });

        foreach (var item in itemsToArrange)
        {
            Vector2Int? bestPos = FindBestPosition(item);
            if (bestPos.HasValue)
            {
                item.gridPosition = bestPos.Value;
                PlaceItemOnGrid(item);
            }
        }

        ShowStatus("Items auto-arranged", Color.green);
        LogActivity("Auto-arrange completed");
    }

    private void GenerateRandomLoadout()
    {
        ClearGrid();

        int itemCount = UnityEngine.Random.Range(5, 15);
        int failedAttempts = 0;

        for (int i = 0; i < itemCount && failedAttempts < 50; i++)
        {
            // Create items with reasonable sizes for the grid
            int maxWidth = Mathf.Min(4, gridWidth / 2);
            int maxHeight = Mathf.Min(4, gridHeight / 3);

            var mockItem = CreateMockItem($"Item_{UnityEngine.Random.Range(0, 100)}",
                UnityEngine.Random.Range(1, maxWidth),
                UnityEngine.Random.Range(1, maxHeight));

            var placedItem = new PlacedItem(mockItem, Vector2Int.zero);

            // Try to place with auto-rotation if needed
            Vector2Int? pos = FindBestPosition(placedItem);

            if (!pos.HasValue && mockItem.allowRotation)
            {
                placedItem.isRotated = true;
                pos = FindBestPosition(placedItem);
            }

            if (pos.HasValue)
            {
                placedItem.gridPosition = pos.Value;
                PlaceItemOnGrid(placedItem);
            }
            else
            {
                failedAttempts++;
                i--; // Try again with a different item
            }
        }

        ShowStatus($"Generated {placedItems.Count} random items", Color.green);
        LogActivity("Random loadout generated");
    }

    private ItemDefinition CreateMockItem(string name, int width, int height)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.itemID = "mock_" + name.ToLower();
        item.displayName = name;
        item.gridSize = new Vector2Int(width, height);
        item.weight = UnityEngine.Random.Range(0.5f, 5f);
        item.maxStackSize = UnityEngine.Random.Range(1, 20);
        item.baseValue = UnityEngine.Random.Range(10, 100);
        item.allowRotation = UnityEngine.Random.value > 0.5f;
        item.primaryCategory = (ItemCategory) UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemCategory)).Length);
        return item;
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        gridWidth = Mathf.Clamp(newWidth, 4, 20);
        gridHeight = Mathf.Clamp(newHeight, 4, 20);

        bool[,] newGrid = new bool[gridWidth, gridHeight];

        // Copy existing occupancy
        int copyWidth = Mathf.Min(gridOccupancy.GetLength(0), gridWidth);
        int copyHeight = Mathf.Min(gridOccupancy.GetLength(1), gridHeight);

        for (int x = 0; x < copyWidth; x++)
        {
            for (int y = 0; y < copyHeight; y++)
            {
                newGrid[x, y] = gridOccupancy[x, y];
            }
        }

        gridOccupancy = newGrid;

        // Remove items that are now out of bounds
        placedItems.RemoveAll(item =>
        {
            Vector2Int size = item.GetSize();
            return item.gridPosition.x + size.x > gridWidth ||
                   item.gridPosition.y + size.y > gridHeight;
        });

        UpdateHeatmap();
        LogActivity($"Grid resized to {gridWidth}x{gridHeight}");
    }

    private void AddItemToGrid(ItemDefinition itemDef)
    {
        if (currentWeight + itemDef.weight > maxWeight)
        {
            ShowStatus("Weight limit exceeded!", Color.red);
            return;
        }

        var placedItem = new PlacedItem(itemDef, Vector2Int.zero);

        // First try normal orientation
        Vector2Int? position = FindBestPosition(placedItem);

        // If no position found and item can rotate, try auto-rotating for edge placement
        if (!position.HasValue && itemDef.allowRotation && itemDef.gridSize.x != itemDef.gridSize.y)
        {
            // Check if rotating would help (especially for bottom rows)
            placedItem.isRotated = true;
            position = FindBestPosition(placedItem);

            if (position.HasValue)
            {
                ShowStatus($"Auto-rotated {itemDef.displayName} to fit", Color.yellow);
            }
            else
            {
                placedItem.isRotated = false; // Reset if still doesn't fit
            }
        }

        if (position.HasValue)
        {
            placedItem.gridPosition = position.Value;
            PlaceItemOnGrid(placedItem);

            // Add animation
            itemAnimations[placedItem] = 1f;

            ShowStatus($"Added {itemDef.displayName}", Color.green);
            LogActivity($"Item added: {itemDef.displayName}");
        }
        else
        {
            ShowStatus("No space available!", Color.red);
        }
    }

    // Then FindBestPosition can use it
    private Vector2Int? FindBestPosition(PlacedItem item)
    {
        Vector2Int size = item.GetSize();

        // Try normal orientation
        for (int y = 0; y <= gridHeight - size.y; y++)
        {
            for (int x = 0; x <= gridWidth - size.x; x++)
            {
                if (CanPlaceItem(new Vector2Int(x, y), size, item.uniqueId))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        // Try rotated if allowed
        if (item.itemDef.allowRotation && size.x != size.y)
        {
            item.isRotated = !item.isRotated;
            size = item.GetSize();

            for (int y = 0; y <= gridHeight - size.y; y++)
            {
                for (int x = 0; x <= gridWidth - size.x; x++)
                {
                    if (CanPlaceItem(new Vector2Int(x, y), size, item.uniqueId))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            item.isRotated = !item.isRotated;
        }

        return null;
    }

    // GetItemAt method should also be defined before use
    private PlacedItem GetItemAt(Vector2Int position)
    {
        foreach (var item in placedItems)
        {
            Vector2Int size = item.GetSize();
            if (position.x >= item.gridPosition.x &&
                position.x < item.gridPosition.x + size.x &&
                position.y >= item.gridPosition.y &&
                position.y < item.gridPosition.y + size.y)
            {
                return item;
            }
        }
        return null;
    }

    private void PlaceItemOnGrid(PlacedItem item)
    {
        if (!placedItems.Contains(item))
        {
            placedItems.Add(item);
        }

        Vector2Int size = item.GetSize();
        for (int x = item.gridPosition.x; x < item.gridPosition.x + size.x; x++)
        {
            for (int y = item.gridPosition.y; y < item.gridPosition.y + size.y; y++)
            {
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    gridOccupancy[x, y] = true;
                }
            }
        }

        currentWeight += item.itemDef.weight;
        UpdateHeatmap();

        // Auto-save grid state
        if (autoSaveEnabled)
        {
            SaveInventoryGrid();
        }
    }

    private void RemoveItemFromGrid(PlacedItem item)
    {
        placedItems.Remove(item);

        Vector2Int size = item.GetSize();
        for (int x = item.gridPosition.x; x < item.gridPosition.x + size.x; x++)
        {
            for (int y = item.gridPosition.y; y < item.gridPosition.y + size.y; y++)
            {
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    gridOccupancy[x, y] = false;
                }
            }
        }

        currentWeight -= item.itemDef.weight;
        UpdateHeatmap();

        // Auto-save grid state
        if (autoSaveEnabled)
        {
            SaveInventoryGrid();
        }
    }
    private void SaveInventoryGrid()
    {
        var saveData = new InventoryGridSaveData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            maxWeight = maxWeight
        };

        foreach (var item in placedItems)
        {
            if (item != null && item.itemDef != null)
            {
                saveData.savedItems.Add(new InventoryGridSaveData.SavedGridItem
                {
                    itemID = item.itemDef.itemID,
                    gridPosition = item.gridPosition,
                    isRotated = item.isRotated,
                    stackSize = item.stackSize,
                    uniqueId = item.uniqueId
                });
            }
        }

        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        EditorPrefs.SetString("UIT_InventoryGrid", json);

        LogActivity($"Saved {placedItems.Count} items in grid");
    }

    private void LoadInventoryGrid()
    {
        string json = EditorPrefs.GetString("UIT_InventoryGrid", "");

        if (string.IsNullOrEmpty(json))
        {
            InitializeGrid();
            return;
        }

        try
        {
            var saveData = JsonConvert.DeserializeObject<InventoryGridSaveData>(json);

            if (saveData != null)
            {
                // Restore grid settings
                gridWidth = saveData.gridWidth;
                gridHeight = saveData.gridHeight;
                maxWeight = saveData.maxWeight;

                // Initialize fresh grid
                InitializeGrid();

                // Restore items
                int restoredCount = 0;
                foreach (var savedItem in saveData.savedItems)
                {
                    if (itemDatabase != null)
                    {
                        var itemDef = itemDatabase.GetItem(savedItem.itemID);
                        if (itemDef != null)
                        {
                            var placedItem = new PlacedItem(itemDef, savedItem.gridPosition)
                            {
                                isRotated = savedItem.isRotated,
                                stackSize = savedItem.stackSize,
                                uniqueId = savedItem.uniqueId
                            };

                            // Verify it can still be placed
                            Vector2Int size = placedItem.GetSize();
                            if (CanPlaceItem(savedItem.gridPosition, size, placedItem.uniqueId))
                            {
                                placedItem.gridPosition = savedItem.gridPosition;
                                PlaceItemOnGrid(placedItem);
                                restoredCount++;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find item with ID: {savedItem.itemID}");
                        }
                    }
                }

                LogActivity($"Restored {restoredCount} items from saved grid");
                ShowStatus($"Loaded {restoredCount} items", Color.green);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory grid: {e.Message}");
            InitializeGrid();
        }
    }

    private void ClearSavedGrid()
    {
        EditorPrefs.DeleteKey("UIT_InventoryGrid");
        ShowStatus("Saved grid data cleared", Color.yellow);
    }

    private void StartDragging(PlacedItem item, Vector2Int clickPos)
    {
        draggedItem = item;
        isDragging = true;
        dragOffset = clickPos - item.gridPosition;

        RemoveItemFromGrid(item);
        LogActivity($"Started dragging {item.itemDef.displayName}");
    }

    private void PlaceDraggedItem(Rect gridRect, float cellSize)
    {
        if (!isDragging || draggedItem == null) return;

        Vector2Int gridPos = ScreenToGridPosition(Event.current.mousePosition, gridRect, cellSize);
        gridPos -= dragOffset;

        if (canPlaceAtDragPosition)
        {
            draggedItem.gridPosition = gridPos;
            PlaceItemOnGrid(draggedItem);

            // Success animation
            itemAnimations[draggedItem] = 1f;
            ShowStatus($"Placed {draggedItem.itemDef.displayName}", Color.green);
        }
        else if (autoArrangeItems)
        {
            // Try to find alternative position
            Vector2Int? altPos = FindBestPosition(draggedItem);
            if (altPos.HasValue)
            {
                draggedItem.gridPosition = altPos.Value;
                PlaceItemOnGrid(draggedItem);
                ShowStatus($"Auto-placed {draggedItem.itemDef.displayName}", Color.yellow);
            }
            else
            {
                // Return to original position
                PlaceItemOnGrid(draggedItem);
                ShowStatus("Cannot place item here", Color.red);
            }
        }
        else
        {
            // Return to original position
            PlaceItemOnGrid(draggedItem);
            ShowStatus("Invalid placement", Color.red);
        }

        isDragging = false;
        draggedItem = null;
    }

    private void RotateDraggedItem()
    {
        if (draggedItem != null && draggedItem.itemDef.allowRotation)
        {
            draggedItem.isRotated = !draggedItem.isRotated;
            ShowStatus("Item rotated", Color.yellow);
        }
    }

    private Vector2Int ScreenToGridPosition(Vector2 screenPos, Rect gridRect, float cellSize)
    {
        Vector2 localPos = screenPos - new Vector2(gridRect.x, gridRect.y);
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    private void UpdateHeatmap()
    {
        heatmap.Clear();

        // Calculate usage patterns
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int nearbyItems = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight && gridOccupancy[nx, ny])
                        {
                            nearbyItems++;
                        }
                    }
                }

                float heat = nearbyItems / 9f;
                heatmap[new Vector2Int(x, y)] = Color.Lerp(Color.blue, Color.red, heat);
            }
        }
    }

    private float GetGridUsage()
    {
        int occupied = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridOccupancy[x, y]) occupied++;
            }
        }
        return (float)occupied / (gridWidth * gridHeight);
    }

    private float GetPackingEfficiency()
    {
        if (placedItems == null || placedItems.Count == 0)
            return 0;

        float totalItemArea = placedItems
            .Where(i => i != null && i.itemDef != null)
            .Sum(i => i.GetSize().x * i.GetSize().y);

        float gridArea = gridWidth * gridHeight;
        return gridArea > 0 ? totalItemArea / gridArea : 0;
    }

    private int GetTotalValue()
    {
        if (placedItems == null || placedItems.Count == 0)
            return 0;

        return placedItems
            .Where(i => i != null && i.itemDef != null)
            .Sum(i => i.itemDef.baseValue * i.stackSize);
    }
    private float GetSystemHealth()
    {
        float health = 100f;

        if (itemDatabase == null) health -= 25f;
        if (recipeDatabase == null) health -= 25f;
        if (totalItems == 0) health -= 10f;
        if (totalRecipes == 0) health -= 10f;
        if (performanceMetrics.GetValueOrDefault("fps", 60) < 30) health -= 15f;
        if (performanceMetrics.GetValueOrDefault("memory", 0) > 500) health -= 15f;

        return Mathf.Max(0, health) / 100f;
    }

    private void SortItemsBySize()
    {
        AutoArrangeItems();
    }

    private void SortItemsByWeight()
    {
        var sorted = placedItems.OrderByDescending(i => i.itemDef.weight).ToList();
        ClearGrid();

        foreach (var item in sorted)
        {
            Vector2Int? pos = FindBestPosition(item);
            if (pos.HasValue)
            {
                item.gridPosition = pos.Value;
                PlaceItemOnGrid(item);
            }
        }
    }

    private void OptimizeLayout()
    {
        // Advanced packing algorithm
        AutoArrangeItems();
        ShowStatus("Layout optimized", Color.green);
    }

    private bool CanPlaceItem(Vector2Int position, Vector2Int size, string excludeId = "")
    {
        // Check if position is within grid bounds
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > gridWidth ||
            position.y + size.y > gridHeight)
        {
            return false;
        }

        // Check if any cells are occupied
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (gridOccupancy[x, y])
                {
                    // Check if it's occupied by a different item
                    var occupyingItem = GetItemAt(new Vector2Int(x, y));
                    if (occupyingItem != null && occupyingItem.uniqueId != excludeId)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // ========== CRAFTING METHODS ==========

    private void StartCrafting(RecipeDefinition recipe)
    {
        if (recipe == null) return;

        isCrafting = true;
        craftingProgress = 0f;

        // Consume ingredients
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.consumed)
            {
                availableMaterials[ingredient.name] -= ingredient.quantity;
            }
        }

        // Add to queue
        craftingQueue.Add(new CraftingQueue
        {
            recipe = recipe,
            startTime = Time.realtimeSinceStartup,
            duration = recipe.baseCraftTime
        });

        ShowStatus($"Crafting {recipe.recipeName}...", Color.yellow);
        LogActivity($"Started crafting: {recipe.recipeName}");
    }

    private void AddRandomMaterials()
    {
        string[] materials = { "Wood", "Stone", "Iron", "Fiber", "Leather", "Coal", "Gold" };

        foreach (var mat in materials)
        {
            if (!availableMaterials.ContainsKey(mat))
                availableMaterials[mat] = 0;

            availableMaterials[mat] += UnityEngine.Random.Range(5, 20);
        }

        ShowStatus("Added random materials", Color.green);
    }

    // ========== AI BATCH PROCESSOR METHODS ==========

    private string GetItemGenerationPrompt()
    {
        return @"Generate 100 survival game items in JSON format:
{
  'items': [
    {
      'itemID': 'unique_id',
      'displayName': 'Item Name',
      'description': 'Description',
      'category': 'Resource|Tool|Weapon|Food|Medicine',
      'weight': 1.0,
      'maxStackSize': 20,
      'gridWidth': 1,
      'gridHeight': 1,
      'baseValue': 10
    }
  ]
}

Categories: Resource, Tool, Weapon, Food, Medicine, Clothing, Building
Grid sizes: 1x1 for small, 2x2 for medium, 2x3 or 3x2 for large items
Weight: 0.1-10kg realistic weights
Stack sizes: 1 for tools/weapons, 5-99 for resources/consumables";
    }

    private string GetRecipeGenerationPrompt()
    {
        return @"Generate 50 crafting recipes in JSON format:
{
  'recipes': [
    {
      'recipeID': 'unique_id',
      'recipeName': 'Recipe Name',
      'description': 'Description',
      'category': 'Tools|Weapons|Building',
      'workstation': 'CraftingBench|Forge|Campfire',
      'craftTime': 5.0,
      'tier': 1,
      'ingredients': [
        {'name': 'Wood', 'quantity': 2, 'consumed': true}
      ],
      'outputs': [
        {'itemID': 'item_id', 'quantityMin': 1, 'quantityMax': 1}
      ]
    }
  ]
}";
    }

    private void ImportFromClipboard()
    {
        aiResponse = GUIUtility.systemCopyBuffer;
        ProcessAIResponse();
    }

    private void ExportAITemplate()
    {
        var template = new
        {
            items = new[]
            {
                new ItemBatchData
                {
                    itemID = "example_item",
                    displayName = "Example Item",
                    description = "Description here",
                    category = "Resource",
                    weight = 1.0f,
                    maxStackSize = 20,
                    gridWidth = 1,
                    gridHeight = 1
                }
            },
            recipes = new[]
            {
                new RecipeBatchData
                {
                    recipeID = "example_recipe",
                    recipeName = "Example Recipe",
                    category = "Tools",
                    workstation = "CraftingBench",
                    craftTime = 5.0f,
                    tier = 1
                }
            }
        };

        string json = JsonConvert.SerializeObject(template, Formatting.Indented);
        GUIUtility.systemCopyBuffer = json;

        ShowStatus("Template copied to clipboard!", Color.green);
    }

    private void ProcessAIResponse()
    {
        if (string.IsNullOrEmpty(aiResponse))
        {
            ShowStatus("No AI response to process", Color.red);
            return;
        }

        isProcessingAI = true;
        aiProcessingProgress = 0;

        try
        {
            var data = JObject.Parse(aiResponse);

            // Parse items
            if (data["items"] != null)
            {
                pendingAIItems = data["items"].ToObject<List<ItemBatchData>>();
                LogActivity($"Parsed {pendingAIItems.Count} items from AI");
            }

            // Parse recipes  
            if (data["recipes"] != null)
            {
                pendingAIRecipes = data["recipes"].ToObject<List<RecipeBatchData>>();
                LogActivity($"Parsed {pendingAIRecipes.Count} recipes from AI");
            }

            // Validate
            ValidateAIData();

            ShowStatus($"Ready to import {pendingAIItems.Count + pendingAIRecipes.Count} items", Color.green);
        }
        catch (Exception e)
        {
            ShowStatus($"Failed to parse AI response: {e.Message}", Color.red);
            LogActivity($"AI parsing error: {e.Message}");
        }
        finally
        {
            isProcessingAI = false;
        }
    }

    private void ValidateAIData()
    {
        lastValidation = new ValidationReport();

        // Validate items
        foreach (var item in pendingAIItems)
        {
            if (string.IsNullOrEmpty(item.itemID))
            {
                lastValidation.AddError($"Item missing ID: {item.displayName}");
            }

            if (item.weight <= 0 || item.weight > 1000)
            {
                lastValidation.AddWarning($"Unusual weight for {item.itemID}: {item.weight}kg");
            }

            if (item.gridWidth <= 0 || item.gridHeight <= 0 ||
                item.gridWidth > 5 || item.gridHeight > 5)
            {
                lastValidation.AddError($"Invalid grid size for {item.itemID}: {item.gridWidth}x{item.gridHeight}");
            }
        }

        // Validate recipes
        foreach (var recipe in pendingAIRecipes)
        {
            if (string.IsNullOrEmpty(recipe.recipeID))
            {
                lastValidation.AddError($"Recipe missing ID: {recipe.recipeName}");
            }

            if (recipe.craftTime <= 0 || recipe.craftTime > 3600)
            {
                lastValidation.AddWarning($"Unusual craft time for {recipe.recipeID}: {recipe.craftTime}s");
            }
        }

        LogActivity($"Validation complete: {lastValidation.messages.Count} issues found");
    }

    private void ExecuteAIImport()
    {
        int imported = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            // Import items
            foreach (var itemData in pendingAIItems)
            {
                if (ImportAIItem(itemData))
                {
                    imported++;
                    aiProcessingProgress = (float)imported / (pendingAIItems.Count + pendingAIRecipes.Count);
                }
            }

            // Import recipes
            foreach (var recipeData in pendingAIRecipes)
            {
                if (ImportAIRecipe(recipeData))
                {
                    imported++;
                    aiProcessingProgress = (float)imported / (pendingAIItems.Count + pendingAIRecipes.Count);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        pendingAIItems.Clear();
        pendingAIRecipes.Clear();

        UpdateStatistics();
        ShowStatus($"✅ Imported {imported} items successfully!", Color.green);
        LogActivity($"AI batch import completed: {imported} items");
    }

    private bool ImportAIItem(ItemBatchData data)
    {
        try
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemID = data.itemID;
            item.displayName = data.displayName;
            item.description = data.description;
            item.primaryCategory = ParseCategory(data.category);
            item.weight = data.weight;
            item.maxStackSize = data.maxStackSize;
            item.gridSize = new Vector2Int(data.gridWidth, data.gridHeight);
            item.baseValue = data.value;
            item.allowRotation = data.gridWidth != data.gridHeight;

            string path = $"Assets/_WildSurvival/Data/Items/AI_{data.itemID}.asset";

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.CreateAsset(item, path);

            if (itemDatabase != null)
            {
                itemDatabase.AddItem(item);
                itemLookup[item.itemID] = item;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ImportAIRecipe(RecipeBatchData data)
    {
        try
        {
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.recipeID = data.recipeID;
            recipe.recipeName = data.recipeName;
            recipe.description = data.description;
            recipe.category = data.category; // Just use the string directly
            recipe.requiredWorkstation = ParseWorkstation(data.workstation);
            recipe.baseCraftTime = data.craftTime;
            recipe.tier = data.tier;
            recipe.isKnownByDefault = data.unlockedByDefault;

            recipe.ingredients = new RecipeIngredient[0];
            recipe.outputs = new RecipeOutput[0];

            string path = $"Assets/_WildSurvival/Data/Recipes/AI_{data.recipeID}.asset";

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(recipe, path);

            if (recipeDatabase != null)
            {
                recipeDatabase.AddRecipe(recipe);
                recipeLookup[recipe.recipeID] = recipe;
                EditorUtility.SetDirty(recipeDatabase);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to import recipe: {e.Message}");
            return false;
        }
    }

    private ItemCategory ParseCategory(string category)
    {
        if (System.Enum.TryParse<ItemCategory>(category, out var result))
            return result;
        return ItemCategory.Misc;
    }

    private string ParseRecipeCategory(string category)
    {
        // Just return the string as-is for storage
        return string.IsNullOrEmpty(category) ? "Basic" : category;
    }

    private WorkstationType ParseWorkstation(string workstation)
    {
        if (System.Enum.TryParse<WorkstationType>(workstation, out var result))
            return result;
        return WorkstationType.None;
    }

    private CraftingCategory ParseCraftingCategoryFromString(string category)
    {
        if (string.IsNullOrEmpty(category))
            return CraftingCategory.Tools;

        // Try to parse the string to enum
        if (System.Enum.TryParse<CraftingCategory>(category, true, out var result))
            return result;

        // Map common strings to categories
        return category.ToLower() switch
        {
            "tool" or "tools" => CraftingCategory.Tools,
            "weapon" or "weapons" => CraftingCategory.Weapons,
            "cloth" or "clothing" => CraftingCategory.Clothing,
            "build" or "building" => CraftingCategory.Building,
            "cook" or "cooking" or "food" => CraftingCategory.Cooking,
            "med" or "medicine" or "healing" => CraftingCategory.Medicine,
            "process" or "processing" => CraftingCategory.Processing,
            "adv" or "advanced" => CraftingCategory.Advanced,
            _ => CraftingCategory.Tools
        };
    }

    // ========== OTHER UTILITY METHODS ==========

    private void GenerateBulkItems(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var item = CreateMockItem($"Generated_{i}",
                UnityEngine.Random.Range(1, 3), UnityEngine.Random.Range(1, 3));

            // Save to database
            string path = $"Assets/_WildSurvival/Data/Items/Generated_{i}.asset";
            AssetDatabase.CreateAsset(item, path);

            if (itemDatabase != null)
            {
                itemDatabase.AddItem(item);
            }
        }

        AssetDatabase.SaveAssets();
        UpdateStatistics();

        ShowStatus($"Generated {count} items!", Color.green);
        LogActivity($"Bulk generation: {count} items");
    }

    private void ExportDatabase()
    {
        var exportData = new
        {
            metadata = new
            {
                version = VERSION,
                exportDate = System.DateTime.Now,
                itemCount = totalItems,
                recipeCount = totalRecipes
            },
            items = itemDatabase?.GetAllItems().Select(i => new
            {
                i.itemID,
                i.displayName,
                i.description,
                category = i.primaryCategory.ToString(),
                i.weight,
                i.maxStackSize,
                gridSize = new { x = i.gridSize.x, y = i.gridSize.y },
                i.baseValue
            }),
            recipes = recipeDatabase?.GetAllRecipes().Select(r => new
            {
                r.recipeID,
                r.recipeName,
                r.description,
                category = r.category.ToString(),
                workstation = r.requiredWorkstation.ToString(),
                r.baseCraftTime,
                r.tier
            })
        };

        string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
        string path = EditorUtility.SaveFilePanel("Export Database", Application.dataPath,
            $"database_export_{System.DateTime.Now:yyyyMMdd_HHmmss}.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            ShowStatus("Database exported!", Color.green);
            LogActivity("Database exported");
        }
    }

    private void RunSystemTests()
    {
        ShowStatus("Running system tests...", Color.yellow);

        // Test database connections
        bool dbTest = itemDatabase != null && recipeDatabase != null;

        // Test grid operations
        bool gridTest = TestGridOperations();

        // Test AI parsing
        bool aiTest = TestAIParsing();

        string result = $"Tests: DB={dbTest} Grid={gridTest} AI={aiTest}";
        ShowStatus(result, dbTest && gridTest && aiTest ? Color.green : Color.red);
        LogActivity($"System tests: {result}");
    }

    private bool TestGridOperations()
    {
        try
        {
            var testItem = CreateMockItem("TestItem", 2, 2);
            var placed = new PlacedItem(testItem, new Vector2Int(0, 0));
            return CanPlaceItem(Vector2Int.zero, placed.GetSize(), placed.uniqueId);
        }
        catch
        {
            return false;
        }
    }

    private bool TestAIParsing()
    {
        try
        {
            string testJson = "{\"items\":[{\"itemID\":\"test\",\"displayName\":\"Test\"}]}";
            var data = JObject.Parse(testJson);
            return data["items"] != null;
        }
        catch
        {
            return false;
        }
    }

    private void LoadPreferences()
    {
        autoSaveEnabled = EditorPrefs.GetBool("UIT_AutoSave", true);
        showTooltips = EditorPrefs.GetBool("UIT_ShowTooltips", true);
        darkMode = EditorPrefs.GetBool("UIT_DarkMode", true);
        animationsEnabled = EditorPrefs.GetBool("UIT_Animations", true);
        hapticFeedback = EditorPrefs.GetBool("UIT_Haptic", true);
        showGridNumbers = EditorPrefs.GetBool("UIT_GridNumbers", true);
        autoArrangeItems = EditorPrefs.GetBool("UIT_AutoArrange", true);
        autoSaveInterval = EditorPrefs.GetFloat("UIT_AutoSaveInterval", 300f);
    }

    private void SavePreferences()
    {
        EditorPrefs.SetBool("UIT_AutoSave", autoSaveEnabled);
        EditorPrefs.SetBool("UIT_ShowTooltips", showTooltips);
        EditorPrefs.SetBool("UIT_DarkMode", darkMode);
        EditorPrefs.SetBool("UIT_Animations", animationsEnabled);
        EditorPrefs.SetBool("UIT_Haptic", hapticFeedback);
        EditorPrefs.SetBool("UIT_GridNumbers", showGridNumbers);
        EditorPrefs.SetBool("UIT_AutoArrange", autoArrangeItems);
        EditorPrefs.SetFloat("UIT_AutoSaveInterval", autoSaveInterval);
    }

    private void ResetPreferences()
    {
        autoSaveEnabled = true;
        showTooltips = true;
        darkMode = true;
        animationsEnabled = true;
        hapticFeedback = true;
        showGridNumbers = true;
        autoArrangeItems = true;
        autoSaveInterval = 300f;

        SavePreferences();
        ShowStatus("Preferences reset to defaults", Color.yellow);
    }

    // ========== HELPER CLASSES ==========

    [System.Serializable]
    public class PlacedItem
    {
        public ItemDefinition itemDef;
        public Vector2Int gridPosition;
        public bool isRotated;
        public int stackSize = 1;
        public Color displayColor;
        public string uniqueId;

        public PlacedItem(ItemDefinition def, Vector2Int pos)
        {
            itemDef = def;
            gridPosition = pos;
            isRotated = false;
            displayColor = new Color(
                UnityEngine.Random.Range(0.3f, 0.7f),
                UnityEngine.Random.Range(0.3f, 0.7f),
                UnityEngine.Random.Range(0.3f, 0.7f),
                0.8f
            );
            uniqueId = System.Guid.NewGuid().ToString();
        }

        public Vector2Int GetSize()
        {
            if (itemDef == null)
                return Vector2Int.one; // Default size if item is null

            return isRotated ?
                new Vector2Int(itemDef.gridSize.y, itemDef.gridSize.x) :
                itemDef.gridSize;
        }
    }

    [System.Serializable]
    public class CraftingQueue
    {
        public RecipeDefinition recipe;
        public float startTime;
        public float duration;
    }

    [System.Serializable]
    public class ItemBatchData
    {
        public string itemID;
        public string displayName;
        public string description;
        public string category;
        public int value;
        public float weight;
        public int maxStackSize;
        public int gridWidth;
        public int gridHeight;
    }

    [System.Serializable]
    public class RecipeBatchData
    {
        public string recipeID;
        public string recipeName;
        public string description;
        public string category;
        public string workstation;
        public float craftTime;
        public int tier;
        public bool unlockedByDefault;
    }

    [System.Serializable]
    public class ValidationReport
    {
        public bool hasErrors = false;
        public List<string> messages = new List<string>();

        public void AddError(string message)
        {
            hasErrors = true;
            messages.Add($"❌ {message}");
        }

        public void AddWarning(string message)
        {
            messages.Add($"⚠️ {message}");
        }
    }

    [System.Serializable]
    public class AnalyticsDataPoint
    {
        public System.DateTime timestamp;
        public int itemCount;
        public int recipeCount;
        public float gridEfficiency;
    }

    public class ParticleEffect
    {
        public Vector2 position;
        public Vector2 velocity;
        public float life;
        public float rotation;
        public bool active;

        public void Spawn(Vector2 pos)
        {
            position = pos;
            velocity = new Vector2(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-50, 50));
            life = 1f;
            rotation = UnityEngine.Random.Range(0, 360);
            active = true;
        }

        public void Update(float deltaTime)
        {
            if (!active) return;

            position += velocity * deltaTime;
            velocity *= 0.95f;
            life -= deltaTime;
            rotation += deltaTime * 100f;

            if (life <= 0)
            {
                active = false;
            }
        }
    }
    // ========== DATABASE OPERATIONS ==========

    // Also need to add this list for the import log
    private List<string> importLog = new List<string>();
    private void ScanForOrphanedAssets()
    {
        int orphaned = 0;

        // Find all ItemDefinition assets
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition");
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

            if (item != null && itemDatabase != null)
            {
                if (!itemDatabase.GetAllItems().Contains(item))
                {
                    itemDatabase.AddItem(item);
                    orphaned++;
                }
            }
        }

        ShowStatus($"Found and added {orphaned} orphaned items", Color.yellow);
        LogActivity($"Scanned for orphaned assets: {orphaned} found");
    }

    private void ImportFromItemDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int imported = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData oldItem = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (oldItem != null)
            {
                ItemDefinition newItem = ConvertItemDataToDefinition(oldItem);
                if (newItem != null && itemDatabase != null)
                {
                    itemDatabase.AddItem(newItem);
                    imported++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        UpdateStatistics();

        ShowStatus($"Imported {imported} items from ItemData", Color.green);
    }

    private void ImportDatabaseFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            var data = JObject.Parse(json);

            // Import items
            if (data["items"] != null)
            {
                var items = data["items"].ToObject<List<ItemBatchData>>();
                foreach (var itemData in items)
                {
                    ImportAIItem(itemData);
                }
            }

            // Import recipes
            if (data["recipes"] != null)
            {
                var recipes = data["recipes"].ToObject<List<RecipeBatchData>>();
                foreach (var recipeData in recipes)
                {
                    ImportAIRecipe(recipeData);
                }
            }

            AssetDatabase.SaveAssets();
            UpdateStatistics();

            ShowStatus("JSON import complete!", Color.green);
        }
        catch (Exception e)
        {
            ShowStatus($"Import failed: {e.Message}", Color.red);
        }
    }

    private void ExportDatabaseToJSON()
    {
        var exportData = new
        {
            metadata = new
            {
                version = VERSION,
                exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                itemCount = totalItems,
                recipeCount = totalRecipes
            },
            items = itemDatabase?.GetAllItems().Select(i => new
            {
                i.itemID,
                i.displayName,
                i.description,
                category = i.primaryCategory.ToString(),
                i.weight,
                i.maxStackSize,
                gridSize = new { x = i.gridSize.x, y = i.gridSize.y },
                i.baseValue,
                i.hasDurability,
                i.maxDurability
            }),
            recipes = recipeDatabase?.GetAllRecipes().Select(r => new
            {
                r.recipeID,
                r.recipeName,
                r.description,
                category = r.category.ToString(),
                workstation = r.requiredWorkstation.ToString(),
                r.baseCraftTime,
                r.tier
            })
        };

        string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
        string path = EditorUtility.SaveFilePanel("Export Database", Application.dataPath,
            $"database_export_{DateTime.Now:yyyyMMdd_HHmmss}.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            ShowStatus("Database exported to JSON!", Color.green);
            LogActivity("Database exported to JSON");
        }
    }

    private void ExportDatabaseToCSV()
    {
        // Export items to CSV
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ItemID,DisplayName,Category,Weight,StackSize,GridWidth,GridHeight,Value");

        if (itemDatabase != null)
        {
            foreach (var item in itemDatabase.GetAllItems())
            {
                csv.AppendLine($"{item.itemID},{item.displayName},{item.primaryCategory}," +
                    $"{item.weight},{item.maxStackSize},{item.gridSize.x},{item.gridSize.y},{item.baseValue}");
            }
        }

        string path = EditorUtility.SaveFilePanel("Export to CSV", Application.dataPath,
            $"items_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv", "csv");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, csv.ToString());
            ShowStatus("Database exported to CSV!", Color.green);
        }
    }

    private void ValidateAllItems()
    {
        int issues = 0;
        importLog.Clear();

        if (itemDatabase != null)
        {
            foreach (var item in itemDatabase.GetAllItems())
            {
                if (string.IsNullOrEmpty(item.itemID))
                {
                    LogActivity($"⚠️ Item '{item.name}' has no ID");
                    issues++;
                }

                if (item.weight <= 0)
                {
                    LogActivity($"⚠️ Item '{item.displayName}' has invalid weight: {item.weight}");
                    issues++;
                }

                if (item.gridSize.x <= 0 || item.gridSize.y <= 0)
                {
                    LogActivity($"⚠️ Item '{item.displayName}' has invalid grid size");
                    issues++;
                }
            }
        }

        if (issues == 0)
        {
            ShowStatus("✅ All items validated successfully!", Color.green);
        }
        else
        {
            ShowStatus($"⚠️ Found {issues} validation issues", Color.yellow);
        }
    }

    private void ValidateAllRecipes()
    {
        int issues = 0;

        if (recipeDatabase != null)
        {
            foreach (var recipe in recipeDatabase.GetAllRecipes())
            {
                if (string.IsNullOrEmpty(recipe.recipeID))
                {
                    LogActivity($"⚠️ Recipe '{recipe.name}' has no ID");
                    issues++;
                }

                if (recipe.ingredients == null || recipe.ingredients.Length == 0)
                {
                    LogActivity($"⚠️ Recipe '{recipe.recipeName}' has no ingredients");
                    issues++;
                }

                if (recipe.outputs == null || recipe.outputs.Length == 0)
                {
                    LogActivity($"⚠️ Recipe '{recipe.recipeName}' has no outputs");
                    issues++;
                }
            }
        }

        if (issues == 0)
        {
            ShowStatus("✅ All recipes validated successfully!", Color.green);
        }
        else
        {
            ShowStatus($"⚠️ Found {issues} validation issues", Color.yellow);
        }
    }

    private void CheckRecipeDependencies()
    {
        int missing = 0;

        if (recipeDatabase != null && itemDatabase != null)
        {
            foreach (var recipe in recipeDatabase.GetAllRecipes())
            {
                if (recipe.ingredients != null)
                {
                    foreach (var ingredient in recipe.ingredients)
                    {
                        if (ingredient.specificItem == null && !string.IsNullOrEmpty(ingredient.name))
                        {
                            // Try to find by name
                            var item = itemDatabase.GetAllItems()
                                .FirstOrDefault(i => i.displayName == ingredient.name);

                            if (item == null)
                            {
                                LogActivity($"⚠️ Missing item '{ingredient.name}' in recipe '{recipe.recipeName}'");
                                missing++;
                            }
                        }
                    }
                }
            }
        }

        if (missing == 0)
        {
            ShowStatus("✅ All recipe dependencies satisfied!", Color.green);
        }
        else
        {
            ShowStatus($"⚠️ Found {missing} missing dependencies", Color.yellow);
        }
    }

    private void CleanDuplicateEntries()
    {
        int removed = 0;

        if (itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems();
            var seen = new HashSet<string>();
            var toRemove = new List<ItemDefinition>();

            foreach (var item in items)
            {
                if (seen.Contains(item.itemID))
                {
                    toRemove.Add(item);
                    removed++;
                }
                else
                {
                    seen.Add(item.itemID);
                }
            }

            foreach (var item in toRemove)
            {
                itemDatabase.RemoveItem(item);
            }
        }

        AssetDatabase.SaveAssets();
        UpdateStatistics();

        ShowStatus($"Removed {removed} duplicate entries", Color.green);
    }

    private void RebuildDatabases()
    {
        // Clear existing
        if (itemDatabase != null)
        {
            var items = itemDatabase.GetAllItems().ToList();
            foreach (var item in items)
            {
                itemDatabase.RemoveItem(item);
            }
        }

        if (recipeDatabase != null)
        {
            var recipes = recipeDatabase.GetAllRecipes().ToList();
            foreach (var recipe in recipes)
            {
                recipeDatabase.RemoveRecipe(recipe);
            }
        }

        // Rescan and populate
        ScanAndPopulateItemDatabase();
        ScanAndPopulateRecipeDatabase();

        UpdateStatistics();

        ShowStatus("Databases rebuilt successfully!", Color.green);
    }

    [System.Serializable]
    public class InventoryGridSaveData
    {
        public List<SavedGridItem> savedItems = new List<SavedGridItem>();
        public int gridWidth = 8;
        public int gridHeight = 10;
        public float maxWeight = 50f;

        [System.Serializable]
        public class SavedGridItem
        {
            public string itemID;
            public Vector2Int gridPosition;
            public bool isRotated;
            public int stackSize;
            public string uniqueId;
        }
    }

    public enum CraftingCategory
    {
        Tools,
        Weapons,
        Clothing,
        Building,
        Cooking,
        Medicine,
        Processing,
        Advanced
    }

}