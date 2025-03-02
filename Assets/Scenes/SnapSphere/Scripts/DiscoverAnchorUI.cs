using System.Collections;
using UnityEngine;
using TMPro;
public class DiscoverAnchorUI : MonoBehaviour
{
    public GeospatialManager GeospatialManager;
    public SSApi SSApi;
    public float Interval = 5f;
    public TextMeshProUGUI DebugText;
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
            // This won't resolve duplicates.
            GeospatialManager.ResolveCloudAnchor(anchor.cloudAnchorId, (ank) =>
            {
                DebugText.text += $"Resolved anchor: {anchor.cloudAnchorId}\n";

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
            DebugText.text += $"Discovering GeoObject: {geoObject.Id}\n";
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
