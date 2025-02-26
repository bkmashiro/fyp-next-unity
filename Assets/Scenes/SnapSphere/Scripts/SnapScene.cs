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

  /// <summary>
  /// Load scene from server
  /// 
  /// Load scene meta data, and user have to load anchors first, then 
  /// the objects linked to the anchors will be loaded.
  /// </summary>
  /// <param name="id"></param>
  public void LoadScene(string id)
  {
    // RestClient.Get("https://snap-sphere.firebaseio.com/snap-scene/" + id + ".json").Then(response =>
    // {
    //   Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Text);
    //   foreach (KeyValuePair<string, string> entry in data)
    //   {
    //     SpatialObject spatialObject = new SpatialObject();
    //     spatialObject.Deserialize(entry.Value);
    //     spatialObjects.Add(spatialObject);
    //   }
    // });
  }
}