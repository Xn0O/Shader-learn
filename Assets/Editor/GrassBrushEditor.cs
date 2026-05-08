using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class GrassBrushEditor : EditorWindow
{
    [MenuItem("Tools/刷刷草")]
    public static void ShowWindow()
    {
        GetWindow<GrassBrushEditor>("刷刷草");
    }

    [SerializeField]
    private GameObject grassPrefab;
    private float brushSize = 5f;
    private float brushDensity = 0.5f;
    private bool randomRotation = true;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private int selectedLayer = 0; // 改为存储层索引
    private string[] layerNames; // 存储层名称数组

    private bool isPainting = false;
    private Transform grassParent;
    private List<GameObject> paintedGrass = new List<GameObject>();

    private void OnEnable()
    {
        // 初始化层名称数组
        layerNames = new string[32];
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            layerNames[i] = string.IsNullOrEmpty(layerName) ? $"Layer {i}" : layerName;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("草刷设置", EditorStyles.boldLabel);

        grassPrefab = (GameObject)EditorGUILayout.ObjectField("草预制件", grassPrefab, typeof(GameObject), false);
        brushSize = EditorGUILayout.Slider("笔刷大小", brushSize, 0.5f, 200f);
        brushDensity = EditorGUILayout.Slider("笔刷密度", brushDensity, 0.1f, 1f);
        randomRotation = EditorGUILayout.Toggle("随机旋转", randomRotation);
        minScale = EditorGUILayout.FloatField("最小缩放", minScale);
        maxScale = EditorGUILayout.FloatField("最大缩放", maxScale);

        // 方法1：使用 Popup 显示层名称
        selectedLayer = EditorGUILayout.Popup("地面图层", selectedLayer, layerNames);

        // 或者方法2：使用 LayerField（需要转换）
        // selectedLayer = EditorGUILayout.LayerField("地面图层", selectedLayer);

        EditorGUILayout.Space();

        if (GUILayout.Button(isPainting ? "停止刷草" : "开始刷草"))
        {
            isPainting = !isPainting;
            if (isPainting)
            {
                StartPainting();
            }
            else
            {
                StopPainting();
            }
        }

        if (GUILayout.Button("清除所有草"))
        {
            ClearAllGrass();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("在场景视图中点击地面来刷草", MessageType.Info);

        // 显示当前选择的层信息（调试用）
        EditorGUILayout.LabelField("当前层掩码", (1 << selectedLayer).ToString());
        EditorGUILayout.LabelField("当前层名称", LayerMask.LayerToName(selectedLayer));
    }

    private void StartPainting()
    {
        if (grassParent == null)
        {
            GameObject parentObj = new GameObject("PaintedGrass");
            grassParent = parentObj.transform;
            grassParent.position = Vector3.zero;
        }

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void StopPainting()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (isPainting && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            // 将层索引转换为位掩码
            int layerMask = 1 << selectedLayer;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                PaintGrass(hit.point);
                e.Use();
            }
            else
            {
                Debug.LogWarning($"在层 {LayerMask.LayerToName(selectedLayer)} 上没有检测到碰撞！");
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

                Handles.color = new Color(0, 1, 0, 0.1f);
                Handles.DrawSolidDisc(hit.point, Vector3.up, brushSize);
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

        int grassCount = Mathf.RoundToInt(brushSize * brushDensity * 10f);
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

                EditorUtility.SetDirty(grass);
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
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}