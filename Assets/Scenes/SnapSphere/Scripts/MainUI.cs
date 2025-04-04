using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MainUI : MonoBehaviour
{

    private Scene scene;
    private GeospatialManager GeospatialManager;
    private CloudAnchorManager CloudAnchorManager;
    private SSApi SSApi;
    public float DiscoverInterval = 5f;
    public float AutoSaveInterval = 15f;
    public TextMeshProUGUI DebugText;
    public TextMeshProUGUI HintText;

    private ARAnchor _anchor;
    private MapQualityIndicator qualityIndicator;
    private GeoSpatialImageData geoSpatialImage = new();
    private GameObject currentCommentGO;
    public GameObject CloudAnchorPrefab;
    public GameObject AnchorResolvedPrefab;
    public GameObject MapQualityIndicatorPrefab;
    public GameObject spatialImagePrefab;
    public GameObject spatialCommentPrefab;
    public TMP_InputField commentInput;

    public GameObject cameraModeComp;
    public GameObject cursorModeComp;
    public GameObject commentModeComp;
    public GameObject anchorModeComp;
    public GameObject toolsPanel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GeospatialManager = FindFirstObjectByType<GeospatialManager>();
        CloudAnchorManager = FindFirstObjectByType<CloudAnchorManager>();
        SSApi = FindFirstObjectByType<SSApi>();

        SetTool(Tool.Cursor);

        //add component Scene to the scene
        var scene = new GameObject("DefaultScene");
        scene.AddComponent<Scene>();
        scene.transform.SetParent(this.transform);
        this.scene = scene.GetComponent<Scene>();

        StartCoroutine(RepeatFunction(DiscoverInterval, CheckNearbyAnchors));
        StartCoroutine(RepeatFunction(AutoSaveInterval, AutoSave));
    }

    // Update is called once per frame
    void Update()
    {
        if (tool == Tool.Anchor)
        {
            UpdatePlaceAnchor();
        }
    }

    public enum EditMode
    {
        View,
        Edit
    }

    EditMode editMode = EditMode.View;

    public void OnEditModeButtonClick()
    {
        editMode = editMode == EditMode.View ? EditMode.Edit : EditMode.View;

        if (editMode == EditMode.View)
        {
            SetTool(Tool.Cursor);
        }

        toolsPanel.SetActive(editMode != EditMode.View);
    }

    public enum Tool
    {
        Cursor,
        Camera,
        Comment,
        Anchor
    }

    Tool tool = Tool.Cursor;

    public void SetTool(Tool tool)
    {
        SetHint("Set tool to " + tool);
        this.tool = tool;

        if (cameraModeComp != null) cameraModeComp.SetActive(tool == Tool.Camera);
        if (cursorModeComp != null) cursorModeComp.SetActive(tool == Tool.Cursor);
        if (commentModeComp != null) commentModeComp.SetActive(tool == Tool.Comment);
        if (anchorModeComp != null) anchorModeComp.SetActive(tool == Tool.Anchor);
    }

    public void OnCursorToolButtonClick()
    {
        SetTool(Tool.Cursor);
    }

    public void OnCameraToolButtonClick()
    {
        SetTool(Tool.Camera);
    }

    public void OnCommentToolButtonClick()
    {
        SetTool(Tool.Comment);
    }

    public void OnAnchorToolButtonClick()
    {
        SetTool(Tool.Anchor);
    }

    public async void OnCaptureButtonClick()
    {
        if (tool == Tool.Camera)
        {
            try
            {
                // 请求拍照并等待结果
                var photo = await FindFirstObjectByType<CameraSnapshotManager>().TakePhotoAsync();

                if (photo == null)
                {
                    Debug.LogError("Photo is null, unable to apply texture.");
                    return;
                }

                var plane = CreatePlaneInView(photo, 0.7f, Camera.main);
                var anchor = plane.AddComponent<ARAnchor>();
                geoSpatialImage = new GeoSpatialImageData
                {
                    texture = photo,
                    position = plane.transform.position,
                    rotation = plane.transform.rotation,
                    scale = plane.transform.localScale,
                    spatialImageGO = plane,
                    pose = GeospatialManager.EarthManager.Convert(
                        new Pose(plane.transform.position, plane.transform.rotation)
                    )
                };

                LinkPhotoAndAnchor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error capturing photo: {ex}");
            }
        }
    }

    public async void OnSendCommentButtonClick()
    {
        if (tool == Tool.Comment)
        {
            bool isCommentSuccess = await SendComment();
            if (!isCommentSuccess)
            {
                Debug.LogWarning("Comment text is empty!");
                return;
            }
        }
    }

    public void LOG(string message)
    {
        DebugText.text += $"{message}\n";
    }

    public void SetHint(string message)
    {
        HintText.text = message;
    }

    #region DiscoverAnchor
    async void CheckNearbyAnchors()
    {
        var currentGeoPos = GeospatialManager.EarthManager.CameraGeospatialPose;
        var anchors = await SSApi.GetAnchorsWithin(
            currentGeoPos.Latitude,
            currentGeoPos.Longitude,
            1000
        );

        foreach (var anchor in anchors)
        {
            // This won't resolve duplicates.
            GeospatialManager.ResolveCloudAnchor(anchor.cloudAnchorId, (ank) =>
            {
                LOG($"Resolved anchor: {anchor.cloudAnchorId}");
                CloudAnchorManager.AddResolvedAnchor(anchor.cloudAnchorId, null, ank.Anchor);
                // load the GeoObjects related to the anchor
                DiscoverAnchor(anchor.cloudAnchorId);
            });
        }
    }

    async void DiscoverAnchor(string anchorId)
    {
        var geoObjects = await SSApi.DiscoverAnchor(anchorId);
        foreach (var geoObject in geoObjects)
        {
            LOG($"Discovering GeoObject: {geoObject["id"]}");
            // var spatialObject = await SpatialObject.CreateInstance(geoObject);
            var anchor = CloudAnchorManager.GetCloudAnchor(anchorId);
            // var spatialObject = await SpatialImage.CreateInstanceWithRelativePosition(geoObject, anchor.cloudAnchor.transform);
            var spatialObject = await SpatialObject.CreateInstanceWithRelativePosition(geoObject, anchor.GetTransform());

            // spatialObject.transform.SetParent(anchor.cloudAnchor.transform);
        }
    }

    IEnumerator RepeatFunction(float interval, System.Action function)
    {
        while (true)
        {
            function();
            yield return new WaitForSeconds(interval);
        }
    }
    #endregion

    #region Tools

    #region Anchor
    private void UpdatePlaceAnchor()
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
            Debug.Log("Ignoring touch on UI.");
            return;
        }

        // Perform hit test and place a pawn object.
        PerformHitTest(touch.position);
    }

    private void UpdatePlaneVisibility(bool visible)
    {
        foreach (var plane in GeospatialManager.PlaneManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }

    private void PerformHitTest(Vector2 touchPos)
    {
        // if on an UI element, ignore
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        List<ARRaycastHit> hitResults = new();
        GeospatialManager.RaycastManager.Raycast(
            touchPos, hitResults, TrackableType.PlaneWithinPolygon);

        // If there was an anchor placed, then instantiate the corresponding object.
        var planeType = PlaneAlignment.HorizontalUp;
        if (hitResults.Count > 0)
        {
            ARPlane plane = GeospatialManager.PlaneManager.GetPlane(hitResults[0].trackableId);
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
                    new Vector3(0.0f, GeospatialManager.MainCamera.transform.eulerAngles.y, 0.0f);
            }

            _anchor = GeospatialManager.AnchorManager.AttachAnchor(plane, hitPose);
        }

        if (_anchor != null)
        {
            Instantiate(CloudAnchorPrefab, _anchor.transform);

            // Attach map quality indicator to this anchor.
            var indicatorGO =
                Instantiate(MapQualityIndicatorPrefab, _anchor.transform);
            qualityIndicator = indicatorGO.GetComponent<MapQualityIndicator>();
            qualityIndicator.DrawIndicator(planeType, GeospatialManager.MainCamera);

            // InstructionText.text = " To save this location, walk around the object to " +
            //     "capture it from different angles";
            // DebugText.text = "Waiting for sufficient mapping quaility...";

            // Hide plane generator so users can focus on the object they placed.
            GeospatialManager.HostCloudAnchor(_anchor, qualityIndicator);
            // geoSpatialImage.anchor = _anchor;
            UpdatePlaneVisibility(false);
        }

        GeospatialManager.OnAnchorHosted.AddListener(async (anchorId) =>
            {
                // geoSpatialImage.cloudAnchorId = anchorId;
                Debug.Log($"Linked photo and anchor, cloudAnchorId: {geoSpatialImage.cloudAnchorId}");
                CloudAnchorManager.AddResolvedAnchor(anchorId, _anchor, null);
                // SSApi.Echo("Linked photo and anchor, cloudAnchorId: " + geoSpatialImage.cloudAnchorId);
                var geoPosition = GeospatialManager.EarthManager.Convert(geoSpatialImage.anchor.pose);
                await SSApi.CreateCloudAnchorRecord(geoSpatialImage.cloudAnchorId, new double[] { geoPosition.Longitude, geoPosition.Altitude, geoPosition.Latitude });
            });
    }

    public async void LinkPhotoAndAnchor()
    {
        // add anchor data
        var nearestAnchor = CloudAnchorManager.GetClosestAnchor(geoSpatialImage.position);
        if (nearestAnchor == null)
        {
            Debug.LogError("No resolved anchor found nearby!");
            return;
        }

        geoSpatialImage.cloudAnchorId = nearestAnchor.id;
        geoSpatialImage.anchor = nearestAnchor.arAnchor;
        geoSpatialImage.cloudAnchor = nearestAnchor.cloudAnchor;

        if (geoSpatialImage.position == null)
        {
            Debug.LogError("Position is null!");
            return;
        }

        await SSApi.SaveGeoSpatialImage(geoSpatialImage);
    }
    #endregion

    #region Camera
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

    #endregion

    #region Comment

    private async Task<bool> SendComment()
    {
        if (string.IsNullOrEmpty(commentInput.text))
        {
            Debug.LogWarning("Comment text is empty!");
            return false;
        }

        // 获取相机位置作为评论位置
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Main camera not found!");
            return false;
        }

        // 获取最近的锚点
        var closestAnchor = CloudAnchorManager.GetClosestAnchor(camera.transform.position);
        if (closestAnchor == null)
        {
            Debug.LogError("No resolved anchor found nearby!");
            return false;
        }

        // 创建评论预制体
        currentCommentGO = Instantiate(spatialCommentPrefab);
        currentCommentGO.transform.parent = closestAnchor.cloudAnchor.transform;
        // currentCommentGO.transform.localPosition = Vector3.zero;
        // currentCommentGO.transform.localRotation = Quaternion.identity;

        // Position the comment in front of the camera
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraPosition = camera.transform.position;
        float distanceFromCamera = 1.0f; // 1 meter in front of camera
        Vector3 commentPosition = cameraPosition + (cameraForward * distanceFromCamera);

        // Set the world position and make it face the camera
        currentCommentGO.transform.position = commentPosition;
        currentCommentGO.transform.LookAt(camera.transform);
        currentCommentGO.transform.Rotate(0, 180, 0); // Rotate 180 degrees so text faces camera


        // 设置文本
        var textMeshPro = currentCommentGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textMeshPro != null)
        {
            textMeshPro.text = commentInput.text;
        }

        try
        {

            // 创建评论数据
            var commentData = new SSApi.GeoSpatialCommentData
            {
                spatialCommentGO = currentCommentGO,
                anchor = closestAnchor.cloudAnchor.transform,
                pose = GeospatialManager.EarthManager.Convert(
                    new Pose(currentCommentGO.transform.position, currentCommentGO.transform.rotation)
                ),
                cloudAnchorId = closestAnchor.id,
                text = commentInput.text
            };

            // 保存评论
            await SSApi.SaveGeoSpatialComment(commentData);

            // 清空输入框
            commentInput.text = "";

            Debug.Log("Comment sent successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending comment: {e.Message}");
        }

        return true;
    }

    #endregion


    #region Cursor
    private void UpdateCursor()
    {
        if (tool == Tool.Cursor)
        {
            Touch touch;
            if (Input.touchCount < 1 ||
                (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Ignore the touch if it's pointing on UI objects.
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                Debug.Log("Ignoring touch on UI.");
                return;
            }

            //TODO
        }
    }
    #endregion

    #endregion


    private void AutoSave()
    {
        _ = scene.SaveAllObjects();
    }
}
