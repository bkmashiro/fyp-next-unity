using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleTransformController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask handleLayer;
    [SerializeField] private LayerMask objectLayer;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 0.05f;
    [SerializeField] private float rotationSpeed = 0.5f;
    [SerializeField] private float scaleSpeed = 0.01f;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Handle Settings")]
    [SerializeField] private float handleScaleFactor = 1.0f;
    [SerializeField] private float minHandleScale = 0.1f;
    [SerializeField] private float maxHandleScale = 2.0f;

    // 当前选中的物体
    private Transform _selectedObject;
    private Renderer _selectedRenderer;

    // 手柄引用
    private Transform _positionHandles;
    private Transform _rotationHandles;
    private Transform _scaleHandles;

    // 当前激活的手柄类型
    public enum HandleType { None, Position, Rotation, Scale }
    private HandleType _currentHandleType = HandleType.None;

    // 拖动状态
    private bool _isDragging = false;
    private Transform _draggedHandle = null;
    private Vector2 _dragStartPosition;
    private Vector3 _objectStartPosition;
    private Quaternion _objectStartRotation;
    private Vector3 _objectStartScale;
    private Vector3 _dragPlaneNormal;
    private Plane _dragPlane;

    // 触摸状态
    private float _touchStartTime;
    private Vector2 _touchStartPosition;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 获取手柄引用
        _positionHandles = transform.Find("PositionHandles");
        _rotationHandles = transform.Find("RotationHandles");
        _scaleHandles = transform.Find("ScaleHandles");

        // 初始时隐藏所有手柄
        SetHandleVisibility(HandleType.None);
    }

    private void Update()
    {
        // 处理触摸输入
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            // // Debug.Log("touch.position: " + touch.position);
            // 检查是否点击到UI
            if (IsPointerOverUIObject(touch.position)) return;
            // // Debug.Log("Not over UI");
            HandleTouchInput(touch);
        }
        // 处理鼠标输入
        else
        {
            HandleMouseInput();
        }

        // 更新手柄位置
        if (_selectedObject != null)
        {
            transform.position = _selectedObject.position;
            UpdateHandleScale();
        }
    }

    private void HandleTouchInput(Touch touch)
    {
        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        // // Debug.Log("Touch phase: " + touch.phase + ", position: " + touch.position);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _touchStartTime = Time.time;
                _touchStartPosition = touch.position;
                // // Debug.Log("Touch began at: " + touch.position);

                // 检测点击的是物体还是手柄
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, handleLayer))
                {
                    // // Debug.Log("Touch hit handle: " + hit.transform.name);
                    // 点击了手柄
                    _draggedHandle = hit.transform;
                    _isDragging = true;

                    // 记录初始状态
                    _dragStartPosition = touch.position;
                    if (_selectedObject != null)
                    {
                        _objectStartPosition = _selectedObject.position;
                        _objectStartRotation = _selectedObject.rotation;
                        _objectStartScale = _selectedObject.localScale;
                        // // Debug.Log("Selected object position: " + _objectStartPosition);
                    }
                    else
                    {
                        // // Debug.LogWarning("No selected object when trying to drag handle!");
                    }
                }
                else if (Physics.Raycast(ray, out hit, 1000f, objectLayer))
                {
                    // // Debug.Log("Touch hit object: " + hit.transform.name);
                    // 点击了物体
                    SelectObject(hit.transform);
                }
                else
                {
                    // // Debug.Log("Touch did not hit anything");
                }
                break;

            case TouchPhase.Moved:
                if (_isDragging && _draggedHandle != null && _selectedObject != null)
                {
                    // // Debug.Log("Touch moved, dragging handle: " + _draggedHandle.name);
                    // 计算屏幕空间偏移
                    Vector2 screenDelta = touch.position - _dragStartPosition;
                    // // Debug.Log("Screen delta: " + screenDelta);

                    // 根据手柄类型执行不同的操作
                    // 获取手柄的父对象
                    Transform handleParent = GetHandleParent(_draggedHandle);
                    // // Debug.Log("Handle parent: " + (handleParent != null ? handleParent.name : "null"));

                    if (handleParent == _positionHandles)
                    {
                        // // Debug.Log("Moving object");
                        // 移动物体：直接使用手柄的forward方向
                        Vector3 moveDirection = _draggedHandle.up;

                        // 如果是Z轴方向，将方向取反
                        if (_draggedHandle.name.Contains("Z"))
                        {
                            moveDirection = -moveDirection;
                        }

                        // 计算屏幕空间中的移动方向
                        Vector3 screenDirection = new Vector3(screenDelta.x, screenDelta.y, 0).normalized;

                        // 将屏幕方向投影到相机平面上
                        Vector3 cameraRight = mainCamera.transform.right;
                        Vector3 cameraUp = mainCamera.transform.up;

                        // 计算世界空间中的移动方向
                        Vector3 worldDirection = (cameraRight * screenDirection.x + cameraUp * screenDirection.y).normalized;

                        // 将世界空间方向投影到物体的移动方向上
                        Vector3 projectedDirection = Vector3.Project(worldDirection, moveDirection).normalized;

                        // 计算移动距离
                        float moveDistance = screenDelta.magnitude * moveSpeed;

                        // 应用移动
                        Vector3 newPosition = _objectStartPosition + projectedDirection * moveDistance;
                        _selectedObject.position = newPosition;
                        
                        // 调试信息
                        // Debug.Log($"Parent moved to: {_selectedObject.position}");
                        // foreach (Transform child in _selectedObject)
                        // {
                        //     if (child != null)
                        //     {
                        //         // Debug.Log($"Child '{child.name}' - Local: {child.localPosition}, World: {child.position}");
                        //         // 检查是否有特殊组件
                        //         var components = child.GetComponents<Component>();
                        //         foreach (var comp in components)
                        //         {
                        //             if (comp != null && !(comp is Transform))
                        //             {
                        //                 // Debug.Log($"  - Has component: {comp.GetType().Name}");
                        //             }
                        //         }
                        //     }
                        // }
                    }
                    else if (handleParent == _rotationHandles)
                    {
                        // // Debug.Log("Rotating object");
                        // 旋转物体：绕着手柄的上方向旋转
                        Vector3 rotationAxis = _draggedHandle.up;

                        // 计算屏幕空间中的移动方向
                        Vector3 screenDirection = new Vector3(screenDelta.x, screenDelta.y, 0).normalized;

                        // 将屏幕方向投影到相机平面上
                        Vector3 cameraRight = mainCamera.transform.right;
                        Vector3 cameraUp = mainCamera.transform.up;

                        // 计算世界空间中的移动方向
                        Vector3 worldDirection = (cameraRight * screenDirection.x + cameraUp * screenDirection.y).normalized;

                        // 将世界空间方向投影到圆环的切线方向上
                        // 圆环的切线方向是圆环的forward方向
                        Vector3 tangentDirection = _draggedHandle.forward;
                        Vector3 projectedDirection = Vector3.Project(worldDirection, tangentDirection).normalized;

                        // 计算旋转角度
                        // 使用投影后的方向与圆环的切线方向的点积来确定旋转方向
                        float dotProduct = Vector3.Dot(projectedDirection, tangentDirection);
                        // 使用投影后的方向与圆环的up方向的叉积来确定旋转的正负
                        float crossProduct = Vector3.Dot(Vector3.Cross(projectedDirection, tangentDirection), rotationAxis);

                        // 计算旋转角度
                        // 根据手柄名称调整旋转方向
                        float directionMultiplier = 1.0f;
                        if (_draggedHandle.name.Contains("X"))
                        {
                            directionMultiplier = -1.0f; // X轴圆环反向
                        }
                        else if (_draggedHandle.name.Contains("Y"))
                        {
                            directionMultiplier = 1.0f; // Y轴圆环正向
                        }
                        else if (_draggedHandle.name.Contains("Z"))
                        {
                            directionMultiplier = -1.0f; // Z轴圆环反向
                        }

                        float angle = -Mathf.Sign(crossProduct) * screenDelta.magnitude * rotationSpeed * directionMultiplier;

                        _selectedObject.rotation = Quaternion.AngleAxis(angle, rotationAxis) * _objectStartRotation;
                    }
                    else if (handleParent == _scaleHandles)
                    {
                        // // Debug.Log("Scaling object");
                        // 缩放物体：沿着手柄的前向方向缩放
                        float scaleFactor = 1f + screenDelta.y * scaleSpeed;
                        _selectedObject.localScale = _objectStartScale * scaleFactor;
                    }
                    else
                    {
                        // // Debug.LogWarning("Handle parent not recognized: " + (handleParent != null ? handleParent.name : "null"));
                    }
                }
                else
                {
                    // // Debug.Log("Not dragging or no handle or no selected object");
                    // if (!_isDragging) // Debug.Log("_isDragging is false");
                    // if (_draggedHandle == null) // Debug.Log("_draggedHandle is null");
                    // if (_selectedObject == null) // Debug.Log("_selectedObject is null");
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                // // Debug.Log("Touch ended or canceled");
                // 长按（超过0.5秒）视为取消选择
                if (Time.time - _touchStartTime > 0.5f && !_isDragging)
                {
                    DeselectObject();
                }

                _isDragging = false;
                _draggedHandle = null;
                break;
        }
    }

    // 获取手柄的父对象
    private Transform GetHandleParent(Transform handle)
    {
        if (handle == null) return null;

        // 向上查找直到找到正确的手柄父物体
        Transform current = handle;
        while (current != null &&
               current != _positionHandles &&
               current != _rotationHandles &&
               current != _scaleHandles)
        {
            current = current.parent;
        }

        return current;
    }

    private void HandleMouseInput()
    {
        // 检查鼠标位置是否在屏幕范围内
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width ||
            Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUIObject(Input.mousePosition)) return;

            // 检测点击的是物体还是手柄
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, handleLayer))
            {
                // // Debug.Log("Raycast hit handle: " + hit.transform.name);

                // 点击了手柄
                _draggedHandle = hit.transform;
                _isDragging = true;

                // 记录初始状态
                _dragStartPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                if (_selectedObject != null)
                {
                    _objectStartPosition = _selectedObject.position;
                    _objectStartRotation = _selectedObject.rotation;
                    _objectStartScale = _selectedObject.localScale;
                    // // Debug.Log("Selected object position: " + _objectStartPosition);
                }
                else
                {
                    // // Debug.LogWarning("No selected object when trying to drag handle!");
                }
            }
            else if (Physics.Raycast(ray, out hit, 1000f, objectLayer))
            {
                // 点击了物体
                SelectObject(hit.transform);
            }
        }
        // 鼠标拖动
        else if (Input.GetMouseButton(0) && _isDragging && _draggedHandle != null && _selectedObject != null)
        {
            // // Debug.Log("Dragging handle: " + _draggedHandle.name);

            // 获取手柄的父对象
            Transform handleParent = GetHandleParent(_draggedHandle);
            // // Debug.Log("Handle parent: " + (handleParent != null ? handleParent.name : "null"));

            // 计算屏幕空间偏移
            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 screenDelta = currentMousePos - _dragStartPosition;
            // // Debug.Log("Screen delta: " + screenDelta);

            // 根据手柄类型执行不同的操作
            if (handleParent == _positionHandles)
            {
                // // Debug.Log("Moving object");
                // 移动物体：直接使用手柄的forward方向
                Vector3 moveDirection = _draggedHandle.up;

                // 如果是Z轴方向，将方向取反
                if (_draggedHandle.name.Contains("Z"))
                {
                    moveDirection = -moveDirection;
                }

                // 计算屏幕空间中的移动方向
                Vector3 screenDirection = new Vector3(screenDelta.x, screenDelta.y, 0).normalized;

                // 将屏幕方向投影到相机平面上
                Vector3 cameraRight = mainCamera.transform.right;
                Vector3 cameraUp = mainCamera.transform.up;

                // 计算世界空间中的移动方向
                Vector3 worldDirection = (cameraRight * screenDirection.x + cameraUp * screenDirection.y).normalized;

                // 将世界空间方向投影到物体的移动方向上
                Vector3 projectedDirection = Vector3.Project(worldDirection, moveDirection).normalized;

                // 计算移动距离
                float moveDistance = screenDelta.magnitude * moveSpeed;

                // 应用移动
                Vector3 newPosition = _objectStartPosition + projectedDirection * moveDistance;
                _selectedObject.position = newPosition;
                
                // 调试信息
                // Debug.Log($"Parent moved to: {_selectedObject.position}");
                foreach (Transform child in _selectedObject)
                {
                    if (child != null)
                    {
                        // Debug.Log($"Child '{child.name}' - Local: {child.localPosition}, World: {child.position}");
                        // 检查是否有特殊组件
                        var components = child.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp != null && !(comp is Transform))
                            {
                                // Debug.Log($"  - Has component: {comp.GetType().Name}");
                            }
                        }
                    }
                }
            }
            else if (handleParent == _rotationHandles)
            {
                // // Debug.Log("Rotating object");
                // 旋转物体：绕着手柄的上方向旋转
                Vector3 rotationAxis = _draggedHandle.up;

                // 计算屏幕空间中的移动方向
                Vector3 screenDirection = new Vector3(screenDelta.x, screenDelta.y, 0).normalized;

                // 将屏幕方向投影到相机平面上
                Vector3 cameraRight = mainCamera.transform.right;
                Vector3 cameraUp = mainCamera.transform.up;

                // 计算世界空间中的移动方向
                Vector3 worldDirection = (cameraRight * screenDirection.x + cameraUp * screenDirection.y).normalized;

                // 将世界空间方向投影到圆环的切线方向上
                // 圆环的切线方向是圆环的forward方向
                Vector3 tangentDirection = _draggedHandle.forward;
                Vector3 projectedDirection = Vector3.Project(worldDirection, tangentDirection).normalized;

                // 计算旋转角度
                // 使用投影后的方向与圆环的切线方向的点积来确定旋转方向
                float dotProduct = Vector3.Dot(projectedDirection, tangentDirection);
                // 使用投影后的方向与圆环的up方向的叉积来确定旋转的正负
                float crossProduct = Vector3.Dot(Vector3.Cross(projectedDirection, tangentDirection), rotationAxis);

                // 计算旋转角度
                // 根据手柄名称调整旋转方向
                float directionMultiplier = 1.0f;
                if (_draggedHandle.name.Contains("X"))
                {
                    directionMultiplier = -1.0f; // X轴圆环反向
                }
                else if (_draggedHandle.name.Contains("Y"))
                {
                    directionMultiplier = 1.0f; // Y轴圆环正向
                }
                else if (_draggedHandle.name.Contains("Z"))
                {
                    directionMultiplier = -1.0f; // Z轴圆环反向
                }

                float angle = -Mathf.Sign(crossProduct) * screenDelta.magnitude * rotationSpeed * directionMultiplier;

                _selectedObject.rotation = Quaternion.AngleAxis(angle, rotationAxis) * _objectStartRotation;
            }
            else if (handleParent == _scaleHandles)
            {
                // // Debug.Log("Scaling object");
                // 缩放物体：沿着手柄的前向方向缩放
                float scaleFactor = 1f + screenDelta.y * scaleSpeed;
                _selectedObject.localScale = _objectStartScale * scaleFactor;
            }
            else
            {
                // // Debug.LogWarning("Handle parent not recognized: " + (handleParent != null ? handleParent.name : "null"));
            }
        }
        // 鼠标释放
        else if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {
                //  // Debug.Log("End dragging");
                _isDragging = false;
                _draggedHandle = null;
            }
        }
        // 右键点击取消选择
        else if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverUIObject(Input.mousePosition)) return;
            DeselectObject();
        }
    }

    private void SelectObject(Transform obj)
    {
        // 取消之前的选择
        DeselectObject();

        // 选择新物体
        _selectedObject = obj;
        _selectedRenderer = obj.GetComponent<Renderer>();

        if (_selectedRenderer != null)
        {
            // 高亮显示
            _selectedRenderer.material.color = highlightColor;
        }

        // 显示变换手柄
        SetHandleVisibility(HandleType.Position);

        // 将手柄移动到物体位置
        transform.position = _selectedObject.position;
        UpdateHandleScale();
    }

    private void DeselectObject()
    {
        if (_selectedObject != null && _selectedRenderer != null)
        {
            // 恢复原始颜色
            _selectedRenderer.material.color = defaultColor;
        }

        _selectedObject = null;
        _selectedRenderer = null;

        // 隐藏变换手柄
        SetHandleVisibility(HandleType.None);
    }

    private void SetHandleVisibility(HandleType type)
    {
        // 隐藏所有手柄
        if (_positionHandles != null) _positionHandles.gameObject.SetActive(false);
        if (_rotationHandles != null) _rotationHandles.gameObject.SetActive(false);
        if (_scaleHandles != null) _scaleHandles.gameObject.SetActive(false);

        // 显示指定类型的手柄
        switch (type)
        {
            case HandleType.Position:
                if (_positionHandles != null) _positionHandles.gameObject.SetActive(true);
                break;
            case HandleType.Rotation:
                if (_rotationHandles != null) _rotationHandles.gameObject.SetActive(true);
                break;
            case HandleType.Scale:
                if (_scaleHandles != null) _scaleHandles.gameObject.SetActive(true);
                break;
        }

        _currentHandleType = type;
    }

    private void UpdateHandleScale()
    {
        if (_selectedObject == null) return;

        float objectSize = 1.0f;
        if (_selectedRenderer != null)
        {
            objectSize = _selectedRenderer.bounds.size.magnitude;
        }
        else
        {
            objectSize = _selectedObject.localScale.magnitude;
        }

        float handleScale = Mathf.Clamp(objectSize * handleScaleFactor, minHandleScale, maxHandleScale);

        if (_positionHandles != null) _positionHandles.localScale = Vector3.one * handleScale;
        if (_rotationHandles != null) _rotationHandles.localScale = Vector3.one * handleScale;
        if (_scaleHandles != null) _scaleHandles.localScale = Vector3.one * handleScale;
    }

    private bool IsPointerOverUIObject(Vector2 position)
    {
        // 创建一个射线
        Ray ray = mainCamera.ScreenPointToRay(position);

        // 先检测是否点击到3D物体
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, objectLayer | handleLayer))
        {
            return false; // 如果点击到3D物体，允许操作
        }

        // 如果没有点击到3D物体，再检查UI
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }

    // 公共方法，用于切换手柄类型
    public void SetHandleType(HandleType type)
    {
        if (_selectedObject != null)
        {
            SetHandleVisibility(type);
        }
    }
}