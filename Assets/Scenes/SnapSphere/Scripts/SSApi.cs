using System.Threading.Tasks;
using Models;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class SSApi : MonoBehaviour
{

  public string ServerUrl = "http://localhost:3001";
  [SerializeField] public Texture2D testTexture;
  public GameObject testObject;

  public async Task<string> Post<T>(string path, T data)
  {
    var tcs = new TaskCompletionSource<string>();
    Debug.Log($"Post to {ServerUrl + path}");
    RestClient.Post(ServerUrl + path,
      JsonConvert.SerializeObject(data)
    ).Then(response =>
    {
      tcs.SetResult(response.Text);
    }).Catch(err =>
    {
      tcs.SetException(err);
      Debug.LogError(err);
    });

    return await tcs.Task;
  }

  public async Task<K> Post<T, K>(string path, T data)
  {
    var tcs = new TaskCompletionSource<K>();
    Debug.Log($"Post to {ServerUrl + path}");

    RestClient.Post(ServerUrl + path, JsonConvert.SerializeObject(data)).Then(response =>
    {
      tcs.SetResult(JsonConvert.DeserializeObject<K>(response.Text));
    }).Catch(err =>
    {
      tcs.SetException(err);
      Debug.LogError(err);
    });

    return await tcs.Task;
  }

  public async Task<K> Get<K>(string path)
  {
    var tcs = new TaskCompletionSource<K>();
    Debug.Log($"Get from {ServerUrl + path}");

    RestClient.Get(ServerUrl + path).Then(response =>
    {
      tcs.SetResult(JsonConvert.DeserializeObject<K>(response.Text));
    }).Catch(err =>
    {
      tcs.SetException(err);
      Debug.LogError(err);
    });

    return await tcs.Task;
  }

  public async Task<string> CreateGeoImage(string imageId, GeoSpatialImageData data)
  {
    var pose = data.pose;
    var _transform = data.anchor != null ? data.anchor.transform : (data.cloudAnchor != null ? data.cloudAnchor.transform : null);
    if (_transform == null)
    {
      Debug.LogError("Transform is null!");
      throw new Exception("Transform is null!");  
    }
    var (localPos, localRot) = ConvertToLocalTransform(data.spatialImageGO.transform, _transform);
    var geoimg = new
    {
      ossFileId = imageId,
      position = new
      {
        type = "Point",
        coordinates = new double[] { pose.Longitude, pose.Latitude }
      },
      altitude = pose.Altitude,
      orientation = new double[] { pose.EunRotation.x, pose.EunRotation.y, pose.EunRotation.z, pose.EunRotation.w },
      cloudAnchorId = data.cloudAnchorId,
      metadata = new Dictionary<string, object>
      {
        { "HorizontalAccuracy", pose.HorizontalAccuracy },
        { "VerticalAccuracy", pose.VerticalAccuracy },
        { "OrientationYawAccuracy", pose.OrientationYawAccuracy },
      },
      relPosition = new
      {
        type = "Point",
        coordinates = new double[] { localPos.x, localPos.y }
      },
      relAltitude = localPos.z,
      relOrientation = new double[] { localRot.x, localRot.y, localRot.z, localRot.w },
      scale = new double[] { data.spatialImageGO.transform.localScale.x, data.spatialImageGO.transform.localScale.y, data.spatialImageGO.transform.localScale.z }
    };

    return await Post("/geo-image", geoimg);
  }

  // cloudAnchorId: string
  // anchorPosition: number[]
  public async Task<string> CreateCloudAnchorRecord(string cloudAnchorId, double[] anchorPosition)
  {
    var anchor = new
    {
      cloudAnchorId,
      anchorPosition
    };

    return await Post("/cloud-anchor", anchor);
  }

  public async Task<string> CreateCloudAnchorRecord(string cloudAnchorId, Vector3 pos)
  {
    var anchor = new
    {
      cloudAnchorId,
      position = new double[] { pos.x, pos.z },
      altitude = pos.y
    };

    return await Post("/cloud-anchor", anchor);
  }

  public async Task<UploadResponse> Upload(byte[] data, string fileName)
  {
    var tcs = new TaskCompletionSource<UploadResponse>();

    // 创建 Multipart 表单
    List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", data, fileName, "image/png")  // "file" 必须和后端 FileInterceptor('file') 一致
        };

    var requestHelper = new RequestHelper
    {
      Uri = ServerUrl + "/file/upload",
      Method = "POST",
      FormSections = formData, // 使用 Multipart 表单
      Headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer your_token" }  // 如果需要身份验证
            },
      EnableDebug = true, // 开启调试模式
    };

    RestClient.Request(requestHelper)
        .Then(response =>
        {
          tcs.SetResult(JsonConvert.DeserializeObject<UploadResponse>(response.Text));
        })
        .Catch(err =>
        {
          tcs.SetException(err);
          Debug.LogError("Upload Error: " + err.Message);
        });

    return await tcs.Task;
  }


  public async Task<Texture2D> DownloadTexture(string key)
  {
    var tcs = new TaskCompletionSource<Texture2D>();
    string url = $"{ServerUrl}/file/{key}";

    UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
    request.timeout = 10; // 设置超时

    var asyncOperation = request.SendWebRequest();
    while (!asyncOperation.isDone)
    {
      await Task.Yield(); // 等待请求完成
    }

    if (request.result != UnityWebRequest.Result.Success)
    {
      Debug.LogError($"Error downloading texture: {request.error}");
      tcs.SetException(new Exception(request.error));
    }
    else
    {
      Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
      tcs.SetResult(texture);
    }

    request.Dispose();
    return await tcs.Task;
  }

  [ContextMenu("TestDownloadTexture")]
  public async void TestDownloadTexture()
  {
    var texture = await DownloadTexture("8ad6afd9-233b-43cd-89b6-56597c3d8ac9.png");
    testObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
  }

  Texture2D ConvertToNonCompressed(Texture2D sourceTexture)
  {
    // 创建一个新的 RGBA32 纹理（支持 EncodeToPNG）
    Texture2D newTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

    // 复制像素数据
    RenderTexture rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height);
    Graphics.Blit(sourceTexture, rt);
    RenderTexture previous = RenderTexture.active;
    RenderTexture.active = rt;

    newTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
    newTexture.Apply();

    RenderTexture.active = previous;
    RenderTexture.ReleaseTemporary(rt);

    return newTexture;
  }

  public async Task SaveGeoSpatialImage(GeoSpatialImageData geoSpatialImage)
  {
    var imageBytes = geoSpatialImage.texture.EncodeToPNG();
    var uploadResponse = await Upload(imageBytes, $"{geoSpatialImage.cloudAnchorId}.png");
    Debug.Log($"Uploaded image: {uploadResponse.url}");

    // var geoImageId = await CreateGeoImage(uploadResponse.key, geoSpatialImage.cloudAnchorId, geoSpatialImage.pose);
    var geoImageId = await CreateGeoImage(uploadResponse.key, geoSpatialImage);
    Debug.Log($"Created geo image: {geoImageId}");

  }

  public async Task<GeoImageData[]> GetGeoImagesWithin(
    double latitude, double longitude, double radius)
  {
    var response = await Get<
      GeoImageData[]
    >($"/geo-image/range/{latitude}/{longitude}/{radius}");
    return response;
  }


  public async Task<AnchorData[]> GetAnchorsWithin(
    double latitude, double longitude, double radius)
  {
    var response = await Get<
      AnchorData[]
    >($"/cloud-anchor/range/{latitude}/{longitude}/{radius}");
    return response;
  }

  public async Task<GeoImageData> GetGeoObject(string id)
  {
    var response = await Get<GeoImageData>($"/geo-object/{id}");
    return response;
  }

  public async void Echo(string msg)
  {
    var response = await Post("/", new { message = msg });
    Debug.Log(response);
  }

  Vector3 ConvertToLocalPosition(Vector3 worldPos, Vector3 origin, Quaternion originRotation)
  {
    // 计算相对位置
    Vector3 relativePos = worldPos - origin;

    // 将世界空间的相对位置转换到局部空间
    return Quaternion.Inverse(originRotation) * relativePos;
  }

  public (Vector3, Quaternion) ConvertToLocalTransform(Transform target, Transform origin)
  {
    var localPos = ConvertToLocalPosition(target.position, origin.position, origin.rotation);
    var localRot = Quaternion.Inverse(origin.rotation) * target.rotation;

    return (localPos, localRot);
  }

  public (Vector3, Quaternion) ConvertToLocalTransform(Vector3 worldPos, Quaternion worldRot, Vector3 originPos, Quaternion originRot)
  {
    var localPos = ConvertToLocalPosition(worldPos, originPos, originRot);
    var localRot = Quaternion.Inverse(originRot) * worldRot;

    return (localPos, localRot);
  }

  public async Task<Dictionary<string, object>[]> DiscoverAnchor(string anchorId)
  {
    var response = await Get<Dictionary<string, object>[]>($"/geo-object/anchor/{anchorId}");
    return response;
  }

  [Serializable]
  public class UploadResponse
  {
    public string url;
    public string key;
    public File file;
  }

  [Serializable]
  public class File
  {
    public string key;
    public string originalName;
    public int size;
    public string mimeType;
    public string createdAt;
    public string updatedAt;
    public string deletedAt;
  }

  [Serializable]
  public class GeoPoint
  {
    public string Type { get; set; }
    public double[] Coordinates { get; set; }
  }
  [Serializable]
  public class OssFile
  {
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Key { get; set; }
    public string OriginalName { get; set; }
    public long Size { get; set; }
    public string MimeType { get; set; }
    public object DeletedAt { get; set; }
  }

  public class GeoObjectConverter : JsonConverter
  {
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      JObject jo = JObject.Load(reader);
      if (jo["type"]?.ToString() == "GeoImage")
      {
        return jo.ToObject<GeoImageData>(serializer);
      }
      return jo.ToObject<GeoObjectData>(serializer);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      JObject jo = JObject.FromObject(value, serializer);
      jo.WriteTo(writer);
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(GeoObjectData) || objectType == typeof(GeoImageData);
    }
  }

  [JsonConverter(typeof(GeoObjectConverter))]
  [Serializable]
  public class GeoObjectData
  {
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Id { get; set; }
    public GeoPoint Position { get; set; }
    public double Altitude { get; set; }
    public double[] Orientation { get; set; }
    public double[] Scale { get; set; }
    public GeoPoint Anchor { get; set; }
    public double AnchorLatitude { get; set; }
    public string Metadata { get; set; }
    public string CloudAnchorId { get; set; }
    public GeoPoint RelPosition { get; set; }
    public double RelAltitude { get; set; }
    public double[] RelOrientation { get; set; }
  }

  [Serializable]
  public class GeoImageData : GeoObjectData
  {
    public OssFile OssFile { get; set; }
  }

  [Serializable]
  public class AnchorData
  {
    public int id;
    public string cloudAnchorId;
    public GeoPoint anchor;
  }

  public async Task<Dictionary<string, object>[]> GetGeoObjects()
  {
    var response = await Get<Dictionary<string, object>[]>("/geo-objects");
    return response;
  }

  public async Task<string> CreateGeoComment(string text, GeoSpatialCommentData data)
  {
    var pose = data.pose;
    var (localPos, localRot) = ConvertToLocalTransform(data.spatialCommentGO.transform, data.anchor.transform);
    var comment = new
    {
      text = text,
      position = new
      {
        type = "Point",
        coordinates = new double[] { pose.Longitude, pose.Latitude }
      },
      altitude = pose.Altitude,
      orientation = new double[] { pose.EunRotation.x, pose.EunRotation.y, pose.EunRotation.z, pose.EunRotation.w },
      cloudAnchorId = data.cloudAnchorId,
      metadata = new Dictionary<string, object>
      {
        { "HorizontalAccuracy", pose.HorizontalAccuracy },
        { "VerticalAccuracy", pose.VerticalAccuracy },
        { "OrientationYawAccuracy", pose.OrientationYawAccuracy },
      },
      relPosition = new
      {
        type = "Point",
        coordinates = new double[] { localPos.x, localPos.y }
      },
      relAltitude = localPos.z,
      relOrientation = new double[] { localRot.x, localRot.y, localRot.z, localRot.w },
      scale = new double[] { data.spatialCommentGO.transform.localScale.x, data.spatialCommentGO.transform.localScale.y, data.spatialCommentGO.transform.localScale.z }
    };

    return await Post("/geo-comment", comment);
  }

  public async Task SaveGeoSpatialComment(GeoSpatialCommentData geoSpatialComment)
  {
    var commentId = await CreateGeoComment(geoSpatialComment.text, geoSpatialComment);
    Debug.Log($"Created geo comment: {commentId}");
  }

  public async Task<GeoCommentData[]> GetGeoCommentsWithin(
    double latitude, double longitude, double radius)
  {
    var response = await Get<GeoCommentData[]>($"/geo-comment/range/{latitude}/{longitude}/{radius}");
    return response;
  }

  public async Task<Dictionary<string, object>> UpdateObject(string id, Dictionary<string, object> data)
  {
    var response = await Post<Dictionary<string, object>, Dictionary<string, object>>($"/geo-object/{id}", new Dictionary<string, object>{
      {"id", id},
      {"data", data}
    });
    return response;
  }

  [Serializable]
  public class GeoSpatialCommentData
  {
    public GameObject spatialCommentGO;
    public Transform anchor;
    public GeospatialPose pose;
    public string cloudAnchorId;
    public string text;
  }

  [Serializable]
  public class GeoCommentData : GeoObjectData
  {
    public string Text { get; set; }
  }
}