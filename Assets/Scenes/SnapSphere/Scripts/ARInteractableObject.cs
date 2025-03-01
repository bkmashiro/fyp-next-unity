using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARInteractableObject : MonoBehaviour
{
    private Camera arCamera;
    private bool isSelected = false;
    private Vector2 initialTouchPos;
    private Vector3 initialObjectPos;
    private float initialScale;
    private Quaternion initialRotation;
    private float rotationSpeed = 5f;
    
    void Start()
    {
        arCamera = Camera.main;  // 获取 AR 相机
    }

    void Update()
    {
        if (Input.touchCount == 1) // 单指点击和拖动
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                TrySelectObject(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && isSelected)
            {
                MoveObject(touch.position);
            }
        }
        else if (Input.touchCount == 2 && isSelected) // 双指旋转 & 缩放
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            HandlePinchAndRotate(touch0, touch1);
        }
    }

    // **点击选中 AR 物体**
    private void TrySelectObject(Vector2 touchPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform) // 检测是否点中了当前物体
            {
                isSelected = true;
                initialTouchPos = touchPosition;
                initialObjectPos = transform.position;
                initialRotation = transform.rotation;
                initialScale = transform.localScale.x;
            }
        }
    }

    // **拖动 AR 物体**
    private void MoveObject(Vector2 touchPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f, LayerMask.GetMask("ARPlane")))
        {
            transform.position = hit.point;
        }
    }

    // **双指缩放 & 旋转**
    private void HandlePinchAndRotate(Touch touch0, Touch touch1)
    {
        Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

        float prevDistance = Vector2.Distance(touch0PrevPos, touch1PrevPos);
        float currentDistance = Vector2.Distance(touch0.position, touch1.position);
        float scaleFactor = currentDistance / prevDistance;

        transform.localScale = initialScale * scaleFactor * Vector3.one;

        // **计算旋转**
        Vector2 touchDelta = touch1.position - touch0.position;
        float angle = Mathf.Atan2(touchDelta.y, touchDelta.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }
}
