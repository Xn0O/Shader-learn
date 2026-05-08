using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODController : MonoBehaviour
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
    public bool showDebugInfo = false;

    private Camera mainCamera;
    private float checkTimer = 0f;
    private int currentLOD = 0;

    void Start()
    {
        mainCamera = Camera.main;

        // 验证设置
        if (highDetail == null)
        {
            Debug.LogError("高细节模型未设置！", this);
            enabled = false;
            return;
        }

        // 初始显示高细节
        SwitchToLOD(0);
    }

    void Update()
    {
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
            if (mainCamera == null) return;
        }

        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        int newLOD = CalculateLODLevel(distance);

        if (newLOD != currentLOD)
        {
            SwitchToLOD(newLOD);
            currentLOD = newLOD;

            if (showDebugInfo)
            {
                Debug.Log($"LOD 切换: {currentLOD} (距离: {distance:F1}m)");
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
                if (highDetail != null) highDetail.SetActive(true);
                break;
            case 1: // 中细节
                if (mediumDetail != null) mediumDetail.SetActive(true);
                break;
            case 2: // 低细节
                if (lowDetail != null) lowDetail.SetActive(true);
                break;
            case -1: // 剔除
                // 全部禁用
                break;
        }
    }

    // 在编辑器中可视化LOD范围
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
    }
}