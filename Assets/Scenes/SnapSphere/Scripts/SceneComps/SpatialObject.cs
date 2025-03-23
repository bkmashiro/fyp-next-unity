using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;


public abstract class SpatialObject : MonoBehaviour
{
  protected Dictionary<string, object> data;

  public GameObject instance
  {
    get
    {
      return this.gameObject;
    }
  }
  public string id;
  public Vector3 position { get { return instance.transform.localPosition; } set { instance.transform.localPosition = value; } }
  public Quaternion rotation { get { return instance.transform.localRotation; } set { instance.transform.localRotation = value; } }
  public Vector3 scale { get { return instance.transform.localScale; } set { instance.transform.localScale = value; } }

  public void BindToAnchor(ARAnchor anchor)
  {
    instance.transform.SetParent(anchor.transform, false);
  }

  virtual public void SaveChanges()
  {
    if (instance == null)
    {
      throw new Exception("Instance is null");
    }

    // save all changes to the data
    data["position"] = position;
    data["rotation"] = rotation;
    data["scale"] = scale;
  }

  public void ApplyChanges()
  {
    if (instance == null)
    {
      throw new Exception("Instance is null");
    }

    // apply all changes to the instance
    instance.transform.localPosition = position;
    instance.transform.localRotation = rotation;
    instance.transform.localScale = scale;
  }

  public static async Task<SpatialObject> CreateInstance(Dictionary<string, object> data)
  {
    // if data has "type" field, use it to create the instance
    if (data.ContainsKey("type"))
    {
      // type could be "type": "GeoImage", "GeoComment"
      // we need to create the instance of the type
      var type = data["type"].ToString();
      Debug.Log("Creating instance type: " + type);
      switch (type)
      {
        case "GeoImage":
          return await SpatialImage.CreateInstance(data);
        case "GeoComment":
          return await SpatialComment.CreateInstance(data);
      }
    }
    throw new Exception("Invalid type");
  }

  public static async Task<SpatialObject> CreateInstanceWithRelativePosition(Dictionary<string, object> data, Transform parent)
  {
    // if data has "type" field, use it to create the instance
    if (data.ContainsKey("type"))
    {
      // type could be "type": "GeoImage", "GeoComment"
      // we need to create the instance of the type
      var type = data["type"].ToString();
      Debug.Log("Creating instance type: " + type);
      switch (type)
      {
        case "GeoImage":
          return await SpatialImage.CreateInstanceWithRelativePosition(data, parent);
        case "GeoComment":
          return await SpatialComment.CreateInstanceWithRelativePosition(data, parent);
      }
    }
    throw new Exception("Invalid type");
  }
}