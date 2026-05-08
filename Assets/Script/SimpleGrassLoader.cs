using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleGrassLoader : MonoBehaviour
{
    [Header("加载设置")]
    public float loadDistance = 30f;      // 加载距离
    public float unloadDistance = 40f;    // 卸载距离
    public int loadPerFrame = 5;          // 每帧加载数量

    [Header("淡入效果")]
    public bool enableFadeIn = true;      // 启用淡入动画
    public float fadeDuration = 1f;       // 淡入时间

    private List<GameObject> allGrass = new List<GameObject>();
    private List<GameObject> loadedGrass = new List<GameObject>();
    private Transform player;

    void Start()
    {
        player = Camera.main?.transform;
        if (player == null)
        {
            Debug.LogError("SimpleGrassLoader: 未找到摄像机！");
            return;
        }

        // 自动收集所有草
        CollectAllGrass();
        Debug.Log($"找到 {allGrass.Count} 棵草");
    }

    void Update()
    {
        if (player == null) return;

        // 每帧处理加载
        ProcessLoading();
    }

    void CollectAllGrass()
    {
        // 方法1：通过标签查找
        GameObject[] grassByTag = GameObject.FindGameObjectsWithTag("Grass");
        allGrass.AddRange(grassByTag);

        // 方法2：通过父物体查找
        GameObject grassParent = GameObject.Find("PaintedGrass");
        if (grassParent != null)
        {
            foreach (Transform child in grassParent.transform)
            {
                if (!allGrass.Contains(child.gameObject))
                {
                    allGrass.Add(child.gameObject);
                }
            }
        }

        // 初始禁用所有草
        foreach (GameObject grass in allGrass)
        {
            if (grass != null)
            {
                grass.SetActive(false);
            }
        }
    }

    void ProcessLoading()
    {
        int loadedThisFrame = 0;

        foreach (GameObject grass in allGrass)
        {
            if (grass == null) continue;

            float distance = Vector3.Distance(grass.transform.position, player.position);
            bool isLoaded = loadedGrass.Contains(grass);

            if (!isLoaded && distance <= loadDistance && loadedThisFrame < loadPerFrame)
            {
                // 需要加载
                LoadGrass(grass);
                loadedThisFrame++;
            }
            else if (isLoaded && distance > unloadDistance)
            {
                // 需要卸载
                UnloadGrass(grass);
            }
        }
    }

    void LoadGrass(GameObject grass)
    {
        grass.SetActive(true);
        loadedGrass.Add(grass);

        if (enableFadeIn)
        {
            StartCoroutine(FadeInGrass(grass));
        }
    }

    void UnloadGrass(GameObject grass)
    {
        if (enableFadeIn)
        {
            StartCoroutine(FadeOutGrass(grass));
        }
        else
        {
            grass.SetActive(false);
        }

        loadedGrass.Remove(grass);
    }

    IEnumerator FadeInGrass(GameObject grass)
    {
        Vector3 originalScale = grass.transform.localScale;
        Vector3 startScale = originalScale * 0.3f;

        grass.transform.localScale = startScale;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            // 平滑缩放动画
            grass.transform.localScale = Vector3.Lerp(startScale, originalScale, progress);

            yield return null;
        }

        grass.transform.localScale = originalScale;
    }

    IEnumerator FadeOutGrass(GameObject grass)
    {
        Vector3 originalScale = grass.transform.localScale;
        Vector3 targetScale = originalScale * 0.3f;

        float timer = 0f;
        while (timer < fadeDuration * 0.5f) // 淡出时间短一些
        {
            timer += Time.deltaTime;
            float progress = timer / (fadeDuration * 0.5f);

            grass.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);

            yield return null;
        }

        grass.SetActive(false);
        grass.transform.localScale = originalScale; // 恢复原始大小
    }

    // 手动添加草（给草刷工具用）
    public void AddGrass(GameObject grass)
    {
        if (grass != null && !allGrass.Contains(grass))
        {
            allGrass.Add(grass);
            grass.SetActive(false); // 初始禁用
        }
    }

    // 调试信息
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 100));
        GUILayout.Label("草加载状态:");
        GUILayout.Label($"总数量: {allGrass.Count}");
        GUILayout.Label($"已加载: {loadedGrass.Count}");
        GUILayout.Label($"加载距离: {loadDistance}m");
        GUILayout.EndArea();
    }
}