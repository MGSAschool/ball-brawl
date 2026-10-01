using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// "Staphf it!" — Red Light, Green Light. Players may move during Green; any player
/// whose Rigidbody speed exceeds the movement threshold during Red is eliminated.
/// Reaching RedLightGoalZone counts as finishing. Server-authoritative.
/// </summary>
public class RedLightGreenLightMinigame : NetworkBehaviour
{
    public enum Phase { Green, Red }
    public enum MinigameStatus { Waiting, Playing, Finished }

    [Header("Phase Timing")]
    public float minPhaseDuration = 2f;
    public float maxPhaseDuration = 5f;
    public float matchTimeLimit = 60f;

    [Header("Elimination")]
    public float movementThreshold = 0.35f;

    [Header("Auto Start")]
    public bool autoStart = true;
    public float autoStartDelay = 5f;

    public NetworkVariable<Phase> CurrentPhase = new NetworkVariable<Phase>(Phase.Green,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<MinigameStatus> Status = new NetworkVariable<MinigameStatus>(MinigameStatus.Waiting,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly Dictionary<ulong, NetworkObject> participants = new Dictionary<ulong, NetworkObject>();
    private readonly HashSet<ulong> eliminated = new HashSet<ulong>();
    private readonly HashSet<ulong> finished = new HashSet<ulong>();
    private bool autoStartPending;

    public void RegisterPlayerServer(NetworkObject player)
    {
        if (!IsServer) return;
        participants[player.OwnerClientId] = player;

        if (autoStart && Status.Value == MinigameStatus.Waiting && !autoStartPending)
        {
            autoStartPending = true;
            StartCoroutine(AutoStartRoutine());
        }
    }

    private IEnumerator AutoStartRoutine()
    {
        yield return new WaitForSeconds(autoStartDelay);
        autoStartPending = false;
        StartMinigameServer();
    }

    public void UnregisterPlayerServer(NetworkObject player)
    {
        if (!IsServer) return;
        participants.Remove(player.OwnerClientId);
    }

    public void ReportGoalReachedServer(ulong clientId)
    {
        if (!IsServer || Status.Value != MinigameStatus.Playing) return;
        if (eliminated.Contains(clientId)) return;
        finished.Add(clientId);
        CheckForEnd();
    }

    public void StartMinigameServer()
    {
        if (!IsServer || Status.Value == MinigameStatus.Playing) return;
        eliminated.Clear();
        finished.Clear();
        StartCoroutine(RunMinigame());
    }

    private IEnumerator RunMinigame()
    {
        Status.Value = MinigameStatus.Playing;
        CurrentPhase.Value = Phase.Green;

        float elapsed = 0f;
        while (elapsed < matchTimeLimit && Status.Value == MinigameStatus.Playing)
        {
            float phaseDuration = Random.Range(minPhaseDuration, maxPhaseDuration);
            float phaseTimer = 0f;

            while (phaseTimer < phaseDuration)
            {
                yield return null;
                phaseTimer += Time.deltaTime;
                elapsed += Time.deltaTime;

                if (CurrentPhase.Value == Phase.Red) CheckMovementDuringRed();
                if (elapsed >= matchTimeLimit || AllResolved()) break;
            }

            if (elapsed >= matchTimeLimit || AllResolved()) break;
            CurrentPhase.Value = CurrentPhase.Value == Phase.Green ? Phase.Red : Phase.Green;
        }

        Status.Value = MinigameStatus.Finished;
    }

    private void CheckMovementDuringRed()
    {
        foreach (var kvp in participants)
        {
            ulong clientId = kvp.Key;
            NetworkObject player = kvp.Value;
            if (player == null || eliminated.Contains(clientId) || finished.Contains(clientId)) continue;

            if (player.TryGetComponent<Rigidbody>(out var rb) && rb.linearVelocity.magnitude > movementThreshold)
                eliminated.Add(clientId);
        }
        CheckForEnd();
    }

    private bool AllResolved()
    {
        foreach (var clientId in participants.Keys)
            if (!eliminated.Contains(clientId) && !finished.Contains(clientId)) return false;
        return true;
    }

    private void CheckForEnd()
    {
        if (AllResolved()) Status.Value = MinigameStatus.Finished;
    }
}
