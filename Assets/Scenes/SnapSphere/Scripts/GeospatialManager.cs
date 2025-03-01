using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Android;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;
using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors;
using System;
/// <summary>
/// All possible error states. Used to inform other components' behaviors.
/// </summary>
[System.Flags] public enum ErrorState { Null = 0, NoError = 1, Tracking = 2, Message = 4, Camera = 8, Location = 16 }

public class GeospatialManager : MonoBehaviour
{
    [Header("[ AR Components ]")]
    public XROrigin SessionOrigin;
    public ARSession Session;
    public ARAnchorManager AnchorManager;
    public AREarthManager EarthManager;
    public ARCoreExtensions ARCoreExtensions;
    public ARRaycastManager RaycastManager;
    public ARPlaneManager PlaneManager;

    /// <summary>
    /// True while Earth Manager is tracking and accuracy minimums are met
    /// </summary>
    public bool IsTracking { get => _trackingValid; }

    /// <summary>
    /// True once we've reached target accuracy and for the remainder of the session
    /// </summary>
    public bool IsAccuracyTargetReached { get => _targetAccuracyReached; }

    /// <summary>
    /// The current error message, if there is one
    /// </summary>
    public string CurrentErrorMessage { get => _errorMessage; }

    /// <summary>
    /// Best horizontal accuracy value reached at any point during the current session
    /// </summary>
    public double BestHorizontalAccuracy { get => _bestHorizontalAccuracy; }

    /// <summary>
    /// Best heading accuracy value reached at any point during the current session
    /// </summary>
    public double BestHeadingAccuracy { get => _bestHeadingAccuracy; }

    /// <summary>
    /// Best altitude accuracy value reached at any point during the current session
    /// </summary>
    public double BestVerticalAccuracy { get => _bestVerticalAccuracy; }

    /// <summary>
    /// Current error state enum
    /// </summary>
    public ErrorState CurrentErrorState { get => _errorState; }

    public Camera MainCamera { get => Camera.main; }

    /// <summary>
    /// Raised once when all components are ready
    /// </summary>
    [HideInInspector] public UnityEvent InitCompleted;

    /// <summary>
    /// Raised on any frame that accuracy has reached better values than any previous 
    /// </summary>
    [HideInInspector] public UnityEvent AccuracyImproved;

    /// <summary>
    /// Raised once when the specified target accuracy values are reached
    /// </summary>
    [HideInInspector] public UnityEvent TargetAccuracyReached;
    [HideInInspector] public UnityEvent<string> OnAnchorHosted;
    [HideInInspector] public UnityEvent<string, ResolveCloudAnchorResult> OnAnchorResolved;

    /// <summary>
    /// Raised on any frame that there is a change in error state
    /// Includes the error state enum and error message string if applicable
    /// </summary>
    [HideInInspector] public UnityEvent<ErrorState, string> ErrorStateChanged;

    [Header("[ Accuracy Minimums ] - Required to start experience")]
    [SerializeField] private float _minimumHorizontalAccuracy = 10;
    [SerializeField] private float _minimumHeadingAccuracy = 15;
    [SerializeField] private float _minimumVerticalAccuracy = 1.5f;

    [Header("[ Accuracy Targets ] - Event raised when reached")]
    [SerializeField] private float _targetHorizontalAccuracy = 1;
    [SerializeField] private float _targetHeadingAccuracy = 2;
    [SerializeField] private float _targetVerticalAccuracy = 0.5f;

    [Header("[ Settings ]")]
    [SerializeField] private float _initTime = 10f;

    private ErrorState _errorState = ErrorState.NoError;
    private string _errorMessage;
    private double _bestHorizontalAccuracy = Mathf.Infinity;
    private double _bestHeadingAccuracy = Mathf.Infinity;
    private double _bestVerticalAccuracy = Mathf.Infinity;
    private bool _trackingValid,
                 _enablingGeospatial,
                 _initComplete,
                 _targetAccuracyReached,
                 _requestCamPerm,
                 _requestLocPerm,
                 _startedAR;

    public List<ResolveCloudAnchorPromise> _resolvePromises = new();
    public List<ResolveCloudAnchorResult> _resolveResults = new();

    private HostCloudAnchorPromise _hostPromise;
    private HostCloudAnchorResult _hostResult;
    private AnchorController.CloudAnchorHistory _hostedCloudAnchor;
    private IEnumerator _hostCoroutine;
    private ARAnchor _anchor;
    private MapQualityIndicator _qualityIndicator;
    private AnchorController anchorController;
    public TMPro.TextMeshProUGUI InstructionText;
    public TMPro.TextMeshProUGUI DebugText;
    public GameObject MapQualityIndicatorPrefab;

    private void Start()
    {
        anchorController = FindFirstObjectByType<AnchorController>();
        _qualityIndicator = FindFirstObjectByType<MapQualityIndicator>();
        SetErrorState(ErrorState.NoError);

#if UNITY_IOS && !UNITY_EDITOR
            Debug.Log("Start location services.");
            Input.location.Start();
#endif
    }

    private void Update()
    {
        if (!CheckCameraPermission())
            return;

        if (!CheckLocationPermission())
            return;

        if (!_startedAR)
        {
            SessionOrigin.gameObject.SetActive(true);
            Session.gameObject.SetActive(true);
            ARCoreExtensions.gameObject.SetActive(true);
            _startedAR = true;
        }

        UpdateSessionState();

        if (ARSession.state != ARSessionState.SessionInitializing &&
            ARSession.state != ARSessionState.SessionTracking)
        {
            return;
        }

        FeatureSupported featureSupport = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);

        switch (featureSupport)
        {
            case FeatureSupported.Unknown:
                SetErrorState(ErrorState.Message, "Geospatial API encountered an unknown error.");
                return;
            case FeatureSupported.Unsupported:
                SetErrorState(ErrorState.Message, "Geospatial API is not supported by this device.");
                enabled = false;
                return;
            case FeatureSupported.Supported:
                if (ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode == GeospatialMode.Disabled)
                {
                    Debug.Log("Enabling Geospatial Mode...");
                    ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode =
                        GeospatialMode.Enabled;
                    _enablingGeospatial = true;
                    return;
                }
                break;
        }
        /// Waiting for new configuration taking effect
        if (_enablingGeospatial)
        {
            _initTime -= Time.deltaTime;

            if (_initTime < 0)
            {
                Debug.Log("Geospatial Mode enabled.");
                _enablingGeospatial = false;
            }
            else
                return;
        }

        /// Check earth state
        EarthState earthState = EarthManager.EarthState;

        if (earthState != EarthState.Enabled)
        {
            SetErrorState(ErrorState.Message, "Error: Unable to start Geospatial AR");
            // enabled = false;
            // retry
            _initTime = 3f;
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
            bool isSessionReady = ARSession.state == ARSessionState.SessionTracking &&
            Input.location.status == LocationServiceStatus.Running;
#else
        bool isSessionReady = ARSession.state == ARSessionState.SessionTracking;

#endif

        /// **** Init Complete ****
        if (!_initComplete)
        {
            InitCompleted.Invoke();
            _initComplete = true;
            SetErrorState(ErrorState.Tracking);

            Debug.Log("Geospatial AR Session Ready.");
        }

        if (TrackingIsValid())
        {
            // Debug.Log("Tracking is Valid.");

            if (CheckAccuracyImproved())
            {
                /// Raise event if accuracy has improved since last check
                AccuracyImproved.Invoke();
            }

            if (!_targetAccuracyReached && CheckTargetAccuracyReached())
            {
                Debug.Log("** Target Accuracy Reached!! **");
                /// Raise event if target accuracy reached
                TargetAccuracyReached.Invoke();
                _targetAccuracyReached = true;
            }
        }

        // if tracking is valid, start hosting cloud anchor
        ResolvingCloudAnchors();

        HostingCloudAnchor();
    }

    /// <summary>
    /// Ensure we have Camera usage permission
    /// </summary>
    /// <returns></returns>
    private bool CheckCameraPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            if (_errorState != ErrorState.Camera)
                SetErrorState(ErrorState.Camera);


            if (!_requestCamPerm) Permission.RequestUserPermission(Permission.Camera);
            _requestCamPerm = true;
            return false;
        }

        if (_errorState == ErrorState.Camera)
            SetErrorState(ErrorState.NoError);

        return true;
    }

    /// <summary>
    /// Ensure we have Location usage permission
    /// </summary>
    /// <returns></returns>
    private bool CheckLocationPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            if (_errorState != ErrorState.Location)
                SetErrorState(ErrorState.Location);

            if (!_requestLocPerm) Permission.RequestUserPermission(Permission.FineLocation);
            _requestLocPerm = true;
            return false;
        }

        if (_errorState == ErrorState.Location)
            SetErrorState(ErrorState.NoError);

        return true;
    }

    /// <summary>
    /// Monitor the state of the AR session
    /// </summary>
    private void UpdateSessionState()
    {
        /// Pressing 'back' button quits the app.
        // if (Input.GetKeyUp(KeyCode.Escape))
        // {
        //     Application.Quit();
        // }

        /// Only allow the screen to sleep when not tracking.
        var sleepTimeout = SleepTimeout.NeverSleep;
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            sleepTimeout = SleepTimeout.SystemSetting;
        }

        Screen.sleepTimeout = sleepTimeout;

        /// ARSession Status
        if (ARSession.state != ARSessionState.CheckingAvailability &&
            ARSession.state != ARSessionState.Ready &&
            ARSession.state != ARSessionState.SessionInitializing &&
            ARSession.state != ARSessionState.SessionTracking)
        {
            Debug.Log("ARSession error state: " + ARSession.state);
            SetErrorState(ErrorState.Message, "AR Error Encountered: " + ARSession.state);
            enabled = false;
        }

#if UNITY_IOS && !UNITY_EDITOR
            else if (Input.location.status == LocationServiceStatus.Failed)
            {
                SetErrorState(ErrorState.Message, "Please start the app again and grant precise location permission.");
            }
#endif
        else if (SessionOrigin == null || Session == null || ARCoreExtensions == null)
        {
            Debug.Log("Missing AR Components.");
            SetErrorState(ErrorState.Message, "Error: Something Went Wrong");
            return;
        }
    }

    /// <summary>
    /// Set error state and raise event if needed
    /// </summary>
    /// <param name="errorState"></param>
    /// <param name="message"></param>
    private void SetErrorState(ErrorState errorState, string message = null)
    {
        Debug.Log("Error State: " + errorState + " - " + message);
        if (_errorState != errorState)
        {
            _errorState = errorState;
            ErrorStateChanged.Invoke(_errorState, message);
        }
    }

    /// <summary>
    /// Returns whether or not both conditions are true:
    /// <list type="bullet"><item>
    /// Earth Manager is tracking correctly</item><item>
    /// Current accuracy meets the specified minimums</item></list>
    /// Sets error state appropriately.
    /// </summary>
    /// <returns></returns>
    private bool TrackingIsValid()
    {
        bool valid = false;

        if (!valid && EarthManager.EarthTrackingState == TrackingState.Tracking)
        {
            /// Have we met the minimums?
            valid = EarthManager.CameraGeospatialPose.HeadingAccuracy <= _minimumHeadingAccuracy &&
                    EarthManager.CameraGeospatialPose.VerticalAccuracy <= _minimumVerticalAccuracy &&
                    EarthManager.CameraGeospatialPose.HorizontalAccuracy <= _minimumHorizontalAccuracy;
        }

        if (valid != _trackingValid)
        {
            _trackingValid = valid;
            SetErrorState(_trackingValid ? ErrorState.NoError : ErrorState.Tracking);
            Debug.Log("Tracking Valid: " + _trackingValid);
        }

        return valid;
    }

    /// <summary>
    /// Compare current tracking accuracy against best values.
    /// Return whether or not accuracy has improved since the last check.
    /// </summary>
    /// <returns></returns>
    private bool CheckAccuracyImproved()
    {
        bool horizontal = EarthManager.CameraGeospatialPose.HorizontalAccuracy < _bestHorizontalAccuracy;
        bool heading = EarthManager.CameraGeospatialPose.HeadingAccuracy < _bestHeadingAccuracy;
        bool vertical = EarthManager.CameraGeospatialPose.VerticalAccuracy < _bestVerticalAccuracy;

        bool improved = false;

        if (horizontal)
        {
            improved = true;
            _bestHorizontalAccuracy = EarthManager.CameraGeospatialPose.HorizontalAccuracy;
        }
        if (heading)
        {
            improved = true;
            _bestHeadingAccuracy = EarthManager.CameraGeospatialPose.HeadingAccuracy;
        }
        if (vertical)
        {
            improved = true;
            _bestVerticalAccuracy = EarthManager.CameraGeospatialPose.VerticalAccuracy;
        }

        return improved;
    }

    /// <summary>
    /// Return whether or not we've reached our specified target tracking accuracy values
    /// </summary>
    /// <returns></returns>
    private bool CheckTargetAccuracyReached()
    {
        return EarthManager.CameraGeospatialPose.HorizontalAccuracy <= _targetHorizontalAccuracy &&
               EarthManager.CameraGeospatialPose.HeadingAccuracy <= _targetHeadingAccuracy &&
               EarthManager.CameraGeospatialPose.VerticalAccuracy <= _targetVerticalAccuracy;
    }

    /// <summary>
    /// Creates and returns a new Geospatial Anchor at the current camera position
    /// We use this when creating and updating new Placeables Groups
    /// </summary>
    /// <returns></returns>
    public ARGeospatialAnchor RequestGeospatialAnchor()
    {
        GeospatialPose pose = EarthManager.CameraGeospatialPose;
        Quaternion quaternion = Quaternion.AngleAxis(180f - (float)pose.Heading, Vector3.up);
        return AnchorManager.AddAnchor(pose.Latitude, pose.Longitude, pose.Altitude, quaternion);
    }

    /// <summary>
    /// Creates and returns a new Geospatial Anchor at the specified Geospatial Pose
    /// We use this when restoring saved Placeables Groups from memory
    /// </summary>
    /// <param name="pose"></param>
    /// <returns></returns>
    public ARGeospatialAnchor RequestGeospatialAnchor(GeospatialPose pose)
    {
        Quaternion quaternion = Quaternion.AngleAxis(180f - (float)pose.Heading, Vector3.up);
        return AnchorManager.AddAnchor(pose.Latitude, pose.Longitude, pose.Altitude, quaternion);
    }

    public GeospatialPose PoseToGeospatialPose(Pose pose)
    {
        return EarthManager.Convert(pose);
    }

    private void ResolvingCloudAnchors()
    {
        // No Cloud Anchor for resolving.
        if (anchorController.ResolvingSet.Count == 0)
        {
            return;
        }

        // There are pending or finished resolving tasks.
        if (_resolvePromises.Count > 0 || _resolveResults.Count > 0)
        {
            return;
        }

        // ARCore session is not ready for resolving.
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            return;
        }

        Debug.LogFormat("Attempting to resolve {0} Cloud Anchor(s): {1}",
            anchorController.ResolvingSet.Count,
            string.Join(",", new List<string>(anchorController.ResolvingSet).ToArray()));
        foreach (string cloudId in anchorController.ResolvingSet)
        {
            var promise = anchorController.AnchorManager.ResolveCloudAnchorAsync(cloudId);
            if (promise.State == PromiseState.Done)
            {
                Debug.LogFormat("Faild to resolve Cloud Anchor " + cloudId);
                OnAnchorResolvedFinished(false, cloudId);
            }
            else
            {
                _resolvePromises.Add(promise);
                var coroutine = ResolveAnchor(cloudId, promise);
                StartCoroutine(coroutine);
            }
        }

        anchorController.ResolvingSet.Clear();
    }

    private void OnAnchorResolvedFinished(bool success, string cloudId, string response = null)
    {
        if (success)
        {
            InstructionText.text = "Resolve success!";
            DebugText.text =
                string.Format("Succeed to resolve the Cloud Anchor: {0}.", cloudId);
        }
        else
        {
            InstructionText.text = "Resolve failed.";
            DebugText.text = "Failed to resolve Cloud Anchor: " + cloudId +
                (response == null ? "." : "with error " + response + ".");
        }
    }

    private IEnumerator ResolveAnchor(string cloudId, ResolveCloudAnchorPromise promise)
    {
        yield return promise;
        var result = promise.Result;
        _resolvePromises.Remove(promise);
        _resolveResults.Add(result);

        if (result.CloudAnchorState == CloudAnchorState.Success)
        {
            OnAnchorResolvedFinished(true, cloudId);
            // Instantiate(CloudAnchorPrefab, result.Anchor.transform);
            OnAnchorResolved.Invoke(cloudId, result);
        }
        else
        {
            OnAnchorResolvedFinished(false, cloudId, result.CloudAnchorState.ToString());
        }
    }

    public Pose GetCameraPose()
    {
        return new Pose(SessionOrigin.Camera.transform.position, SessionOrigin.Camera.transform.rotation);
    }

    private void HostingCloudAnchor()
    {
        // There is no anchor for hosting.
        if (_anchor == null)
        {
            return;
        }

        // There is a pending or finished hosting task.
        if (_hostPromise != null || _hostResult != null)
        {
            return;
        }

        // Update map quality:
        int qualityState = 2;
        // Can pass in ANY valid camera pose to the mapping quality API.
        // Ideally, the pose should represent users’ expected perspectives.
        FeatureMapQuality quality =
            AnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());
        DebugText.text = GetCameraPose() + "Current mapping quality: " + quality;
        qualityState = (int)quality;
        _qualityIndicator.UpdateQualityState(qualityState);
        // Debug.Log("Current mapping quality: " + quality);
        // Hosting instructions:
        var cameraDist = (_qualityIndicator.transform.position -
            anchorController.MainCamera.transform.position).magnitude;
        // Debug.Log("Dist: " + cameraDist);

        if (cameraDist < _qualityIndicator.Radius * 1.5f)
        {
            InstructionText.text = "You are too close, move backward.";
            return;
        }
        else if (cameraDist > 10.0f)
        {
            InstructionText.text = "You are too far, come closer.";
            return;
        }
        else if (_qualityIndicator.ReachTopviewAngle)
        {
            InstructionText.text =
                "You are looking from the top view, move around from all sides.";
            return;
        }
        else if (!_qualityIndicator.ReachQualityThreshold)
        {
            InstructionText.text = "Save the object here by capturing it from all sides.";
            return;
        }

        // Start hosting:
        InstructionText.text = "Processing...";
        DebugText.text = "Mapping quality has reached sufficient threshold, " +
            "creating Cloud Anchor.";
        DebugText.text = string.Format(
            "FeatureMapQuality has reached {0}, triggering CreateCloudAnchor.",
            anchorController.AnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose()));

        // Creating a Cloud Anchor with lifetime = 1 day.
        // This is configurable up to 365 days when keyless authentication is used.
        var promise = anchorController.AnchorManager.HostCloudAnchorAsync(_anchor, 1);
        if (promise.State == PromiseState.Done)
        {
            Debug.LogFormat("Failed to host a Cloud Anchor.");
            OnAnchorHostedFinished(false);
        }
        else
        {
            _hostPromise = promise;
            _hostCoroutine = HostAnchor();
            StartCoroutine(_hostCoroutine);
        }
    }

    private IEnumerator HostAnchor()
    {
        yield return _hostPromise;
        _hostResult = _hostPromise.Result;
        _hostPromise = null;

        if (_hostResult.CloudAnchorState == CloudAnchorState.Success)
        {
            int count = anchorController.LoadCloudAnchorHistory().Collection.Count;
            _hostedCloudAnchor =
                new AnchorController.CloudAnchorHistory("CloudAnchor" + count, _hostResult.CloudAnchorId);
            OnAnchorHostedFinished(true, _hostResult.CloudAnchorId);
        }
        else
        {
            OnAnchorHostedFinished(false, _hostResult.CloudAnchorState.ToString());
        }
    }

    private void OnAnchorHostedFinished(bool success, string response = null)
    {
        if (success)
        {
            InstructionText.text = "Finish!";
            // Invoke("DoHideInstructionBar", 1.5f);
            DebugText.text =
                string.Format("Succeed to host the Cloud Anchor: {0}.", _hostedCloudAnchor.Id);
            Debug.Log("Succeed to host the Cloud Anchor: " + response);
            // Display name panel and hide instruction bar.
            DebugText.text = _hostedCloudAnchor.Name;
            // NamePanel.SetActive(true);
            // SetSaveButtonActive(true);

            OnAnchorHosted.Invoke(_hostedCloudAnchor.Id);
        }
        else
        {
            InstructionText.text = "Host failed.";
            DebugText.text = "Failed to host a Cloud Anchor" + (response == null ? "." :
                "with error " + response + ".");
        }
    }

    public void HostCloudAnchor(ARAnchor anchor, MapQualityIndicator qualityIndicator)
    {
        // Debug.Log("HostCloudAnchor called.");

        _anchor = anchor;
        _qualityIndicator = qualityIndicator;
    }

    public void ResolveCloudAnchor(string cloudAnchorId)
    {
        if (anchorController.ResolvingSet.Contains(cloudAnchorId))
        {
            return;
        }

        anchorController.ResolvingSet.Add(cloudAnchorId);
    }

    public void ResolveCloudAnchor(string cloudAnchorId, Action<ResolveCloudAnchorResult> onResolved)
    {
        ResolveCloudAnchor(cloudAnchorId);

        UnityAction<string, ResolveCloudAnchorResult> callback = null;
        callback = (id, result) =>
        {
            if (id == cloudAnchorId)
            {
                OnAnchorResolved.RemoveListener(callback);
                onResolved(result);
            }
        };

        OnAnchorResolved.AddListener(callback);
    }
}
