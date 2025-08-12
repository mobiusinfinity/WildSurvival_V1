using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// Data structure for journal entries including quests, discoveries, and recipes.
/// </summary>
[System.Serializable]
public class JournalEntry
{
    [Header("Basic Information")]
    [SerializeField] private string entryID;
    [SerializeField] private string title;
    [SerializeField] private string description;
    [SerializeField] private JournalCategory category;
    [SerializeField] private Sprite icon;
    [SerializeField] private DateTime dateDiscovered;
    [SerializeField] private bool isRead;
    [SerializeField] private bool isImportant;
    [SerializeField] private bool isHidden;

    [Header("Quest Data")]
    [SerializeField] private List<QuestObjective> objectives = new List<QuestObjective>();
    [SerializeField] private QuestStatus questStatus = QuestStatus.NotStarted;
    [SerializeField] private int experienceReward;
    [SerializeField] private List<ItemReward> itemRewards = new List<ItemReward>();
    [SerializeField] private string questGiver;
    [SerializeField] private string turnInNPC;

    [Header("Discovery Data")]
    [SerializeField] private DiscoveryType discoveryType;
    [SerializeField] private string locationFound;
    [SerializeField] private string biomeType;
    [SerializeField] private int discoveriesRequired;

    [Header("Recipe Data")]
    [SerializeField] private List<ItemRequirement> ingredients = new List<ItemRequirement>();
    [SerializeField] private string resultItemID;
    [SerializeField] private int resultQuantity = 1;
    [SerializeField] private float craftingTime = 1f;
    [SerializeField] private string craftingStation;
    [SerializeField] private int craftingLevel;

    [Header("Metadata")]
    [SerializeField] private List<string> tags = new List<string>();
    [SerializeField] private int sortOrder;
    [SerializeField] private string unlockCondition;

    #region Properties
    public string EntryID => entryID;
    public string Title => title;
    public string Description => description;
    public JournalCategory Category => category;
    public Sprite Icon => icon;
    public DateTime DateDiscovered => dateDiscovered;
    public bool IsRead => isRead;
    public bool IsImportant => isImportant;
    public bool IsHidden => isHidden;

    public List<QuestObjective> Objectives => objectives;
    public QuestStatus Status => questStatus;
    public int ExperienceReward => experienceReward;
    public List<ItemReward> ItemRewards => itemRewards;

    public DiscoveryType DiscoveryType => discoveryType;
    public string LocationFound => locationFound;

    public List<ItemRequirement> Ingredients => ingredients;
    public string ResultItemID => resultItemID;
    public int ResultQuantity => resultQuantity;
    public float CraftingTime => craftingTime;

    public bool IsCompleted => questStatus == QuestStatus.Completed;
    public bool IsActive => questStatus == QuestStatus.Active;
    public bool IsQuest => category == JournalCategory.Quest;
    public bool IsRecipe => category == JournalCategory.Recipe;
    public bool IsDiscovery => category == JournalCategory.Discovery;
    #endregion

    #region Constructors
    public JournalEntry()
    {
        entryID = System.Guid.NewGuid().ToString();
        dateDiscovered = DateTime.Now;
        objectives = new List<QuestObjective>();
        itemRewards = new List<ItemReward>();
        ingredients = new List<ItemRequirement>();
        tags = new List<string>();
    }

    public JournalEntry(string id, string title, string description, JournalCategory category)
    {
        this.entryID = id;
        this.title = title;
        this.description = description;
        this.category = category;
        this.dateDiscovered = DateTime.Now;

        objectives = new List<QuestObjective>();
        itemRewards = new List<ItemReward>();
        ingredients = new List<ItemRequirement>();
        tags = new List<string>();
    }
    #endregion

    #region Public Methods
    public void MarkAsRead()
    {
        isRead = true;
    }

    public void SetQuestStatus(QuestStatus newStatus)
    {
        questStatus = newStatus;

        if (newStatus == QuestStatus.Completed)
        {
            CompleteAllObjectives();
        }
    }

    public void UpdateObjective(int objectiveIndex, int progress)
    {
        if (objectiveIndex >= 0 && objectiveIndex < objectives.Count)
        {
            objectives[objectiveIndex].UpdateProgress(progress);
            CheckQuestCompletion();
        }
    }

    public void CompleteObjective(int objectiveIndex)
    {
        if (objectiveIndex >= 0 && objectiveIndex < objectives.Count)
        {
            objectives[objectiveIndex].Complete();
            CheckQuestCompletion();
        }
    }

    public bool HasTag(string tag)
    {
        return tags.Contains(tag.ToLower());
    }

    public void AddTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !tags.Contains(tag.ToLower()))
        {
            tags.Add(tag.ToLower());
        }
    }

    public float GetCompletionPercentage()
    {
        if (!IsQuest || objectives.Count == 0) return 0f;

        int completedCount = 0;
        foreach (var objective in objectives)
        {
            if (objective.IsCompleted) completedCount++;
        }

        return (float)completedCount / objectives.Count * 100f;
    }

    public string GetFormattedDescription()
    {
        string formatted = description;

        // Replace tokens with actual values
        formatted = formatted.Replace("{PLAYER_NAME}", "Survivor");
        formatted = formatted.Replace("{LOCATION}", locationFound);
        formatted = formatted.Replace("{DATE}", dateDiscovered.ToShortDateString());

        return formatted;
    }
    #endregion

    #region Private Methods
    private void CheckQuestCompletion()
    {
        if (!IsQuest) return;

        bool allCompleted = true;
        foreach (var objective in objectives)
        {
            if (!objective.IsCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted && questStatus == QuestStatus.Active)
        {
            questStatus = QuestStatus.ReadyToTurnIn;
        }
    }

    private void CompleteAllObjectives()
    {
        foreach (var objective in objectives)
        {
            objective.Complete();
        }
    }
    #endregion
}

#region Enums
public enum JournalCategory
{
    Quest,
    Discovery,
    Recipe,
    Tutorial,
    Lore,
    Note,
    Map
}

public enum QuestStatus
{
    NotStarted,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed,
    Abandoned
}

public enum DiscoveryType
{
    Location,
    Flora,
    Fauna,
    Mineral,
    Artifact,
    Structure,
    NPC,
    Lore
}
#endregion

#region Support Classes
[System.Serializable]
public class QuestObjective
{
    [SerializeField] private string description;
    [SerializeField] private int targetCount;
    [SerializeField] private int currentCount;
    [SerializeField] private bool isCompleted;
    [SerializeField] private bool isOptional;
    [SerializeField] private bool isHidden;

    public string Description => description;
    public int TargetCount => targetCount;
    public int CurrentCount => currentCount;
    public bool IsCompleted => isCompleted;
    public bool IsOptional => isOptional;
    public bool IsHidden => isHidden;
    public float Progress => targetCount > 0 ? (float)currentCount / targetCount : 0f;

    public QuestObjective(string desc, int target, bool optional = false)
    {
        description = desc;
        targetCount = target;
        currentCount = 0;
        isCompleted = false;
        isOptional = optional;
        isHidden = false;
    }

    public void UpdateProgress(int newCount)
    {
        currentCount = Mathf.Clamp(newCount, 0, targetCount);
        if (currentCount >= targetCount)
        {
            Complete();
        }
    }

    public void IncrementProgress(int amount = 1)
    {
        UpdateProgress(currentCount + amount);
    }

    public void Complete()
    {
        currentCount = targetCount;
        isCompleted = true;
    }

    public string GetProgressText()
    {
        if (isHidden && !isCompleted) return "???";
        if (targetCount <= 1) return isCompleted ? "✓" : "○";
        return $"{currentCount}/{targetCount}";
    }
}

[System.Serializable]
public class ItemReward
{
    [SerializeField] private string itemID;
    [SerializeField] private int quantity;
    [SerializeField] private float chance = 1f;

    public string ItemID => itemID;
    public int Quantity => quantity;
    public float Chance => chance;

    public ItemReward(string id, int qty, float dropChance = 1f)
    {
        itemID = id;
        quantity = qty;
        chance = Mathf.Clamp01(dropChance);
    }
}

[System.Serializable]
public class ItemRequirement
{
    [SerializeField] private string itemID;
    [SerializeField] private int quantity;
    [SerializeField] private bool consumeItem = true;

    public string ItemID => itemID;
    public int Quantity => quantity;
    public bool ConsumeItem => consumeItem;

    public ItemRequirement(string id, int qty, bool consume = true)
    {
        itemID = id;
        quantity = qty;
        consumeItem = consume;
    }
}
#endregion