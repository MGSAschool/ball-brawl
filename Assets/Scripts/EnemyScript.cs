using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] private float moveSpeed = 5f;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void OnTriggerStay(Collider other)
    {
        Vector3 followDir = (other.transform.position - transform.position).normalized;

        if (other.CompareTag("Player") && other.transform.position.magnitude > 0.1f)
        {
            
            rb.AddForce(followDir * moveSpeed, ForceMode.Acceleration);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rb.AddForce(-rb.linearVelocity, ForceMode.VelocityChange);
        }
    }
}