using System;
using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

public class TakePhotoUI : MonoBehaviour
{
    public GameObject spatialImagePrefab;
    private GeospatialManager _geospatialManager;
    void OnEnable()
    {
        _geospatialManager = FindFirstObjectByType<GeospatialManager>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void OnButtonClicked()
    {
        try
        {
            // 请求拍照并等待结果
            var photo = await FindFirstObjectByType<CameraSnapshotManager>().TakePhotoAsync();

            if (photo == null)
            {
                Debug.LogError("Photo is null, unable to apply texture.");
                return;
            }

            var plane = CreatePlaneInView(photo, 0.7f, Camera.main);
            // Add anchor
            var anchor = plane.AddComponent<ARAnchor>();

            // Add anchor to the ARAnchorManager
            _geospatialManager.HostCloudAnchor(anchor);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error capturing photo: {ex}");
        }
    }


    public GameObject CreatePlaneInView(Texture2D texture, float distance, Camera mainCamera)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not assigned!");
            throw new ArgumentNullException(nameof(mainCamera));
        }

        if (texture == null)
        {
            Debug.LogError("Texture is null!");
            throw new ArgumentNullException(nameof(texture));
        }

        // 计算平面位置：距离摄像机 N 米
        Vector3 planePosition = mainCamera.transform.position + mainCamera.transform.forward * distance;

        // 创建平面实例
        GameObject plane = Instantiate(spatialImagePrefab);
        plane.transform.position = planePosition;

        // 设置平面朝向：正对摄像机
        plane.transform.rotation = Quaternion.LookRotation(plane.transform.position - mainCamera.transform.position);

        // 计算平面大小以匹配摄像机视野
        float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * mainCamera.aspect;


        // 设置平面大小
        plane.transform.localScale = new Vector3(width, height, 1f);

        // 应用材质
        Renderer renderer = plane.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = texture;
        }
        else
        {
            Debug.LogError("Plane prefab does not have a Renderer component!");
        }
        plane.SetActive(true);
        Debug.Log($"Created plane at distance {distance} meters with size {width}x{height}");

        return plane;
    }


}
