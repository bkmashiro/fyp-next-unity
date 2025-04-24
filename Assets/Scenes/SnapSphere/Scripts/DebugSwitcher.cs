using UnityEngine;

public class DebugSwitcher : MonoBehaviour
{
    // Array of GameObjects to toggle for debug visualization
    [SerializeField]
    private GameObject[] debugObjects;

    // Current state of debug objects visibility
    private bool isDebugVisible = true;

    /// <summary>
    /// Toggles the visibility of all debug objects
    /// </summary>
    public void ToggleDebugObjects()
    {
        isDebugVisible = !isDebugVisible;
        
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isDebugVisible);
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
