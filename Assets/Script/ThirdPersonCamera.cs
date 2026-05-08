using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    private GameObject _mainCamera;
    [Header("Cenimachine")]
    [Tooltip("跟随目标")]
    public GameObject CameraTarget;
    [Tooltip("上最大角度")]
    public float TopClamp = 70.0f;
    [Tooltip("下最大角度")]
    public float BottomClamp = -30.0f;

    [Header("灵敏度设置")]
    [Tooltip("水平旋转灵敏度")]
    public float HorizontalSensitivity = 1.0f;
    [Tooltip("垂直旋转灵敏度")]
    public float VerticalSensitivity = 1.0f;
    [Tooltip("是否反转Y轴")]
    public bool InvertY = false;

    [Header("锁定模式设置")]
    public float lockOnSmoothTime = 0.1f;
    public Vector3 lockOnOffset = new Vector3(0, 1.5f, 0);
    public float lockOnDistance = 4f;
    public float lockOnHeight = 1f;
    public float lockOnMaxAngle = 60f; // 锁定后镜头最大偏移角度

    private const float _threshold = 0.01f;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // 锁定模式变量
    private bool isLockOnMode = false;
    private Transform lockOnTarget;
    private Vector3 cameraVelocity;
    private bool allowCameraControlWhenLocked = false;
    private float lockedCameraSensitivity = 0.5f;

    // 相机偏移
    private Vector3 cameraOffset;
    private Quaternion lockedCameraBaseRotation;

    void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _cinemachineTargetYaw = CameraTarget.transform.rotation.eulerAngles.y;

        // 初始化相机偏移
        if (_mainCamera != null)
        {
            cameraOffset = _mainCamera.transform.position - CameraTarget.transform.position;
        }
    }

    private void Update()
    {
        if (isLockOnMode && lockOnTarget != null)
        {
            UpdateLockOnCamera();
        }
        else
        {
            UpdateFreeCamera();
        }
    }

    void UpdateFreeCamera()
    {
        if (_look.sqrMagnitude >= _threshold)
        {
            float verticalInput = _look.y;

            if (InvertY)
            {
                verticalInput = -verticalInput;
            }

            _cinemachineTargetYaw += _look.x * HorizontalSensitivity;
            _cinemachineTargetPitch += verticalInput * VerticalSensitivity;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);

        // 更新相机位置
        UpdateCameraPosition();
    }

    void UpdateLockOnCamera()
    {
        if (lockOnTarget == null)
        {
            SetLockOnMode(false, null);
            return;
        }

        // 计算基础锁定位置
        Vector3 targetLookAt = lockOnTarget.position + lockOnOffset;

        // 计算从玩家到目标的向量
        Vector3 toTarget = targetLookAt - CameraTarget.transform.position;
        toTarget.y = 0; // 保持水平

        // 计算基础相机位置
        Vector3 baseCameraPosition = CameraTarget.transform.position - toTarget.normalized * lockOnDistance + Vector3.up * lockOnHeight;

        // 应用玩家输入（如果允许）
        Vector3 finalCameraPosition = baseCameraPosition;

        if (allowCameraControlWhenLocked && _look.sqrMagnitude >= _threshold)
        {
            // 计算输入偏移
            float yawOffset = _look.x * lockedCameraSensitivity;
            float pitchOffset = _look.y * lockedCameraSensitivity * (InvertY ? -1 : 1);

            // 限制偏移角度
            yawOffset = Mathf.Clamp(yawOffset, -lockOnMaxAngle, lockOnMaxAngle);
            pitchOffset = Mathf.Clamp(pitchOffset, -lockOnMaxAngle * 0.5f, lockOnMaxAngle * 0.5f);

            // 应用偏移
            Quaternion yawRotation = Quaternion.AngleAxis(yawOffset, Vector3.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchOffset, CameraTarget.transform.right);

            finalCameraPosition = baseCameraPosition;
            Vector3 offset = finalCameraPosition - targetLookAt;
            offset = yawRotation * pitchRotation * offset;
            finalCameraPosition = targetLookAt + offset;
        }

        // 平滑移动相机
        if (_mainCamera != null)
        {
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position,
                finalCameraPosition,
                ref cameraVelocity,
                lockOnSmoothTime
            );

            // 让相机始终看向目标
            _mainCamera.transform.LookAt(targetLookAt);
        }

        // 更新相机目标的旋转，使其大致面向锁定目标
        if (toTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget);
            CameraTarget.transform.rotation = Quaternion.Slerp(
                CameraTarget.transform.rotation,
                targetRotation,
                lockOnSmoothTime * Time.deltaTime * 5f
            );
        }

        // 更新内部角度变量以保持一致性
        Vector3 euler = CameraTarget.transform.rotation.eulerAngles;
        _cinemachineTargetYaw = euler.y;
        _cinemachineTargetPitch = euler.x;
    }

    void UpdateCameraPosition()
    {
        if (_mainCamera != null)
        {
            // 基于相机目标的旋转更新相机位置
            Vector3 desiredPosition = CameraTarget.transform.position +
                                    CameraTarget.transform.forward * -cameraOffset.z +
                                    Vector3.up * cameraOffset.y;

            _mainCamera.transform.position = desiredPosition;
            _mainCamera.transform.LookAt(CameraTarget.transform.position);
        }
    }

    // 设置锁定模式
    public void SetLockOnMode(bool lockOn, Transform target = null, bool allowControl = false, float controlSensitivity = 0.5f)
    {
        isLockOnMode = lockOn;
        lockOnTarget = target;
        allowCameraControlWhenLocked = allowControl;
        lockedCameraSensitivity = controlSensitivity;

        if (lockOn && target != null)
        {
            Debug.Log($"相机进入锁定模式，目标: {target.name}, 允许控制: {allowControl}");
        }
        else
        {
            Debug.Log("相机退出锁定模式");
        }
    }

    // 更新锁定目标（用于目标切换）
    public void SetLockTarget(Transform target)
    {
        lockOnTarget = target;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private Vector2 _look;
    public void OnLook(InputValue value)
    {
        _look = value.Get<Vector2>();
    }

    // 公共方法
    public bool IsInLockOnMode() => isLockOnMode;
    public Transform GetLockOnTarget() => lockOnTarget;
}