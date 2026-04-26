using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Photon.Pun;
using ExitGames.Client.Photon;

/// <summary>
/// CloudLink - CloudAnchorManager.cs
/// Manages Azure Spatial Anchor hosting and resolving across multiple devices.
/// Replaces ARCore Cloud Anchors with Azure Spatial Anchors for iOS (ARKit) compatibility.
/// Developed on Windows (HP); tested on iOS via Unity's iOS Build Support.
///
/// Prerequisites:
///   - Azure Spatial Anchors SDK for Unity (imported via Package Manager)
///   - Azure account with a Spatial Anchors resource created
///   - Set AccountId, AccountDomain, and AccountKey in Inspector (from Azure Portal)
///   - iOS build: Xcode on a Mac is required to sign + deploy the IPA to device
///     (Build on HP in Unity → transfer project to Mac → open in Xcode → deploy)
///
/// Authors: Karma Muslim (original), Marckins Azard (final edits)
/// </summary>
public class CloudAnchorManager : MonoBehaviour
{
    #region Inspector Fields
    [Header("AR Components")]
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject sharedObjectPrefab;

    [Header("Azure Spatial Anchors Configuration")]
    [Tooltip("Account ID from Azure Portal → Spatial Anchors resource → Keys")]
    [SerializeField] private string azureAccountId     = "YOUR_AZURE_ACCOUNT_ID";
    [Tooltip("Account Domain e.g. eastus.mixedreality.azure.com")]
    [SerializeField] private string azureAccountDomain = "YOUR_AZURE_ACCOUNT_DOMAIN";
    [Tooltip("Primary key from Azure Portal → Spatial Anchors resource → Keys")]
    [SerializeField] private string azureAccountKey    = "YOUR_AZURE_ACCOUNT_KEY";

    [Header("Anchor Settings")]
    [SerializeField] private int anchorExpirationDays = 1;
    #endregion

    #region Private State
    private SpatialAnchorManager spatialAnchorManager;
    private CloudSpatialAnchor   hostedCloudAnchor;
    private bool isHosting   = false;
    private bool isResolving = false;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private const string ROOM_PROP_ANCHOR_ID = "AzureAnchorId";
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        spatialAnchorManager = GetComponent<SpatialAnchorManager>();
        if (spatialAnchorManager == null)
            spatialAnchorManager = gameObject.AddComponent<SpatialAnchorManager>();

        // Inject credentials at runtime (avoids committing keys to source control)
        spatialAnchorManager.AuthenticationMode              = AuthenticationMode.ApiKey;
        spatialAnchorManager.SpatialAnchorsAccountId     = azureAccountId;
        spatialAnchorManager.SpatialAnchorsAccountDomain = azureAccountDomain;
        spatialAnchorManager.SpatialAnchorsAccountKey    = azureAccountKey;
    }

    private void Update()
    {
        // Only MasterClient places the anchor on tap
        if (PhotonNetwork.IsMasterClient && !isHosting && !isResolving)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                TryHostAnchor(Input.GetTouch(0).position);
        }
    }
    #endregion

    #region Hosting
    private void TryHostAnchor(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            isHosting = true;
            StartCoroutine(HostAnchorCoroutine(hits[0].pose));
        }
    }

    private IEnumerator HostAnchorCoroutine(Pose hitPose)
    {
        Debug.Log("[CloudLink] Starting Azure Spatial Anchor session...");
        yield return spatialAnchorManager.StartSessionAsync().AsCoroutine();

        ARAnchor localAnchor = anchorManager.AddAnchor(hitPose);
        if (localAnchor == null)
        {
            Debug.LogError("[CloudLink] Failed to create local AR anchor.");
            isHosting = false;
            yield break;
        }

        hostedCloudAnchor = new CloudSpatialAnchor
        {
            LocalAnchor = localAnchor.nativePtr,
            Expiration  = DateTimeOffset.Now.AddDays(anchorExpirationDays)
        };

        // ASA requires enough environmental scan data before creating (~3-5 sec of movement)
        Debug.Log("[CloudLink] Scanning environment — move device slowly around the anchor area.");
        while (!spatialAnchorManager.IsReadyForCreate)
        {
            float progress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"[CloudLink] Scan progress: {progress * 100:F0}%");
            yield return new WaitForSeconds(0.5f);
        }

        bool hosted = false;
        spatialAnchorManager.Session.CreateAnchorAsync(hostedCloudAnchor)
            .GetAwaiter().OnCompleted(() => hosted = true);
        yield return new WaitUntil(() => hosted);

        if (!string.IsNullOrEmpty(hostedCloudAnchor.Identifier))
        {
            Debug.Log($"[CloudLink] Azure anchor hosted. ID: {hostedCloudAnchor.Identifier}");
            ShareAnchorId(hostedCloudAnchor.Identifier);
            SpawnSharedObject(localAnchor.transform.position, localAnchor.transform.rotation);
        }
        else
        {
            Debug.LogError("[CloudLink] Azure anchor hosting failed — no identifier returned.");
        }

        isHosting = false;
    }

    private void ShareAnchorId(string anchorId)
    {
        Hashtable props = new Hashtable { { ROOM_PROP_ANCHOR_ID, anchorId } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
    #endregion

    #region Resolving
    /// <summary>
    /// Called when Photon room custom properties update (anchor ID broadcast by host).
    /// Wire this to MultiplayerManager.OnRoomPropertiesUpdate.
    /// </summary>
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey(ROOM_PROP_ANCHOR_ID))
        {
            string anchorId = (string)propertiesThatChanged[ROOM_PROP_ANCHOR_ID];
            StartCoroutine(ResolveAnchorCoroutine(anchorId));
        }
    }

    private IEnumerator ResolveAnchorCoroutine(string anchorId)
    {
        isResolving = true;
        Debug.Log($"[CloudLink] Resolving Azure anchor ID: {anchorId}");
        yield return spatialAnchorManager.StartSessionAsync().AsCoroutine();

        var criteria = new AnchorLocateCriteria
        {
            Identifiers = new[] { anchorId },
            Strategy    = LocateStrategy.VisualInformation
        };

        CloudSpatialAnchorWatcher watcher = spatialAnchorManager.Session.CreateWatcher(criteria);
        spatialAnchorManager.AnchorLocated += OnAnchorLocated;

        // Wait up to 30 seconds for resolution
        float elapsed = 0f;
        while (isResolving && elapsed < 30f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (isResolving)
        {
            Debug.LogError("[CloudLink] Anchor resolution timed out after 30 seconds.");
            isResolving = false;
        }

        watcher.Stop();
        spatialAnchorManager.AnchorLocated -= OnAnchorLocated;
    }

    private void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        if (args.Status == LocateAnchorStatus.Located)
        {
            isResolving = false;
            Debug.Log("[CloudLink] Azure anchor located successfully.");

            // Must dispatch to Unity main thread for Transform/Instantiate calls
            UnityDispatcher.InvokeOnAppThread(() =>
            {
                Pose anchorPose = args.Anchor.GetPose();
                SpawnSharedObject(anchorPose.position, anchorPose.rotation);
            });
        }
        else
        {
            Debug.LogError($"[CloudLink] Anchor locate failed: {args.Status}");
        }
    }
    #endregion

    #region Object Spawning
    private void SpawnSharedObject(Vector3 position, Quaternion rotation)
    {
        // Non-master clients do NOT call Instantiate — Photon syncs the object automatically
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(sharedObjectPrefab.name, position, rotation);
            Debug.Log("[CloudLink] Shared object spawned via Photon.");
        }
    }
    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        if (spatialAnchorManager != null && spatialAnchorManager.IsSessionStarted)
            spatialAnchorManager.StopSession();
    }
    #endregion
}
