using System;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform _orientation;
    [SerializeField] private Transform _playerObject;
    private Rigidbody _rigidbody;

    private PlayerController _playerController;
    public PlayerCam cam;
    
    [Header("Sliding")]
    [SerializeField] private float _maxSliderTime;
    [SerializeField] private float _slideForce;
    [SerializeField] private float _slideTimer;
    
    [SerializeField] private float _slideYScale;
    [SerializeField] private float _startYScale;

    [Header("Inputs")]
    [SerializeField] private KeyCode _slideKey = KeyCode.LeftControl;

    private float _horizontalInput;
    private float _verticalInput;

    private bool _isSliding;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _startYScale = _playerObject.localScale.y;
    }

    private void Update()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(_slideKey) && (_horizontalInput!= 0 ||_verticalInput!= 0))
        {
            StartSlide();
        }
        if (Input.GetKeyUp(_slideKey) && _playerController.sliding)
        {
            StopSlide();
        }
    }

    private void FixedUpdate()
    {
        if (_playerController.sliding)
        {
            SlidingMovement();
        }
    }

    private void StartSlide()
    {
        cam.DoFov(_playerController.slideFov);
        _playerController.sliding = true;
        _playerObject.localScale = new Vector3(_playerObject.localScale.x, _slideYScale, _playerObject.localScale.z);
        _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        _slideTimer = _maxSliderTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;
        if(!_playerController.OnSlope() || _rigidbody.linearVelocity.y > -0.1f)
        {
            _rigidbody.AddForce(inputDirection.normalized * _slideForce, ForceMode.Force);
            _slideTimer -= Time.deltaTime;
        }
        else
        {
            _rigidbody.AddForce(_playerController.GetSlopeMoveDirection(inputDirection) * _slideForce, ForceMode.Force);
        }
        if (_slideTimer <= 0)
            StopSlide();
    }
    private void StopSlide()
    {
        cam.DoFov(_playerController.defaultFov);

        _playerController.sliding = false;
        _playerObject.localScale = new Vector3(_playerObject.localScale.x, _startYScale, _playerObject.localScale.z);

    }
}
