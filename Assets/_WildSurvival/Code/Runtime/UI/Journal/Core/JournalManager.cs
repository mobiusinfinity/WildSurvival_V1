using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central manager for the journal system.
/// Handles entry management, quest tracking, and save/load operations.
/// </summary>
public class JournalManager : MonoBehaviour
{
    #region Singleton
    private static JournalManager instance;
    public static JournalManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JournalManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("JournalManager");
                    instance = go.AddComponent<JournalManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    #region Events
    public static event Action<JournalEntry> OnEntryAdded;
    public static event Action<JournalEntry> OnEntryUpdated;
    public static event Action<JournalEntry> OnEntryCompleted;
    public static event Action<JournalEntry> OnQuestStarted;
    public static event Action<JournalEntry> OnQuestCompleted;
    public static event Action<string> OnObjectiveCompleted;
    public static event Action OnJournalUpdated;
    #endregion

    #region Configuration
    [Header("Journal Configuration")]
    [SerializeField] private int maxActiveQuests = 5;
    [SerializeField] private bool autoTrackNewQuests = true;
    [SerializeField] private bool showNotifications = true;
    [SerializeField] private float notificationDuration = 3f;

    [Header("Starting Entries")]
    [SerializeField] private List<JournalEntry> startingEntries = new List<JournalEntry>();

    [Header("Entry Prefabs")]
    [SerializeField] private List<JournalEntry> availableQuests = new List<JournalEntry>();
    [SerializeField] private List<JournalEntry> availableDiscoveries = new List<JournalEntry>();
    [SerializeField] private List<JournalEntry> availableRecipes = new List<JournalEntry>();
    #endregion

    #region State
    private Dictionary<string, JournalEntry> allEntries = new Dictionary<string, JournalEntry>();
    private List<JournalEntry> activeQuests = new List<JournalEntry>();
    private List<JournalEntry> completedQuests = new List<JournalEntry>();
    private List<JournalEntry> discoveries = new List<JournalEntry>();
    private List<JournalEntry> recipes = new List<JournalEntry>();
    private List<JournalEntry> trackedQuests = new List<JournalEntry>();

    private int totalQuestsCompleted = 0;
    private int totalDiscoveries = 0;
    private int totalRecipesUnlocked = 0;
    #endregion

    #region Properties
    public Dictionary<string, JournalEntry> AllEntries => allEntries;
    public List<JournalEntry> ActiveQuests => activeQuests;
    public List<JournalEntry> CompletedQuests => completedQuests;
    public List<JournalEntry> Discoveries => discoveries;
    public List<JournalEntry> Recipes => recipes;
    public List<JournalEntry> TrackedQuests => trackedQuests;

    public int TotalQuestsCompleted => totalQuestsCompleted;
    public int TotalDiscoveries => totalDiscoveries;
    public int TotalRecipesUnlocked => totalRecipesUnlocked;

    public bool HasActiveQuests => activeQuests.Count > 0;
    public bool CanAcceptMoreQuests => activeQuests.Count < maxActiveQuests;
    public int ActiveQuestCount => activeQuests.Count;
    public int UnreadEntryCount => allEntries.Values.Count(e => !e.IsRead);
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeJournal();
    }

    private void Start()
    {
        AddStartingEntries();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    #endregion

    #region Initialization
    private void InitializeJournal()
    {
        allEntries = new Dictionary<string, JournalEntry>();
        activeQuests = new List<JournalEntry>();
        completedQuests = new List<JournalEntry>();
        discoveries = new List<JournalEntry>();
        recipes = new List<JournalEntry>();
        trackedQuests = new List<JournalEntry>();
    }

    private void AddStartingEntries()
    {
        foreach (var entry in startingEntries)
        {
            if (entry != null)
            {
                AddEntry(entry);
            }
        }
    }
    #endregion

    #region Public Methods - Entry Management
    public JournalEntry AddEntry(JournalEntry entry)
    {
        if (entry == null || allEntries.ContainsKey(entry.EntryID))
            return null;

        // Create a copy of the entry
        JournalEntry newEntry = new JournalEntry(
            entry.EntryID,
            entry.Title,
            entry.Description,
            entry.Category
        );

        // Copy other properties
        foreach (var obj in entry.Objectives)
        {
            newEntry.Objectives.Add(new QuestObjective(obj.Description, obj.TargetCount, obj.IsOptional));
        }
        foreach (var reward in entry.ItemRewards)
        {
            newEntry.ItemRewards.Add(new ItemReward(reward.ItemID, reward.Quantity, reward.Chance));
        }
        foreach (var req in entry.Ingredients)
        {
            newEntry.Ingredients.Add(new ItemRequirement(req.ItemID, req.Quantity, req.ConsumeItem));
        }

        // Add to main dictionary
        allEntries[newEntry.EntryID] = newEntry;

        // Categorize the entry
        switch (newEntry.Category)
        {
            case JournalCategory.Quest:
                if (newEntry.Status == QuestStatus.Active)
                {
                    if (activeQuests.Count >= maxActiveQuests)
                    {
                        Debug.LogWarning($"Cannot accept quest '{newEntry.Title}' - quest limit reached!");
                        return null;
                    }
                    activeQuests.Add(newEntry);
                    if (autoTrackNewQuests)
                    {
                        TrackQuest(newEntry);
                    }
                    OnQuestStarted?.Invoke(newEntry);
                }
                else if (newEntry.Status == QuestStatus.Completed)
                {
                    completedQuests.Add(newEntry);
                }
                break;

            case JournalCategory.Discovery:
                discoveries.Add(newEntry);
                totalDiscoveries++;
                break;

            case JournalCategory.Recipe:
                recipes.Add(newEntry);
                totalRecipesUnlocked++;
                break;
        }

        // Fire events
        OnEntryAdded?.Invoke(newEntry);
        OnJournalUpdated?.Invoke();

        // Show notification
        if (showNotifications)
        {
            ShowNotification($"New {newEntry.Category}: {newEntry.Title}");
        }

        return newEntry;
    }

    public void RemoveEntry(string entryID)
    {
        if (!allEntries.ContainsKey(entryID))
            return;

        JournalEntry entry = allEntries[entryID];
        allEntries.Remove(entryID);

        // Remove from category lists
        activeQuests.Remove(entry);
        completedQuests.Remove(entry);
        discoveries.Remove(entry);
        recipes.Remove(entry);
        trackedQuests.Remove(entry);

        OnJournalUpdated?.Invoke();
    }

    public JournalEntry GetEntry(string entryID)
    {
        return allEntries.TryGetValue(entryID, out JournalEntry entry) ? entry : null;
    }

    public List<JournalEntry> GetEntriesByCategory(JournalCategory category)
    {
        return allEntries.Values.Where(e => e.Category == category).ToList();
    }

    public List<JournalEntry> GetEntriesByTag(string tag)
    {
        return allEntries.Values.Where(e => e.HasTag(tag)).ToList();
    }

    public List<JournalEntry> SearchEntries(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return new List<JournalEntry>();

        string term = searchTerm.ToLower();
        return allEntries.Values.Where(e =>
            e.Title.ToLower().Contains(term) ||
            e.Description.ToLower().Contains(term) ||
            e.HasTag(term)
        ).ToList();
    }
    #endregion

    #region Public Methods - Quest Management
    public bool StartQuest(string questID)
    {
        var entry = GetEntry(questID);
        if (entry == null || !entry.IsQuest)
            return false;

        if (!CanAcceptMoreQuests)
        {
            Debug.LogWarning("Cannot accept more quests - limit reached!");
            return false;
        }

        entry.SetQuestStatus(QuestStatus.Active);

        if (!activeQuests.Contains(entry))
            activeQuests.Add(entry);

        OnQuestStarted?.Invoke(entry);
        OnEntryUpdated?.Invoke(entry);
        OnJournalUpdated?.Invoke();

        if (showNotifications)
        {
            ShowNotification($"Quest Started: {entry.Title}");
        }

        return true;
    }

    public void CompleteQuest(string questID)
    {
        var entry = GetEntry(questID);
        if (entry == null || !entry.IsQuest)
            return;

        entry.SetQuestStatus(QuestStatus.Completed);

        activeQuests.Remove(entry);
        trackedQuests.Remove(entry);

        if (!completedQuests.Contains(entry))
        {
            completedQuests.Add(entry);
            totalQuestsCompleted++;
        }

        OnQuestCompleted?.Invoke(entry);
        OnEntryCompleted?.Invoke(entry);
        OnJournalUpdated?.Invoke();

        if (showNotifications)
        {
            ShowNotification($"Quest Completed: {entry.Title}");
        }

        // Grant rewards
        GrantQuestRewards(entry);
    }

    public void AbandonQuest(string questID)
    {
        var entry = GetEntry(questID);
        if (entry == null || !entry.IsQuest)
            return;

        entry.SetQuestStatus(QuestStatus.Abandoned);
        activeQuests.Remove(entry);
        trackedQuests.Remove(entry);

        OnEntryUpdated?.Invoke(entry);
        OnJournalUpdated?.Invoke();
    }

    public void UpdateQuestObjective(string questID, int objectiveIndex, int progress)
    {
        var entry = GetEntry(questID);
        if (entry == null || !entry.IsQuest)
            return;

        entry.UpdateObjective(objectiveIndex, progress);

        // Check if quest is ready to turn in
        if (entry.Status == QuestStatus.ReadyToTurnIn)
        {
            if (showNotifications)
            {
                ShowNotification($"Quest Ready: {entry.Title}");
            }
        }

        OnEntryUpdated?.Invoke(entry);
        OnJournalUpdated?.Invoke();
    }

    public void TrackQuest(JournalEntry quest)
    {
        if (quest == null || !quest.IsQuest || !quest.IsActive)
            return;

        if (!trackedQuests.Contains(quest))
        {
            trackedQuests.Add(quest);
            OnJournalUpdated?.Invoke();
        }
    }

    public void UntrackQuest(JournalEntry quest)
    {
        if (trackedQuests.Remove(quest))
        {
            OnJournalUpdated?.Invoke();
        }
    }
    #endregion

    #region Public Methods - Discovery & Recipes
    public void UnlockDiscovery(string discoveryID)
    {
        var discovery = availableDiscoveries.FirstOrDefault(d => d.EntryID == discoveryID);
        if (discovery != null)
        {
            AddEntry(discovery);
        }
    }

    public void UnlockRecipe(string recipeID)
    {
        var recipe = availableRecipes.FirstOrDefault(r => r.EntryID == recipeID);
        if (recipe != null)
        {
            AddEntry(recipe);
        }
    }

    public bool HasRecipe(string recipeID)
    {
        return recipes.Any(r => r.EntryID == recipeID);
    }

    public bool HasDiscovery(string discoveryID)
    {
        return discoveries.Any(d => d.EntryID == discoveryID);
    }
    #endregion

    #region Private Methods
    private void GrantQuestRewards(JournalEntry quest)
    {
        if (quest == null || !quest.IsQuest)
            return;

        // Grant experience
        if (quest.ExperienceReward > 0)
        {
            // TODO: Add to player experience system
            Debug.Log($"Granted {quest.ExperienceReward} XP");
        }

        // Grant items
        foreach (var reward in quest.ItemRewards)
        {
            if (UnityEngine.Random.value <= reward.Chance)
            {
                // TODO: Add to inventory
                Debug.Log($"Granted {reward.Quantity}x {reward.ItemID}");
            }
        }
    }

    private void ShowNotification(string message)
    {
        // TODO: Implement UI notification system
        Debug.Log($"[Journal] {message}");
    }
    #endregion

    #region Save/Load
    public JournalSaveData GetSaveData()
    {
        var saveData = new JournalSaveData();

        foreach (var entry in allEntries.Values)
        {
            saveData.entries.Add(new EntrySaveData
            {
                entryID = entry.EntryID,
                isRead = entry.IsRead,
                status = entry.Status,
                objectiveProgress = entry.Objectives.Select(o => o.CurrentCount).ToList()
            });
        }

        saveData.trackedQuestIDs = trackedQuests.Select(q => q.EntryID).ToList();
        saveData.totalQuestsCompleted = totalQuestsCompleted;
        saveData.totalDiscoveries = totalDiscoveries;
        saveData.totalRecipesUnlocked = totalRecipesUnlocked;

        return saveData;
    }

    public void LoadSaveData(JournalSaveData saveData)
    {
        if (saveData == null)
            return;

        InitializeJournal();

        // Load statistics
        totalQuestsCompleted = saveData.totalQuestsCompleted;
        totalDiscoveries = saveData.totalDiscoveries;
        totalRecipesUnlocked = saveData.totalRecipesUnlocked;

        // Reload entries with saved progress
        foreach (var entrySave in saveData.entries)
        {
            // Find the original entry template
            JournalEntry template = FindEntryTemplate(entrySave.entryID);
            if (template != null)
            {
                var entry = AddEntry(template);
                if (entry != null)
                {
                    // Restore progress
                    entry.SetQuestStatus(entrySave.status);
                    if (entrySave.isRead)
                        entry.MarkAsRead();

                    // Restore objective progress
                    for (int i = 0; i < entrySave.objectiveProgress.Count && i < entry.Objectives.Count; i++)
                    {
                        entry.UpdateObjective(i, entrySave.objectiveProgress[i]);
                    }
                }
            }
        }

        // Restore tracked quests
        foreach (var questID in saveData.trackedQuestIDs)
        {
            var quest = GetEntry(questID);
            if (quest != null)
            {
                TrackQuest(quest);
            }
        }

        OnJournalUpdated?.Invoke();
    }

    private JournalEntry FindEntryTemplate(string entryID)
    {
        // Search in all available entry lists
        var allTemplates = new List<JournalEntry>();
        allTemplates.AddRange(startingEntries);
        allTemplates.AddRange(availableQuests);
        allTemplates.AddRange(availableDiscoveries);
        allTemplates.AddRange(availableRecipes);

        return allTemplates.FirstOrDefault(e => e != null && e.EntryID == entryID);
    }
    #endregion
}

#region Save Data
[System.Serializable]
public class JournalSaveData
{
    public List<EntrySaveData> entries = new List<EntrySaveData>();
    public List<string> trackedQuestIDs = new List<string>();
    public int totalQuestsCompleted;
    public int totalDiscoveries;
    public int totalRecipesUnlocked;
}

[System.Serializable]
public class EntrySaveData
{
    public string entryID;
    public bool isRead;
    public QuestStatus status;
    public List<int> objectiveProgress = new List<int>();
}
#endregion