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
        // update relPosition (directly use local position, rotation)
        data["relPosition"] = new Dictionary<string, object> { { "coordinates", new float[] { transform.localPosition.x, transform.localPosition.z } } };
        data["relOrientation"] = new float[] { transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w };
        data["relAltitude"] = transform.localPosition.y;
        data["scale"] = new float[] { transform.localScale.x, transform.localScale.y, transform.localScale.z };
        Debug.Log("saved data image: " + transform.localPosition);
    }

    public static new async Task<SpatialImage> CreateInstance(Dictionary<string, object> data)
    {
        var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("GeoImage"));
        var spatialImage = instance.GetComponentInChildren<SpatialImage>();
        spatialImage.data = data;
        spatialImage.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
        spatialImage.id = data["id"].ToString();
        spatialImage.UpdateHashCode();
        var position = ((Newtonsoft.Json.Linq.JObject)data["position"]).ToObject<Dictionary<string, object>>();
        var coordinates = ((Newtonsoft.Json.Linq.JArray)position["coordinates"]).ToObject<float[]>();
        var altitude = Convert.ToSingle(data["altitude"]);

        try
        {
            instance.transform.localPosition = new Vector3(coordinates[0], altitude, coordinates[1]);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting position: {e.Message}");
            Debug.LogError($"Error details: {e}");
        }

        var orientation = ((Newtonsoft.Json.Linq.JArray)data["orientation"]).ToObject<float[]>();
        instance.transform.localRotation = new Quaternion(orientation[0], orientation[1], orientation[2], orientation[3]);

        var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
        instance.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

        if (data.ContainsKey("ossFile"))
        {
            var ossFile = ((Newtonsoft.Json.Linq.JObject)data["ossFile"]).ToObject<Dictionary<string, object>>();
            var texture = await FindFirstObjectByType<SSApi>().DownloadTexture(ossFile["key"].ToString());
            instance.GetComponentInChildren<Renderer>().material.mainTexture = texture;
        }

        return spatialImage;
    }

    public static async Task<SpatialImage> CreateInstanceWithRelativePosition(Dictionary<string, object> data, Transform anchorTransform)
    {
        var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("GeoImage"));
        var spatialImage = instance.GetComponentInChildren<SpatialImage>();
        spatialImage.data = data;
        spatialImage.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
        spatialImage.id = data["id"].ToString();
        spatialImage.UpdateHashCode();  
        instance.transform.SetParent(anchorTransform, false);

        var relPosition = ((Newtonsoft.Json.Linq.JObject)data["relPosition"]).ToObject<Dictionary<string, object>>();
        var relCoordinates = ((Newtonsoft.Json.Linq.JArray)relPosition["coordinates"]).ToObject<float[]>();
        var relAltitude = Convert.ToSingle(data["relAltitude"]);

        instance.transform.localPosition = new Vector3(relCoordinates[0], relAltitude, relCoordinates[1]);

        var relOrientation = ((Newtonsoft.Json.Linq.JArray)data["relOrientation"]).ToObject<float[]>();
        instance.transform.localRotation = new Quaternion(
            relOrientation[0],
            relOrientation[1],
            relOrientation[2],
            relOrientation[3]
        );

        var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
        instance.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);
        var ossFile = ((Newtonsoft.Json.Linq.JObject)data["ossFile"]).ToObject<Dictionary<string, object>>();   
        var texture = await FindFirstObjectByType<SSApi>().DownloadTexture(ossFile["key"].ToString());
        instance.GetComponentInChildren<Renderer>().material.mainTexture = texture;

        // Debug.Log($"Created SpatialImage instance:");
        // Debug.Log($"  - ID: {spatialImage.id}");
        // Debug.Log($"  - Cloud Anchor ID: {spatialImage.cloudAnchorId}");
        // Debug.Log($"  - Local Position: {instance.transform.localPosition}");
        // Debug.Log($"  - Local Rotation: {instance.transform.localRotation.eulerAngles}");
        Debug.Log($"  - Local Scale: {instance.transform.localScale}");
        // Debug.Log($"  - World Position: {instance.transform.position}");
        // Debug.Log($"  - World Rotation: {instance.transform.rotation.eulerAngles}");
        // Debug.Log($"  - Parent: {instance.transform.parent?.name ?? "None"}");

        return spatialImage;
    }
}