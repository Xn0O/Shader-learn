using UnityEngine;
using System.Collections.Generic;

public class GrassRegionManager : MonoBehaviour
{
    [System.Serializable]
    public class GrassRegion
    {
        public string regionName;
        public Bounds bounds;
        public List<GameObject> grassInRegion = new List<GameObject>();
        public bool isActive = true;
    }

    public List<GrassRegion> regions = new List<GrassRegion>();
    public Transform player;
    public float activationDistance = 50f;

    private ObjectPoolGrassPainter grassPainter;

    void Start()
    {
        grassPainter = FindObjectOfType<ObjectPoolGrassPainter>();
        if (player == null)
        {
            player = Camera.main.transform;
        }
    }

    void Update()
    {
        UpdateRegionActivation();
    }

    void UpdateRegionActivation()
    {
        foreach (GrassRegion region in regions)
        {
            float distance = Vector3.Distance(player.position, region.bounds.center);
            bool shouldBeActive = distance <= activationDistance;

            if (shouldBeActive != region.isActive)
            {
                SetRegionActive(region, shouldBeActive);
            }
        }
    }

    void SetRegionActive(GrassRegion region, bool active)
    {
        region.isActive = active;

        foreach (GameObject grass in region.grassInRegion)
        {
            if (grass != null)
            {
                grass.SetActive(active);
            }
        }

        Debug.Log($"区域 {region.regionName} {(active ? "激活" : "禁用")}");
    }

    public void AddGrassToRegion(GameObject grass, Vector3 position)
    {
        foreach (GrassRegion region in regions)
        {
            if (region.bounds.Contains(position))
            {
                region.grassInRegion.Add(grass);
                return;
            }
        }

        // 如果没有找到区域，创建新区域
        CreateNewRegion(position, grass);
    }

    void CreateNewRegion(Vector3 position, GameObject grass)
    {
        GrassRegion newRegion = new GrassRegion();
        newRegion.regionName = $"Region_{regions.Count}";
        newRegion.bounds = new Bounds(position, Vector3.one * 20f);
        newRegion.grassInRegion = new List<GameObject> { grass };
        newRegion.isActive = true;

        regions.Add(newRegion);
    }

    // 在场景中显示区域边界
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        foreach (GrassRegion region in regions)
        {
            Gizmos.DrawWireCube(region.bounds.center, region.bounds.size);

            // 显示区域名称
#if UNITY_EDITOR
            UnityEditor.Handles.Label(region.bounds.center, region.regionName);
#endif
        }
    }
}