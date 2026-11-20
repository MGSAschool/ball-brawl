using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float spinSpeed = 180f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2.5f;

    private Vector3 startPosition;
    private float bobOffset;

    private void Start()
    {
        startPosition = transform.position;
        bobOffset = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        transform.position = startPosition + Vector3.up * bob;
    }

    private void OnTriggerEnter(Collider other)
    {
        BrawlPlayer player = other.GetComponentInParent<BrawlPlayer>();
        if (player == null || MatchManager.Instance == null) return;

        if (MatchManager.Instance.AwardCoins(player, value)) Destroy(gameObject);
    }
}
