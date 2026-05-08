using UnityEngine;

public class SimpleLODController : MonoBehaviour
{
    [Header("LOD 模型")]
    public GameObject highDetail;     // 高细节模型
    public GameObject mediumDetail;   // 中细节模型  
    public GameObject lowDetail;      // 低细节模型

    [Header("LOD 距离设置")]
    public float mediumDistance = 10f;  // 切换到中细节的距离
    public float lowDistance = 20f;     // 切换到低细节的距离
    public float cullDistance = 30f;    // 完全隐藏的距离

    [Header("性能设置")]
    public float checkInterval = 1f;    // 检查间隔（秒）

    [Header("调试")]
    public bool showDebugInfo = true;   // 改为true以便调试
    public bool alwaysShowHighDetail = false; // 强制显示高细节（调试用）

    private Camera mainCamera;
    private float checkTimer = 0f;
    private int currentLOD = 0;

    void Start()
    {
        mainCamera = Camera.main;

        // 验证设置
        if (highDetail == null)
        {
            Debug.LogError($"{gameObject.name}: 高细节模型未设置！", this);
            enabled = false;
            return;
        }

        // 初始显示高细节
        SwitchToLOD(0);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: LOD控制器启动 - 高细节: {highDetail != null}, 中细节: {mediumDetail != null}, 低细节: {lowDetail != null}");
        }
    }

    void Update()
    {
        if (alwaysShowHighDetail)
        {
            SwitchToLOD(0);
            return;
        }

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            UpdateLOD();
        }
    }

    void UpdateLOD()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                if (showDebugInfo) Debug.LogWarning($"{gameObject.name}: 未找到主摄像机");
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        int newLOD = CalculateLODLevel(distance);

        if (newLOD != currentLOD)
        {
            SwitchToLOD(newLOD);
            currentLOD = newLOD;

            if (showDebugInfo)
            {
                string lodName = GetLODName(newLOD);
                Debug.Log($"{gameObject.name}: LOD 切换: {lodName} (距离: {distance:F1}m, 当前LOD: {currentLOD})");
            }
        }
    }

    int CalculateLODLevel(float distance)
    {
        if (distance > cullDistance)
            return -1;  // 剔除
        else if (distance > lowDistance && lowDetail != null)
            return 2;   // 低细节
        else if (distance > mediumDistance && mediumDetail != null)
            return 1;   // 中细节
        else
            return 0;   // 高细节
    }

    string GetLODName(int lodLevel)
    {
        switch (lodLevel)
        {
            case 0: return "高细节";
            case 1: return "中细节";
            case 2: return "低细节";
            case -1: return "已剔除";
            default: return "未知";
        }
    }

    void SwitchToLOD(int lodLevel)
    {
        // 禁用所有
        if (highDetail != null) highDetail.SetActive(false);
        if (mediumDetail != null) mediumDetail.SetActive(false);
        if (lowDetail != null) lowDetail.SetActive(false);

        // 启用对应的LOD级别
        switch (lodLevel)
        {
            case 0: // 高细节
                if (highDetail != null)
                {
                    highDetail.SetActive(true);
                    if (showDebugInfo && currentLOD != 0)
                        Debug.Log($"{gameObject.name}: 切换到高细节");
                }
                break;
            case 1: // 中细节
                if (mediumDetail != null)
                {
                    mediumDetail.SetActive(true);
                    if (showDebugInfo && currentLOD != 1)
                        Debug.Log($"{gameObject.name}: 切换到中细节");
                }
                break;
            case 2: // 低细节
                if (lowDetail != null)
                {
                    lowDetail.SetActive(true);
                    if (showDebugInfo && currentLOD != 2)
                        Debug.Log($"{gameObject.name}: 切换到低细节");
                }
                break;
            case -1: // 剔除
                if (showDebugInfo && currentLOD != -1)
                    Debug.Log($"{gameObject.name}: 已剔除");
                break;
        }
    }

    // 在编辑器中显示当前状态
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 绘制LOD距离范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, mediumDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lowDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, cullDistance);

        // 显示当前LOD状态
        string status = $"LOD: {GetLODName(currentLOD)}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, status);
    }

    // 公开方法用于调试
    public void ForceLODUpdate()
    {
        UpdateLOD();
    }

    public string GetCurrentStatus()
    {
        if (mainCamera == null) return "摄像机未找到";
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        return $"距离: {distance:F1}m, LOD: {GetLODName(currentLOD)}";
    }
}