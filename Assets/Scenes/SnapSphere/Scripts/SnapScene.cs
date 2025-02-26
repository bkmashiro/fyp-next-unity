using System.Threading.Tasks;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;


public class SnapScene : MonoBehaviour
{
  List<SpatialObject> spatialObjects = new();
  CloudAnchorManager cloudAnchorManager = new();


  public void SaveChanges()
  {
    foreach (SpatialObject spatialObject in spatialObjects)
    {
      // RestClient.Put("https://snap-sphere.firebaseio.com/snap-scene/" + spatialObject.id + ".json", spatialObject.Serialize());
    }
  }
}