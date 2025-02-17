using System;
using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TakePhotoUI : MonoBehaviour
{
    public GameObject spatialImagePrefab;
    private GeospatialManager _geospatialManager;

    public GameObject CloudAnchorPrefab;
    public GameObject MapQualityIndicatorPrefab;
    public MapQualityIndicator _qualityIndicator;
    void OnEnable()
    {
        _geospatialManager = FindFirstObjectByType<GeospatialManager>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // If the player has not touched the screen then the update is complete.
        Touch touch;
        if (Input.touchCount < 1 ||
            (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        // Ignore the touch if it's pointing on UI objects.
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        // Perform hit test and place a pawn object.
        PerformHitTest(touch.position);
    }

    public async void TestCloudAnchor()
    {
        SceneManager.LoadScene("CreateAnchor");
    }

    public async void OnButtonClicked()
    {
        // try
        // {
        //     // 请求拍照并等待结果
        //     var photo = await FindFirstObjectByType<CameraSnapshotManager>().TakePhotoAsync();

        //     if (photo == null)
        //     {
        //         Debug.LogError("Photo is null, unable to apply texture.");
        //         return;
        //     }

        //     var plane = CreatePlaneInView(photo, 0.7f, Camera.main);
        //     // Add anchor
        //     var anchor = plane.AddComponent<ARAnchor>();

        //     // Add anchor to the ARAnchorManager
        //     _geospatialManager.HostCloudAnchor(anchor);
        // }
        // catch (Exception ex)
        // {
        //     Debug.LogError($"Error capturing photo: {ex}");
        // }
    }

    public GameObject CreatePlaneInView(Texture2D texture, float distance, Camera mainCamera)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not assigned!");
            throw new ArgumentNullException(nameof(mainCamera));
        }

        if (texture == null)
        {
            Debug.LogError("Texture is null!");
            throw new ArgumentNullException(nameof(texture));
        }

        // 计算平面位置：距离摄像机 N 米
        Vector3 planePosition = mainCamera.transform.position + mainCamera.transform.forward * distance;

        // 创建平面实例
        GameObject plane = Instantiate(spatialImagePrefab);
        plane.transform.position = planePosition;

        // 设置平面朝向：正对摄像机
        plane.transform.rotation = Quaternion.LookRotation(plane.transform.position - mainCamera.transform.position);

        // 计算平面大小以匹配摄像机视野
        float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * mainCamera.aspect;


        // 设置平面大小
        plane.transform.localScale = new Vector3(width, height, 1f);

        // 应用材质
        Renderer renderer = plane.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = texture;
        }
        else
        {
            Debug.LogError("Plane prefab does not have a Renderer component!");
        }
        plane.SetActive(true);
        Debug.Log($"Created plane at distance {distance} meters with size {width}x{height}");

        return plane;
    }

    ARAnchor _anchor;
    private void PerformHitTest(Vector2 touchPos)
    {
        List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
        _geospatialManager.RaycastManager.Raycast(
            touchPos, hitResults, TrackableType.PlaneWithinPolygon);

        // If there was an anchor placed, then instantiate the corresponding object.
        var planeType = PlaneAlignment.HorizontalUp;
        if (hitResults.Count > 0)
        {
            ARPlane plane = _geospatialManager.PlaneManager.GetPlane(hitResults[0].trackableId);
            if (plane == null)
            {
                Debug.LogWarningFormat("Failed to find the ARPlane with TrackableId {0}",
                    hitResults[0].trackableId);
                return;
            }

            planeType = plane.alignment;
            var hitPose = hitResults[0].pose;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // Point the hitPose rotation roughly away from the raycast/camera
                // to match ARCore.
                hitPose.rotation.eulerAngles =
                    new Vector3(0.0f, _geospatialManager.MainCamera.transform.eulerAngles.y, 0.0f);
            }

            _anchor = _geospatialManager.AnchorManager.AttachAnchor(plane, hitPose);
        }

        if (_anchor != null)
        {
            Instantiate(CloudAnchorPrefab, _anchor.transform);

            // Attach map quality indicator to this anchor.
            var indicatorGO =
                Instantiate(MapQualityIndicatorPrefab, _anchor.transform);
            _qualityIndicator = indicatorGO.GetComponent<MapQualityIndicator>();
            _qualityIndicator.DrawIndicator(planeType, _geospatialManager.MainCamera);

            // InstructionText.text = " To save this location, walk around the object to " +
            //     "capture it from different angles";
            // DebugText.text = "Waiting for sufficient mapping quaility...";

            // Hide plane generator so users can focus on the object they placed.
            UpdatePlaneVisibility(false);
        }
    }

    private void UpdatePlaneVisibility(bool visible)
    {
        foreach (var plane in _geospatialManager.PlaneManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }
}
