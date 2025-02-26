

public class CloudAnchorManager
{
  Dictionary<CloudAnchor> cloudAnchors = new();

  public void CreateCloudAnchor(Vector3 position, Quaternion rotation)
  {
    CloudAnchor cloudAnchor = new CloudAnchor();
    cloudAnchor.approxPosition = position;
    cloudAnchor.approxRotation = rotation;
    cloudAnchor.state = CloudAnchor.CloudAnchorState.Pending;
    cloudAnchors.Add(cloudAnchor);
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
  enum CloudAnchorState
  {
    Pending,
    Resolved,
    Failed
  }

  public CloudAnchorState state;
  public ARCloudAnchor cloudAnchor;
  public string id;

  Vector3 approxPosition;
  Quaternion approxRotation;

  void ShowGuide()
  {
    // Show guide
  }
}