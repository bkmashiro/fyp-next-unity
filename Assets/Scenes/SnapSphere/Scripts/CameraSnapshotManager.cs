using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CameraSnapshotManager : MonoBehaviour
{
    ARCameraManager arCameraManager;

    private Texture2D cameraTexture; // 用于存储当前相机帧数据
    private Texture2D photo;         // 用于保存拍摄的照片
    private bool isProcessingPhoto = false;

    void Start()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogError("ARCameraManager not set！");
        }
    }

    /// <summary>
    /// 异步方法：拍照并返回照片的 Texture2D
    /// </summary>
    /// <returns>拍摄的照片（Texture2D）</returns>
    public async Task<Texture2D> TakePhotoAsync()
    {
        // throw new NotImplementedException();
        if (isProcessingPhoto)
        {
            Debug.LogWarning("Processing photo, please wait ...");
            return null;
        }

        if (arCameraManager == null)
        {
            Debug.LogError("ARCameraManager not assigned!");
            return null;
        }

        Debug.Log("Requesting photo capture...");
        isProcessingPhoto = true;

        // 使用 TaskCompletionSource 等待照片捕获完成
        var tcs = new TaskCompletionSource<Texture2D>();
        arCameraManager.frameReceived += OnCameraFrameReceived;

        void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (arCameraManager.TryAcquireLatestCpuImage(out var image))
            {
                using (image)
                {
                    if (cameraTexture == null || cameraTexture.width != image.width || cameraTexture.height != image.height)
                    {
                        cameraTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
                    }

                    var conversionParams = new XRCpuImage.ConversionParams
                    {
                        inputRect = new RectInt(0, 0, image.width, image.height),
                        outputDimensions = new Vector2Int(image.width, image.height),
                        outputFormat = TextureFormat.RGBA32,
                        transformation = XRCpuImage.Transformation.MirrorX
                    };

                    var rawTextureData = cameraTexture.GetRawTextureData<byte>();
                    image.Convert(conversionParams, rawTextureData);
                    cameraTexture.Apply();

                    photo = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGBA32, false);
                    photo.SetPixels32(cameraTexture.GetPixels32());
                    photo.Apply();

                    Debug.Log("Photo captured.");

                    // 通知任务完成
                    tcs.TrySetResult(photo);
                    arCameraManager.frameReceived -= OnCameraFrameReceived;
                }
            }
        }

        // 等待照片处理完成
        var capturedPhoto = await tcs.Task;
        isProcessingPhoto = false;

        if (capturedPhoto == null)
        {
            Debug.LogError("Failed to capture photo.");
        }

        return capturedPhoto;
    }
}
