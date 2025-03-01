using System.Collections;
using UnityEngine;

public class DiscoverAnchorUI : MonoBehaviour
{
    public GeospatialManager GeospatialManager;
    public SSApi SSApi;
    public float Interval = 5f;
    void Start()
    {
        GeospatialManager = FindFirstObjectByType<GeospatialManager>();
        SSApi = FindFirstObjectByType<SSApi>();

        StartCoroutine(RepeatFunction(Interval, CheckNearbyAnchors));
    }

    async void CheckNearbyAnchors()
    {
        var currentGeoPos = GeospatialManager.EarthManager.CameraGeospatialPose;
        var anchors = await SSApi.GetAnchorsWithin(
            currentGeoPos.Latitude,
            currentGeoPos.Longitude,
            1000
        );

        foreach (var anchor in anchors)
        {
            GeospatialManager.ResolveCloudAnchor(anchor.cloudAnchorId, (anchor) =>
            {
                Debug.Log("Resolved anchor: {anchor.cloudAnchorId}");
            });
        }
    }

    async void DiscoverAnchor(string anchorId)
    {
        // var anchor = await SSApi.GetAnchor(anchorId);
        // GeospatialManager.ResolveCloudAnchor(anchor.cloudAnchorId, (anchor) =>
        // {
        //     Debug.Log("Resolved anchor: {anchor.cloudAnchorId}");
        // });
    }

    IEnumerator RepeatFunction(float interval, System.Action function)
    {
        while (true)
        {
            function();
            yield return new WaitForSeconds(interval);
        }
    }
}
