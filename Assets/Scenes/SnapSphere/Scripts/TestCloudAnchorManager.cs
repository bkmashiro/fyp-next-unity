using System.Threading.Tasks;
using Models;
using Proyecto26;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;



public class TestCloudAnchorManager : MonoBehaviour
{
    public CloudAnchorManager cloudAnchorManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    string testSOData = @"{
        ""type"": ""GeoImage"",
        ""createdAt"": ""2025-03-02T14:49:39.463Z"",
        ""updatedAt"": ""2025-03-02T14:49:39.463Z"",
        ""id"": ""f73ee6fd-c4ad-496e-a74a-ae59cf58a9d2"",
        ""position"": {
            ""type"": ""Point"",
            ""coordinates"": [
                -7.11775526,
                52.252268956
            ]
        },
        ""altitude"": 68.69302405230701,
        ""orientation"": [
            -0.03982938081026077,
            -0.9394129514694214,
            0.3197208046913147,
            -0.11702785640954971
        ],
        ""scale"": [
            1,
            1,
            1
        ],
        ""anchor"": {
            ""type"": ""Point"",
            ""coordinates"": [
                0,
                0
            ]
        },
        ""anchor_latitude"": 0,
        ""metadata"": ""{\""HorizontalAccuracy\"":41.93200969684763,\""VerticalAccuracy\"":1.652525766476299,\""OrientationYawAccuracy\"":23.66068107237971}"",
        ""relPosition"": {
            ""type"": ""Point"",
            ""coordinates"": [
                -0.30226469,
                0.897665203
            ]
        },
        ""relAltitude"": 0.112695023,
        ""relOrientation"": [
            0.30140209197998047,
            -0.33569270372390747,
            0.11496134102344513,
            0.8850146532058716
        ],
        ""cloudAnchor"": {
            ""id"": 3,
            ""cloudAnchorId"": ""ua-fcc782fca8659eb672783225b19dc8c5"",
            ""anchor"": {
                ""type"": ""Point"",
                ""coordinates"": [
                    1.848609209,
                    0.149961382
                ]
            }
        },
        ""ossFile"": {
            ""createdAt"": ""2025-03-02T14:49:39.282Z"",
            ""updatedAt"": ""2025-03-02T14:49:39.282Z"",
            ""key"": ""03a33b30-0f2f-451e-a589-856f6f172095.png"",
            ""originalName"": ""ua-fcc782fca8659eb672783225b19dc8c5.png"",
            ""size"": 427762,
            ""mimeType"": ""image/png"",
            ""deletedAt"": null
        }
    }";
    string testAnchorData = @"{
        ""id"": ""f73ee6fd-c4ad-496e-a74a-ae59cf58a9d2"",
        ""position"": {
            ""type"": ""Point"",
            ""coordinates"": [
                0,
                0
            ]
        },
    }";
    async void Start()
    {
        cloudAnchorManager = FindFirstObjectByType<CloudAnchorManager>();
        var anchor = cloudAnchorManager.CreateCloudAnchor(JsonConvert.DeserializeObject<CloudAnchorManager.CreateCloudAnchorData>(testAnchorData));

        var spatialObject = await SpatialObject.CreateInstance(JsonConvert.DeserializeObject<Dictionary<string, object>>(testSOData));
        anchor.AddChild(spatialObject);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
