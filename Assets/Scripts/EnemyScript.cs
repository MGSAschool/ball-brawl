using System.Collections;
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

        if (other.CompareTag("Player"))
        {
            
            rb.AddForce(followDir * moveSpeed, ForceMode.Acceleration);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(Idle());
        }
    }

    IEnumerator Idle()
    {
        yield return new WaitForSeconds(2.0f);
        rb.linearVelocity = Vector3.zero;
    }
}