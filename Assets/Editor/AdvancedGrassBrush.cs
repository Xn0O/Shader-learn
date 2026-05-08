using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class AdvancedGrassBrush : EditorWindow
{
    [MenuItem("Tools/高级草刷")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedGrassBrush>("高级草刷");
    }

    [Header("草预制件")]
    public GameObject grassPrefab;

    [Header("笔刷设置")]
    private float brushSize = 3f;
    private float brushDensity = 0.3f;
    private bool randomRotation = true;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private int selectedLayer = 0;

    [Header("LOD 设置")]
    private bool enableLOD = true;
    private float mediumDistance = 50f;
    private float lowDistance = 80f;
    private float cullDistance = 100f;

    [Header("性能设置")]
    private int maxGrassCount = 1000;
    private bool showGrassCount = true;

    private bool isPainting = false;
    private Transform grassParent;
    private List<GameObject> paintedGrass = new List<GameObject>();

    private void OnGUI()
    {
        GUILayout.Label("草刷设置", EditorStyles.boldLabel);

        grassPrefab = (GameObject)EditorGUILayout.ObjectField("草预制件", grassPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        GUILayout.Label("笔刷设置", EditorStyles.boldLabel);
        brushSize = EditorGUILayout.Slider("笔刷大小", brushSize, 1f, 200f);
        brushDensity = EditorGUILayout.Slider("笔刷密度", brushDensity, 0.1f, 1f);
        randomRotation = EditorGUILayout.Toggle("随机旋转", randomRotation);
        minScale = EditorGUILayout.FloatField("最小缩放", minScale);
        maxScale = EditorGUILayout.FloatField("最大缩放", maxScale);
        selectedLayer = EditorGUILayout.LayerField("地面图层", selectedLayer);

        EditorGUILayout.Space();
        GUILayout.Label("LOD 设置", EditorStyles.boldLabel);

        enableLOD = EditorGUILayout.Toggle("启用 LOD", enableLOD);
        if (enableLOD)
        {
            mediumDistance = EditorGUILayout.FloatField("中细节距离", mediumDistance);
            lowDistance = EditorGUILayout.FloatField("低细节距离", lowDistance);
            cullDistance = EditorGUILayout.FloatField("剔除距离", cullDistance);

            EditorGUILayout.HelpBox($"LOD范围: 0-{mediumDistance}m(高) {mediumDistance}-{lowDistance}m(中) {lowDistance}-{cullDistance}m(低) {cullDistance}m+(剔除)", MessageType.Info);
        }

        EditorGUILayout.Space();
        GUILayout.Label("性能设置", EditorStyles.boldLabel);
        maxGrassCount = EditorGUILayout.IntField("最大草数量", maxGrassCount);
        showGrassCount = EditorGUILayout.Toggle("显示数量", showGrassCount);

        if (showGrassCount)
        {
            EditorGUILayout.LabelField($"当前草数量: {paintedGrass.Count}/{maxGrassCount}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button(isPainting ? "停止刷草" : "开始刷草"))
        {
            isPainting = !isPainting;
            if (isPainting) StartPainting();
            else StopPainting();
        }

        if (GUILayout.Button("清除所有草"))
        {
            ClearAllGrass();
        }

        if (GUILayout.Button("测试LOD系统"))
        {
            TestLODSystem();
        }

        EditorGUILayout.HelpBox("点击开始刷草，然后在场景中点击地面放置草", MessageType.Info);
    }

    private void StartPainting()
    {
        if (grassParent == null)
        {
            grassParent = new GameObject("PaintedGrass").transform;
        }
        SceneView.duringSceneGui += OnSceneGUI;
        Debug.Log("开始刷草模式");
    }

    private void StopPainting()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Debug.Log("停止刷草模式");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (isPainting && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            int layerMask = 1 << selectedLayer;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                PaintGrass(hit.point);
                e.Use();
            }
        }

        // 显示笔刷范围
        if (isPainting)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            int layerMask = 1 << selectedLayer;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit.point, Vector3.up, brushSize);

                // 显示笔刷信息
                string info = $"笔刷大小: {brushSize}\n密度: {brushDensity}";
                Handles.Label(hit.point + Vector3.up * 0.5f, info);
            }
        }
    }

    private void PaintGrass(Vector3 position)
    {
        if (grassPrefab == null)
        {
            Debug.LogError("请先设置草预制件！");
            return;
        }

        // 检查数量限制
        if (paintedGrass.Count >= maxGrassCount)
        {
            Debug.LogWarning($"已达到最大草数量限制: {maxGrassCount}");
            return;
        }

        int grassCount = Mathf.RoundToInt(brushSize * brushDensity * 8f);
        grassCount = Mathf.Min(grassCount, maxGrassCount - paintedGrass.Count);

        int layerMask = 1 << selectedLayer;
        int createdCount = 0;

        for (int i = 0; i < grassCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * brushSize;
            Vector3 spawnPos = position + new Vector3(randomCircle.x, 0, randomCircle.y);

            RaycastHit groundHit;
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out groundHit, 10f, layerMask))
            {
                GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab);
                grass.transform.position = groundHit.point;

                if (randomRotation)
                {
                    grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                }

                // 随机缩放
                float scale = Random.Range(minScale, maxScale);
                grass.transform.localScale = Vector3.one * scale;

                // 设置 LOD
                if (enableLOD)
                {
                    SetupLOD(grass);
                }

                grass.transform.SetParent(grassParent);
                paintedGrass.Add(grass);
                createdCount++;

                Undo.RegisterCreatedObjectUndo(grass, "Create Grass");
            }
        }

        Debug.Log($"创建了 {createdCount} 棵草，总计 {paintedGrass.Count}/{maxGrassCount}");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void SetupLOD(GameObject grass)
    {
        SimpleLODController lodController = grass.GetComponent<SimpleLODController>();
        if (lodController == null)
        {
            Debug.LogWarning($"草预制件 {grassPrefab.name} 没有 SimpleLODController 组件！");
            return;
        }

        // 设置 LOD 距离
        lodController.mediumDistance = mediumDistance;
        lodController.lowDistance = lowDistance;
        lodController.cullDistance = cullDistance;
    }

    private void TestLODSystem()
    {
        if (grassPrefab == null)
        {
            Debug.LogError("请先设置草预制件！");
            return;
        }

        // 在场景中心创建一个测试草
        Vector3 testPosition = Vector3.zero;
        RaycastHit groundHit;

        if (Physics.Raycast(testPosition + Vector3.up * 10f, Vector3.down, out groundHit, 20f, 1 << selectedLayer))
        {
            GameObject testGrass = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab);
            testGrass.transform.position = groundHit.point;
            testGrass.name = "LOD_Test_Grass";

            if (enableLOD)
            {
                SetupLOD(testGrass);
            }

            // 启用调试信息
            SimpleLODController lodController = testGrass.GetComponent<SimpleLODController>();
            if (lodController != null)
            {
                lodController.showDebugInfo = true;
            }

            Debug.Log("创建了 LOD 测试草，移动摄像机来测试 LOD 切换");
        }
    }

    private void ClearAllGrass()
    {
        if (grassParent != null)
        {
            Undo.DestroyObjectImmediate(grassParent.gameObject);
            grassParent = null;
        }
        paintedGrass.Clear();
        Debug.Log("已清除所有草");
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}
