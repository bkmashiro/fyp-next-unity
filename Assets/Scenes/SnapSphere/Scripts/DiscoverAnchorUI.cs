using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;

public class DiscoverAnchorUI : MonoBehaviour
{
    public GeospatialManager GeospatialManager;
    public CloudAnchorManager CloudAnchorManager;
    public SSApi SSApi;
    public float Interval = 5f;
    public TextMeshProUGUI DebugText;
    void Start()
    {
        GeospatialManager = FindFirstObjectByType<GeospatialManager>();
        CloudAnchorManager = FindFirstObjectByType<CloudAnchorManager>();
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
            // This won't resolve duplicates.
            GeospatialManager.ResolveCloudAnchor(anchor.cloudAnchorId, (ank) =>
            {
                DebugText.text += $"Resolved anchor: {anchor.cloudAnchorId}\n";
                // CloudAnchorManager.AddResolvedAnchor(anchor.cloudAnchorId, ank.Anchor);
                // load the GeoObjects related to the anchor
                DiscoverAnchor(anchor.cloudAnchorId);
            });
        }
    }

    async void DiscoverAnchor(string anchorId)
    {
        var geoObjects = await SSApi.DiscoverAnchor(anchorId);
        foreach (var geoObject in geoObjects)
        {
            DebugText.text += $"Discovering GeoObject: {geoObject["id"]}\n";
            // var spatialObject = await SpatialObject.CreateInstance(geoObject);
            var anchor = CloudAnchorManager.GetCloudAnchor(anchorId);
            // var spatialObject = await SpatialImage.CreateInstanceWithRelativePosition(geoObject, anchor.cloudAnchor.transform);
            var spatialObject = await SpatialObject.CreateInstanceWithRelativePosition(geoObject, anchor.arAnchor.transform);

            spatialObject.transform.SetParent(this.transform);
        }
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
