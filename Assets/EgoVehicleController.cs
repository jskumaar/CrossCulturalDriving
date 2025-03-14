// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// // This class extends NetworkVehicleController with ego-specific indicator functionality
// public class EgoVehicleController : NetworkVehicleController
// {
//     // Reference to this controller that can be accessed statically
//     public static EgoVehicleController Instance { get; private set; }
    
//     [Header("Indicator Settings")]
//     [SerializeField] private GameObject leftIndicatorObject;
//     [SerializeField] private GameObject rightIndicatorObject;
//     [SerializeField] private float blinkRate = 0.5f; // Blinks per second
//     [SerializeField] private AudioClip indicatorSound;
//     [SerializeField] private float indicatorVolume = 0.5f;
    
//     // Private fields for indicator state tracking
//     private bool _indicatorLeftActive = false;
//     private bool _indicatorRightActive = false;
//     private AudioSource _audioSource;
//     private float _lastBlinkTime;
//     private bool _blinkState = false;
    
//     // Standard Unity lifecycle method
//     // the Awake method to handle indicator blinking (no Awake in NetworkVehicleController; so override not necessary)
//     protected void Awake()
//     {
//         Instance = this;
        
//         // Set up audio source for indicator sounds if needed
//         if (indicatorSound != null && _audioSource == null)
//         {
//             _audioSource = gameObject.AddComponent<AudioSource>();
//             _audioSource.clip = indicatorSound;
//             _audioSource.loop = false;
//             _audioSource.volume = indicatorVolume;
//             _audioSource.playOnAwake = false;
//         }
//     }
    
//     // Find indicator objects if not set in inspector
//     protected virtual void Start()
//     {
//         // Try to find indicators by name if not assigned
//         if (leftIndicatorObject == null)
//         {
//             Transform leftInd = transform.Find("LeftIndicator");
//             if (leftInd != null) leftIndicatorObject = leftInd.gameObject;
//         }
        
//         if (rightIndicatorObject == null)
//         {
//             Transform rightInd = transform.Find("RightIndicator");
//             if (rightInd != null) rightIndicatorObject = rightInd.gameObject;
//         }
        
//         // Initialize indicator objects as inactive
//         if (leftIndicatorObject != null) leftIndicatorObject.SetActive(false);
//         if (rightIndicatorObject != null) rightIndicatorObject.SetActive(false);
//     }
    
//     // Public accessors that ensure only one indicator can be active at a time
//     public bool IndicatorLeftActive
//     {
//         get { return _indicatorLeftActive; }
//         set 
//         { 
//             _indicatorLeftActive = value; 
//             if (value) _indicatorRightActive = false;
//             UpdateIndicatorVisuals();
//         }
//     }
    
//     public bool IndicatorRightActive
//     {
//         get { return _indicatorRightActive; }
//         set 
//         { 
//             _indicatorRightActive = value; 
//             if (value) _indicatorLeftActive = false;
//             UpdateIndicatorVisuals();
//         }
//     }
    
//     // the Update method to handle indicator blinking (no update in NetworkVehicleController; so override not necessary)
//     protected void Update()
//     {
        
//         // Handle indicator blinking
//         if (_indicatorLeftActive || _indicatorRightActive)
//         {
//             // Update blink state based on time
//             if (Time.time - _lastBlinkTime > (1f / blinkRate))
//             {
//                 _blinkState = !_blinkState;
//                 _lastBlinkTime = Time.time;
                
//                 // Play sound on blink state change if configured
//                 if (_blinkState && _audioSource != null && indicatorSound != null)
//                 {
//                     _audioSource.Play();
//                 }
                
//                 UpdateIndicatorVisuals();
//             }
//         }
//     }
    
//     // Implement indicator visual behavior
//     private void UpdateIndicatorVisuals()
//     {
//         if (leftIndicatorObject != null)
//         {
//             leftIndicatorObject.SetActive(_indicatorLeftActive && _blinkState);
//         }
        
//         if (rightIndicatorObject != null)
//         {
//             rightIndicatorObject.SetActive(_indicatorRightActive && _blinkState);
//         }
//     }
    
//     // Method to turn off all indicators
//     public void TurnOffIndicators()
//     {
//         _indicatorLeftActive = false;
//         _indicatorRightActive = false;
//         UpdateIndicatorVisuals();
//     }
// }