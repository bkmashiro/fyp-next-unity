using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Scene : MonoBehaviour
{
    public Dictionary<string, SpatialObject> spatialObjects = new();
    private SSApi api;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        api = FindFirstObjectByType<SSApi>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddSpatialObject(SpatialObject spatialObject)
    {
        spatialObject.parentScene = this;
        spatialObjects.Add(spatialObject.id, spatialObject);
    }

    public void RemoveSpatialObject(SpatialObject spatialObject)
    {
        spatialObject.parentScene = null;
        spatialObjects.Remove(spatialObject.id);
    }

    public Task<Dictionary<string, object>> SaveObject(SpatialObject spatialObject)
    {
        return api.UpdateObject(spatialObject.id, spatialObject.data);
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> SaveAllObjects()
    {
        var tasks = spatialObjects.Values.Select(SaveObject);
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r["id"].ToString(), r => r);
    }
}
