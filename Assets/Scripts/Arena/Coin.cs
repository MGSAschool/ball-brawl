using System;
using UnityEngine;

// Collectable coin spawned by CoinRush. Any brawler who touches it during a match scores.
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float spinSpeed = 180f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2.5f;

    public event Action<Coin> Collected;

    private Vector3 basePosition;
    private float bobOffset;

    private void Start()
    {
        basePosition = transform.position;
        bobOffset = UnityEngine.Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<BrawlPlayer>();
        if (player == null || MatchManager.Instance == null) return;

        if (MatchManager.Instance.AwardCoins(player, value))
        {
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
