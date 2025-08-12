using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using Debug = UnityEngine.Debug;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor
{
    /// <summary>
    /// Gamified Development Progress System - Turn your game dev into a game!
    /// Complete quests, earn XP, level up, and ship your game!
    /// </summary>
    public class WildSurvivalProgressQuest : EditorWindow
    {
        // ==================== DEVELOPER STATS ====================
        [System.Serializable]
        public class DeveloperProfile
        {
            public string developerName = "Game Dev Hero";
            public int level = 1;
            public float currentXP = 0;
            public float xpToNextLevel = 100;
            public int totalTasksCompleted = 0;
            public int currentStreak = 0;
            public int bestStreak = 0;
            public DateTime lastCommitTime;
            public float totalDevHours = 0;
            public List<Achievement> unlockedAchievements = new List<Achievement>();

            // Stats
            public int codeTasksCompleted = 0;
            public int artTasksCompleted = 0;
            public int designTasksCompleted = 0;
            public int bugFixesCompleted = 0;

            public float GetLevelProgress() => currentXP / xpToNextLevel;

            public void AddXP(float xp)
            {
                currentXP += xp;
                while (currentXP >= xpToNextLevel)
                {
                    currentXP -= xpToNextLevel;
                    level++;
                    xpToNextLevel = 100 * Mathf.Pow(1.2f, level);
                }
            }
        }

        [System.Serializable]
        public class Achievement
        {
            public string id;
            public string name;
            public string description;
            public Texture2D icon;
            public bool unlocked;
            public DateTime unlockedDate;
        }

        // ==================== QUEST SYSTEM ====================
        [System.Serializable]
        public class DevelopmentQuest
        {
            public string id;
            public string title;
            public string description;
            public QuestCategory category;
            public QuestPriority priority;
            public QuestStatus status;

            public List<QuestTask> tasks = new List<QuestTask>();
            public float xpReward = 50;
            public int estimatedHours = 2;
            public DateTime createdDate;
            public DateTime? completedDate;
            public List<string> dependencies = new List<string>();
            public string gitBranch;

            // Quality metrics
            public bool hasCodeReview = false;
            public bool hasUnitTests = false;
            public bool hasDocumentation = false;
            public float codeQualityScore = 0;

            public float GetProgress()
            {
                if (tasks.Count == 0) return 0;
                return tasks.Count(t => t.completed) / (float)tasks.Count;
            }

            public bool IsBlocked()
            {
                // Check if dependencies are completed
                return dependencies.Any(d => GetQuestById(d)?.status != QuestStatus.Completed);
            }

            public float GetQualityBonus()
            {
                float bonus = 1f;
                if (hasCodeReview) bonus += 0.2f;
                if (hasUnitTests) bonus += 0.3f;
                if (hasDocumentation) bonus += 0.2f;
                if (codeQualityScore > 0.8f) bonus += 0.3f;
                return bonus;
            }
        }

        // ==================== DAILY QUESTS ====================
        [System.Serializable]
        public class DailyQuest
        {
            public string id;
            public string title;
            public string description;
            public DailyQuestType type;
            public bool completed;
            public float xpReward = 20;
            public DateTime date;

            public enum DailyQuestType
            {
                CodeCleanup,
                Documentation,
                Testing,
                Refactoring,
                OrganizeAssets,
                BackupProject,
                ReviewCode,
                PlanTomorrow,
                LearnSomething
            }
        }

        // ==================== SIDE QUESTS ====================
        [System.Serializable]
        public class SideQuest
        {
            public string id;
            public string title;
            public string description;
            public SideQuestType type;
            public bool completed;
            public float xpReward = 30;
            public string[] rewards; // Unlocks, bonuses, etc.

            public enum SideQuestType
            {
                CodeQuality,
                ProjectStructure,
                Performance,
                Documentation,
                Community,
                Learning,
                Tools,
                Workflow
            }
        }

        [System.Serializable]
        public class QuestTask
        {
            public string description;
            public bool completed;
            public float xpValue = 10;
            public DateTime? completedTime;
        }

        public enum QuestCategory
        {
            CoreSystems,
            Inventory,
            Crafting,
            Combat,
            AI,
            Environment,
            UI,
            Audio,
            Polish,
            Optimization,
            BugFix,
            Documentation
        }

        public enum QuestPriority
        {
            Critical,   // 🔴 Must have for launch
            High,       // 🟡 Important features
            Medium,     // 🟢 Nice to have
            Low,        // 🔵 Polish/extras
            Backlog     // ⚪ Future updates
        }

        public enum QuestStatus
        {
            Locked,
            Available,
            InProgress,
            Testing,
            Completed
        }

        // ==================== MILESTONE SYSTEM ====================
        [System.Serializable]
        public class Milestone
        {
            public string name;
            public string description;
            public List<string> requiredQuestIds;
            public bool achieved;
            public DateTime? achievedDate;
            public float bonusXP = 500;

            public float GetProgress(List<DevelopmentQuest> allQuests)
            {
                if (requiredQuestIds.Count == 0) return 0;
                int completed = requiredQuestIds.Count(id =>
                    allQuests.FirstOrDefault(q => q.id == id)?.status == QuestStatus.Completed);
                return completed / (float)requiredQuestIds.Count;
            }
        }

        // ==================== WINDOW STATE ====================
        private static DeveloperProfile profile;
        private static List<DevelopmentQuest> allQuests = new List<DevelopmentQuest>();
        private static List<Milestone> milestones = new List<Milestone>();
        private static List<DailyQuest> dailyQuests = new List<DailyQuest>();
        private static List<SideQuest> sideQuests = new List<SideQuest>();

        // Code Quality Tracking
        private static CodeHealthMetrics codeHealth = new CodeHealthMetrics();

        [System.Serializable]
        public class CodeHealthMetrics
        {
            public float namingConventionScore = 1f;
            public float commentCoverage = 0f;
            public float methodComplexity = 0f;
            public int compilerWarnings = 0;
            public int todoComments = 0;
            public float testCoverage = 0f;
            public DateTime lastRefactoring;
            public List<string> codeSmells = new List<string>();

            public float GetOverallHealth()
            {
                float health = 0;
                health += namingConventionScore * 0.25f;
                health += commentCoverage * 0.2f;
                health += (1f - Mathf.Clamp01(methodComplexity / 10f)) * 0.2f;
                health += (1f - Mathf.Clamp01(compilerWarnings / 10f)) * 0.15f;
                health += testCoverage * 0.2f;
                return Mathf.Clamp01(health);
            }

            public string GetHealthGrade()
            {
                float health = GetOverallHealth();
                if (health >= 0.9f) return "A+";
                if (health >= 0.8f) return "A";
                if (health >= 0.7f) return "B";
                if (health >= 0.6f) return "C";
                if (health >= 0.5f) return "D";
                return "F";
            }
        }

        // Motivation System
        private static MotivationSystem motivation = new MotivationSystem();

        [System.Serializable]
        public class MotivationSystem
        {
            public float currentMotivation = 100f;
            public float maxMotivation = 100f;
            public List<string> recentAccomplishments = new List<string>();
            public DateTime lastBreak;
            public int consecutiveWorkDays = 0;
            public bool feelingBurnout = false;

            public void AddMotivation(float amount, string reason)
            {
                currentMotivation = Mathf.Min(currentMotivation + amount, maxMotivation);
                recentAccomplishments.Add($"{DateTime.Now:HH:mm} - {reason}");
                if (recentAccomplishments.Count > 10)
                    recentAccomplishments.RemoveAt(0);
            }

            public void DrainMotivation(float amount)
            {
                currentMotivation = Mathf.Max(0, currentMotivation - amount);
                if (currentMotivation < 30)
                    feelingBurnout = true;
            }

            public Color GetMotivationColor()
            {
                if (currentMotivation > 80) return Color.green;
                if (currentMotivation > 50) return Color.yellow;
                if (currentMotivation > 30) return new Color(1f, 0.5f, 0);
                return Color.red;
            }

            public string GetMotivationTip()
            {
                if (currentMotivation > 80)
                    return "You're on fire! Keep up the amazing work! 🔥";
                if (currentMotivation > 50)
                    return "Doing great! Maybe tackle a fun side quest? 🎯";
                if (currentMotivation > 30)
                    return "Take a short break, you've earned it! ☕";
                return "Time for a real break. Step away and recharge! 🌳";
            }
        }

        private Vector2 scrollPosition;
        private QuestCategory filterCategory = QuestCategory.CoreSystems;
        private bool showAllCategories = true;
        private DevelopmentQuest selectedQuest;
        private string newTaskDescription = "";

        // Visual Elements
        private AnimBool showQuestDetails;
        private float lastSaveTime;
        private bool isDirty;

        // Git Integration
        private bool gitEnabled = true;
        private string gitRepoPath = "";

        // Animation & Effects
        private float xpAnimationValue;
        private float xpAnimationTarget;
        private List<FloatingXP> floatingXPs = new List<FloatingXP>();
        private Texture2D levelUpEffect;
        private float levelUpAnimTime;

        private class FloatingXP
        {
            public float xpAmount;
            public Vector2 position;
            public float lifetime;
            public Color color;
        }

        // Motivational Messages
        private string[] motivationalQuotes = new[]
        {
            "Every line of code brings you closer to launch! 🚀",
            "You're building something amazing! 💪",
            "Progress, not perfection! Keep going! ⭐",
            "Your game is becoming reality! 🎮",
            "Players are going to love this! ❤️",
            "One task at a time, one step closer! 👣",
            "You've got this, game dev hero! 🦸",
            "Building dreams, one commit at a time! 💭"
        };

        [MenuItem("Tools/Wild Survival/Progress Quest %#p", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<WildSurvivalProgressQuest>("🎮 Dev Progress Quest");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        private void OnEnable()
        {
            LoadProgress();
            InitializeDefaultQuests();
            showQuestDetails = new AnimBool(false);
            showQuestDetails.valueChanged.AddListener(Repaint);

            // Start auto-save
            EditorApplication.update += AutoSave;
            EditorApplication.update += UpdateAnimations;

            // Find git repo
            FindGitRepo();
        }

        private void OnDisable()
        {
            SaveProgress();
            EditorApplication.update -= AutoSave;
            EditorApplication.update -= UpdateAnimations;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawMainContent();
            DrawFloatingXP();

            // Handle level up animation
            if (levelUpAnimTime > 0)
            {
                DrawLevelUpEffect();
            }
        }

        private void DrawHeader()
        {
            // Header Background
            var headerRect = EditorGUILayout.BeginVertical();
            GUI.Box(new Rect(headerRect.x, headerRect.y, position.width, 100), "");

            EditorGUILayout.BeginHorizontal();

            // Developer Avatar/Icon
            GUILayout.Space(10);
            GUI.Box(new Rect(10, 10, 80, 80), "👤", new GUIStyle(GUI.skin.box)
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter
            });

            GUILayout.Space(90);

            // Developer Info
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);

            // Name and Level
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(profile.developerName, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20
            }, GUILayout.Width(200));

            // Level Badge
            var levelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            GUI.backgroundColor = new Color(1f, 0.8f, 0f, 0.3f);
            GUILayout.Box($"Level {profile.level}", levelStyle, GUILayout.Width(100), GUILayout.Height(30));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // XP Bar
            EditorGUILayout.Space(5);
            var xpRect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
            DrawXPBar(xpRect);

            // Stats Row
            EditorGUILayout.BeginHorizontal();
            DrawMiniStat("🏆", profile.totalTasksCompleted.ToString(), "Tasks");
            DrawMiniStat("🔥", profile.currentStreak.ToString(), "Streak");
            DrawMiniStat("⏱", $"{profile.totalDevHours:F0}h", "Dev Time");
            DrawMiniStat("⭐", profile.unlockedAchievements.Count.ToString(), "Achievements");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Motivational Quote
            EditorGUILayout.BeginVertical();
            GUILayout.Space(20);
            var quoteStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleRight,
                wordWrap = true
            };
            EditorGUILayout.LabelField(GetMotivationalQuote(), quoteStyle, GUILayout.Width(300));
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(100);
        }

        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Panel - Quest Categories & Daily Quests
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawMotivationPanel();
            EditorGUILayout.Space(10);
            DrawDailyQuests();
            EditorGUILayout.Space(10);
            DrawMilestones();
            EditorGUILayout.Space(10);
            DrawQuestCategories();
            EditorGUILayout.EndVertical();

            // Middle Panel - Quest List
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            DrawQuestList();
            EditorGUILayout.EndVertical();

            // Right Panel - Quest Details or Progress Overview
            EditorGUILayout.BeginVertical();
            if (selectedQuest != null)
            {
                DrawQuestDetails();
            }
            else
            {
                DrawProgressOverview();
                EditorGUILayout.Space(10);
                DrawCodeHealthPanel();
                EditorGUILayout.Space(10);
                DrawSideQuests();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMotivationPanel()
        {
            EditorGUILayout.LabelField("💪 MOTIVATION", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Motivation Bar
            var motivationRect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
            DrawMotivationBar(motivationRect);

            // Motivation Tip
            EditorGUILayout.LabelField(motivation.GetMotivationTip(), EditorStyles.wordWrappedMiniLabel);

            // Quick Boosters
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("☕ Break", GUILayout.Height(25)))
            {
                TakeBreak();
            }
            if (GUILayout.Button("🎵 Music", GUILayout.Height(25)))
            {
                PlayMotivationalMusic();
            }
            if (GUILayout.Button("🏆 Stats", GUILayout.Height(25)))
            {
                ShowAchievements();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawDailyQuests()
        {
            EditorGUILayout.LabelField("📅 DAILY QUESTS", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Generate daily quests if needed
            GenerateDailyQuests();

            foreach (var daily in dailyQuests.Where(d => d.date.Date == DateTime.Today))
            {
                EditorGUILayout.BeginHorizontal();

                bool wasCompleted = daily.completed;
                daily.completed = EditorGUILayout.Toggle(daily.completed, GUILayout.Width(20));

                if (!wasCompleted && daily.completed)
                {
                    CompleteDailyQuest(daily);
                }

                var style = daily.completed ?
                    new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic } :
                    EditorStyles.miniLabel;

                EditorGUILayout.LabelField(daily.title, style);
                EditorGUILayout.LabelField($"+{daily.xpReward}xp", GUILayout.Width(40));

                EditorGUILayout.EndHorizontal();
            }

            // Daily combo bonus
            int completedToday = dailyQuests.Count(d => d.date.Date == DateTime.Today && d.completed);
            if (completedToday > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Daily Combo: {completedToday}/5 🔥", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCodeHealthPanel()
        {
            EditorGUILayout.LabelField("🏥 CODE HEALTH", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Overall Health Grade
            string grade = codeHealth.GetHealthGrade();
            Color gradeColor = GetGradeColor(grade);

            var gradeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = gradeColor }
            };

            EditorGUILayout.LabelField($"Grade: {grade}", gradeStyle, GUILayout.Height(40));

            // Health Metrics
            DrawHealthMetric("Naming Conventions", codeHealth.namingConventionScore);
            DrawHealthMetric("Comment Coverage", codeHealth.commentCoverage);
            DrawHealthMetric("Test Coverage", codeHealth.testCoverage);

            // Warnings
            if (codeHealth.compilerWarnings > 0)
            {
                EditorGUILayout.HelpBox($"⚠️ {codeHealth.compilerWarnings} compiler warnings", MessageType.Warning);
            }

            if (codeHealth.todoComments > 0)
            {
                EditorGUILayout.LabelField($"📝 {codeHealth.todoComments} TODO comments");
            }

            // Code Smells
            if (codeHealth.codeSmells.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Code Smells Detected:", EditorStyles.miniLabel);
                foreach (var smell in codeHealth.codeSmells.Take(3))
                {
                    EditorGUILayout.LabelField($"  • {smell}", EditorStyles.miniLabel);
                }
            }

            // Actions
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 Analyze", GUILayout.Height(25)))
            {
                AnalyzeCodeQuality();
            }
            if (GUILayout.Button("🧹 Clean", GUILayout.Height(25)))
            {
                StartCodeCleanup();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSideQuests()
        {
            EditorGUILayout.LabelField("⭐ SIDE QUESTS", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Available side quests
            var availableSideQuests = sideQuests.Where(s => !s.completed).Take(5);

            foreach (var sideQuest in availableSideQuests)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(GetSideQuestIcon(sideQuest.type), GUILayout.Width(25)))
                {
                    StartSideQuest(sideQuest);
                }

                EditorGUILayout.LabelField(sideQuest.title, EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"+{sideQuest.xpReward}xp", GUILayout.Width(40));

                EditorGUILayout.EndHorizontal();

                // Show rewards
                if (sideQuest.rewards != null && sideQuest.rewards.Length > 0)
                {
                    EditorGUILayout.LabelField($"  Reward: {sideQuest.rewards[0]}",
                        new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic });
                }
            }

            if (!availableSideQuests.Any())
            {
                EditorGUILayout.LabelField("All side quests completed! 🎉", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMilestones()
        {
            EditorGUILayout.LabelField("🏁 MILESTONES", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            foreach (var milestone in milestones)
            {
                EditorGUILayout.BeginHorizontal();

                // Icon
                string icon = milestone.achieved ? "✅" : "🎯";
                EditorGUILayout.LabelField(icon, GUILayout.Width(20));

                // Name
                var style = milestone.achieved ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(milestone.name, style, GUILayout.Width(150));

                // Progress Bar
                if (!milestone.achieved)
                {
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(80), GUILayout.Height(15));
                    float progress = milestone.GetProgress(allQuests);
                    EditorGUI.ProgressBar(rect, progress, $"{progress * 100:F0}%");
                }

                EditorGUILayout.EndHorizontal();

                if (!milestone.achieved)
                {
                    EditorGUILayout.LabelField(milestone.description, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuestCategories()
        {
            EditorGUILayout.LabelField("📋 QUEST CATEGORIES", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            showAllCategories = EditorGUILayout.Toggle("Show All", showAllCategories);

            if (!showAllCategories)
            {
                foreach (QuestCategory category in Enum.GetValues(typeof(QuestCategory)))
                {
                    var quests = allQuests.Where(q => q.category == category).ToList();
                    int completed = quests.Count(q => q.status == QuestStatus.Completed);
                    int total = quests.Count;

                    EditorGUILayout.BeginHorizontal();

                    bool isSelected = filterCategory == category;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

                    if (GUILayout.Button($"{GetCategoryIcon(category)} {category}", GUILayout.Height(25)))
                    {
                        filterCategory = category;
                        showAllCategories = false;
                    }

                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.LabelField($"{completed}/{total}", GUILayout.Width(40));

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            // Add New Quest Button
            EditorGUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("➕ Create New Quest", GUILayout.Height(30)))
            {
                CreateNewQuest();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawQuestList()
        {
            EditorGUILayout.LabelField("🗡️ ACTIVE QUESTS", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var questsToShow = showAllCategories ? allQuests :
                allQuests.Where(q => q.category == filterCategory).ToList();

            // Group by status
            var groupedQuests = questsToShow.GroupBy(q => q.status)
                .OrderBy(g => g.Key);

            foreach (var group in groupedQuests)
            {
                if (group.Count() == 0) continue;

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(GetStatusHeader(group.Key), EditorStyles.boldLabel);

                foreach (var quest in group.OrderByDescending(q => q.priority))
                {
                    DrawQuestCard(quest);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuestCard(DevelopmentQuest quest)
        {
            var cardStyle = GUI.skin.box;
            var rect = EditorGUILayout.BeginVertical(cardStyle);

            // Highlight selected
            if (quest == selectedQuest)
            {
                GUI.Box(rect, "", new GUIStyle { normal = { background = MakeColorTexture(new Color(0, 1, 1, 0.1f)) } });
            }

            // Blocked overlay
            if (quest.IsBlocked())
            {
                GUI.Box(rect, "", new GUIStyle { normal = { background = MakeColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.3f)) } });
            }

            EditorGUILayout.BeginHorizontal();

            // Priority Indicator
            string priorityIcon = GetPriorityIcon(quest.priority);
            EditorGUILayout.LabelField(priorityIcon, GUILayout.Width(20));

            // Quest Title
            var titleStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            if (GUILayout.Button(quest.title, titleStyle))
            {
                selectedQuest = quest;
            }

            // XP Reward
            EditorGUILayout.LabelField($"+{quest.xpReward} XP", GUILayout.Width(60));

            // Status Button
            if (quest.status != QuestStatus.Completed && !quest.IsBlocked())
            {
                GUI.backgroundColor = GetStatusColor(quest.status);
                if (GUILayout.Button(quest.status.ToString(), GUILayout.Width(80)))
                {
                    AdvanceQuestStatus(quest);
                }
                GUI.backgroundColor = Color.white;
            }
            else if (quest.status == QuestStatus.Completed)
            {
                EditorGUILayout.LabelField("✅", GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("🔒", GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            // Progress Bar
            if (quest.tasks.Count > 0)
            {
                var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(5));
                float progress = quest.GetProgress();
                DrawProgressBar(progressRect, progress, GetCategoryColor(quest.category));
            }

            // Task Count
            if (quest.tasks.Count > 0)
            {
                int completedTasks = quest.tasks.Count(t => t.completed);
                EditorGUILayout.LabelField($"Tasks: {completedTasks}/{quest.tasks.Count}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawQuestDetails()
        {
            EditorGUILayout.LabelField("📜 QUEST DETAILS", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Quest Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectedQuest.title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });

            if (GUILayout.Button("✖", GUILayout.Width(25)))
            {
                selectedQuest = null;
                return;
            }
            EditorGUILayout.EndHorizontal();

            // Description
            EditorGUILayout.LabelField(selectedQuest.description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            // Quest Info
            EditorGUILayout.LabelField($"Category: {selectedQuest.category}");
            EditorGUILayout.LabelField($"Priority: {selectedQuest.priority}");
            EditorGUILayout.LabelField($"Base XP: {selectedQuest.xpReward}");

            // Quality Bonus Display
            float qualityBonus = selectedQuest.GetQualityBonus();
            if (qualityBonus > 1f)
            {
                EditorGUILayout.LabelField($"Quality Bonus: x{qualityBonus:F1} 🌟",
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.yellow } });
            }

            EditorGUILayout.LabelField($"Total XP: {selectedQuest.xpReward * qualityBonus:F0}");
            EditorGUILayout.LabelField($"Estimated Time: {selectedQuest.estimatedHours} hours");

            if (!string.IsNullOrEmpty(selectedQuest.gitBranch))
            {
                EditorGUILayout.LabelField($"Git Branch: {selectedQuest.gitBranch}");
            }

            EditorGUILayout.Space(10);

            // Quality Checklist
            EditorGUILayout.LabelField("Quality Checklist:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            selectedQuest.hasCodeReview = EditorGUILayout.Toggle("✓ Code Review", selectedQuest.hasCodeReview);
            selectedQuest.hasUnitTests = EditorGUILayout.Toggle("✓ Unit Tests", selectedQuest.hasUnitTests);
            selectedQuest.hasDocumentation = EditorGUILayout.Toggle("✓ Documentation", selectedQuest.hasDocumentation);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Code Quality:");
            selectedQuest.codeQualityScore = EditorGUILayout.Slider(selectedQuest.codeQualityScore, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Tasks
            EditorGUILayout.LabelField("Tasks:", EditorStyles.boldLabel);

            for (int i = 0; i < selectedQuest.tasks.Count; i++)
            {
                var task = selectedQuest.tasks[i];
                EditorGUILayout.BeginHorizontal();

                bool wasCompleted = task.completed;
                task.completed = EditorGUILayout.Toggle(task.completed, GUILayout.Width(20));

                // Task completed!
                if (!wasCompleted && task.completed)
                {
                    CompleteTask(selectedQuest, task);
                }

                var taskStyle = task.completed ?
                    new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic } :
                    EditorStyles.label;

                EditorGUILayout.LabelField(task.description, taskStyle);
                EditorGUILayout.LabelField($"+{task.xpValue} XP", GUILayout.Width(50));

                if (GUILayout.Button("🗑", GUILayout.Width(25)))
                {
                    selectedQuest.tasks.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add Task
            EditorGUILayout.BeginHorizontal();
            newTaskDescription = EditorGUILayout.TextField(newTaskDescription);
            if (GUILayout.Button("Add Task", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(newTaskDescription))
                {
                    selectedQuest.tasks.Add(new QuestTask
                    {
                        description = newTaskDescription,
                        xpValue = 10
                    });
                    newTaskDescription = "";
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Actions
            if (selectedQuest.status != QuestStatus.Completed)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🎯 Complete Quest", GUILayout.Height(40)))
                {
                    CompleteQuest(selectedQuest);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressOverview()
        {
            EditorGUILayout.LabelField("📊 PROGRESS OVERVIEW", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Overall Progress
            float totalProgress = CalculateOverallProgress();
            var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(30));
            DrawFancyProgressBar(progressRect, totalProgress, "GAME COMPLETION");

            EditorGUILayout.Space(10);

            // Category Progress
            EditorGUILayout.LabelField("Progress by Category:", EditorStyles.boldLabel);

            foreach (QuestCategory category in Enum.GetValues(typeof(QuestCategory)))
            {
                var categoryQuests = allQuests.Where(q => q.category == category).ToList();
                if (categoryQuests.Count == 0) continue;

                float categoryProgress = categoryQuests.Count(q => q.status == QuestStatus.Completed) / (float)categoryQuests.Count;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{GetCategoryIcon(category)} {category}", GUILayout.Width(150));

                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                DrawProgressBar(rect, categoryProgress, GetCategoryColor(category));

                EditorGUILayout.LabelField($"{categoryProgress * 100:F0}%", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // Time Estimate
            EditorGUILayout.LabelField("Time Estimates:", EditorStyles.boldLabel);

            int remainingHours = allQuests.Where(q => q.status != QuestStatus.Completed)
                .Sum(q => q.estimatedHours);

            EditorGUILayout.LabelField($"Remaining Work: {remainingHours} hours");
            EditorGUILayout.LabelField($"At 4h/day: {remainingHours / 4} days");
            EditorGUILayout.LabelField($"At 20h/week: {remainingHours / 20:F1} weeks");

            DateTime estimatedCompletion = DateTime.Now.AddHours(remainingHours);
            EditorGUILayout.LabelField($"Est. Completion: {estimatedCompletion:MMM dd, yyyy}");

            EditorGUILayout.Space(10);

            // Velocity Chart
            EditorGUILayout.LabelField("Development Velocity:", EditorStyles.boldLabel);
            DrawVelocityChart();

            EditorGUILayout.EndVertical();
        }

        private void DrawVelocityChart()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
            GUI.Box(rect, "");

            // Simple velocity visualization
            // In a real implementation, you'd track completion over time
            var innerRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

            // Draw fake data for now
            int days = 7;
            float barWidth = innerRect.width / days;

            for (int i = 0; i < days; i++)
            {
                float velocity = UnityEngine.Random.Range(0.3f, 1f);
                var barRect = new Rect(
                    innerRect.x + i * barWidth + 2,
                    innerRect.y + innerRect.height * (1 - velocity),
                    barWidth - 4,
                    innerRect.height * velocity
                );

                GUI.backgroundColor = Color.Lerp(Color.red, Color.green, velocity);
                GUI.Box(barRect, "");
            }

            GUI.backgroundColor = Color.white;
        }

        // ==================== HELPER METHODS ====================

        private void DrawXPBar(Rect rect)
        {
            // Background
            GUI.Box(rect, "");

            // Fill
            float fillAmount = profile.GetLevelProgress();
            var fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * fillAmount, rect.height - 4);

            // Gradient effect
            GUI.backgroundColor = Color.Lerp(Color.yellow, Color.green, fillAmount);
            GUI.Box(fillRect, "");
            GUI.backgroundColor = Color.white;

            // Text
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(rect, $"XP: {profile.currentXP:F0} / {profile.xpToNextLevel:F0}", textStyle);
        }

        private void DrawProgressBar(Rect rect, float progress, Color color)
        {
            GUI.Box(rect, "");
            var fillRect = new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * progress, rect.height - 2);
            GUI.backgroundColor = color;
            GUI.Box(fillRect, "");
            GUI.backgroundColor = Color.white;
        }

        private void DrawFancyProgressBar(Rect rect, float progress, string label)
        {
            // Background
            GUI.Box(rect, "");

            // Gradient fill
            var fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * progress, rect.height - 4);

            // Create gradient effect with multiple boxes
            int segments = 20;
            float segmentWidth = fillRect.width / segments;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                GUI.backgroundColor = Color.Lerp(Color.red, Color.green, t);
                var segRect = new Rect(fillRect.x + i * segmentWidth, fillRect.y, segmentWidth, fillRect.height);
                GUI.Box(segRect, "");
            }

            GUI.backgroundColor = Color.white;

            // Label
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
            GUI.Label(rect, $"{label}: {progress * 100:F1}%", labelStyle);
        }

        private void DrawMiniStat(string icon, string value, string label)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(icon, style);
            EditorGUILayout.LabelField(value, new GUIStyle(style) { fontStyle = FontStyle.Bold });
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawFloatingXP()
        {
            for (int i = floatingXPs.Count - 1; i >= 0; i--)
            {
                var xp = floatingXPs[i];
                xp.lifetime -= 0.016f;
                xp.position.y -= 30 * 0.016f;

                if (xp.lifetime <= 0)
                {
                    floatingXPs.RemoveAt(i);
                    continue;
                }

                var alpha = Mathf.Clamp01(xp.lifetime);
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(xp.color.r, xp.color.g, xp.color.b, alpha) }
                };

                GUI.Label(new Rect(xp.position.x, xp.position.y, 100, 30), $"+{xp.xpAmount} XP", style);
            }
        }

        private void DrawLevelUpEffect()
        {
            levelUpAnimTime -= 0.016f;

            var rect = new Rect(position.width / 2 - 200, position.height / 2 - 50, 400, 100);

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 40,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };

            GUI.Box(rect, $"🎉 LEVEL UP! 🎉\nLevel {profile.level}", style);
        }

        // ==================== ACTIONS ====================

        private void CompleteTask(DevelopmentQuest quest, QuestTask task)
        {
            task.completedTime = DateTime.Now;

            // Add XP with animation
            AddXPWithAnimation(task.xpValue);

            // Update streak
            UpdateStreak();

            // Check if quest is complete
            if (quest.GetProgress() >= 1f)
            {
                CompleteQuest(quest);
            }
            else
            {
                // Git commit for task
                if (gitEnabled)
                {
                    GitCommit($"✅ {quest.title}: {task.description}");
                }
            }

            isDirty = true;
        }

        private void CompleteQuest(DevelopmentQuest quest)
        {
            quest.status = QuestStatus.Completed;
            quest.completedDate = DateTime.Now;

            // Calculate XP with quality bonus
            float qualityMultiplier = quest.GetQualityBonus();
            float totalXP = quest.xpReward * qualityMultiplier;

            // Add XP
            AddXPWithAnimation(totalXP);

            // Update stats
            profile.totalTasksCompleted++;
            UpdateStreak();

            // Motivation boost for quality work
            if (qualityMultiplier > 1.5f)
            {
                motivation.AddMotivation(20, "Excellent quality work!");
                ShowFloatingText("⭐ QUALITY BONUS! ⭐", Color.yellow);
            }

            // Git commit
            if (gitEnabled)
            {
                string qualityNote = qualityMultiplier > 1.5f ? " [HIGH QUALITY]" : "";
                GitCommit($"🎯 Completed Quest: {quest.title}{qualityNote}");
            }

            // Check milestones
            CheckMilestones();

            // Play completion sound
            EditorApplication.Beep();

            isDirty = true;
        }

        // New Helper Methods
        private void DrawMotivationBar(Rect rect)
        {
            GUI.Box(rect, "");

            float fillAmount = motivation.currentMotivation / motivation.maxMotivation;
            var fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * fillAmount, rect.height - 4);

            GUI.backgroundColor = motivation.GetMotivationColor();
            GUI.Box(fillRect, "");
            GUI.backgroundColor = Color.white;

            var textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(rect, $"Motivation: {motivation.currentMotivation:F0}%", textStyle);
        }

        private void DrawHealthMetric(string label, float value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(15));
            Color barColor = value > 0.8f ? Color.green : value > 0.5f ? Color.yellow : Color.red;
            DrawProgressBar(rect, value, barColor);

            EditorGUILayout.LabelField($"{value * 100:F0}%", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateDailyQuests()
        {
            // Remove old daily quests
            dailyQuests.RemoveAll(d => d.date.Date < DateTime.Today);

            // Check if we already have today's quests
            if (dailyQuests.Any(d => d.date.Date == DateTime.Today))
                return;

            // Generate new daily quests
            var dailyTypes = new[]
            {
                (DailyQuest.DailyQuestType.CodeCleanup, "Clean up code warnings", 25),
                (DailyQuest.DailyQuestType.Documentation, "Add XML comments to 5 methods", 20),
                (DailyQuest.DailyQuestType.Testing, "Write 3 unit tests", 30),
                (DailyQuest.DailyQuestType.OrganizeAssets, "Organize project folders", 15),
                (DailyQuest.DailyQuestType.ReviewCode, "Review and refactor 1 class", 35)
            };

            foreach (var (type, title, xp) in dailyTypes)
            {
                dailyQuests.Add(new DailyQuest
                {
                    id = Guid.NewGuid().ToString(),
                    title = title,
                    type = type,
                    xpReward = xp,
                    date = DateTime.Today
                });
            }
        }

        private void GenerateSideQuests()
        {
            if (sideQuests.Count > 0) return;

            // Code Quality Side Quests
            sideQuests.Add(new SideQuest
            {
                id = "sq_naming",
                title = "Naming Convention Master",
                description = "Fix all naming convention violations",
                type = SideQuest.SideQuestType.CodeQuality,
                xpReward = 100,
                rewards = new[] { "Unlock: Auto-naming tool" }
            });

            sideQuests.Add(new SideQuest
            {
                id = "sq_zero_warnings",
                title = "Zero Warnings Challenge",
                description = "Eliminate all compiler warnings",
                type = SideQuest.SideQuestType.CodeQuality,
                xpReward = 150,
                rewards = new[] { "Badge: Clean Coder" }
            });

            // Project Structure Side Quests
            sideQuests.Add(new SideQuest
            {
                id = "sq_folder_structure",
                title = "Architect's Vision",
                description = "Organize perfect folder structure",
                type = SideQuest.SideQuestType.ProjectStructure,
                xpReward = 80,
                rewards = new[] { "Template: Pro folder structure" }
            });

            sideQuests.Add(new SideQuest
            {
                id = "sq_assemblies",
                title = "Assembly Master",
                description = "Set up proper assembly definitions",
                type = SideQuest.SideQuestType.ProjectStructure,
                xpReward = 120,
                rewards = new[] { "Faster compile times!" }
            });

            // Performance Side Quests
            sideQuests.Add(new SideQuest
            {
                id = "sq_60fps",
                title = "Smooth Operator",
                description = "Achieve stable 60 FPS",
                type = SideQuest.SideQuestType.Performance,
                xpReward = 200,
                rewards = new[] { "Performance Profiler Pro" }
            });

            // Learning Side Quests
            sideQuests.Add(new SideQuest
            {
                id = "sq_design_pattern",
                title = "Pattern Recognition",
                description = "Implement 3 design patterns",
                type = SideQuest.SideQuestType.Learning,
                xpReward = 150,
                rewards = new[] { "Design Patterns Cheat Sheet" }
            });
        }

        private void CompleteDailyQuest(DailyQuest daily)
        {
            AddXPWithAnimation(daily.xpReward);

            // Check for daily combo
            int completedToday = dailyQuests.Count(d => d.date.Date == DateTime.Today && d.completed);
            if (completedToday == 5)
            {
                // All dailies complete! Bonus!
                AddXPWithAnimation(50);
                motivation.AddMotivation(30, "Completed all daily quests!");
                ShowFloatingText("🎯 DAILY COMBO!", Color.yellow);
            }

            motivation.AddMotivation(10, $"Completed: {daily.title}");
            isDirty = true;
        }

        private void StartSideQuest(SideQuest sideQuest)
        {
            // Open relevant tool or guide
            switch (sideQuest.type)
            {
                case SideQuest.SideQuestType.CodeQuality:
                    AnalyzeCodeQuality();
                    break;
                case SideQuest.SideQuestType.ProjectStructure:
                    OpenProjectStructureGuide();
                    break;
                case SideQuest.SideQuestType.Performance:
                    OpenProfiler();
                    break;
            }

            Debug.Log($"Started side quest: {sideQuest.title}");
        }

        private void AnalyzeCodeQuality()
        {
            Debug.Log("Analyzing code quality...");

            // Simulate code analysis
            codeHealth.namingConventionScore = UnityEngine.Random.Range(0.6f, 1f);
            codeHealth.commentCoverage = UnityEngine.Random.Range(0.3f, 0.8f);
            codeHealth.testCoverage = UnityEngine.Random.Range(0.1f, 0.6f);
            codeHealth.compilerWarnings = UnityEngine.Random.Range(0, 10);
            codeHealth.todoComments = UnityEngine.Random.Range(0, 20);

            // Find code smells
            codeHealth.codeSmells.Clear();
            if (UnityEngine.Random.value > 0.5f)
                codeHealth.codeSmells.Add("Large class detected: PlayerController");
            if (UnityEngine.Random.value > 0.5f)
                codeHealth.codeSmells.Add("Duplicate code in InventoryManager");
            if (UnityEngine.Random.value > 0.5f)
                codeHealth.codeSmells.Add("Complex method: CraftingSystem.ProcessRecipe()");

            Repaint();
        }

        private void StartCodeCleanup()
        {
            if (EditorUtility.DisplayDialog("Code Cleanup",
                "This will:\n• Fix naming conventions\n• Remove unused usings\n• Format code\n• Organize methods\n\nProceed?",
                "Clean it up!", "Cancel"))
            {
                Debug.Log("Starting code cleanup...");

                // Simulate cleanup
                EditorUtility.DisplayProgressBar("Code Cleanup", "Analyzing files...", 0.2f);
                System.Threading.Thread.Sleep(500);
                EditorUtility.DisplayProgressBar("Code Cleanup", "Fixing naming conventions...", 0.4f);
                System.Threading.Thread.Sleep(500);
                EditorUtility.DisplayProgressBar("Code Cleanup", "Formatting code...", 0.6f);
                System.Threading.Thread.Sleep(500);
                EditorUtility.DisplayProgressBar("Code Cleanup", "Organizing imports...", 0.8f);
                System.Threading.Thread.Sleep(500);
                EditorUtility.ClearProgressBar();

                // Improve metrics
                codeHealth.namingConventionScore = 1f;
                codeHealth.compilerWarnings = 0;

                // Reward
                AddXPWithAnimation(50);
                motivation.AddMotivation(15, "Code cleaned up!");

                Debug.Log("Code cleanup complete!");
            }
        }

        private void TakeBreak()
        {
            motivation.lastBreak = DateTime.Now;
            motivation.AddMotivation(20, "Refreshed after break!");
            motivation.feelingBurnout = false;

            EditorUtility.DisplayDialog("Break Time!",
                "Great idea! Take 5-10 minutes to:\n\n• Stretch\n• Get water\n• Look away from screen\n• Take a short walk\n\nYou've earned it! 🌟",
                "I'm back!");
        }

        private void PlayMotivationalMusic()
        {
            Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ"); // Epic game dev music
            motivation.AddMotivation(10, "Music boost!");
        }

        private void ShowAchievements()
        {
            var message = $"🏆 Your Achievements:\n\n";
            message += $"Level: {profile.level}\n";
            message += $"Tasks Completed: {profile.totalTasksCompleted}\n";
            message += $"Current Streak: {profile.currentStreak} days\n";
            message += $"Best Streak: {profile.bestStreak} days\n";
            message += $"Total Dev Time: {profile.totalDevHours:F0} hours\n";
            message += $"Achievements Unlocked: {profile.unlockedAchievements.Count}\n\n";
            message += "Keep up the amazing work! 💪";

            EditorUtility.DisplayDialog("Your Stats", message, "I'm awesome!");
            motivation.AddMotivation(15, "Reviewed achievements!");
        }

        private void ShowFloatingText(string text, Color color)
        {
            floatingXPs.Add(new FloatingXP
            {
                xpAmount = 0, // Use for text display
                position = new Vector2(position.width / 2, position.height / 2),
                lifetime = 3f,
                color = color
            });

            Debug.Log(text);
        }

        private void OpenProjectStructureGuide()
        {
            EditorUtility.DisplayDialog("Project Structure Guide",
                "Recommended Structure:\n\n" +
                "📁 _Project\n" +
                "  📁 Code\n" +
                "    📁 Runtime (game logic)\n" +
                "    📁 Editor (tools)\n" +
                "  📁 Art\n" +
                "  📁 Audio\n" +
                "  📁 Prefabs\n" +
                "  📁 ScriptableObjects\n\n" +
                "Keep it clean and organized!",
                "Got it!");
        }

        private void OpenProfiler()
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        }

        private Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "A+" => Color.green,
                "A" => Color.green,
                "B" => Color.yellow,
                "C" => new Color(1f, 0.5f, 0),
                "D" => new Color(1f, 0.3f, 0),
                "F" => Color.red,
                _ => Color.gray
            };
        }

        private string GetSideQuestIcon(SideQuest.SideQuestType type)
        {
            return type switch
            {
                SideQuest.SideQuestType.CodeQuality => "🎯",
                SideQuest.SideQuestType.ProjectStructure => "📁",
                SideQuest.SideQuestType.Performance => "⚡",
                SideQuest.SideQuestType.Documentation => "📚",
                SideQuest.SideQuestType.Community => "👥",
                SideQuest.SideQuestType.Learning => "🎓",
                SideQuest.SideQuestType.Tools => "🔧",
                SideQuest.SideQuestType.Workflow => "⚙️",
                _ => "⭐"
            };
        }

        private void AddXPWithAnimation(float xp)
        {
            var oldLevel = profile.level;
            profile.AddXP(xp);

            // Create floating XP
            floatingXPs.Add(new FloatingXP
            {
                xpAmount = xp,
                position = new Vector2(position.width / 2, position.height / 2),
                lifetime = 2f,
                color = Color.yellow
            });

            // Check for level up
            if (profile.level > oldLevel)
            {
                levelUpAnimTime = 3f;
                Debug.Log($"🎉 LEVEL UP! You are now level {profile.level}!");
            }

            Repaint();
        }

        private void AdvanceQuestStatus(DevelopmentQuest quest)
        {
            switch (quest.status)
            {
                case QuestStatus.Available:
                    quest.status = QuestStatus.InProgress;
                    if (gitEnabled && !string.IsNullOrEmpty(quest.gitBranch))
                    {
                        GitCheckout(quest.gitBranch);
                    }
                    break;
                case QuestStatus.InProgress:
                    quest.status = QuestStatus.Testing;
                    break;
                case QuestStatus.Testing:
                    CompleteQuest(quest);
                    break;
            }
            isDirty = true;
        }

        private void UpdateStreak()
        {
            var now = DateTime.Now;
            if (profile.lastCommitTime.Date == now.Date.AddDays(-1))
            {
                profile.currentStreak++;
            }
            else if (profile.lastCommitTime.Date != now.Date)
            {
                profile.currentStreak = 1;
            }

            profile.lastCommitTime = now;
            profile.bestStreak = Mathf.Max(profile.bestStreak, profile.currentStreak);
        }

        private void CheckMilestones()
        {
            foreach (var milestone in milestones)
            {
                if (!milestone.achieved && milestone.GetProgress(allQuests) >= 1f)
                {
                    milestone.achieved = true;
                    milestone.achievedDate = DateTime.Now;
                    AddXPWithAnimation(milestone.bonusXP);
                    Debug.Log($"🏆 Milestone Achieved: {milestone.name}!");
                    EditorApplication.Beep();
                }
            }
        }

        // ==================== GIT INTEGRATION ====================

        private void FindGitRepo()
        {
            string currentDir = Application.dataPath;
            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Directory.Exists(Path.Combine(currentDir, ".git")))
                {
                    gitRepoPath = currentDir;
                    break;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
        }

        private void GitCommit(string message)
        {
            if (!gitEnabled || string.IsNullOrEmpty(gitRepoPath)) return;

            try
            {
                // Add all changes
                RunGitCommand("add -A");

                // Commit with message
                RunGitCommand($"commit -m \"{message}\"");

                // Push to remote
                RunGitCommand("push");

                Debug.Log($"Git: {message}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Git commit failed: {e.Message}");
            }
        }

        private void GitCheckout(string branch)
        {
            if (!gitEnabled || string.IsNullOrEmpty(gitRepoPath)) return;

            try
            {
                RunGitCommand($"checkout -b {branch}");
                Debug.Log($"Git: Switched to branch {branch}");
            }
            catch
            {
                try
                {
                    RunGitCommand($"checkout {branch}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Git checkout failed: {e.Message}");
                }
            }
        }

        private void RunGitCommand(string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = gitRepoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception(error);
                }
            }
        }

        // ==================== DATA PERSISTENCE ====================

        private void SaveProgress()
        {
            var json = JsonUtility.ToJson(profile);
            EditorPrefs.SetString("WS_DevProfile", json);

            var questsJson = JsonUtility.ToJson(new Serialization<DevelopmentQuest>(allQuests));
            EditorPrefs.SetString("WS_Quests", questsJson);

            var milestonesJson = JsonUtility.ToJson(new Serialization<Milestone>(milestones));
            EditorPrefs.SetString("WS_Milestones", milestonesJson);
        }

        private void LoadProgress()
        {
            var profileJson = EditorPrefs.GetString("WS_DevProfile", "");
            if (!string.IsNullOrEmpty(profileJson))
            {
                profile = JsonUtility.FromJson<DeveloperProfile>(profileJson);
            }
            else
            {
                profile = new DeveloperProfile();
            }

            var questsJson = EditorPrefs.GetString("WS_Quests", "");
            if (!string.IsNullOrEmpty(questsJson))
            {
                allQuests = JsonUtility.FromJson<Serialization<DevelopmentQuest>>(questsJson).target;
            }

            var milestonesJson = EditorPrefs.GetString("WS_Milestones", "");
            if (!string.IsNullOrEmpty(milestonesJson))
            {
                milestones = JsonUtility.FromJson<Serialization<Milestone>>(milestonesJson).target;
            }
        }

        private void AutoSave()
        {
            if (isDirty && Time.realtimeSinceStartup - lastSaveTime > 5f)
            {
                SaveProgress();
                isDirty = false;
                lastSaveTime = Time.realtimeSinceStartup;
            }
        }

        private void UpdateAnimations()
        {
            // Update floating XP
            if (floatingXPs.Count > 0)
            {
                Repaint();
            }

            // Update level up animation
            if (levelUpAnimTime > 0)
            {
                Repaint();
            }
        }

        // ==================== INITIALIZATION ====================

        private void InitializeDefaultQuests()
        {
            if (allQuests.Count > 0) return;

            GenerateSideQuests();

            // Core Systems with quality focus
            allQuests.Add(new DevelopmentQuest
            {
                id = "core_boot",
                title = "Bootstrap System",
                description = "Create the game initialization and scene loading system",
                category = QuestCategory.CoreSystems,
                priority = QuestPriority.Critical,
                status = QuestStatus.Available,
                xpReward = 100,
                estimatedHours = 4,
                tasks = new List<QuestTask>
                {
                    new QuestTask { description = "Create GameManager singleton", xpValue = 20 },
                    new QuestTask { description = "Implement scene loading flow", xpValue = 30 },
                    new QuestTask { description = "Add service locator pattern", xpValue = 25 },
                    new QuestTask { description = "Write unit tests", xpValue = 15 },
                    new QuestTask { description = "Document architecture", xpValue = 10 }
                }
            });

            // Inventory System with clean code focus
            allQuests.Add(new DevelopmentQuest
            {
                id = "inv_grid",
                title = "Tetris Inventory Grid",
                description = "Implement the shape-based inventory system with clean, maintainable code",
                category = QuestCategory.Inventory,
                priority = QuestPriority.Critical,
                status = QuestStatus.Available,
                xpReward = 200,
                estimatedHours = 8,
                tasks = new List<QuestTask>
                {
                    new QuestTask { description = "Design data structures", xpValue = 20 },
                    new QuestTask { description = "Create grid with proper naming", xpValue = 30 },
                    new QuestTask { description = "Implement SOLID principles", xpValue = 25 },
                    new QuestTask { description = "Add rotation system", xpValue = 30 },
                    new QuestTask { description = "Create UI visualization", xpValue = 40 },
                    new QuestTask { description = "Add drag & drop", xpValue = 40 },
                    new QuestTask { description = "Write comprehensive tests", xpValue = 15 }
                }
            });

            // Code Quality Quest
            allQuests.Add(new DevelopmentQuest
            {
                id = "quality_foundation",
                title = "Quality Foundation",
                description = "Establish coding standards and project structure",
                category = QuestCategory.CoreSystems,
                priority = QuestPriority.Critical,
                status = QuestStatus.Available,
                xpReward = 150,
                estimatedHours = 3,
                tasks = new List<QuestTask>
                {
                    new QuestTask { description = "Set up .editorconfig", xpValue = 20 },
                    new QuestTask { description = "Create coding standards doc", xpValue = 30 },
                    new QuestTask { description = "Configure assembly definitions", xpValue = 25 },
                    new QuestTask { description = "Set up folder structure", xpValue = 25 },
                    new QuestTask { description = "Add code analyzers", xpValue = 20 },
                    new QuestTask { description = "Create README.md", xpValue = 30 }
                }
            });

            // Add more default quests...

            // Initialize Milestones
            milestones.Add(new Milestone
            {
                name = "Clean Code Base",
                description = "All code follows standards",
                requiredQuestIds = new List<string> { "quality_foundation" },
                bonusXP = 300
            });

            milestones.Add(new Milestone
            {
                name = "Alpha Build",
                description = "Core gameplay loop complete",
                requiredQuestIds = new List<string> { "core_boot", "inv_grid" },
                bonusXP = 500
            });

            milestones.Add(new Milestone
            {
                name = "Beta Build",
                description = "All major systems implemented",
                requiredQuestIds = new List<string>(), // Add quest IDs
                bonusXP = 1000
            });

            milestones.Add(new Milestone
            {
                name = "Steam Ready",
                description = "Game ready for Steam release!",
                requiredQuestIds = new List<string>(), // Add quest IDs
                bonusXP = 2000
            });
        }

        private void CreateNewQuest()
        {
            var quest = new DevelopmentQuest
            {
                id = Guid.NewGuid().ToString(),
                title = "New Quest",
                description = "Describe what needs to be done",
                category = QuestCategory.CoreSystems,
                priority = QuestPriority.Medium,
                status = QuestStatus.Available,
                xpReward = 50,
                estimatedHours = 2,
                createdDate = DateTime.Now,
                tasks = new List<QuestTask>()
            };

            allQuests.Add(quest);
            selectedQuest = quest;
            isDirty = true;
        }

        // ==================== UTILITY METHODS ====================

        private string GetCategoryIcon(QuestCategory category)
        {
            return category switch
            {
                QuestCategory.CoreSystems => "⚙️",
                QuestCategory.Inventory => "🎒",
                QuestCategory.Crafting => "🔨",
                QuestCategory.Combat => "⚔️",
                QuestCategory.AI => "🤖",
                QuestCategory.Environment => "🌲",
                QuestCategory.UI => "🖼️",
                QuestCategory.Audio => "🔊",
                QuestCategory.Polish => "✨",
                QuestCategory.Optimization => "⚡",
                QuestCategory.BugFix => "🐛",
                QuestCategory.Documentation => "📚",
                _ => "📋"
            };
        }

        private string GetPriorityIcon(QuestPriority priority)
        {
            return priority switch
            {
                QuestPriority.Critical => "🔴",
                QuestPriority.High => "🟡",
                QuestPriority.Medium => "🟢",
                QuestPriority.Low => "🔵",
                QuestPriority.Backlog => "⚪",
                _ => "⚪"
            };
        }

        private Color GetCategoryColor(QuestCategory category)
        {
            return category switch
            {
                QuestCategory.CoreSystems => Color.red,
                QuestCategory.Inventory => Color.blue,
                QuestCategory.Crafting => new Color(0.8f, 0.5f, 0),
                QuestCategory.Combat => Color.magenta,
                QuestCategory.AI => Color.cyan,
                QuestCategory.Environment => Color.green,
                QuestCategory.UI => Color.yellow,
                _ => Color.gray
            };
        }

        private Color GetStatusColor(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Available => Color.white,
                QuestStatus.InProgress => Color.yellow,
                QuestStatus.Testing => Color.cyan,
                QuestStatus.Completed => Color.green,
                _ => Color.gray
            };
        }

        private string GetStatusHeader(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Locked => "🔒 LOCKED",
                QuestStatus.Available => "📋 AVAILABLE",
                QuestStatus.InProgress => "🚧 IN PROGRESS",
                QuestStatus.Testing => "🧪 TESTING",
                QuestStatus.Completed => "✅ COMPLETED",
                _ => status.ToString()
            };
        }

        private float CalculateOverallProgress()
        {
            if (allQuests.Count == 0) return 0;

            // Weight critical quests more heavily
            float totalWeight = 0;
            float completedWeight = 0;

            foreach (var quest in allQuests)
            {
                float weight = quest.priority switch
                {
                    QuestPriority.Critical => 3f,
                    QuestPriority.High => 2f,
                    QuestPriority.Medium => 1.5f,
                    QuestPriority.Low => 1f,
                    QuestPriority.Backlog => 0.5f,
                    _ => 1f
                };

                totalWeight += weight;
                if (quest.status == QuestStatus.Completed)
                {
                    completedWeight += weight;
                }
            }

            return totalWeight > 0 ? completedWeight / totalWeight : 0;
        }

        private string GetMotivationalQuote()
        {
            // Return a different quote based on progress or time of day
            float progress = CalculateOverallProgress();

            if (progress < 0.2f)
                return motivationalQuotes[0];
            else if (progress < 0.5f)
                return motivationalQuotes[UnityEngine.Random.Range(1, 4)];
            else if (progress < 0.8f)
                return motivationalQuotes[UnityEngine.Random.Range(4, 6)];
            else
                return motivationalQuotes[UnityEngine.Random.Range(6, motivationalQuotes.Length)];
        }

        private Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static DevelopmentQuest GetQuestById(string id)
        {
            return allQuests.FirstOrDefault(q => q.id == id);
        }

        [System.Serializable]
        private class Serialization<T>
        {
            public List<T> target;
            public Serialization(List<T> target) { this.target = target; }
        }
    }
}