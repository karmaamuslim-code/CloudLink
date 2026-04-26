using UnityEngine;
using Photon.Pun;

/// <summary>
/// CloudLink - SharedObject.cs
/// Synchronizes position and rotation of a shared AR object across all Photon clients.
/// Supports one-finger drag (move) and two-finger twist (rotate).
/// Authors: Karma Muslim (original), Marckins Azard (final edits)
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class SharedObject : MonoBehaviourPun, IPunObservable
{
    #region Network State
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    [SerializeField] private float lerpSpeed = 10f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            HandleTouchInput();
        }
        else
        {
            // Smooth remote interpolation to reduce jitter
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * lerpSpeed);
        }
    }
    #endregion

    #region Touch Input
    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                // Move object in XZ plane relative to camera
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(touch.position.x, touch.position.y, 2f));
                transform.position = worldPos;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                // Two-finger twist gesture for Y-axis rotation
                Vector2 t0Prev = t0.position - t0.deltaPosition;
                Vector2 t1Prev = t1.position - t1.deltaPosition;

                float prevAngle = Mathf.Atan2(t1Prev.y - t0Prev.y, t1Prev.x - t0Prev.x) * Mathf.Rad2Deg;
                float curAngle  = Mathf.Atan2(t1.position.y - t0.position.y,
                                               t1.position.x - t0.position.x) * Mathf.Rad2Deg;

                float angleDelta = Mathf.DeltaAngle(prevAngle, curAngle);
                transform.Rotate(0f, -angleDelta, 0f, Space.World);
            }
        }
    }
    #endregion

    #region Photon Serialization
    /// <summary>Called by Photon at each network tick to sync state.</summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Owner sends current transform
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Remote clients receive and store for lerp
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
    #endregion
}
