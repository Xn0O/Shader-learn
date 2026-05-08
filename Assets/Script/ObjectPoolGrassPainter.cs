using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolGrassPainter : MonoBehaviour
{
    [Header("刷草设置")]
    public GrassPool grassPool;
    public LayerMask groundLayer = 1;

    [Header("笔刷设置")]
    public float brushSize = 5f;
    [Range(0.1f, 1f)]
    public float brushDensity = 0.5f;
    public bool randomRotation = true;
    public bool randomScale = true;

    [Header("缩放范围")]
    [Range(0.5f, 2f)]
    public float minScale = 0.8f;
    [Range(0.5f, 2f)]
    public float maxScale = 1.2f;

    [Header("操作设置")]
    public KeyCode paintKey = KeyCode.P;
    public KeyCode eraseKey = KeyCode.E;
    public KeyCode clearAllKey = KeyCode.C;

    [Header("性能显示")]
    public bool showDebugInfo = true;

    private bool isPainting = false;
    private bool isErasing = false;
    private List<GameObject> currentBrushGrass = new List<GameObject>();

    void Start()
    {
        if (grassPool.grassPrefab == null)
        {
            Debug.LogError("请先设置草的预制件！");
            return;
        }

        grassPool.InitializePool();
        Debug.Log("刷草系统初始化完成");
    }

    void Update()
    {
        HandleInput();

        if (isPainting)
        {
            PaintGrass();
        }
        else if (isErasing)
        {
            EraseGrass();
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            ShowDebugInfo();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(paintKey))
        {
            isPainting = true;
            isErasing = false;
            Debug.Log("刷草模式激活");
        }

        if (Input.GetKeyDown(eraseKey))
        {
            isErasing = true;
            isPainting = false;
            Debug.Log("擦除模式激活");
        }

        if (Input.GetKeyDown(clearAllKey))
        {
            grassPool.ClearAllGrass();
            currentBrushGrass.Clear();
            Debug.Log("已清除所有草");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPainting = false;
            isErasing = false;
            Debug.Log("退出刷草模式");
        }

        if (Input.GetKey(KeyCode.LeftBracket))
        {
            brushSize = Mathf.Max(0.5f, brushSize - Time.deltaTime * 2f);
        }
        if (Input.GetKey(KeyCode.RightBracket))
        {
            brushSize = Mathf.Min(20f, brushSize + Time.deltaTime * 2f);
        }
    }

    void PaintGrass()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                Vector3 paintPosition = hit.point;
                int grassCount = Mathf.RoundToInt(brushSize * brushDensity * 5f);

                for (int i = 0; i < grassCount; i++)
                {
                    SpawnSingleGrass(paintPosition);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            currentBrushGrass.Clear();
        }
    }

    void SpawnSingleGrass(Vector3 centerPosition)
    {
        Vector2 randomCircle = Random.insideUnitCircle * brushSize;
        Vector3 spawnPos = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

        RaycastHit groundHit;
        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out groundHit, 10f, groundLayer))
        {
            GameObject grass = grassPool.GetGrass();
            grass.transform.position = groundHit.point;

            if (randomRotation)
            {
                grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            }

            if (randomScale)
            {
                float scale = Random.Range(minScale, maxScale);
                grass.transform.localScale = Vector3.one * scale;
            }

            currentBrushGrass.Add(grass);
        }
    }

    void EraseGrass()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                Collider[] hitColliders = Physics.OverlapSphere(hit.point, brushSize);

                foreach (Collider col in hitColliders)
                {
                    if (col.CompareTag("Grass") || col.gameObject.layer == LayerMask.NameToLayer("Grass"))
                    {
                        grassPool.ReturnGrass(col.gameObject);
                    }
                }
            }
        }
    }

    void ShowDebugInfo()
    {
        Debug.Log($"对象池状态 - 活跃: {grassPool.GetActiveCount()}, 可用: {grassPool.GetAvailableCount()}, 总计: {grassPool.poolSize}");
    }

    void OnDrawGizmos()
    {
        if (isPainting || isErasing)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                Gizmos.color = isPainting ? Color.green : Color.red;
                Gizmos.DrawWireSphere(hit.point, brushSize);
            }
        }
    }
}