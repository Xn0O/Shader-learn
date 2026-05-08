using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class SimpleGrassBrush : EditorWindow
{
    [MenuItem("Tools/简单草刷")]
    public static void ShowWindow()
    {
        GetWindow<SimpleGrassBrush>("简单草刷");
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

    [Header("渐进加载")]
    private bool useProgressiveLoading = true;

    private bool isPainting = false;
    private Transform grassParent;
    private List<GameObject> paintedGrass = new List<GameObject>();
    private SimpleGrassLoader grassLoader;

    private void OnGUI()
    {
        GUILayout.Label("草刷设置", EditorStyles.boldLabel);

        grassPrefab = (GameObject)EditorGUILayout.ObjectField("草预制件", grassPrefab, typeof(GameObject), false);
        brushSize = EditorGUILayout.Slider("笔刷大小", brushSize, 1f, 50f);
        brushDensity = EditorGUILayout.Slider("笔刷密度", brushDensity, 0.1f, 1f);
        randomRotation = EditorGUILayout.Toggle("随机旋转", randomRotation);
        minScale = EditorGUILayout.FloatField("最小缩放", minScale);
        maxScale = EditorGUILayout.FloatField("最大缩放", maxScale);
        selectedLayer = EditorGUILayout.LayerField("地面图层", selectedLayer);

        EditorGUILayout.Space();
        GUILayout.Label("渐进加载", EditorStyles.boldLabel);
        useProgressiveLoading = EditorGUILayout.Toggle("使用渐进加载", useProgressiveLoading);

        if (useProgressiveLoading)
        {
            if (GUILayout.Button("创建加载器"))
            {
                CreateLoader();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"当前草数量: {paintedGrass.Count}", EditorStyles.miniLabel);

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

        EditorGUILayout.HelpBox("点击开始刷草，在场景中点击地面放置草", MessageType.Info);
    }

    private void StartPainting()
    {
        if (grassParent == null)
        {
            grassParent = new GameObject("PaintedGrass").transform;
        }

        if (useProgressiveLoading)
        {
            FindOrCreateLoader();
        }

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void StopPainting()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void FindOrCreateLoader()
    {
        grassLoader = FindObjectOfType<SimpleGrassLoader>();
        if (grassLoader == null)
        {
            CreateLoader();
        }
    }

    private void CreateLoader()
    {
        GameObject loaderObj = new GameObject("GrassLoader");
        grassLoader = loaderObj.AddComponent<SimpleGrassLoader>();
        Debug.Log("创建了草加载器");
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

        if (isPainting)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            int layerMask = 1 << selectedLayer;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit.point, Vector3.up, brushSize);
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

        int grassCount = Mathf.RoundToInt(brushSize * brushDensity * 8f);
        int layerMask = 1 << selectedLayer;

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

                float scale = Random.Range(minScale, maxScale);
                grass.transform.localScale = Vector3.one * scale;

                grass.transform.SetParent(grassParent);
                paintedGrass.Add(grass);

                // 设置标签（给加载器识别用）
                grass.tag = "Grass";

                // 注册到加载器或直接显示
                if (useProgressiveLoading && grassLoader != null)
                {
                    grassLoader.AddGrass(grass);
                }
                else
                {
                    grass.SetActive(true);
                }

                Undo.RegisterCreatedObjectUndo(grass, "Create Grass");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void ClearAllGrass()
    {
        if (grassParent != null)
        {
            Undo.DestroyObjectImmediate(grassParent.gameObject);
            grassParent = null;
        }
        paintedGrass.Clear();
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}