using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;
using TMPro;

public class SpatialComment : SpatialObject
{
  public string text;
  public static string prefabName = "SpatialComment";
  public string cloudAnchorId;

  public SpatialComment(Dictionary<string, object> data)
  {
    this.data = data;
    this.text = data["text"].ToString();
  }

  public override void SaveChanges()
  {
    base.SaveChanges();
    data["text"] = text;
    data["cloudAnchor"] = new Dictionary<string, object> { { "cloudAnchorId", cloudAnchorId } };
  }

  public static async Task<SpatialComment> CreateInstance(Dictionary<string, object> data)
  {
    var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("SpatialComment"));
    var spatialComment = instance.GetComponentInChildren<SpatialComment>();
    spatialComment.data = data;
    spatialComment.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
    spatialComment.id = data["id"].ToString();
    spatialComment.text = data["text"].ToString();

    Debug.Log($"Full data received: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

    var position = ((Newtonsoft.Json.Linq.JObject)data["position"]).ToObject<Dictionary<string, object>>();
    Debug.Log($"Position data: {Newtonsoft.Json.JsonConvert.SerializeObject(position)}");

    var coordinates = ((Newtonsoft.Json.Linq.JArray)position["coordinates"]).ToObject<float[]>();
    var altitude = Convert.ToSingle(data["altitude"]);
    Debug.Log($"Coordinates array length: {coordinates.Length}, Altitude: {altitude}");
    Debug.Log($"Coordinates values: [{string.Join(", ", coordinates)}]");

    try
    {
      spatialComment.position = new Vector3(coordinates[0], altitude, coordinates[1]);
      Debug.Log($"Successfully set position to: {spatialComment.position}");
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Error setting position: {e.Message}");
      Debug.LogError($"Error details: {e}");
    }

    var orientation = ((Newtonsoft.Json.Linq.JArray)data["orientation"]).ToObject<float[]>();
    spatialComment.rotation = new Quaternion(orientation[0], orientation[1], orientation[2], orientation[3]);

    var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
    spatialComment.scale = new Vector3(scale[0], scale[1], scale[2]);

    // Set text to TextMeshPro component
    var textMeshPro = instance.GetComponentInChildren<TextMeshPro>();
    if (textMeshPro != null)
    {
      textMeshPro.text = spatialComment.text;
    }
    else
    {
      Debug.LogError("TextMeshPro component not found in SpatialComment prefab!");
    }

    return spatialComment;
  }

  public static async Task<SpatialComment> CreateInstanceWithRelativePosition(Dictionary<string, object> data, Transform anchorTransform)
  {
    Debug.Log($"Creating instance with relative position, data: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

    var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("SpatialComment"));
    var spatialComment = instance.GetComponentInChildren<SpatialComment>();
    spatialComment.data = data;
    spatialComment.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
    spatialComment.id = data["id"].ToString();
    spatialComment.text = data["text"].ToString();

    // Set parent to anchor transform
    instance.transform.parent = anchorTransform;

    // Get relative position data
    var relPosition = ((Newtonsoft.Json.Linq.JObject)data["relPosition"]).ToObject<Dictionary<string, object>>();
    var relCoordinates = ((Newtonsoft.Json.Linq.JArray)relPosition["coordinates"]).ToObject<float[]>();
    var relAltitude = Convert.ToSingle(data["relAltitude"]);

    Debug.Log($"Relative position coordinates: [{string.Join(", ", relCoordinates)}], altitude: {relAltitude}");

    // Set local position and rotation
    instance.transform.localPosition = new Vector3(relCoordinates[0], relAltitude, relCoordinates[1]);

    var relOrientation = ((Newtonsoft.Json.Linq.JArray)data["relOrientation"]).ToObject<float[]>();
    instance.transform.localRotation = new Quaternion(
      relOrientation[0],
      relOrientation[1],
      relOrientation[2],
      relOrientation[3]
    );

    // Set scale
    var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
    instance.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

    // Set text to TextMeshPro component
    var textMeshPro = instance.GetComponentInChildren<TextMeshPro>();
    if (textMeshPro != null)
    {
      textMeshPro.text = spatialComment.text;
    }
    else
    {
      Debug.LogError("TextMeshPro component not found in SpatialComment prefab!");
    }

    return spatialComment;
  }
}