

using Google.XR.ARCoreExtensions;
using UnityEngine;
using System.Collections.Generic;

public class CloudAnchorManager
{
  Dictionary<string, CloudAnchor> cloudAnchors = new();
  public void CreateCloudAnchor(Vector3 position, Quaternion rotation)
  {
    CloudAnchor cloudAnchor = new();
    cloudAnchor.approxPosition = position;
    cloudAnchor.approxRotation = rotation;
    cloudAnchor.state = CloudAnchor.CloudAnchorState.Pending;
    cloudAnchors.Add(cloudAnchor.id, cloudAnchor);
    cloudAnchor.ShowGuide();
  }

  public CloudAnchor GetCloudAnchor(string id)
  {
    if (!cloudAnchors.ContainsKey(id))
    {
      // try to fetch from server, and guide user to the anchor
    }

    return cloudAnchors[id];
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
  public Quaternion approxRotation;

  public void ShowGuide()
  {
    // Show guide
  }
}