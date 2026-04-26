using System;
using UnityEngine;
using Agora.Rtc;

/// <summary>
/// CloudLink - VoiceManager.cs
/// Manages Agora RTC voice channel with Forward Error Correction (FEC) enabled.
/// Singleton pattern; initialized automatically when a Photon room is joined.
/// Authors: Karma Muslim (original), Marckins Azard (final edits)
/// </summary>
public class VoiceManager : MonoBehaviour
{
    #region Singleton
    public static VoiceManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("Agora Configuration")]
    [SerializeField] private string appId = "YOUR_AGORA_APP_ID";
    [SerializeField] private string channelName = "ARCollabVoice";

    [Header("Audio Quality")]
    [SerializeField] private bool enableFEC = true;
    [SerializeField] private bool enableDTX = true;   // Discontinuous transmission (saves bandwidth)
    #endregion

    #region Private State
    private IRtcEngine rtcEngine;
    private bool isMuted = false;
    private bool isInitialized = false;
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
    }

    private void OnDestroy()
    {
        ShutdownVoice();
    }
    #endregion

    #region Public API
    /// <summary>Initialize Agora engine and join the shared voice channel.</summary>
    public void InitializeVoice()
    {
        if (isInitialized) return;

        rtcEngine = RtcEngine.CreateAgoraRtcEngine();

        RtcEngineContext context = new RtcEngineContext
        {
            appId           = appId,
            channelProfile  = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION,
            audioScenario   = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT
        };

        int ret = rtcEngine.Initialize(context);
        if (ret != 0)
        {
            Debug.LogError($"[CloudLink] Agora init failed: {ret}");
            return;
        }

        // Enable audio subsystem
        rtcEngine.EnableAudio();

        // Forward Error Correction: reconstructs lost packets without re-sending
        // Adds ~30% overhead but significantly improves audio under packet loss (>5%)
        if (enableFEC)
        {
            rtcEngine.SetParameters("{\"che.audio.enable.fec\": true}");
            Debug.Log("[CloudLink] FEC enabled for audio stream.");
        }

        // Discontinuous transmission: silences background noise packets
        if (enableDTX)
        {
            rtcEngine.SetParameters("{\"che.audio.enable.dtx\": true}");
        }

        // Join without a token (use Agora console to generate token for production)
        rtcEngine.JoinChannel("", channelName, 0);
        isInitialized = true;
        Debug.Log($"[CloudLink] Voice initialized. Channel: {channelName}");
    }

    /// <summary>Toggle local microphone mute state.</summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        rtcEngine?.MuteLocalAudioStream(isMuted);
        Debug.Log($"[CloudLink] Muted: {isMuted}");
    }

    /// <summary>Mute or unmute a specific remote user.</summary>
    public void MuteRemoteUser(uint uid, bool mute)
    {
        rtcEngine?.MuteRemoteAudioStream(uid, mute);
    }

    public bool IsMuted => isMuted;
    #endregion

    #region Shutdown
    private void ShutdownVoice()
    {
        if (rtcEngine != null)
        {
            rtcEngine.LeaveChannel();
            rtcEngine.Dispose();
            rtcEngine = null;
            isInitialized = false;
            Debug.Log("[CloudLink] Voice engine disposed.");
        }
    }
    #endregion
}
