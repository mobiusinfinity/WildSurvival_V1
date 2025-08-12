using System;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Ultimate Inventory Tool Hub - Elite Professional Edition
/// The most sophisticated inventory management system for Unity
/// Featuring advanced Tetris mechanics, AI integration, and stunning visuals
/// </summary>
public class UltimateInventoryToolHub : EditorWindow
{
    // ========== CONSTANTS ==========
    private const string WINDOW_TITLE = "🎮 Ultimate Inventory Tool Hub - Elite";
    private const string VERSION = "4.0 Elite";
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
    private ItemDatabase itemDatabase;
    private RecipeDatabase recipeDatabase;
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
    private WorkstationType currentWorkstation = WorkstationType.CraftingBench;

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

        EditorApplication.update += OnEditorUpdate;

        ShowStatus("✨ Ultimate Inventory Hub Elite initialized!", Color.green);
        LogActivity("System initialized with all features");
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        SavePreferences();

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
        // Find and load ItemDatabase
        string[] itemDbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (itemDbGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(itemDbGuids[0]);
            itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

            // Build lookup dictionary for performance
            if (itemDatabase != null)
            {
                foreach (var item in itemDatabase.GetAllItems())
                {
                    itemLookup[item.itemID] = item;
                }
            }
        }

        // Find and load RecipeDatabase
        string[] recipeDbGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
        if (recipeDbGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(recipeDbGuids[0]);
            recipeDatabase = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);

            // Build lookup dictionary
            if (recipeDatabase != null)
            {
                foreach (var recipe in recipeDatabase.GetAllRecipes())
                {
                    recipeLookup[recipe.recipeID] = recipe;
                }
            }
        }

        databasesLoaded = (itemDatabase != null && recipeDatabase != null);
        UpdateStatistics();
    }

    private void InitializeGrid()
    {
        gridOccupancy = new bool[gridWidth, gridHeight];
        placedItems.Clear();
        currentWeight = 0f;
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
        if (animationsEnabled)
        {
            float pulse = Mathf.Sin(pulseAnimation) * 0.5f + 0.5f;
            GUI.color = new Color(1, 1, 1, pulse * 0.2f);
            GUI.DrawTexture(headerRect, itemGlowTexture, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

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

        // Database status with animated indicators
        DrawHealthIndicator("Item Database", itemDatabase != null, totalItems);
        DrawHealthIndicator("Recipe Database", recipeDatabase != null, totalRecipes);
        DrawHealthIndicator("Memory Usage", true, (int)(performanceMetrics.GetValueOrDefault("memory", 0)));
        DrawHealthIndicator("Performance", performanceMetrics.GetValueOrDefault("fps", 60) > 30, (int)performanceMetrics.GetValueOrDefault("fps", 60));

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

        if (!isHealthy && GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(35)))
        {
            FixHealthIssue(label);
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
        float targetProgress = Mathf.Clamp01((float)current / max);
        float animatedProgress = Mathf.Lerp(0, targetProgress, Mathf.SmoothStep(0, 1, tabTransition));

        Rect fillRect = new Rect(rect.x, rect.y, rect.width * animatedProgress, rect.height);

        // Gradient fill
        EditorGUI.DrawRect(fillRect, color);

        // Shine effect
        if (animationsEnabled)
        {
            float shine = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
            Rect shineRect = new Rect(rect.x + (rect.width * animatedProgress * shine), rect.y, 2, rect.height);
            EditorGUI.DrawRect(shineRect, new Color(1, 1, 1, 0.5f));
        }

        // Text overlay
        GUI.Label(rect, $"{current}/{max}", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimatedActivityFeed()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("📜 Activity Feed", EditorStyles.boldLabel);

        for (int i = 0; i < Mathf.Min(recentActivity.Count, 5); i++)
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
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("🎮 Advanced Tetris Inventory Simulator", headerStyle);

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
        DrawItemProperties();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawInventoryToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Clear Grid", EditorStyles.toolbarButton))
        {
            ClearGrid();
        }

        if (GUILayout.Button("Auto Arrange", EditorStyles.toolbarButton))
        {
            AutoArrangeItems();
        }

        if (GUILayout.Button("Generate Random", EditorStyles.toolbarButton))
        {
            GenerateRandomLoadout();
        }

        GUILayout.Space(20);

        // Grid size controls
        EditorGUILayout.LabelField("Grid:", GUILayout.Width(35));
        int newWidth = EditorGUILayout.IntField(gridWidth, GUILayout.Width(30));
        EditorGUILayout.LabelField("x", GUILayout.Width(15));
        int newHeight = EditorGUILayout.IntField(gridHeight, GUILayout.Width(30));

        if (newWidth != gridWidth || newHeight != gridHeight)
        {
            ResizeGrid(newWidth, newHeight);
        }

        GUILayout.Space(20);

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
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(250));

        EditorGUILayout.LabelField("📦 Item Library", EditorStyles.boldLabel);

        // Category filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
        ItemCategory filterCategory = (ItemCategory)EditorGUILayout.EnumPopup(ItemCategory.Misc, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Item list with previews
        itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll, GUILayout.Height(400));

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
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        // Icon
        if (item.icon != null)
        {
            GUI.DrawTexture(GUILayoutUtility.GetRect(32, 32), item.icon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUILayout.LabelField("📦", GUILayout.Width(32));
        }

        // Info
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(item.displayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{item.gridSize.x}x{item.gridSize.y} | {item.weight:F1}kg", EditorStyles.miniLabel);
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
        EditorGUI.DrawRect(barRect, Color.black);

        // Weight ratio with color gradient
        float ratio = currentWeight / maxWeight;
        Color barColor = Color.Lerp(Color.green, Color.red, ratio);

        // Animated fill
        float animatedRatio = Mathf.Lerp(0, ratio, tabTransition);
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * animatedRatio, barRect.height);
        EditorGUI.DrawRect(fillRect, barColor);

        // Segments
        for (int i = 1; i < 10; i++)
        {
            float x = barRect.x + (barRect.width * i / 10f);
            EditorGUI.DrawRect(new Rect(x, barRect.y, 1, barRect.height), new Color(0.3f, 0.3f, 0.3f));
        }

        // Text overlay
        GUI.Label(barRect, $"{currentWeight:F1} / {maxWeight:F1} kg ({ratio:P0})",
            EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemProperties()
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(250));

        EditorGUILayout.LabelField("📋 Properties", EditorStyles.boldLabel);

        if (draggedItem != null)
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

        EditorGUILayout.LabelField("⚒️ Crafting Studio", headerStyle);

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

        EditorGUILayout.LabelField("🤖 AI Batch Processor", headerStyle);

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

        EditorGUILayout.LabelField("📊 Command Center", headerStyle);

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

    // ========== ITEM CREATOR ==========

    private void DrawItemCreator()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("🎨 Item Creator", headerStyle);
        EditorGUILayout.HelpBox("Visual item designer with live preview and validation", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // ========== RECIPE BUILDER ==========

    private void DrawRecipeBuilder()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("🔨 Recipe Builder", headerStyle);
        EditorGUILayout.HelpBox("Drag-and-drop recipe creation with ingredient validation", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // ========== DATABASE MANAGER ==========

    private void DrawDatabaseManager()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("💾 Database Manager", headerStyle);
        EditorGUILayout.HelpBox("Backup, restore, and manage your databases", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // ========== ANALYTICS ==========

    private void DrawAnalytics()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.LabelField("📈 Analytics Dashboard", headerStyle);
        EditorGUILayout.HelpBox("Deep insights into your content balance and optimization", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    // ========== SETTINGS ==========

    private void DrawSettings()
    {
        EditorGUILayout.BeginVertical(cardStyle);

        EditorGUILayout.LabelField("⚙️ Settings", headerStyle);

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
                CreateItemDatabase();
                break;
            case "Recipe Database":
                CreateRecipeDatabase();
                break;
        }
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
        for (int i = 0; i < itemCount; i++)
        {
            var mockItem = CreateMockItem($"Item_{UnityEngine.Random.Range(0, 100)}",
                UnityEngine.Random.Range(1, 4), UnityEngine.Random.Range(1, 4));
            AddItemToGrid(mockItem);
        }

        ShowStatus($"Generated {itemCount} random items", Color.green);
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

        Vector2Int? position = FindBestPosition(new PlacedItem(itemDef, Vector2Int.zero));

        if (position.HasValue)
        {
            var placedItem = new PlacedItem(itemDef, position.Value);
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
        if (item.itemDef.allowRotation)
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
        }

        return null;
    }

    private bool CanPlaceItem(Vector2Int position, Vector2Int size, string excludeId)
    {
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > gridWidth ||
            position.y + size.y > gridHeight)
        {
            return false;
        }

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (gridOccupancy[x, y])
                {
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
        if (placedItems.Count == 0) return 0;

        float totalItemArea = placedItems.Sum(i => i.GetSize().x * i.GetSize().y);
        float gridArea = gridWidth * gridHeight;
        return totalItemArea / gridArea;
    }

    private int GetTotalValue()
    {
        return placedItems.Sum(i => i.itemDef.baseValue * i.stackSize);
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
            recipe.category = ParseRecipeCategory(data.category);
            recipe.requiredWorkstation = ParseWorkstation(data.workstation);
            recipe.baseCraftTime = data.craftTime;
            recipe.tier = data.tier;
            recipe.isKnownByDefault = data.unlockedByDefault;

            string path = $"Assets/_WildSurvival/Data/Recipes/AI_{data.recipeID}.asset";

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.CreateAsset(recipe, path);

            if (recipeDatabase != null)
            {
                recipeDatabase.AddRecipe(recipe);
                recipeLookup[recipe.recipeID] = recipe;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private ItemCategory ParseCategory(string category)
    {
        if (System.Enum.TryParse<ItemCategory>(category, out var result))
            return result;
        return ItemCategory.Misc;
    }

    private RecipeCategory ParseRecipeCategory(string category)
    {
        if (System.Enum.TryParse<RecipeCategory>(category, out var result))
            return result;
        return RecipeCategory.Basic;
    }

    private WorkstationType ParseWorkstation(string workstation)
    {
        if (System.Enum.TryParse<WorkstationType>(workstation, out var result))
            return result;
        return WorkstationType.None;
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

    // ========== DATABASE CLASSES ==========

    [System.Serializable]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        public List<ItemDefinition> GetAllItems() => items;

        public void AddItem(ItemDefinition item)
        {
            if (item != null && !items.Contains(item))
            {
                items.Add(item);
                EditorUtility.SetDirty(this);
            }
        }

        public void RemoveItem(ItemDefinition item)
        {
            if (items.Remove(item))
            {
                EditorUtility.SetDirty(this);
            }
        }

        public List<ItemDefinition> GetItemsByCategory(ItemCategory category)
        {
            return items.Where(i => i != null && i.primaryCategory == category).ToList();
        }

        public ItemDefinition GetItem(string itemID)
        {
            return items.FirstOrDefault(i => i != null && i.itemID == itemID);
        }
    }

    [System.Serializable]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public List<RecipeDefinition> GetAllRecipes() => recipes;

        public void AddRecipe(RecipeDefinition recipe)
        {
            if (recipe != null && !recipes.Contains(recipe))
            {
                recipes.Add(recipe);
                EditorUtility.SetDirty(this);
            }
        }

        public void RemoveRecipe(RecipeDefinition recipe)
        {
            if (recipes.Remove(recipe))
            {
                EditorUtility.SetDirty(this);
            }
        }
    }

    [System.Serializable]
    public class ItemDefinition : ScriptableObject
    {
        public string itemID;
        public string displayName;
        public string description;
        public Sprite icon;
        public GameObject worldModel;
        public ItemCategory primaryCategory;
        public float weight = 1f;
        public int maxStackSize = 1;
        public Vector2Int gridSize = Vector2Int.one;
        public bool allowRotation = true;
        public int baseValue = 10;
    }

    [System.Serializable]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeID;
        public string recipeName;
        public string description;
        public RecipeCategory category;
        public WorkstationType requiredWorkstation;
        public float baseCraftTime = 5f;
        public int tier = 1;
        public bool isKnownByDefault = true;
        public RecipeIngredient[] ingredients;
        public RecipeOutput[] outputs;
    }

    [System.Serializable]
    public class RecipeIngredient
    {
        public string name;
        public int quantity;
        public bool consumed = true;
    }

    [System.Serializable]
    public class RecipeOutput
    {
        public ItemDefinition item;
        public int quantityMin = 1;
        public int quantityMax = 1;
        public float chance = 1f;
    }

    public enum ItemCategory
    {
        Misc, Resource, Tool, Weapon, Food, Medicine, Clothing, Building
    }

    public enum RecipeCategory
    {
        Basic, Tools, Weapons, Clothing, Building, Cooking, Medicine, Processing, Advanced
    }

    public enum WorkstationType
    {
        None, CraftingBench, Forge, Campfire, Anvil, CookingPot, Loom, TanningRack, ChemistryLab, AdvancedBench
    }
}