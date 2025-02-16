using UnityEngine;

public class QualityIndicator : MonoBehaviour
{
    public float Radius = 5f;
    public bool ReachTopviewAngle { get; set; } = false;
    public bool ReachQualityThreshold { get; set; } = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateQualityState(int qualityState)
    {
        Debug.Log($"Quality state: {qualityState}");
    }
}
