using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Google.XR.ARCoreExtensions;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SendCommentUI : MonoBehaviour
{
    [SerializeField] public TMP_InputField commentInput;
    [SerializeField] public Button sendButton;
    [SerializeField] private SSApi ssApi;
    [SerializeField] private CloudAnchorManager cloudAnchorManager;
    [SerializeField] private GameObject spatialCommentPrefab;
    [SerializeField] private GeospatialManager geospatialManager;

    private GameObject currentCommentGO;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClick);
        geospatialManager = FindFirstObjectByType<GeospatialManager>();
        ssApi = FindFirstObjectByType<SSApi>();
        cloudAnchorManager = FindFirstObjectByType<CloudAnchorManager>();
    }

    private async void OnSendButtonClick()
    {
        if (string.IsNullOrEmpty(commentInput.text))
        {
            Debug.LogWarning("Comment text is empty!");
            return;
        }

        // 获取相机位置作为评论位置
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        // 获取最近的锚点
        var closestAnchor = cloudAnchorManager.GetClosestAnchor(camera.transform.position);
        if (closestAnchor == null)
        {
            Debug.LogError("No resolved anchor found nearby!");
            return;
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
                pose = geospatialManager.EarthManager.Convert(
                    new Pose(currentCommentGO.transform.position, currentCommentGO.transform.rotation)
                ),
                cloudAnchorId = closestAnchor.id,
                text = commentInput.text
            };

            // 保存评论
            await ssApi.SaveGeoSpatialComment(commentData);
            
            // 清空输入框
            commentInput.text = "";
            
            Debug.Log("Comment sent successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error sending comment: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
