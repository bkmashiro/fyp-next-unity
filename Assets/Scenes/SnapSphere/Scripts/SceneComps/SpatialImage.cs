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
        var instance = Instantiate(FindFirstObjectByType<CloudAnchorManager>().GetPrefab("SpatialImage"));
        var spatialImage = instance.GetComponentInChildren<SpatialImage>();
        spatialImage.data = data;
        spatialImage.cloudAnchorId = ((Newtonsoft.Json.Linq.JObject)data["cloudAnchor"])["cloudAnchorId"].ToString();
        spatialImage.id = data["id"].ToString();

        var position = ((Newtonsoft.Json.Linq.JObject)data["position"]).ToObject<Dictionary<string, object>>();
        var coordinates = ((Newtonsoft.Json.Linq.JArray)position["coordinates"]).ToObject<float[]>();
        spatialImage.position = new Vector3(coordinates[0], coordinates[1], coordinates[2]);

        var orientation = ((Newtonsoft.Json.Linq.JArray)data["orientation"]).ToObject<float[]>();
        spatialImage.rotation = new Quaternion(orientation[0], orientation[1], orientation[2], orientation[3]);

        var scale = ((Newtonsoft.Json.Linq.JArray)data["scale"]).ToObject<float[]>();
        spatialImage.scale = new Vector3(scale[0], scale[1], scale[2]);

        var texture = await FindFirstObjectByType<SSApi>().DownloadTexture(data["ossFileKey"].ToString());
        var renderer = instance.GetComponentInChildren<Renderer>();
        renderer.material.mainTexture = texture;

        return spatialImage;
    }

}