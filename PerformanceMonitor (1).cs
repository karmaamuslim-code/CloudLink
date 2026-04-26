using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// CloudLink - PerformanceMonitor.cs
/// Tracks and displays real-time network/performance metrics:
///   - Round-trip latency (Photon ping)
///   - Frames per second
///   - Estimated packet loss rate
/// Data is also logged to a CSV-compatible string for post-test analysis.
/// Authors: Karma Muslim (original), Marckins Azard (final edits)
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI References")]
    [SerializeField] private Text latencyText;
    [SerializeField] private Text fpsText;
    [SerializeField] private Text packetLossText;

    [Header("Sampling")]
    [SerializeField] private float updateInterval = 0.5f;   // seconds between UI refresh
    [SerializeField] private int   latencyHistorySize = 30; // rolling window for average
    #endregion

    #region Private State
    private float   lastUpdateTime = 0f;
    private int     frameCount     = 0;
    private float   currentFPS     = 0f;
    private int     currentLatency = 0;
    private float   packetLossEst  = 0f;

    private Queue<int> latencyHistory = new Queue<int>();
    private List<string> logEntries   = new List<string>(); // for CSV export
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        logEntries.Add("Time,LatencyMs,FPS,PacketLossEst");
    }

    private void Update()
    {
        frameCount++;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            // FPS
            currentFPS = frameCount / (Time.time - lastUpdateTime);
            frameCount = 0;
            lastUpdateTime = Time.time;

            // Latency
            if (PhotonNetwork.IsConnected)
            {
                currentLatency = PhotonNetwork.GetPing();
                UpdateLatencyHistory(currentLatency);
                EstimatePacketLoss();
            }

            UpdateUI();
            LogEntry();
        }
    }
    #endregion

    #region Metrics
    private void UpdateLatencyHistory(int ping)
    {
        latencyHistory.Enqueue(ping);
        if (latencyHistory.Count > latencyHistorySize)
            latencyHistory.Dequeue();
    }

    /// <summary>
    /// Heuristic: jitter (std dev of ping) correlates with packet loss.
    /// High jitter (>50ms) suggests ~5-10% packet loss in typical Photon conditions.
    /// </summary>
    private void EstimatePacketLoss()
    {
        if (latencyHistory.Count < 2) return;

        float mean = 0f;
        foreach (int p in latencyHistory) mean += p;
        mean /= latencyHistory.Count;

        float variance = 0f;
        foreach (int p in latencyHistory) variance += (p - mean) * (p - mean);
        float stdDev = Mathf.Sqrt(variance / latencyHistory.Count);

        // Rough heuristic mapping jitter to packet loss %
        packetLossEst = Mathf.Clamp(stdDev / 10f, 0f, 30f);
    }

    public int   GetAverageLatency()
    {
        if (latencyHistory.Count == 0) return 0;
        int sum = 0;
        foreach (int p in latencyHistory) sum += p;
        return sum / latencyHistory.Count;
    }

    public float GetCurrentFPS() => currentFPS;
    public float GetPacketLossEstimate() => packetLossEst;
    #endregion

    #region UI
    private void UpdateUI()
    {
        if (latencyText    != null) latencyText.text    = $"Latency: {currentLatency} ms (avg {GetAverageLatency()} ms)";
        if (fpsText        != null) fpsText.text        = $"FPS: {currentFPS:F1}";
        if (packetLossText != null) packetLossText.text = $"Pkt Loss Est: {packetLossEst:F1}%";
    }
    #endregion

    #region Logging
    private void LogEntry()
    {
        logEntries.Add($"{Time.time:F2},{currentLatency},{currentFPS:F1},{packetLossEst:F2}");
    }

    /// <summary>Returns CSV string of all recorded metrics for export.</summary>
    public string ExportCSV() => string.Join("\n", logEntries);
    #endregion
}
