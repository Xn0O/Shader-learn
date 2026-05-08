using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMove : MonoBehaviour
{
    private CharacterController _controller;
    private GameObject _mainCamera;
    public GameObject playerModle;
    private Animator _animator;

    // 移动参数
    public float speed = 6.0f;
    float _targetRot = 0.0f;

    // 平滑旋转
    public float RotationSmoothTime = 0.1f;
    float _rotationVelocity;

    // 跳跃参数
    [Header("跳跃设置")]
    public float jumpHeight = 3.0f;
    public float gravity = -20f;
    public float jumpCooldown = 0.1f;

    // 跳跃状态
    private bool _jumpPressed = false;
    private float _verticalVelocity;
    private float _lastJumpTime = -1f;
    private bool _isJumping = false;

    // 输入
    private Vector2 _move;

    // 鼠标控制
    [Header("鼠标设置")]
    public bool lockCursor = true;
    private bool _cursorLocked = true;

    public float Speed { get; internal set; }

    void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _controller = GetComponent<CharacterController>();

        if (playerModle != null)
        {
            _animator = playerModle.GetComponent<Animator>();
        }

        // 初始化鼠标状态
        UpdateCursorState();
    }

    void Update()
    {
        // 检测ESC键按下
        HandleCursorInput();

        // 1. 先处理所有输入
        ProcessInput();

        // 2. 处理移动（水平）
        HandleMovement();

        // 3. 处理跳跃和重力（垂直）
        HandleJumpAndGravity();

        // 4. 重置输入状态
        ResetInput();
    }

    void HandleCursorInput()
    {
        // 检测ESC键按下
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _cursorLocked = !_cursorLocked;
            UpdateCursorState();
        }

        // 可选：点击鼠标左键重新锁定光标
        if (Mouse.current.leftButton.wasPressedThisFrame && !_cursorLocked)
        {
            _cursorLocked = true;
            UpdateCursorState();
        }
    }

    void UpdateCursorState()
    {
        if (_cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ProcessInput()
    {
        // 这里我们只是记录输入状态，不执行逻辑
    }

    void HandleMovement()
    {
        if (_move != Vector2.zero)
        {
            HandleFreeMovement();
        }
        else
        {
            // 设置静止动画
            _animator.SetBool("Move", false);
        }
    }

    void HandleFreeMovement()
    {
        // 计算输入方向
        Vector3 inputDir = new Vector3(_move.x, 0.0f, _move.y).normalized;

        // 计算旋转角度
        _targetRot = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;

        // 平滑旋转
        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRot, ref _rotationVelocity, RotationSmoothTime);

        // 旋转玩家
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

        // 计算移动方向
        Vector3 targetDir = Quaternion.Euler(0.0f, _targetRot, 0.0f) * Vector3.forward;

        // 水平移动
        Vector3 horizontalVelocity = targetDir.normalized * (speed * Time.deltaTime);

        // 设置移动动画
        _animator.SetBool("Move", true);

        // 应用水平移动
        _controller.Move(horizontalVelocity);
    }

    void HandleJumpAndGravity()
    {
        // 改进的地面检测 - 使用多个检测点
        bool isGrounded = ImprovedGroundedCheck();

        // 重置跳跃状态当落地时
        if (isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f;
            _isJumping = false;
        }

        // 处理跳跃输入
        if (_jumpPressed && isGrounded && CanJump())
        {
            // 执行跳跃
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _lastJumpTime = Time.time;
            _isJumping = true;
        }

        // 应用重力
        _verticalVelocity += gravity * Time.deltaTime;

        // 应用垂直移动
        Vector3 verticalMove = new Vector3(0, _verticalVelocity * Time.deltaTime, 0);
        _controller.Move(verticalMove);
    }

    bool ImprovedGroundedCheck()
    {
        // 使用多个射线检测点，提高移动中的检测准确性
        Vector3[] raycastPoints = new Vector3[]
        {
            transform.position,
            transform.position + transform.forward * _controller.radius * 0.7f,
            transform.position - transform.forward * _controller.radius * 0.7f,
            transform.position + transform.right * _controller.radius * 0.7f,
            transform.position - transform.right * _controller.radius * 0.7f
        };

        float rayLength = _controller.height / 2 + 0.1f;

        foreach (Vector3 point in raycastPoints)
        {
            if (Physics.Raycast(point, Vector3.down, rayLength))
            {
                return true;
            }
        }

        // 备用：使用CharacterController的检测
        return _controller.isGrounded;
    }

    bool CanJump()
    {
        // 检查跳跃冷却时间
        return Time.time - _lastJumpTime >= jumpCooldown;
    }

    void ResetInput()
    {
        // 重置跳跃输入状态
        _jumpPressed = false;
    }

    // 移动输入
    void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    // 跳跃输入
    void OnJump(InputValue value)
    {
        _jumpPressed = value.isPressed;
    }

    // 在场景视图中显示地面检测射线
    void OnDrawGizmosSelected()
    {
        if (_controller != null)
        {
            // 绘制多个检测射线
            Gizmos.color = Color.blue;
            Vector3[] raycastPoints = new Vector3[]
            {
                transform.position,
                transform.position + transform.forward * _controller.radius * 0.7f,
                transform.position - transform.forward * _controller.radius * 0.7f,
                transform.position + transform.right * _controller.radius * 0.7f,
                transform.position - transform.right * _controller.radius * 0.7f
            };

            float rayLength = _controller.height / 2 + 0.1f;

            foreach (Vector3 point in raycastPoints)
            {
                Gizmos.DrawLine(point, point + Vector3.down * rayLength);
            }
        }
    }

    // 可选：提供公共方法来控制光标状态
    public void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        UpdateCursorState();
    }

    public bool IsCursorLocked()
    {
        return _cursorLocked;
    }
}