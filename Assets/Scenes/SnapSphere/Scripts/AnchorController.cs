using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AnchorController : MonoBehaviour
{
    public List<string> ResolvingSet = new List<string>();
    public ARAnchorManager AnchorManager;
    public float Radius = 5f;
    public Camera MainCamera
    {
        get
        {
            return Camera.main;
        }
    }

    void OnEnable()
    {
        AnchorManager = FindFirstObjectByType<ARAnchorManager>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateQualityState(int qualityState) { }
    public CloudAnchorHistoryCollection LoadCloudAnchorHistory()
    {
        return new CloudAnchorHistoryCollection();
    }

    [Serializable]
    public struct CloudAnchorHistory
    {

        public string Name;

        public string Id;

        public string SerializedTime;

        public CloudAnchorHistory(string name, string id, DateTime time)
        {
            Name = name;
            Id = id;
            SerializedTime = time.ToString();
        }

        public CloudAnchorHistory(string name, string id) : this(name, id, DateTime.Now)
        {
        }

        public DateTime CreatedTime
        {
            get
            {
                return Convert.ToDateTime(SerializedTime);
            }
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }


    [Serializable]
    public class CloudAnchorHistoryCollection
    {
        /// <summary>
        /// A list of Cloud Anchor History Data.
        /// </summary>
        public List<CloudAnchorHistory> Collection = new List<CloudAnchorHistory>();
    }
}
