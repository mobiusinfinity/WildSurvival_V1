using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all in-game notifications and UI feedback
/// Place this file at: Assets/_WildSurvival/Code/Runtime/UI/Core/NotificationSystem.cs
/// </summary>
public class NotificationSystem : MonoBehaviour
{
    public enum NotificationType
    {
        Info,
        Warning,
        Critical,
        Success,
        Item,
        Achievement,
        Error,
        Quest,
        Combat,
        Fire,    // Add this for fire notifications
        Loot,
        Crafting, // Add this too
    }

    [System.Serializable]
    public class NotificationStyle
    {
        public NotificationType Type;
        public Color BackgroundColor = Color.black;
        public Color TextColor = Color.white;
        public AudioClip Sound;
        public float Duration = 3f;
    }

    [Header("UI References")]
    [SerializeField] private Transform _notificationContainer;
    [SerializeField] private GameObject _notificationPrefab;
    [SerializeField] private int _maxVisibleNotifications = 5;

    [Header("Styles")]
    [SerializeField] private NotificationStyle[] _styles;

    [Header("Animation")]
    [SerializeField] private float _slideInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    private Queue<GameObject> _notificationPool = new();
    private List<GameObject> _activeNotifications = new();
    private Dictionary<NotificationType, NotificationStyle> _styleMap = new();

    // Singleton
    private static NotificationSystem _instance;
    public static NotificationSystem Instance => _instance;

    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
            InitializeStyles();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStyles()
    {
        // Default styles if not configured
        if (_styles == null || _styles.Length == 0)
        {
            _styles = new NotificationStyle[]
            {
                new NotificationStyle
                {
                    Type = NotificationType.Info,
                    BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f),
                    TextColor = Color.white,
                    Duration = 3f
                },
                new NotificationStyle
                {
                    Type = NotificationType.Warning,
                    BackgroundColor = new Color(0.8f, 0.6f, 0f, 0.9f),
                    TextColor = Color.white,
                    Duration = 4f
                },
                new NotificationStyle
                {
                    Type = NotificationType.Critical,
                    BackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f),
                    TextColor = Color.white,
                    Duration = 5f
                },
                new NotificationStyle
                {
                    Type = NotificationType.Success,
                    BackgroundColor = new Color(0.2f, 0.7f, 0.3f, 0.9f),
                    TextColor = Color.white,
                    Duration = 3f
                },
                new NotificationStyle
                {
                    Type = NotificationType.Item,
                    BackgroundColor = new Color(0.3f, 0.4f, 0.7f, 0.9f),
                    TextColor = Color.white,
                    Duration = 2.5f
                },
                new NotificationStyle
                {
                    Type = NotificationType.Achievement,
                    BackgroundColor = new Color(0.7f, 0.5f, 0.1f, 0.9f),
                    TextColor = Color.white,
                    Duration = 6f
                }
            };
        }

        // Build lookup dictionary
        _styleMap.Clear();
        foreach (var style in _styles)
        {
            _styleMap[style.Type] = style;
        }
    }

    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Always log to console as fallback
        Debug.Log($"[{type}] {message}");

        // If UI is not set up, just use console
        if (_notificationContainer == null || _notificationPrefab == null)
        {
            return;
        }

        // Check if we have room for more notifications
        if (_activeNotifications.Count >= _maxVisibleNotifications)
        {
            // Remove oldest notification
            RemoveOldestNotification();
        }

        // Create or get from pool
        GameObject notif = GetNotificationObject();
        ConfigureNotification(notif, message, type);

        _activeNotifications.Add(notif);
        StartCoroutine(AnimateNotification(notif, GetStyle(type).Duration));
    }

    private GameObject GetNotificationObject()
    {
        GameObject notif;

        if (_notificationPool.Count > 0)
        {
            notif = _notificationPool.Dequeue();
            notif.SetActive(true);
        }
        else
        {
            notif = Instantiate(_notificationPrefab, _notificationContainer);
        }

        return notif;
    }

    private void ConfigureNotification(GameObject notif, string message, NotificationType type)
    {
        var style = GetStyle(type);

        // Set text
        var text = notif.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = message;
            text.color = style.TextColor;
        }

        // Set background color
        var image = notif.GetComponent<Image>();
        if (image != null)
        {
            image.color = style.BackgroundColor;
        }

        // Play sound if available
        if (style.Sound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(style.Sound, Camera.main.transform.position, 0.5f);
        }
    }

    private IEnumerator AnimateNotification(GameObject notif, float duration)
    {
        // Simple fade in
        var canvasGroup = notif.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notif.AddComponent<CanvasGroup>();
        }

        // Fade in
        float fadeInTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / fadeInTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Wait for duration
        yield return new WaitForSeconds(duration - fadeInTime - _fadeOutDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / _fadeOutDuration);
            yield return null;
        }

        // Return to pool
        ReturnToPool(notif);
    }

    private void RemoveOldestNotification()
    {
        if (_activeNotifications.Count > 0)
        {
            var oldest = _activeNotifications[0];
            _activeNotifications.RemoveAt(0);
            StopCoroutine(AnimateNotification(oldest, 0));
            ReturnToPool(oldest);
        }
    }

    private void ReturnToPool(GameObject notif)
    {
        _activeNotifications.Remove(notif);
        notif.SetActive(false);
        _notificationPool.Enqueue(notif);
    }

    private NotificationStyle GetStyle(NotificationType type)
    {
        if (_styleMap.TryGetValue(type, out var style))
        {
            return style;
        }

        // Return default style
        return new NotificationStyle
        {
            Type = type,
            BackgroundColor = Color.gray,
            TextColor = Color.white,
            Duration = 3f
        };
    }

    public void ClearAll()
    {
        StopAllCoroutines();

        foreach (var notif in _activeNotifications)
        {
            ReturnToPool(notif);
        }

        _activeNotifications.Clear();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}