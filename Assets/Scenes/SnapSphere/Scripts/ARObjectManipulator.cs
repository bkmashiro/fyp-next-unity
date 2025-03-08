using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARObjectManipulator : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    private GameObject selectedObject;
    private Vector2 touchPosition;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                SelectObject(touchPosition);
            }
            else if (touch.phase == TouchPhase.Moved && selectedObject != null)
            {
                MoveObject(touchPosition);
            }
        }
        else if (Input.touchCount == 2)
        {
            ScaleObject();
        }
    }

    void SelectObject(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("ARObject"))
            {
                selectedObject = hit.transform.gameObject;
            }
        }
    }

    void MoveObject(Vector2 screenPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            selectedObject.transform.position = hitPose.position;
        }
    }

    void ScaleObject()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float prevDistance = (touch1.position - touch1.deltaPosition - (touch2.position - touch2.deltaPosition)).magnitude;
            float currentDistance = (touch1.position - touch2.position).magnitude;
            float scaleFactor = currentDistance / prevDistance;

            if (selectedObject != null)
            {
                selectedObject.transform.localScale *= scaleFactor;
            }
        }
    }

    public void SnapToSurface()
    {
        if (selectedObject == null) return;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector3 objectPosition = selectedObject.transform.position;

        if (raycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), hits, TrackableType.PlaneWithinPolygon))
        {
            selectedObject.transform.position = new Vector3(objectPosition.x, hits[0].pose.position.y, objectPosition.z);
        }
    }
}
