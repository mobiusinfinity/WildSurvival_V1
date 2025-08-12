using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// UI component for journal category tabs.
/// Handles tab selection and content filtering.
/// </summary>
public class JournalTab : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Configuration
    [Header("Tab Configuration")]
    [SerializeField] private JournalCategory category;
    [SerializeField] private string tabName = "Tab";
    [SerializeField] private bool isDefaultTab = false;

    [Header("UI References")]
    [SerializeField] private Button tabButton;
    [SerializeField] private Image tabBackground;
    [SerializeField] private Image tabIcon;
    [SerializeField] private TextMeshProUGUI tabLabel;
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private GameObject notificationBadge;
    [SerializeField] private TextMeshProUGUI notificationCount;
    [SerializeField] private GameObject selectedIndicator;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Icons")]
    [SerializeField] private Sprite questIcon;
    [SerializeField] private Sprite discoveryIcon;
    [SerializeField] private Sprite recipeIcon;
    [SerializeField] private Sprite tutorialIcon;
    [SerializeField] private Sprite loreIcon;
    [SerializeField] private Sprite noteIcon;
    [SerializeField] private Sprite mapIcon;

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    #endregion

    #region Events
    public static event Action<JournalTab> OnTabSelected;
    public event Action<JournalCategory> OnCategorySelected;
    #endregion

    #region State
    private bool isActive = false;
    private bool isHovered = false;
    private bool isInteractable = true;
    private int unreadCount = 0;
    private float currentTransition = 0f;
    private Color currentColor;
    #endregion

    #region Properties
    public JournalCategory Category => category;
    public string TabName => tabName;
    public bool IsActive => isActive;
    public bool IsDefaultTab => isDefaultTab;
    public int UnreadCount => unreadCount;
    public GameObject ContentPanel => contentPanel;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeTab();
    }

    private void Start()
    {
        if (isDefaultTab)
        {
            SelectTab();
        }
        else
        {
            SetActive(false);
        }
    }

    private void Update()
    {
        UpdateVisualTransition();
    }

    private void OnEnable()
    {
        if (tabButton != null)
        {
            tabButton.onClick.AddListener(OnTabClicked);
        }
    }

    private void OnDisable()
    {
        if (tabButton != null)
        {
            tabButton.onClick.RemoveListener(OnTabClicked);
        }
    }
    #endregion

    #region Initialization
    private void InitializeTab()
    {
        // Get components if not assigned
        if (tabButton == null)
            tabButton = GetComponent<Button>();

        if (tabBackground == null)
            tabBackground = GetComponent<Image>();

        if (tabLabel == null)
            tabLabel = GetComponentInChildren<TextMeshProUGUI>();

        // Set tab name
        if (tabLabel != null && !string.IsNullOrEmpty(tabName))
        {
            tabLabel.text = tabName;
        }

        // Set icon based on category
        SetCategoryIcon();

        // Initialize visual state
        currentColor = normalColor;
        if (tabBackground != null)
        {
            tabBackground.color = currentColor;
        }

        // Hide notification badge initially
        if (notificationBadge != null)
        {
            notificationBadge.SetActive(false);
        }

        // Hide selected indicator initially
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(false);
        }
    }

    private void SetCategoryIcon()
    {
        if (tabIcon == null)
            return;

        Sprite iconToUse = null;

        switch (category)
        {
            case JournalCategory.Quest:
                iconToUse = questIcon;
                break;
            case JournalCategory.Discovery:
                iconToUse = discoveryIcon;
                break;
            case JournalCategory.Recipe:
                iconToUse = recipeIcon;
                break;
            case JournalCategory.Tutorial:
                iconToUse = tutorialIcon;
                break;
            case JournalCategory.Lore:
                iconToUse = loreIcon;
                break;
            case JournalCategory.Note:
                iconToUse = noteIcon;
                break;
            case JournalCategory.Map:
                iconToUse = mapIcon;
                break;
        }

        if (iconToUse != null)
        {
            tabIcon.sprite = iconToUse;
        }
    }
    #endregion

    #region Public Methods
    public void SelectTab()
    {
        if (!isInteractable)
            return;

        // Deselect all other tabs
        foreach (var tab in FindObjectsOfType<JournalTab>())
        {
            if (tab != this)
            {
                tab.SetActive(false);
            }
        }

        SetActive(true);

        // Fire events
        OnTabSelected?.Invoke(this);
        OnCategorySelected?.Invoke(category);
    }

    public void SetActive(bool active)
    {
        isActive = active;

        // Update visual state
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(active);
        }

        // Show/hide content panel
        if (contentPanel != null)
        {
            contentPanel.SetActive(active);
        }

        // Update color
        UpdateTabColor();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (tabButton != null)
        {
            tabButton.interactable = interactable;
        }

        UpdateTabColor();
    }

    public void UpdateUnreadCount(int count)
    {
        unreadCount = Mathf.Max(0, count);

        if (notificationBadge != null)
        {
            notificationBadge.SetActive(unreadCount > 0);

            if (notificationCount != null)
            {
                if (unreadCount > 99)
                {
                    notificationCount.text = "99+";
                }
                else
                {
                    notificationCount.text = unreadCount.ToString();
                }
            }
        }
    }

    public void RefreshContent()
    {
        // Get entries for this category
        var entries = JournalManager.Instance.GetEntriesByCategory(category);

        // Count unread entries
        int unread = 0;
        foreach (var entry in entries)
        {
            if (!entry.IsRead)
            {
                unread++;
            }
        }

        UpdateUnreadCount(unread);
    }

    public void ShowNotification()
    {
        // Play animation or effect
        if (notificationBadge != null)
        {
            // Simple pulse animation without LeanTween
            StartCoroutine(PulseAnimation());
        }
    }

    private System.Collections.IEnumerator PulseAnimation()
    {
        if (notificationBadge == null) yield break;

        Vector3 originalScale = notificationBadge.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        // Scale up
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            notificationBadge.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            notificationBadge.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        notificationBadge.transform.localScale = originalScale;
    }
    #endregion

    #region Event Handlers
    private void OnTabClicked()
    {
        SelectTab();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isInteractable)
        {
            SelectTab();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isInteractable)
        {
            isHovered = true;
            UpdateTabColor();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateTabColor();
    }
    #endregion

    #region Visual Updates
    private void UpdateTabColor()
    {
        Color targetColor = normalColor;

        if (!isInteractable)
        {
            targetColor = disabledColor;
        }
        else if (isActive)
        {
            targetColor = selectedColor;
        }
        else if (isHovered)
        {
            targetColor = hoverColor;
        }

        // Start transition
        currentTransition = 0f;
        StartCoroutine(TransitionToColor(targetColor));
    }

    private System.Collections.IEnumerator TransitionToColor(Color targetColor)
    {
        Color startColor = currentColor;

        while (currentTransition < 1f)
        {
            currentTransition += Time.deltaTime / transitionDuration;
            float t = transitionCurve.Evaluate(currentTransition);

            currentColor = Color.Lerp(startColor, targetColor, t);

            if (tabBackground != null)
            {
                tabBackground.color = currentColor;
            }

            yield return null;
        }

        currentColor = targetColor;
        if (tabBackground != null)
        {
            tabBackground.color = currentColor;
        }
    }

    private void UpdateVisualTransition()
    {
        // Additional visual updates if needed
    }
    #endregion

    #region Utility
    public void SetTabName(string name)
    {
        tabName = name;

        if (tabLabel != null)
        {
            tabLabel.text = name;
        }
    }

    public void SetTabIcon(Sprite icon)
    {
        if (tabIcon != null && icon != null)
        {
            tabIcon.sprite = icon;
        }
    }

    public void HighlightTab(float duration = 1f)
    {
        if (!isActive)
        {
            StartCoroutine(FlashHighlight(duration));
        }
    }

    private System.Collections.IEnumerator FlashHighlight(float duration)
    {
        Color originalColor = currentColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);

            if (tabBackground != null)
            {
                tabBackground.color = Color.Lerp(originalColor, selectedColor, t);
            }

            yield return null;
        }

        if (tabBackground != null)
        {
            tabBackground.color = originalColor;
        }
    }
    #endregion
}