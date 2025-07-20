using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Action OnSprint;
    public Action OnIdle;
    public Action OnAir;
    public Action OnWalking;
    public Action OnJump;
    public Action OnCrouch;
    public Action OnSlide;
    public Action OnWallRun;
    public Action OnDash;
    public Action OnFreeze;
    public Action OnSwing;
    public Action OnSlam;
    
    [Header("Movement")]
    public float moveSpeed;

    public float walkSpeed;
    public float sprintSpeed;
    
    public float wallRunSpeed;
    public float slideSpeed;
    public float dashSpeed;
    public float swingSpeed;
    public float slammingSpeed;

    public float maxYSpeed;
    
    public float dashSpeedChangeFactor;
    public float grappleSpeedMultiplier;
    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    
    [SerializeField] private float _groundDrag;
    
    [Header("Air")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;

    [Header("Crouching")] 
    [SerializeField] private float _crouchSpeed;
    [SerializeField] private float _crouchYScale;
    [SerializeField] private float _startYScale;

    [Header("Slope Handling")] 
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    
    [Header("Components")]
    [SerializeField] private Transform orientation;
    public Rigidbody rb;
    
    [Header("CameraEffects")]
    [SerializeField] private PlayerCam playerCam;
    public float defaultFov = 85f;
    public float crouchFov = 95f;
    public float walkFov = 95f;
    public float sprintFov = 95f;
    public float slideFov = 95f;
    public float dashFov = 95f;
    public float wallRunFov = 95f;
    public float grappleFov = 95f;
    public float freezeFov = 95f;
    public float swingFov = 95f;
    public float slammingFov = 95f;
    
    private float _horizontalInput;
    private float _verticalInput;
    
    private bool _grounded;
    private bool _readyToJump;
    private bool _exitingSlope;

    public bool sliding;
    public bool dashing;
    public bool crouching;
    public bool wallRunning;
    public bool freeze;
    public bool swinging;
    public bool slamming;

    public bool activeGrapple;
    public bool inAir;
    
    private Vector3 _moveDirection;
    private float speedChangeFactor;
    private bool _keepMomentum;
    
    public MovementState state;
    public MovementState lastState;
    public GrappleState grappleState;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        wallrunning,
        dashing,
        air,
        idle,
        slide,
        freeze,
        swing,
        slam
    }
    public enum  GrappleState
    {
        pull,
        swing
    }
    #region States
    private void SetCrouching()
    {
        state = MovementState.crouching;
        _desiredMoveSpeed = _crouchSpeed;
        playerCam.DoFov(crouchFov);
    }
    private void SetSwing()
    {
        state = MovementState.swing;
        _desiredMoveSpeed = swingSpeed;
        playerCam.DoFov(swingFov);
    }
    private void SetSlam()
    {
        state = MovementState.slam;
        _desiredMoveSpeed = slammingSpeed;
        playerCam.DoFov(slammingFov);
    }
    private void SetSprinting()
    {
        state = MovementState.sprinting;
        _desiredMoveSpeed = sprintSpeed;
        playerCam.DoFov(sprintFov);

    }
    private void SetWalking()
    {
        state = MovementState.walking;
        _desiredMoveSpeed = walkSpeed;
        playerCam.DoFov(walkFov);

    }
    private void SetAir()
    {
        inAir = true;
        state = MovementState.air;
    }
    private void SetIdle()
    {
        state = MovementState.idle;
        playerCam.DoFov(defaultFov);
    } 
    private void SetWallRunning()
    {
        state = MovementState.wallrunning;
        _desiredMoveSpeed = wallRunSpeed;
    }
    private void SetDash()
    {
        state = MovementState.dashing;
        _desiredMoveSpeed = dashSpeed;
    }
    private void SetFreeze()
    {
        state = MovementState.freeze;
        _desiredMoveSpeed = 0;
        rb.linearVelocity = Vector3.zero;
        playerCam.DoFov(freezeFov);
    }
    private void SetSlide()
    {
        playerCam.DoFov(slideFov);

        state = MovementState.slide;
        if (OnSlope() && rb.linearVelocity.y < 0.1f)
        {
            _desiredMoveSpeed = slideSpeed;
        }
        else
        {
            _desiredMoveSpeed = sprintSpeed;
        }
    }
    private void StateHandler()
    {
        if (slamming)
        {
            OnSlam?.Invoke();
        }
        else if (swinging)
        {
            OnSwing?.Invoke();
        }
        else if (freeze)
        {
            OnFreeze?.Invoke();
        }
        else if (dashing)
        {
            OnDash?.Invoke();
            speedChangeFactor = dashSpeedChangeFactor;
        }
        else if (wallRunning)
        {
            OnWallRun?.Invoke();
        }
        else if (sliding)
        {
            OnSlide?.Invoke();
        }
        else if(_grounded && Input.GetKey(_sprintKey))
        {
            OnSprint?.Invoke();
        }
        else if(_grounded && !crouching)
        {
            OnWalking?.Invoke();
        }
        else if(!_grounded && !crouching)
        {
            OnAir?.Invoke();
            if (_desiredMoveSpeed < sprintSpeed)
                _desiredMoveSpeed = walkSpeed;
            else
                _desiredMoveSpeed = sprintSpeed;
        }
        else if(_grounded && crouching)
        {
            OnCrouch?.Invoke();
        }

        bool desiredMoveSpeedHasChanged = _desiredMoveSpeed != _lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) _keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if (_keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = _desiredMoveSpeed;
            }
        }

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
        lastState = state;
    }
    #endregion
    #region Unity Functions
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        SetPlayerSettings();
    }
    private void OnEnable()
    {
        OnJump += Jump;
        OnCrouch += SetCrouching;
        OnSprint += SetSprinting;
        OnWalking += SetWalking;
        OnAir += SetAir;
        OnIdle += SetIdle;
        OnSlide += SetSlide;
        OnWallRun += SetWallRunning;
        OnDash += SetDash;
        OnFreeze += SetFreeze;
        OnSwing += SetSwing;
        OnSlam += SetSlam;
    }
    private void OnDisable()
    {
        OnJump -= Jump;
        OnCrouch -= SetCrouching;
        OnSprint -= SetSprinting;
        OnWalking -= SetWalking;
        OnAir -= SetAir;
        OnIdle -= SetIdle;
        OnSlide -= SetSlide;
        OnWallRun -= SetWallRunning;
        OnDash -= SetDash;
        OnFreeze -= SetFreeze;
        OnSwing -= SetSwing;
        OnSlam -= SetSlam;

    }
    private void Update()
    {
        _grounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.3f, whatIsGround);
        
        if (state == MovementState.walking || state == MovementState.sprinting ||  state == MovementState.crouching && !activeGrapple)
            rb.linearDamping = _groundDrag;
        else
            rb.linearDamping = 0;
        
        MyInput();
        HandleGrappleMode();
        SpeedControl();
        StateHandler();
        //Debug.Log(state);
    } 
    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            inAir = false;
        }
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
            GetComponent<Grappling>().StopGrapple();
        }
    }

    #endregion
    #region CustomFunctions

    private void HandleGrappleMode()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            grappleState = GrappleState.pull;
            Debug.Log(grappleState);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            grappleState = GrappleState.swing;
            Debug.Log(grappleState);
        }
    }
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

            time += Time.deltaTime * boostFactor;

            yield return null;
        }

        moveSpeed = _desiredMoveSpeed;
        speedChangeFactor = 1f;
        _keepMomentum = false;
    }
    private void SetPlayerSettings()
    {
        rb.freezeRotation = true;
        _readyToJump = true;
        _startYScale = transform.localScale.y;
    }

    private void MyInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(_jumpKey) && _readyToJump && _grounded)
        {
            _readyToJump = false;

            OnJump?.Invoke();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f,ForceMode.Impulse);
            crouching = true;
        }

        if (Input.GetKeyUp(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
            crouching = false;
        }

    }
    private void MovePlayer()
    {
        if (activeGrapple) return;
        if (swinging) return;
        
        if (state== MovementState.dashing)
        {
            return;
        }
        _moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

        if(_grounded)
            rb.AddForce(_moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if(!_grounded)
            rb.AddForce(_moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        
        if (OnSlope() && !_exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(_moveDirection) * moveSpeed * 20f, ForceMode.Force);
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 50f, ForceMode.Force);
            }
        }

        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;
        
        if (OnSlope() && !_exitingSlope)
        {
            if (rb.linearVelocity.magnitude> moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if(flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            } 
        }

        if (maxYSpeed!= 0 && rb.linearVelocity.y > maxYSpeed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxYSpeed, rb.linearVelocity.z);
        }
    }

    private void Jump()
    {
        _exitingSlope = true;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        _readyToJump = true;
        _exitingSlope = false;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position,Vector3.down,out _slopeHit,_playerHeight*.5f+.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }
        return false;
    }
    
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized;
    }
    public void ResetRestrictions()
    {
        playerCam.DoFov(85f);

        activeGrapple = false;
    }

    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity),0.05f);
        Invoke(nameof(ResetRestrictions),0.5f);
    }
    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.linearVelocity = velocityToSet * grappleSpeedMultiplier;
        
        playerCam.DoFov(grappleFov);
    }
    //Önemli
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        //Başlangıç noktası ile bitiş noktası arasındaki Y eksenindeki mesafeyi hesaplar.
        //Pozitif veya negatif olabilir (yukarı ya da aşağı bir fark).
        
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);
        //XZ düzlemindeki (yatay düzlemdeki) mesafeyi hesaplar.
        //Y ekseni sabit tutulur (0 olarak ayarlanır) çünkü bu hesaplama sadece yatay hareketle ilgilidir.
        
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        //Y eksenindeki hız nesnenin en yüksek noktaya (trajectoryHeight) ulaşabilmesi için gereken başlangıç hızını hesaplar.
        // Formul sudur Vy = Kok -2.g.h
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + Mathf.Sqrt(2 * (displacementY - trajectoryHeight)/ gravity));
        //XZ düzlemindeki hız, yatay hareketi sağlar ve hem mesafe hem de uçuş süresi dikkate alınarak hesaplanır.
        return velocityXZ + velocityY;

    }
    #endregion
}