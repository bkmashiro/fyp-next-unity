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
using Newtonsoft.Json.Linq;

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

                // 计算平面位置和朝向
                Vector3 planePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.7f;
                Quaternion planeRotation = Quaternion.LookRotation(planePosition - Camera.main.transform.position);
                
                // 计算平面大小以匹配摄像机视野
                float height = 2f * 0.7f * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float width = height * Camera.main.aspect;
                Vector3 planeScale = new Vector3(width, height, 1f);
                
                // 获取地理坐标
                var geoPose = GeospatialManager.EarthManager.Convert(new Pose(planePosition, planeRotation));
                
                // 获取最近的锚点
                var nearestAnchor = CloudAnchorManager.GetClosestAnchor(planePosition);
                if (nearestAnchor == null)
                {
                    Debug.LogError("No resolved anchor found nearby!");
                    return;
                }
                
                // 计算相对于锚点的位置
                var (relPosition, relRotation) = SSApi.ConvertToLocalTransform(
                    planePosition,
                    planeRotation,
                    nearestAnchor.GetTransform().position,
                    nearestAnchor.GetTransform().rotation
                );
                Debug.Log($"relPosition: {relPosition}, relRotation: {relRotation}, planeScale: {planeScale}");
                // 创建数据字典，使用 JObject 和 JArray 类型
                var tempData = new Dictionary<string, object>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["cloudAnchor"] = new JObject { { "cloudAnchorId", nearestAnchor.id } },
                    ["relPosition"] = new JObject 
                    { 
                        ["type"] = "Point",
                        ["coordinates"] = new JArray { relPosition.x, relPosition.z } 
                    },
                    ["relAltitude"] = relPosition.y,
                    ["relOrientation"] = new JArray { relRotation.x, relRotation.y, relRotation.z, relRotation.w },
                    ["scale"] = new JArray { planeScale.x, planeScale.y, planeScale.z }
                    // we omit ossFile, then it won't download the texture
                    // ["ossFile"] = new Dictionary<string, object> { { "key", "temp" } }
                };

                // 上传图片到oss
                var imageBytes = photo.EncodeToPNG();
                var uploadResponse = await SSApi.Upload(imageBytes, $"{nearestAnchor.id}.png");
                tempData["ossFile"] = new JObject { { "key", uploadResponse.key } };

                // 使用 SpatialImage.CreateInstanceWithRelativePosition 创建实例
                var spatialImage = await SpatialImage.CreateInstanceWithRelativePosition(tempData, nearestAnchor.GetTransform());
                
                // 应用纹理
                var renderer = spatialImage.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = photo;
                }
                
                // 添加锚点
                // var anchor = spatialImage.gameObject.AddComponent<ARAnchor>();
                
                // 设置 geoSpatialImage 数据
                geoSpatialImage = new GeoSpatialImageData
                {
                    texture = photo,
                    position = spatialImage.transform.position,
                    rotation = spatialImage.transform.rotation,
                    scale = new Vector3(planeScale.x, planeScale.y, planeScale.z),
                    spatialImageGO = spatialImage.gameObject,
                    pose = geoPose,
                    cloudAnchorId = nearestAnchor.id,
                    anchor = nearestAnchor.arAnchor,
                    cloudAnchor = nearestAnchor.cloudAnchor,
                    relOrientation_override = relRotation
                };

                // 保存到服务器
                await SSApi.SaveGeoSpatialImage(geoSpatialImage);
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

            // register the spatial object to the scene
            scene.AddSpatialObject(spatialObject);
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
        Debug.Log($"PerformHitTest called with touchPos: {touchPos}");
        
        // if on an UI element, ignore
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current is null!");
            return;
        }
        
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Ignoring touch on UI element");
            return;
        }

        if (GeospatialManager == null)
        {
            Debug.LogError("GeospatialManager is null!");
            return;
        }

        if (GeospatialManager.RaycastManager == null)
        {
            Debug.LogError("GeospatialManager.RaycastManager is null!");
            return;
        }

        List<ARRaycastHit> hitResults = new();
        Debug.Log("Performing raycast...");
        GeospatialManager.RaycastManager.Raycast(
            touchPos, hitResults, TrackableType.PlaneWithinPolygon);
        Debug.Log($"Raycast hit {hitResults.Count} planes");

        // If there was an anchor placed, then instantiate the corresponding object.
        var planeType = PlaneAlignment.HorizontalUp;
        if (hitResults.Count > 0)
        {
            if (GeospatialManager.PlaneManager == null)
            {
                Debug.LogError("GeospatialManager.PlaneManager is null!");
                return;
            }

            ARPlane plane = GeospatialManager.PlaneManager.GetPlane(hitResults[0].trackableId);
            if (plane == null)
            {
                Debug.LogWarningFormat("Failed to find the ARPlane with TrackableId {0}",
                    hitResults[0].trackableId);
                return;
            }

            Debug.Log($"Found plane with alignment: {plane.alignment}");
            planeType = plane.alignment;
            var hitPose = hitResults[0].pose;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // Point the hitPose rotation roughly away from the raycast/camera
                // to match ARCore.
                hitPose.rotation.eulerAngles =
                    new Vector3(0.0f, GeospatialManager.MainCamera.transform.eulerAngles.y, 0.0f);
            }

            if (GeospatialManager.AnchorManager == null)
            {
                Debug.LogError("GeospatialManager.AnchorManager is null!");
                return;
            }

            Debug.Log("Attaching anchor to plane...");
            _anchor = GeospatialManager.AnchorManager.AttachAnchor(plane, hitPose);
            Debug.Log($"Anchor created: {_anchor != null}");
        }

        if (_anchor != null)
        {
            Debug.Log("Instantiating CloudAnchorPrefab...");
            Instantiate(CloudAnchorPrefab, _anchor.transform);

            // Attach map quality indicator to this anchor.
            Debug.Log("Instantiating MapQualityIndicator...");
            var indicatorGO =
                Instantiate(MapQualityIndicatorPrefab, _anchor.transform);
            qualityIndicator = indicatorGO.GetComponent<MapQualityIndicator>();
            if (qualityIndicator == null)
            {
                Debug.LogError("Failed to get MapQualityIndicator component!");
                return;
            }

            if (GeospatialManager.MainCamera == null)
            {
                Debug.LogError("GeospatialManager.MainCamera is null!");
                return;
            }

            qualityIndicator.DrawIndicator(planeType, GeospatialManager.MainCamera);

            Debug.Log("Hosting cloud anchor...");
            GeospatialManager.HostCloudAnchor(_anchor, qualityIndicator);
            UpdatePlaneVisibility(false);
        }

        GeospatialManager.OnAnchorHosted.AddListener(async (anchorId) =>
            {
                Debug.Log($"Anchor hosted with ID: {anchorId}");
                // if (geoSpatialImage == null)
                // {
                //     // Debug.LogError("geoSpatialImage is null!");
                //     return;
                // }

                // if (geoSpatialImage.anchor == null)
                // {
                //     Debug.LogError("geoSpatialImage.anchor is null!");
                //     return;
                // }

                // Debug.Log($"Linked photo and anchor, cloudAnchorId: {geoSpatialImage.cloudAnchorId}");
                Debug.Log($"Adding resolved anchor with ID: {anchorId}");
                CloudAnchorManager.AddResolvedAnchor(anchorId, _anchor, null);
                
                Debug.Log($"Converting anchor pose to geospatial position");
                var geoPosition = GeospatialManager.EarthManager.Convert(_anchor.pose);
                Debug.Log($"Geospatial position - Longitude: {geoPosition.Longitude}, Latitude: {geoPosition.Latitude}, Altitude: {geoPosition.Altitude}");
                
                Debug.Log($"Creating cloud anchor record for image ID: {geoSpatialImage.cloudAnchorId}");
                await SSApi.CreateCloudAnchorRecord(anchorId, new double[] { geoPosition.Longitude, geoPosition.Altitude, geoPosition.Latitude });
                Debug.Log("Cloud anchor record created successfully");
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
    // CreatePlaneInView 方法已被删除，因为我们现在使用 SpatialImage.CreateInstance
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


    public void AutoSave()
    {
        _ = scene.SaveAllObjects();
    }
}
