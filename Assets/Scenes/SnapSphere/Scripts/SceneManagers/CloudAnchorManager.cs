

using Google.XR.ARCoreExtensions;
using UnityEngine;
using System.Collections.Generic;
using System;

public class CloudAnchorManager : MonoBehaviour
{

  [Serializable]
  public class PrefabMapEntry
  {
    public string name;
    public GameObject prefab;
  }
  [SerializeField]
  public List<PrefabMapEntry> PrefabMap = new();

  public GameObject GetPrefab(string name)
  {
    return PrefabMap.Find(entry => entry.name == name).prefab;
  }

  [Serializable]
  public class CreateCloudAnchorData
  {
    public string id;
    public SSApi.GeoPoint position;
  }

  Dictionary<string, CloudAnchor> cloudAnchors = new();
  // for debug
  public CloudAnchor CreateCloudAnchor(CreateCloudAnchorData data)
  {
    var position = data.position;
    var coordinates = new float[] { (float)position.Coordinates[0], (float)position.Coordinates[1], 0 };
    var approxPosition = new Vector3(coordinates[0], coordinates[1], coordinates[2]);
    CloudAnchor cloudAnchor = new()
    {
      id = data.id,
      state = CloudAnchor.CloudAnchorState.Pending,
      approxPosition = approxPosition,
    };
    cloudAnchors.Add(cloudAnchor.id, cloudAnchor);
    return cloudAnchor;
  }

  public CloudAnchor GetCloudAnchor(string id)
  {
    if (!cloudAnchors.ContainsKey(id))
    {
      // try to fetch from server, and guide user to the anchor
    }

    return cloudAnchors[id];
  }

  public void AddResolvedAnchor(string id, ARCloudAnchor cloudAnchor)
  {
    var anchor = new CloudAnchor()
    {
      id = id,
      state = CloudAnchor.CloudAnchorState.Resolved,
      cloudAnchor = cloudAnchor,
    };
    cloudAnchors.Add(id, anchor);
  }
}

public class CloudAnchor
{
  public enum CloudAnchorState
  {
    Pending,
    Resolved,
    Failed
  }

  public CloudAnchorState state;
  public ARCloudAnchor cloudAnchor;
  public string id;

  public Vector3 approxPosition;

  public List<SpatialObject> children = new();

  public void ShowGuide()
  {
    // Show guide
  }

  public void AddChild(SpatialObject child)
  {
    children.Add(child);
  }

  public void RemoveChild(SpatialObject child)
  {
    children.Remove(child);
  }


}