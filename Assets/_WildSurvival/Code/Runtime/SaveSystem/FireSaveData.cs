using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Save data for the fire system
/// </summary>
[Serializable]
public class FireSaveData
{
    [Serializable]
    public class FireInstanceData
    {
        public string fireID;
        public FireInstance.FireType fireType;
        public FireInstance.FireState fireState;
        public Vector3 position;
        public Quaternion rotation;
        public float temperature;
        public float fuelAmount;
        public float maxFuel;
        public float burnTimeElapsed;
        public List<FuelItemData> fuelItems;
        public bool isLit;
        public bool isPlayerMade;
        public string customData;

        public FireInstanceData()
        {
            fuelItems = new List<FuelItemData>();
        }
    }

    [Serializable]
    public class FuelItemData
    {
        public string itemID;
        public int quantity;
        public float burnTime;
        public FireInstance.FuelType fuelType;
    }

    public List<FireInstanceData> activeFires;
    public float totalPlayTime;
    public int firesBuilt;
    public int firesExtinguished;
    public float totalBurnTime;
    public string lastSaveTime;

    public FireSaveData()
    {
        activeFires = new List<FireInstanceData>();
        lastSaveTime = DateTime.Now.ToString();
    }

    /// <summary>
    /// Create save data from current fire instances
    /// </summary>
    public static FireSaveData CreateFromCurrentState()
    {
        FireSaveData data = new FireSaveData();

        // Find all active fire instances
        FireInstance[] fires = GameObject.FindObjectsOfType<FireInstance>();

        foreach (var fire in fires)
        {
            if (fire != null && fire.GetState() != FireInstance.FireState.Extinguished)
            {
                FireInstanceData fireData = new FireInstanceData
                {
                    fireID = fire.gameObject.name + "_" + fire.GetInstanceID(),
                    fireType = fire.GetFireType(),
                    fireState = fire.GetState(),
                    position = fire.transform.position,
                    rotation = fire.transform.rotation,
                    temperature = fire.GetCookingTemperature(),
                    fuelAmount = fire.GetFuelAmount(),
                    maxFuel = fire.GetMaxFuelCapacity(),
                    burnTimeElapsed = fire.GetBurnTime(),
                    isLit = fire.GetState() == FireInstance.FireState.Burning ||
                           fire.GetState() == FireInstance.FireState.Blazing,
                    isPlayerMade = fire.IsPlayerMade()
                };

                // Save fuel items
                var fuelList = fire.GetFuelItems();
                if (fuelList != null)
                {
                    foreach (var fuel in fuelList)
                    {
                        fireData.fuelItems.Add(new FuelItemData
                        {
                            itemID = fuel.itemID,
                            quantity = fuel.quantity,
                            burnTime = fuel.burnTime,
                            fuelType = fuel.fuelType
                        });
                    }
                }

                data.activeFires.Add(fireData);
            }
        }

        return data;
    }

    /// <summary>
    /// Restore fires from save data
    /// </summary>
    public void RestoreFireInstances()
    {
        foreach (var fireData in activeFires)
        {
            // Create fire instance based on type
            GameObject firePrefab = GetFirePrefab(fireData.fireType);
            if (firePrefab != null)
            {
                GameObject fireObj = GameObject.Instantiate(firePrefab, fireData.position, fireData.rotation);
                fireObj.name = fireData.fireID;

                FireInstance fire = fireObj.GetComponent<FireInstance>();
                if (fire != null)
                {
                    // Restore fire state
                    fire.LoadFromSaveData(fireData);
                }
            }
        }
    }

    private GameObject GetFirePrefab(FireInstance.FireType fireType)
    {
        // Load appropriate prefab based on fire type
        string prefabPath = fireType switch
        {
            FireInstance.FireType.Campfire => "Prefabs/Fire/Campfire",
            FireInstance.FireType.Torch => "Prefabs/Fire/Torch",
            FireInstance.FireType.Forge => "Prefabs/Fire/Forge",
            FireInstance.FireType.SignalFire => "Prefabs/Fire/SignalFire",
            FireInstance.FireType.CookingFire => "Prefabs/Fire/CookingFire",
            _ => "Prefabs/Fire/Campfire"
        };

        return Resources.Load<GameObject>(prefabPath);
    }
}

/// <summary>
/// Manages saving and loading of fire system data
/// </summary>
public class FireSaveLoadManager : MonoBehaviour
{
    private static FireSaveLoadManager instance;
    public static FireSaveLoadManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FireSaveLoadManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("FireSaveLoadManager");
                    instance = go.AddComponent<FireSaveLoadManager>();
                }
            }
            return instance;
        }
    }

    private const string SAVE_KEY = "FireSystemSaveData";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Save the current fire system state
    /// </summary>
    public void SaveFireSystem()
    {
        FireSaveData data = FireSaveData.CreateFromCurrentState();
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"Fire system saved: {data.activeFires.Count} active fires");
    }

    /// <summary>
    /// Load the fire system state
    /// </summary>
    public void LoadFireSystem()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            FireSaveData data = JsonUtility.FromJson<FireSaveData>(json);

            if (data != null)
            {
                data.RestoreFireInstances();
                Debug.Log($"Fire system loaded: {data.activeFires.Count} fires restored");
            }
        }
        else
        {
            Debug.Log("No fire save data found");
        }
    }

    /// <summary>
    /// Clear all save data
    /// </summary>
    public void ClearSaveData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("Fire save data cleared");
        }
    }

    // Auto-save functionality
    private float autoSaveInterval = 60f; // Save every minute
    private float nextAutoSave;

    private void Start()
    {
        nextAutoSave = Time.time + autoSaveInterval;
    }

    private void Update()
    {
        if (Time.time >= nextAutoSave)
        {
            SaveFireSystem();
            nextAutoSave = Time.time + autoSaveInterval;
        }
    }
}

// Extension methods are now in FireInstanceExtensions.cs, not here