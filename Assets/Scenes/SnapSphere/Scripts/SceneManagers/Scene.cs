using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Scene : MonoBehaviour
{
    public Dictionary<string, SpatialObject> spatialObjects = new();
    public SSApi api;
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

    public async Task<Dictionary<string, object>> SaveObject(SpatialObject spatialObject)
    {
        spatialObject.SaveChanges();
        await spatialObject.Sync();

        return spatialObject.data;
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> SaveAllObjects()
    {
        var tasks = spatialObjects.Values.Select(SaveObject);
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r["id"].ToString(), r => r);
    }
}
