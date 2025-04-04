using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CameraMovement : MonoBehaviour
{
    [Range(0f, 9f)] [SerializeField] private float sensitivity = 2f;
    [Range(0f, 90f)] [SerializeField] private float yRotationLimit = 60f;
    [Range(0f, 90f)] [SerializeField] private float xRotationLimit = 60f;
    [SerializeField] private float zoomSpeed = 10f;

    private Camera _camera;
    private Vector2 _rotation = Vector2.zero;
    private const string XAxis = "Mouse X";
    private const string YAxis = "Mouse Y";
    private float _fieldOfView;
    private Vector2 _lastTouchPosition;

    private void Awake()
    {
        _rotation = transform.localRotation.eulerAngles;
        _camera = GetComponent<Camera>();
        _fieldOfView = _camera.fieldOfView;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Check if touch is over UI
            if (IsPointerOverUIObject(touch.position)) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _lastTouchPosition = touch.position;
                    break;
                    
                case TouchPhase.Moved:
                    Vector2 delta = touch.position - _lastTouchPosition;
                    _rotation.x += delta.x * sensitivity * 0.1f;
                    _rotation.y += delta.y * sensitivity * 0.1f;
                    _lastTouchPosition = touch.position;
                    break;
            }
        }
        else
        {
            UpdateRotation();
        }
        
        UpdateCameraZoom();
    }

    private void UpdateRotation()
    {
        if(sensitivity == 0f) return;
        _rotation.x += Input.GetAxis(XAxis) * sensitivity;
        _rotation.x = Mathf.Clamp(_rotation.x, -xRotationLimit, xRotationLimit);

        _rotation.y += Input.GetAxis(YAxis) * sensitivity;
        _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);

        var xQuaternion = Quaternion.AngleAxis(_rotation.x, Vector3.up);
        var yQuaternion = Quaternion.AngleAxis(_rotation.y, Vector3.left);

        transform.localRotation = xQuaternion * yQuaternion;
    }

    private void UpdateCameraZoom()
    {
        if(zoomSpeed == 0f) return;
        
        // Handle touch zoom
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            _fieldOfView -= difference * zoomSpeed * 0.1f;
        }
        // Handle mouse zoom
        else
        {
            _fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        }
        
        _camera.fieldOfView = Mathf.Clamp(_fieldOfView, 35f, 100f);
        _fieldOfView = _camera.fieldOfView;
    }

    private bool IsPointerOverUIObject(Vector2 position)
    {
        // 创建一个射线
        Ray ray = Camera.main.ScreenPointToRay(position);
        
        // 先检测是否点击到3D物体
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return false; // 如果点击到3D物体，允许操作
        }
        
        // 如果没有点击到3D物体，再检查UI
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        
        // 如果点击到UI，检查UI是否可交互
        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                // 如果UI元素设置了Raycast Target为false，允许穿透
                if (result.gameObject.GetComponent<UnityEngine.UI.Graphic>()?.raycastTarget == false)
                {
                    return false;
                }
            }
        }
        
        return results.Count > 0;
    }
}