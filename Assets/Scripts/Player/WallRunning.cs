using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;
    [SerializeField] private float wallClimbSpeed;
    [SerializeField] private float maxWallRunTime;
    
    private float _wallRunTimer;

    [Header("Input")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode upwardsRunKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode downwardsRunKey = KeyCode.LeftControl;
    
    private bool _upwardsRunning;
    private bool _downwardsRunning;
    private float _horizontalInput;
    private float _verticalInput;

    [Header("Detection")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    
    private RaycastHit _leftWallhit;
    private RaycastHit _rightWallhit;
    private bool _wallLeft;
    private bool _wallRight;

    [Header("Exiting")]
    private bool _exitingWall;
    public float exitWallTime;
    private float _exitWallTimer;

    [Header("Gravity")]
    [SerializeField] private bool useGravity;
    [SerializeField] private float gravityCounterForce;

    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerCam cam;
    
    private PlayerController _pc;
    private PlayerCam _playerCam;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (_pc.wallRunning)
            WallRunningMovement();
    }

    private void CheckForWall()
    {
        _wallRight = Physics.Raycast(transform.position, orientation.right, out _rightWallhit, wallCheckDistance, whatIsWall);
        _wallLeft = Physics.Raycast(transform.position, -orientation.right, out _leftWallhit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        _upwardsRunning = Input.GetKey(upwardsRunKey);
        _downwardsRunning = Input.GetKey(downwardsRunKey);

        if((_wallLeft || _wallRight) && _verticalInput > 0 && AboveGround() && !_exitingWall)
        {
            if (!_pc.wallRunning)
                StartWallRun();

            if (_wallRunTimer > 0)
                _wallRunTimer -= Time.deltaTime;

            if(_wallRunTimer <= 0 && _pc.wallRunning)
            {
                _exitingWall = true;
                _exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(jumpKey)) WallJump();
        }

        else if (_exitingWall)
        {
            if (_pc.wallRunning)
                StopWallRun();

            if (_exitWallTimer > 0)
                _exitWallTimer -= Time.deltaTime;

            if (_exitWallTimer <= 0)
                _exitingWall = false;
        }

        else
        {
            if (_pc.wallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        _pc.wallRunning = true;

        _wallRunTimer = maxWallRunTime;

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        
        cam.DoFov(_pc.wallRunFov);
        if (_wallLeft) cam.DoTilt(-5f);
        if (_wallRight) cam.DoTilt(5f);
    }

    private void WallRunningMovement()
    {
        _rb.useGravity = useGravity;

        Vector3 wallNormal;
        if (_wallRight)
        {
            wallNormal = _rightWallhit.normal;
        }
        else
        {
            wallNormal = _leftWallhit.normal;
        }

        //cross iki vektor capraz carpimi
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        _rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (_upwardsRunning)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, wallClimbSpeed, _rb.linearVelocity.z);
        if (_downwardsRunning)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -wallClimbSpeed, _rb.linearVelocity.z);

        if (!(_wallLeft && _horizontalInput > 0) && !(_wallRight && _horizontalInput < 0))
            _rb.AddForce(-wallNormal * 100, ForceMode.Force);

        if (useGravity)
            _rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        _pc.wallRunning = false;
        cam.DoFov(_pc.defaultFov);
        cam.DoTilt(0f);
    }

    private void WallJump()
    {
        _exitingWall = true;
        _exitWallTimer = exitWallTime;

        Vector3 wallNormal;
        if (_wallRight)
        {
            wallNormal = _rightWallhit.normal;
        }
        else
        {
            wallNormal = _leftWallhit.normal;
        }
        
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
