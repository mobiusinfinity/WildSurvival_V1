using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Main UI controller for the journal system.
/// Manages the journal window, entry display, and user interactions.
/// </summary>
public class JournalUI : MonoBehaviour
{
    #region Configuration
    [Header("UI References")]
    [SerializeField] private GameObject journalWindow;
    [SerializeField] private Transform tabContainer;
    [SerializeField] private Transform entryListContainer;
    [SerializeField] private Transform detailPanelContainer;
    [SerializeField] private GameObject entryListItemPrefab;
    [SerializeField] private GameObject objectiveItemPrefab;

    [Header("Window Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Toggle showCompletedToggle;
    [SerializeField] private Toggle showHiddenToggle;

    [Header("Entry List")]
    [SerializeField] private ScrollRect entryListScrollRect;
    [SerializeField] private TextMeshProUGUI categoryTitle;
    [SerializeField] private TextMeshProUGUI entryCountText;

    [Header("Detail Panel")]
    [SerializeField] private TextMeshProUGUI entryTitle;
    [SerializeField] private TextMeshProUGUI entryDescription;
    [SerializeField] private Image entryIcon;
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private GameObject rewardsPanel;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private Button trackQuestButton;
    [SerializeField] private Button abandonQuestButton;

    [Header("Quest Tracker")]
    [SerializeField] private GameObject questTrackerPanel;
    [SerializeField] private Transform trackedQuestsContainer;
    [SerializeField] private GameObject trackedQuestItemPrefab;
    [SerializeField] private int maxTrackedQuests = 3;

    [Header("Notifications")]
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private float notificationDuration = 3f;

    [Header("Visual Settings")]
    [SerializeField] private Color readEntryColor = Color.white;
    [SerializeField] private Color unreadEntryColor = Color.yellow;
    [SerializeField] private Color completedEntryColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color importantEntryColor = new Color(1f, 0.8f, 0f, 1f);
    #endregion

    #region State
    private JournalManager journalManager;
    private JournalCategory currentCategory = JournalCategory.Quest;
    private JournalEntry currentEntry;
    private List<GameObject> entryListItems = new List<GameObject>();
    private List<GameObject> objectiveItems = new List<GameObject>();
    private List<GameObject> trackedQuestItems = new List<GameObject>();
    private Dictionary<JournalTab, JournalCategory> tabs = new Dictionary<JournalTab, JournalCategory>();
    private bool isOpen = false;
    private bool showCompleted = false;
    private bool showHidden = false;
    private string currentSearchTerm = "";
    #endregion

    #region Properties
    public bool IsOpen => isOpen;
    public JournalCategory CurrentCategory => currentCategory;
    public JournalEntry CurrentEntry => currentEntry;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        journalManager = JournalManager.Instance;
        InitializeUI();
    }

    private void Start()
    {
        // Start with journal closed
        CloseJournal();
        UpdateQuestTracker();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        HandleInput();
    }
    #endregion

    #region Initialization
    private void InitializeUI()
    {
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseJournal);

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(ToggleMinimize);

        if (trackQuestButton != null)
            trackQuestButton.onClick.AddListener(OnTrackQuestClicked);

        if (abandonQuestButton != null)
            abandonQuestButton.onClick.AddListener(OnAbandonQuestClicked);

        // Setup toggles
        if (showCompletedToggle != null)
        {
            showCompletedToggle.onValueChanged.AddListener(OnShowCompletedChanged);
            showCompleted = showCompletedToggle.isOn;
        }

        if (showHiddenToggle != null)
        {
            showHiddenToggle.onValueChanged.AddListener(OnShowHiddenChanged);
            showHidden = showHiddenToggle.isOn;
        }

        // Setup search
        if (searchField != null)
        {
            searchField.onValueChanged.AddListener(OnSearchChanged);
        }

        // Initialize tabs
        InitializeTabs();

        // Hide detail panel initially
        if (detailPanelContainer != null)
            detailPanelContainer.gameObject.SetActive(false);
    }

    private void InitializeTabs()
    {
        var tabComponents = tabContainer.GetComponentsInChildren<JournalTab>();

        foreach (var tab in tabComponents)
        {
            tabs[tab] = tab.Category;

            // Subscribe to tab events
            tab.OnCategorySelected += OnCategorySelected;
        }
    }

    private void SubscribeToEvents()
    {
        JournalManager.OnEntryAdded += OnEntryAdded;
        JournalManager.OnEntryUpdated += OnEntryUpdated;
        JournalManager.OnEntryCompleted += OnEntryCompleted;
        JournalManager.OnJournalUpdated += RefreshUI;
        JournalTab.OnTabSelected += OnTabSelected;
    }

    private void UnsubscribeFromEvents()
    {
        JournalManager.OnEntryAdded -= OnEntryAdded;
        JournalManager.OnEntryUpdated -= OnEntryUpdated;
        JournalManager.OnEntryCompleted -= OnEntryCompleted;
        JournalManager.OnJournalUpdated -= RefreshUI;
        JournalTab.OnTabSelected -= OnTabSelected;
    }
    #endregion

    #region Public Methods
    public void OpenJournal()
    {
        if (isOpen) return;

        isOpen = true;
        journalWindow.SetActive(true);

        // Pause game
        Time.timeScale = 0f;

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void CloseJournal()
    {
        if (!isOpen) return;

        isOpen = false;
        journalWindow.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleJournal()
    {
        if (isOpen)
            CloseJournal();
        else
            OpenJournal();
    }

    public void ShowCategory(JournalCategory category)
    {
        currentCategory = category;
        RefreshEntryList();

        // Update category title
        if (categoryTitle != null)
        {
            categoryTitle.text = category.ToString();
        }
    }

    public void DisplayEntry(JournalEntry entry)
    {
        if (entry == null) return;

        currentEntry = entry;

        // Mark as read
        entry.MarkAsRead();

        // Show detail panel
        if (detailPanelContainer != null)
            detailPanelContainer.gameObject.SetActive(true);

        // Update title
        if (entryTitle != null)
            entryTitle.text = entry.Title;

        // Update description
        if (entryDescription != null)
            entryDescription.text = entry.GetFormattedDescription();

        // Update icon
        if (entryIcon != null && entry.Icon != null)
            entryIcon.sprite = entry.Icon;

        // Update objectives for quests
        if (entry.IsQuest)
        {
            DisplayObjectives(entry);
            DisplayRewards(entry);

            // Show quest buttons
            if (trackQuestButton != null)
            {
                trackQuestButton.gameObject.SetActive(entry.IsActive);
                bool isTracked = journalManager.TrackedQuests.Contains(entry);
                trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text =
                    isTracked ? "Untrack" : "Track";
            }

            if (abandonQuestButton != null)
            {
                abandonQuestButton.gameObject.SetActive(entry.IsActive);
            }
        }
        else
        {
            // Hide quest-specific UI
            if (objectivesContainer != null)
                objectivesContainer.gameObject.SetActive(false);

            if (rewardsPanel != null)
                rewardsPanel.SetActive(false);

            if (trackQuestButton != null)
                trackQuestButton.gameObject.SetActive(false);

            if (abandonQuestButton != null)
                abandonQuestButton.gameObject.SetActive(false);
        }

        // Update recipe info
        if (entry.IsRecipe)
        {
            DisplayRecipeInfo(entry);
        }
    }

    public void ShowNotification(string message, float duration = 0f)
    {
        if (notificationPrefab == null || notificationContainer == null)
            return;

        if (duration <= 0)
            duration = notificationDuration;

        GameObject notification = Instantiate(notificationPrefab, notificationContainer);
        TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
            text.text = message;

        // Auto-destroy after duration
        Destroy(notification, duration);
    }
    #endregion

    #region Private Methods - UI Updates
    private void RefreshUI()
    {
        RefreshEntryList();
        UpdateQuestTracker();
        UpdateTabNotifications();
    }

    private void RefreshEntryList()
    {
        // Clear existing items
        foreach (var item in entryListItems)
        {
            Destroy(item);
        }
        entryListItems.Clear();

        // Get entries for current category
        List<JournalEntry> entries = GetFilteredEntries();

        // Create list items
        foreach (var entry in entries)
        {
            CreateEntryListItem(entry);
        }

        // Update count
        if (entryCountText != null)
        {
            int total = journalManager.GetEntriesByCategory(currentCategory).Count;
            entryCountText.text = $"{entries.Count} / {total}";
        }
    }

    private List<JournalEntry> GetFilteredEntries()
    {
        var entries = journalManager.GetEntriesByCategory(currentCategory);

        // Apply filters
        if (!showCompleted)
        {
            entries = entries.Where(e => !e.IsCompleted).ToList();
        }

        if (!showHidden)
        {
            entries = entries.Where(e => !e.IsHidden).ToList();
        }

        // Apply search
        if (!string.IsNullOrEmpty(currentSearchTerm))
        {
            entries = entries.Where(e =>
                e.Title.ToLower().Contains(currentSearchTerm.ToLower()) ||
                e.Description.ToLower().Contains(currentSearchTerm.ToLower())
            ).ToList();
        }

        // Sort
        entries = entries.OrderBy(e => e.IsCompleted)
                        .ThenBy(e => e.IsRead)
                        .ThenByDescending(e => e.IsImportant)
                        .ThenBy(e => e.Title)
                        .ToList();

        return entries;
    }

    private void CreateEntryListItem(JournalEntry entry)
    {
        if (entryListItemPrefab == null || entryListContainer == null)
            return;

        GameObject item = Instantiate(entryListItemPrefab, entryListContainer);
        entryListItems.Add(item);

        // Setup button
        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => DisplayEntry(entry));
        }

        // Setup text
        TextMeshProUGUI titleText = item.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = entry.Title;

            // Color based on state
            if (!entry.IsRead)
                titleText.color = unreadEntryColor;
            else if (entry.IsCompleted)
                titleText.color = completedEntryColor;
            else if (entry.IsImportant)
                titleText.color = importantEntryColor;
            else
                titleText.color = readEntryColor;
        }

        // Setup icon
        Image icon = item.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && entry.Icon != null)
        {
            icon.sprite = entry.Icon;
        }

        // Show completion percentage for quests
        if (entry.IsQuest)
        {
            TextMeshProUGUI progressText = item.transform.Find("Progress")?.GetComponent<TextMeshProUGUI>();
            if (progressText != null)
            {
                progressText.text = $"{entry.GetCompletionPercentage():F0}%";
            }
        }
    }

    private void DisplayObjectives(JournalEntry entry)
    {
        if (objectivesContainer == null)
            return;

        objectivesContainer.gameObject.SetActive(true);

        // Clear existing objectives
        foreach (var item in objectiveItems)
        {
            Destroy(item);
        }
        objectiveItems.Clear();

        // Create objective items
        foreach (var objective in entry.Objectives)
        {
            if (objective.IsHidden && !objective.IsCompleted)
                continue;

            GameObject item = objectiveItemPrefab != null ?
                Instantiate(objectiveItemPrefab, objectivesContainer) :
                new GameObject("Objective");

            objectiveItems.Add(item);

            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null)
            {
                text = item.AddComponent<TextMeshProUGUI>();
            }

            string checkbox = objective.IsCompleted ? "☑" : "☐";
            string progressText = objective.GetProgressText();
            text.text = $"{checkbox} {objective.Description} {progressText}";

            if (objective.IsCompleted)
            {
                text.fontStyle = FontStyles.Strikethrough;
                text.color = completedEntryColor;
            }
            else if (objective.IsOptional)
            {
                text.fontStyle = FontStyles.Italic;
            }
        }
    }

    private void DisplayRewards(JournalEntry entry)
    {
        if (rewardsPanel == null || rewardsContainer == null)
            return;

        bool hasRewards = entry.ExperienceReward > 0 || entry.ItemRewards.Count > 0;
        rewardsPanel.SetActive(hasRewards);

        if (!hasRewards)
            return;

        // Clear existing rewards
        foreach (Transform child in rewardsContainer)
        {
            Destroy(child.gameObject);
        }

        // Show XP reward
        if (entry.ExperienceReward > 0)
        {
            GameObject xpItem = new GameObject("XP Reward");
            xpItem.transform.SetParent(rewardsContainer);
            TextMeshProUGUI xpText = xpItem.AddComponent<TextMeshProUGUI>();
            xpText.text = $"+{entry.ExperienceReward} XP";
        }

        // Show item rewards
        foreach (var reward in entry.ItemRewards)
        {
            GameObject itemReward = new GameObject("Item Reward");
            itemReward.transform.SetParent(rewardsContainer);
            TextMeshProUGUI itemText = itemReward.AddComponent<TextMeshProUGUI>();
            itemText.text = $"{reward.Quantity}x {reward.ItemID}";
        }
    }

    private void DisplayRecipeInfo(JournalEntry entry)
    {
        // TODO: Display recipe ingredients and crafting info
    }

    private void UpdateQuestTracker()
    {
        if (questTrackerPanel == null || trackedQuestsContainer == null)
            return;

        // Clear existing tracked items
        foreach (var item in trackedQuestItems)
        {
            Destroy(item);
        }
        trackedQuestItems.Clear();

        // Get tracked quests
        var trackedQuests = journalManager.TrackedQuests.Take(maxTrackedQuests);

        // Show/hide tracker panel
        questTrackerPanel.SetActive(trackedQuests.Any());

        // Create tracked quest items
        foreach (var quest in trackedQuests)
        {
            CreateTrackedQuestItem(quest);
        }
    }

    private void CreateTrackedQuestItem(JournalEntry quest)
    {
        if (trackedQuestItemPrefab == null || trackedQuestsContainer == null)
            return;

        GameObject item = Instantiate(trackedQuestItemPrefab, trackedQuestsContainer);
        trackedQuestItems.Add(item);

        // Setup title
        TextMeshProUGUI titleText = item.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = quest.Title;
        }

        // Setup objectives
        Transform objContainer = item.transform.Find("Objectives");
        if (objContainer != null)
        {
            foreach (var objective in quest.Objectives)
            {
                if (objective.IsHidden && !objective.IsCompleted)
                    continue;

                GameObject objItem = new GameObject("Objective");
                objItem.transform.SetParent(objContainer);

                TextMeshProUGUI objText = objItem.AddComponent<TextMeshProUGUI>();
                objText.text = $"• {objective.Description} {objective.GetProgressText()}";
                objText.fontSize = 12;

                if (objective.IsCompleted)
                {
                    objText.color = completedEntryColor;
                }
            }
        }
    }

    private void UpdateTabNotifications()
    {
        foreach (var tab in tabs.Keys)
        {
            tab.RefreshContent();
        }
    }

    private void ToggleMinimize()
    {
        // TODO: Implement minimize functionality
    }
    #endregion

    #region Event Handlers
    private void HandleInput()
    {
        // Toggle journal with J key
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleJournal();
        }

        // Close with Escape
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseJournal();
        }
    }

    private void OnCategorySelected(JournalCategory category)
    {
        ShowCategory(category);
    }

    private void OnTabSelected(JournalTab tab)
    {
        ShowCategory(tab.Category);
    }

    private void OnSearchChanged(string searchTerm)
    {
        currentSearchTerm = searchTerm;
        RefreshEntryList();
    }

    private void OnShowCompletedChanged(bool show)
    {
        showCompleted = show;
        RefreshEntryList();
    }

    private void OnShowHiddenChanged(bool show)
    {
        showHidden = show;
        RefreshEntryList();
    }

    private void OnTrackQuestClicked()
    {
        if (currentEntry == null || !currentEntry.IsQuest)
            return;

        if (journalManager.TrackedQuests.Contains(currentEntry))
        {
            journalManager.UntrackQuest(currentEntry);
        }
        else
        {
            journalManager.TrackQuest(currentEntry);
        }

        // Update button text
        if (trackQuestButton != null)
        {
            bool isTracked = journalManager.TrackedQuests.Contains(currentEntry);
            trackQuestButton.GetComponentInChildren<TextMeshProUGUI>().text =
                isTracked ? "Untrack" : "Track";
        }

        UpdateQuestTracker();
    }

    private void OnAbandonQuestClicked()
    {
        if (currentEntry == null || !currentEntry.IsQuest)
            return;

        // Show confirmation dialog
        // TODO: Implement confirmation dialog

        journalManager.AbandonQuest(currentEntry.EntryID);
        RefreshUI();
    }

    private void OnEntryAdded(JournalEntry entry)
    {
        ShowNotification($"New {entry.Category}: {entry.Title}");
        RefreshUI();
    }

    private void OnEntryUpdated(JournalEntry entry)
    {
        if (currentEntry == entry)
        {
            DisplayEntry(entry);
        }
        RefreshUI();
    }

    private void OnEntryCompleted(JournalEntry entry)
    {
        ShowNotification($"Completed: {entry.Title}");
        RefreshUI();
    }
    #endregion
}