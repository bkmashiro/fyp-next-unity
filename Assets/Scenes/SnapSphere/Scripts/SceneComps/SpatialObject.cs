using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;


public class SpatialObject : MonoBehaviour
{
  public GameObject prefab;
  public GameObject instance;
  public string id;
  public Vector3 position { get { return instance.transform.position; } set { instance.transform.position = value; } }
  public Quaternion rotation { get { return instance.transform.rotation; } set { instance.transform.rotation = value; } }
  public Vector3 scale { get { return instance.transform.localScale; } set { instance.transform.localScale = value; } }

  public void BindToAnchor(Anchor anchor)
  {
    instance.transform.SetParent(anchor.transform, false);
  }

  abstract public string Serialize();
  abstract public void Deserialize(string data);
  virtual public void SaveChanges()
  {
  }
}