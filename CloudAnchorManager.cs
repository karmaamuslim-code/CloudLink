using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Photon.Pun;
using ExitGames.Client.Photon;

/// <summary>
/// CloudLink - CloudAnchorManager.cs
/// Manages ARCore Cloud Anchor hosting and resolving across multiple devices.
/// Host (MasterClient) places anchor; other players receive and resolve the anchor ID.
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

    [Header("Cloud Anchor Settings")]
    [SerializeField] private int anchorTTLDays = 1;
    #endregion

    #region Private State
    private ARCloudAnchor cloudAnchor;
    private bool isHosting = false;
    private bool isResolving = false;
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private const string ROOM_PROP_ANCHOR_ID = "CloudAnchorId";
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        // Only MasterClient places the anchor on tap
        if (PhotonNetwork.IsMasterClient && !isHosting)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                TryHostCloudAnchor(Input.GetTouch(0).position);
            }
        }

        // Poll cloud anchor resolution status
        if (isResolving && cloudAnchor != null)
        {
            if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
            {
                isResolving = false;
                Debug.Log("[CloudLink] Cloud anchor resolved successfully.");
                SpawnSharedObject(cloudAnchor.transform.position, cloudAnchor.transform.rotation);
            }
            else if (cloudAnchor.cloudAnchorState != CloudAnchorState.TaskInProgress)
            {
                isResolving = false;
                Debug.LogError($"[CloudLink] Anchor resolution failed: {cloudAnchor.cloudAnchorState}");
            }
        }
    }
    #endregion

    #region Hosting
    private void TryHostCloudAnchor(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            ARAnchor localAnchor = anchorManager.AddAnchor(hitPose);
            if (localAnchor != null)
            {
                isHosting = true;
                StartCoroutine(HostAnchorCoroutine(localAnchor));
            }
        }
    }

    private IEnumerator HostAnchorCoroutine(ARAnchor anchor)
    {
        Debug.Log("[CloudLink] Hosting cloud anchor...");
        ARCloudAnchor hostedAnchor = anchorManager.HostCloudAnchor(anchor, anchorTTLDays);

        if (hostedAnchor == null)
        {
            Debug.LogError("[CloudLink] HostCloudAnchor returned null.");
            isHosting = false;
            yield break;
        }

        // Poll until hosting completes
        while (hostedAnchor.cloudAnchorState == CloudAnchorState.TaskInProgress)
        {
            yield return null;
        }

        if (hostedAnchor.cloudAnchorState == CloudAnchorState.Success)
        {
            string anchorId = hostedAnchor.cloudAnchorId;
            Debug.Log($"[CloudLink] Cloud anchor hosted. ID: {anchorId}");

            // Broadcast anchor ID to all players via Photon room properties
            ShareAnchorId(anchorId);

            // Host also spawns the shared object
            SpawnSharedObject(hostedAnchor.transform.position, hostedAnchor.transform.rotation);
        }
        else
        {
            Debug.LogError($"[CloudLink] Hosting failed: {hostedAnchor.cloudAnchorState}");
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
    /// <summary>Called when Photon room custom properties are updated (e.g., anchor ID shared by host).</summary>
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey(ROOM_PROP_ANCHOR_ID))
        {
            string anchorId = (string)propertiesThatChanged[ROOM_PROP_ANCHOR_ID];
            ResolveCloudAnchor(anchorId);
        }
    }

    private void ResolveCloudAnchor(string anchorId)
    {
        Debug.Log($"[CloudLink] Resolving anchor ID: {anchorId}");
        cloudAnchor = anchorManager.ResolveCloudAnchorId(anchorId);
        if (cloudAnchor != null)
        {
            isResolving = true;
        }
        else
        {
            Debug.LogError("[CloudLink] ResolveCloudAnchorId returned null.");
        }
    }
    #endregion

    #region Object Spawning
    private void SpawnSharedObject(Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(sharedObjectPrefab.name, position, rotation);
            Debug.Log("[CloudLink] Shared object spawned via Photon.");
        }
    }
    #endregion
}
