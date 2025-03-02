using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;


public class SpatialComment : SpatialObject
{
  public string text;
  public static string prefabName = "SpatialComment";
  public SpatialComment(Dictionary<string, object> data)
  {
    this.data = data;
    this.text = data["text"].ToString();
  }

  public override void SaveChanges()
  {
    base.SaveChanges();
    data["text"] = text;
  }

  public static async Task<SpatialComment> CreateInstance(Dictionary<string, object> data)
  {
    var instance = Instantiate(Resources.Load<GameObject>(prefabName));
    var spatialComment = instance.GetComponent<SpatialComment>();
    spatialComment.data = data;
    spatialComment.text = data["text"].ToString();
    spatialComment.id = data["id"].ToString();

    var position = data["position"] as Dictionary<string, object>;
    var coordinates = (position["coordinates"] as Newtonsoft.Json.Linq.JArray).ToObject<float[]>();
    spatialComment.position = new Vector3(coordinates[0], coordinates[1], coordinates[2]);

    var orientation = (data["orientation"] as Newtonsoft.Json.Linq.JArray).ToObject<float[]>();
    spatialComment.rotation = new Quaternion(orientation[0], orientation[1], orientation[2], orientation[3]);

    var scale = (data["scale"] as Newtonsoft.Json.Linq.JArray).ToObject<float[]>();
    spatialComment.scale = new Vector3(scale[0], scale[1], scale[2]);

    return spatialComment;
  }
}