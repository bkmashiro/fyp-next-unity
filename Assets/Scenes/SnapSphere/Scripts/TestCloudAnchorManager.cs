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
    ""position"": {
        ""type"": ""Point"",
        ""coordinates"": [
            -7.117679275,
            52.252333805
        ]
    },
    ""altitude"": 68.32999621424824,
    ""orientation"": [
        -0.11117800325155258,
        -0.8738502264022827,
        0.23719695210456848,
        -0.40958860516548157
    ],
    ""scale"": [
        1,
        1,
        1
    ],
    ""relPosition"": {
        ""type"": ""Point"",
        ""coordinates"": [
            -7.117679275,
            52.252333805
        ]
    },
    ""relAltitude"": 68.32999621424824,
    ""relOrientation"": [
        -0.11117800325155258,
        -0.8738502264022827,
        0.23719695210456848,
        -0.40958860516548157
    ],
    ""cloudAnchor"": {
        ""id"": 9,
        ""cloudAnchorId"": ""ua-09f32b406cc8decf3489852ef90900df"",
        ""anchor"": {
            ""type"": ""Point"",
            ""coordinates"": [
                0.201530591,
                0.405349851
            ]
        }
    },
    ""metadata"": null,
    ""text"": ""asdsadasdasd"",
    ""createdAt"": ""2025-03-23T16:41:48.598Z"",
    ""updatedAt"": ""2025-03-23T16:41:48.598Z"",
    ""id"": ""8bf8c548-9cd6-42bb-8044-ac621f40ac9e"",
    ""anchor"": ""0101000020E610000000000000000000000000000000000000"",
    ""anchor_latitude"": 0,
    ""type"": ""GeoComment"",
}";
    string testAnchorData = @"{
        ""id"": ""ua-09f32b406cc8decf3489852ef90900df"",
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
