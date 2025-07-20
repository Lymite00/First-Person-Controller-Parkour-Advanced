using System;
using UnityEngine;

public class JumpPad : MonoBehaviour
{

    [Header("Variables")] 
    [SerializeField] private float forceAmount;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("hit");
            Rigidbody playerRb = other.gameObject.GetComponent<Rigidbody>();
            if (playerRb!=null)
            {
                playerRb.AddForce(Vector3.up * forceAmount, ForceMode.Impulse);
            }
        }
    }
}
