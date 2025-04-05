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
  public Dictionary<string, object> data = new Dictionary<string, object>();
  public Scene parentScene = null;
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
    data["scale"] = scale;
    Debug.Log("saved data base: ");
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

  public void Start()
  {
    initHashCode = GetHashCode();
  }

  private int initHashCode = 0;
  public override int GetHashCode()
  {
    if (data == null)
    {
      Debug.Log("data is null");
      return base.GetHashCode();
    }

    int hash = 17;
    foreach (var kvp in data)
    {
      hash = hash * 31 + kvp.Key.GetHashCode();
      if (kvp.Value != null)
      {
        if (kvp.Value is Vector3 vector3)
        {
          hash = hash * 31 + vector3.x.GetHashCode();
          hash = hash * 31 + vector3.y.GetHashCode();
          hash = hash * 31 + vector3.z.GetHashCode();
        }
        else if (kvp.Value is Quaternion quaternion)
        {
          hash = hash * 31 + quaternion.x.GetHashCode();
          hash = hash * 31 + quaternion.y.GetHashCode();
          hash = hash * 31 + quaternion.z.GetHashCode();
          hash = hash * 31 + quaternion.w.GetHashCode();
        }
        else
        {
          hash = hash * 31 + kvp.Value.GetHashCode();
        }
      }
    }
    return hash;
  }

  public void TestHashCode()
  {
    this.SaveChanges();
    Debug.Log("test hashcode: " + GetHashCode());
  }

  public void UpdateHashCode()
  {
    initHashCode = GetHashCode();
  }


  public bool IsHashChanged { get { return GetHashCode() != initHashCode; } }

  public async Task<Dictionary<string, object>> Sync()
  {
    if (IsHashChanged)
    {
      Debug.Log("Syncing changed spatial object: " + id);
      SaveChanges();

      await parentScene.api.UpdateObject(id, data);
    }
    Debug.Log("unchanged spatial object: " + id);
    return data;
  }
}