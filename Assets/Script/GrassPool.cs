using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GrassPool
{
    public GameObject grassPrefab;
    public int poolSize = 1000;
    public Transform poolParent;

    private Queue<GameObject> availableGrass = new Queue<GameObject>();
    private List<GameObject> allGrass = new List<GameObject>();
    private List<GameObject> activeGrass = new List<GameObject>();

    public void InitializePool()
    {
        if (poolParent == null)
        {
            GameObject parentObj = new GameObject("GrassPool");
            poolParent = parentObj.transform;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject grass = GameObject.Instantiate(grassPrefab);
            grass.SetActive(false);
            grass.transform.SetParent(poolParent);
            availableGrass.Enqueue(grass);
            allGrass.Add(grass);
        }

        Debug.Log($"草对象池初始化完成，大小: {poolSize}");
    }

    public GameObject GetGrass()
    {
        if (availableGrass.Count == 0)
        {
            // 如果池子空了，动态扩展
            ExpandPool(100);
        }

        GameObject grass = availableGrass.Dequeue();
        grass.SetActive(true);
        activeGrass.Add(grass);
        return grass;
    }

    public void ReturnGrass(GameObject grass)
    {
        if (grass != null && allGrass.Contains(grass))
        {
            grass.SetActive(false);
            grass.transform.SetParent(poolParent);
            activeGrass.Remove(grass);
            availableGrass.Enqueue(grass);
        }
    }

    private void ExpandPool(int expandSize)
    {
        Debug.Log($"对象池扩展 {expandSize} 个单位");

        for (int i = 0; i < expandSize; i++)
        {
            GameObject grass = GameObject.Instantiate(grassPrefab);
            grass.SetActive(false);
            grass.transform.SetParent(poolParent);
            availableGrass.Enqueue(grass);
            allGrass.Add(grass);
        }

        poolSize += expandSize;
    }

    public void ClearAllGrass()
    {
        foreach (GameObject grass in activeGrass.ToArray())
        {
            ReturnGrass(grass);
        }
        activeGrass.Clear();
    }

    public int GetActiveCount()
    {
        return activeGrass.Count;
    }

    public int GetAvailableCount()
    {
        return availableGrass.Count;
    }
}