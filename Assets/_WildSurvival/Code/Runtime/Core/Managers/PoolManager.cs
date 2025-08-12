using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pooling manager for performance optimization
/// </summary>
public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance;
    public static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PoolManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[POOL_MANAGER]");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
        
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> poolParents = new Dictionary<string, GameObject>();
        
    public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;
            
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
            GameObject parent = new GameObject($"Pool_{key}");
            parent.transform.SetParent(transform);
            poolParents[key] = parent;
        }
            
        GameObject obj;
            
        if (poolDictionary[key].Count > 0)
        {
            obj = poolDictionary[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, poolParents[key].transform);
            obj.name = prefab.name;
        }
            
        return obj;
    }
        
    public void ReturnToPool(GameObject obj)
    {
        string key = obj.name;
            
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolManager] No pool exists for {key}");
            Destroy(obj);
            return;
        }
            
        obj.SetActive(false);
        poolDictionary[key].Enqueue(obj);
    }
        
    public void PrewarmPool(GameObject prefab, int count)
    {
        string key = prefab.name;
            
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
            GameObject parent = new GameObject($"Pool_{key}");
            parent.transform.SetParent(transform);
            poolParents[key] = parent;
        }
            
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, poolParents[key].transform);
            obj.name = prefab.name;
            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }
            
        Debug.Log($"[PoolManager] Prewarmed {count} instances of {key}");
    }
}