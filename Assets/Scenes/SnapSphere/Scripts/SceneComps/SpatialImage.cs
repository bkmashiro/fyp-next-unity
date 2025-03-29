using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;


public class SpatialImage : SpatialObject
{
    public string cloudAnchorId;

    public override void SaveChanges()
    {
        base.SaveChanges();
        data["cloudAnchor"] = new Dictionary<string, object> { { "cloudAnchorId", cloudAnchorId } };
    }

    public static new async Task<SpatialImage> CreateInstance(Dictionary<string, object> data)
    {
        var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("GeoImage"));

        var spatialImage = instance.GetComponentInChildren<SpatialImage>();
        spatialImage.data = data;
        spatialImage.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
        spatialImage.id = data["id"].ToString();

        Debug.Log($"Full data received: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

        var position = ((Newtonsoft.Json.Linq.JObject)data["position"]).ToObject<Dictionary<string, object>>();
        Debug.Log($"Position data: {Newtonsoft.Json.JsonConvert.SerializeObject(position)}");

        var coordinates = ((Newtonsoft.Json.Linq.JArray)position["coordinates"]).ToObject<float[]>();
        var altitude = Convert.ToSingle(data["altitude"]);
        Debug.Log($"Coordinates array length: {coordinates.Length}, Altitude: {altitude}");
        Debug.Log($"Coordinates values: [{string.Join(", ", coordinates)}]");

        try
        {
            spatialImage.position = new Vector3(coordinates[0], altitude, coordinates[1]);
            Debug.Log($"Successfully set position to: {spatialImage.position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting position: {e.Message}");
            Debug.LogError($"Error details: {e}");
        }

        var orientation = ((Newtonsoft.Json.Linq.JArray)data["orientation"]).ToObject<float[]>();
        spatialImage.rotation = new Quaternion(orientation[0], orientation[1], orientation[2], orientation[3]);

        var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
        spatialImage.scale = new Vector3(scale[0], scale[1], scale[2]);

        var ossFile = ((Newtonsoft.Json.Linq.JObject)data["ossFile"]).ToObject<Dictionary<string, object>>();
        var texture = await FindFirstObjectByType<SSApi>().DownloadTexture(ossFile["key"].ToString());
        // Material unlitMaterial = new(Shader.Find("Universal Render Pipeline/Unlit")) { mainTexture = texture };
        // var renderer = instance.GetComponentInChildren<Renderer>();
        // renderer.material = unlitMaterial;
        spatialImage.GetComponentInChildren<Renderer>().material.mainTexture = texture;

        return spatialImage;
    }

    public static async Task<SpatialImage> CreateInstanceWithRelativePosition(Dictionary<string, object> data, Transform anchorTransform)
    {
        Debug.Log($"Creating instance with relative position, data: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

        var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("GeoImage"));
        var spatialImage = instance.GetComponentInChildren<SpatialImage>();
        spatialImage.data = data;
        spatialImage.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
        spatialImage.id = data["id"].ToString();

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

        // Set texture
        var ossFile = ((Newtonsoft.Json.Linq.JObject)data["ossFile"]).ToObject<Dictionary<string, object>>();   
        var texture = await FindFirstObjectByType<SSApi>().DownloadTexture(ossFile["key"].ToString());
        // Material unlitMaterial = new(Shader.Find("Universal Render Pipeline/Unlit")) { mainTexture = texture };
        // var renderer = instance.GetComponentInChildren<Renderer>();
        // renderer.material = unlitMaterial;
        instance.GetComponentInChildren<Renderer>().material.mainTexture = texture;

        return spatialImage;
    }

}