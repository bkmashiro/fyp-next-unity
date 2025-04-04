using System.Collections.Generic;
using TransformHandles;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjSelector : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unselectedColor;
    
    private Camera _camera;
    private CameraMovement _cameraMovement;

    private MyTransformHandleManager _manager;
    
    private Handle _lastHandle;
    private Dictionary<Transform, Handle> _handleDictionary;
    private bool _isDragging = false;
    private float _touchStartTime;
    private Vector2 _touchStartPosition;

    private void Awake()
    {
        _camera = Camera.main;
        if (_camera != null) _cameraMovement = _camera.GetComponent<CameraMovement>();
        
        _manager = MyTransformHandleManager.Instance;
        _handleDictionary = new Dictionary<Transform, Handle>();
    }

    private void Update()
    {
        // 处理触摸输入
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // 检查是否点击到UI
            if (IsPointerOverUIObject(touch.position)) return;
            
            var ray = _camera.ScreenPointToRay(touch.position);
            
            // 触摸开始
            if (touch.phase == TouchPhase.Began)
            {
                _touchStartTime = Time.time;
                _touchStartPosition = touch.position;
                
                if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
                {
                    var hitTransform = hit.transform;
                    if(_handleDictionary.ContainsKey(hitTransform)) return;
                    if (_lastHandle == null) { CreateHandle(hitTransform); }
                    else { AddTarget(hitTransform); }
                    
                    var children = hitTransform.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        SelectObject(child);
                    }
                }
            }
            // 触摸移动
            else if (touch.phase == TouchPhase.Moved && _lastHandle != null)
            {
                _isDragging = true;
            }
            // 触摸结束
            else if (touch.phase == TouchPhase.Ended)
            {
                // 长按（超过0.5秒）视为右键点击
                if (Time.time - _touchStartTime > 0.5f)
                {
                    if (Physics.Raycast(ray, out var hit))
                    {
                        var hitTransform = hit.transform;
                        if(!_handleDictionary.ContainsKey(hitTransform)) return;
                        RemoveTarget(hitTransform);
                        DeselectObject(hitTransform);
                        var children = hitTransform.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            DeselectObject(child);
                        }
                    }
                }
                
                _isDragging = false;
            }
        }
        // 处理鼠标输入
        else
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUIObject(Input.mousePosition)) return;
                
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
                {
                    var hitTransform = hit.transform;
                    if(_handleDictionary.ContainsKey(hitTransform)) return;
                    CreateHandle(hitTransform);
                    
                    var children = hitTransform.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        SelectObject(child);
                    }
                }
            }
            // Add the object to handle if exists, else create a new handle
            else if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUIObject(Input.mousePosition)) return;
                
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
                {
                    var hitTransform = hit.transform;
                    if(_handleDictionary.ContainsKey(hitTransform)) return;
                    if (_lastHandle == null) { CreateHandle(hitTransform); }
                    else { AddTarget(hitTransform); }
                    
                    var children = hitTransform.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        SelectObject(child);
                    }
                }
            }
            // Remove the object from handle
            if (Input.GetMouseButtonDown(1))
            {
                if (IsPointerOverUIObject(Input.mousePosition)) return;
                
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    var hitTransform = hit.transform;
                    if(!_handleDictionary.ContainsKey(hitTransform)) return;
                    RemoveTarget(hitTransform);
                    DeselectObject(hitTransform);
                    var children = hitTransform.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        DeselectObject(child);
                    }
                }
            }

            // Create new handle for object
            if (Input.GetMouseButton(2))
            {
                if (IsPointerOverUIObject(Input.mousePosition)) return;
                
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out var hit, 1000f, layerMask)) return;
                if(_handleDictionary.ContainsKey(hit.transform)) return;
                var hitTransform = hit.transform;
                CreateHandle(hitTransform);
                SelectObject(hitTransform);
            }
        }
    }

    private bool IsPointerOverUIObject(Vector2 position)
    {
        // 创建一个射线
        Ray ray = _camera.ScreenPointToRay(position);
        
        // 先检测是否点击到3D物体
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
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

    private void DeselectObject(Transform hitInfoTransform)
    {
        _handleDictionary.Remove(hitInfoTransform);

        hitInfoTransform.tag = "Untagged";
        var rendererComponent = hitInfoTransform.gameObject.GetComponent<Renderer>();
        if (rendererComponent == null) rendererComponent = hitInfoTransform.GetComponentInChildren<Renderer>();
        rendererComponent.material.color = unselectedColor;
    }

    private void SelectObject(Transform hitInfoTransform)
    {
        _handleDictionary.Add(hitInfoTransform, _lastHandle);

        hitInfoTransform.tag = "Selected";
        var rendererComponent = hitInfoTransform.gameObject.GetComponent<Renderer>();
        if (rendererComponent == null) rendererComponent =  hitInfoTransform.GetComponentInChildren<Renderer>();
        rendererComponent.material.color = selectedColor;
    }
    
    private void CreateHandle(Transform hitTransform)
    {
        var handle = _manager.CreateHandle(hitTransform);
        _lastHandle = handle;
        
        handle.OnInteractionStartEvent += OnHandleInteractionStart;
        handle.OnInteractionEvent += OnHandleInteraction;
        handle.OnInteractionEndEvent += OnHandleInteractionEnd;
        handle.OnHandleDestroyedEvent += OnHandleDestroyed;
    }

    private void AddTarget(Transform hitTransform)
    {
        _manager.AddTarget(hitTransform, _lastHandle);
    }
    
    private void RemoveTarget(Transform hitTransform)
    {
        var handle = _handleDictionary[hitTransform];
        if (_lastHandle == handle) _lastHandle = null;

        _manager.RemoveTarget(hitTransform, handle);
    }

    private void OnHandleInteractionStart(Handle handle)
    {
        _cameraMovement.enabled = false;
    }

    private static void OnHandleInteraction(Handle handle)
    {
        Debug.Log($"{handle.name} is being interacted with");
    }
    
    private void OnHandleInteractionEnd(Handle handle)
    {
        _cameraMovement.enabled = true;
    }
    
    private void OnHandleDestroyed(Handle handle)
    {
        handle.OnInteractionStartEvent -= OnHandleInteractionStart;
        handle.OnInteractionEvent -= OnHandleInteraction;
        handle.OnInteractionEndEvent -= OnHandleInteractionEnd;
        handle.OnHandleDestroyedEvent -= OnHandleDestroyed;
    }
}