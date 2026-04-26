using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// CloudLink - MultiplayerManager.cs
/// Handles Photon PUN 2 networking: connection, room management, and player sync.
/// Authors: Karma Muslim (original), Marckins Azard (final edits)
/// Meetings: April 14, April 16, April 25
/// </summary>
public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static MultiplayerManager Instance { get; private set; }
    #endregion

    #region Fields
    [Header("Room Settings")]
    [SerializeField] private string roomName = "ARCollabRoom";
    [SerializeField] private byte maxPlayers = 4;

    private bool isConnecting = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Auto-sync scene for all players in same room
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        ConnectToPhoton();
    }
    #endregion

    #region Connection Methods
    /// <summary>Initiates connection to Photon Cloud using settings from PhotonServerSettings asset.</summary>
    public void ConnectToPhoton()
    {
        isConnecting = true;
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[CloudLink] Connecting to Photon Master Server...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            JoinOrCreateRoom();
        }
    }

    private void JoinOrCreateRoom()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }
    #endregion

    #region Photon Callbacks
    public override void OnConnectedToMaster()
    {
        Debug.Log("[CloudLink] Connected to Photon Master Server.");
        if (isConnecting)
        {
            JoinOrCreateRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[CloudLink] Joined room: {PhotonNetwork.CurrentRoom.Name} | Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        // Trigger voice initialization once room is established
        VoiceManager.Instance?.InitializeVoice();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[CloudLink] Player joined: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[CloudLink] Player left: {otherPlayer.NickName}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[CloudLink] Disconnected: {cause}. Retrying...");
        isConnecting = false;
        // Auto-reconnect after short delay
        Invoke(nameof(ConnectToPhoton), 3f);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[CloudLink] Failed to join room: {message} (code {returnCode})");
    }
    #endregion
}
