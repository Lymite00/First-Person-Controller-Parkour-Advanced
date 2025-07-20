using System;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Components")] 
    [SerializeField] private TMP_Text _playerSpeedText;

    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GameObject.Find("Player").GetComponent<PlayerController>();
    }

    private void FixedUpdate()
    {
        _playerSpeedText.text = "Speed:"+ _playerController.rb.linearVelocity.magnitude.ToString("N1");
    }
}
