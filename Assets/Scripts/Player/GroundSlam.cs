using System;
using UnityEngine;

public class GroundSlam : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerCam;
    private Rigidbody _rb;
    private PlayerController _pc;

    [Header("Slamming")]
    [SerializeField] private float slamForce;
    [SerializeField] private float slamDuration;
    [SerializeField] private float freezeDuration = 0.3f;
    [SerializeField] private float slamHeight = 2f;
    public bool canSlam;
    private bool isSlamming;
    private bool isFreezing;
    private float slamTimer;
    private float freezeTimer;

    [Header("CameraEffects")]
    [SerializeField] private PlayerCam cam;

    [Header("Input")]
    [SerializeField] private KeyCode slamKey = KeyCode.E;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(slamKey) && _pc.inAir && !isSlamming && !isFreezing && IsAboveSlamHeight())
        {
            StartSlam();
        }
    }

    private void FixedUpdate()
    {
        if (isFreezing)
        {
            Freezing();
        }
        else if (isSlamming)
        {
            Slamming();
        }
    }

    private void StartSlam()
    {
        isFreezing = true;
        freezeTimer = freezeDuration;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        cam.DoFov(_pc.slammingFov);
    }

    private void Freezing()
    {
        freezeTimer -= Time.fixedDeltaTime;

        if (freezeTimer <= 0)
        {
            isFreezing = false;
            isSlamming = true;
            slamTimer = slamDuration;
            _rb.isKinematic = false;
            _rb.AddForce(Vector3.down * slamForce, ForceMode.Impulse);
        }
    }

    private void Slamming()
    {
        slamTimer -= Time.fixedDeltaTime;

        if (slamTimer <= 0 || IsGrounded())
        {
            StopSlam();
        }
    }

    private void StopSlam()
    {
        cam.DoShake(0.3f, 10f);
        isSlamming = false;
        cam.DoFov(_pc.defaultFov);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    private bool IsAboveSlamHeight()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
        {
            return hit.distance > slamHeight;
        }
        return false;
    }
}
