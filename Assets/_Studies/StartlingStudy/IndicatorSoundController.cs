using UnityEngine;

public class IndicatorSoundController : MonoBehaviour
{
    [Header("Audio Source")]

    public AudioSource indicatorAudioSource; // Assign in Unity Inspector or will be created at runtime

    public AudioClip indicatorSoundClip; // Assign in Unity Inspector - the "tick" sound for indicators



    [Header("Vehicle Reference")]
    public NetworkVehicleController networkVehicleController; // Assign the Network Vehicle Controller


    private float buttonPressTime = 0f; // Time tracking

    [Header("Indicator Sound Settings")]
    public float indicatorVolume = 0.7f;
    private bool leftIndicatorActive = false;
    private bool rightIndicatorActive = false;
    private bool indicatorSoundIsPlaying = false;





    void Start()
    {
        // Configure the indicator audio source
        if (indicatorAudioSource != null && indicatorSoundClip != null)
        {
            indicatorAudioSource.clip = indicatorSoundClip;
            indicatorAudioSource.loop = true; // Important: Loop the indicator sound since it has intervals
            indicatorAudioSource.volume = 0f;
        }

    
        if (networkVehicleController == null)
        {
            networkVehicleController = GetComponent<NetworkVehicleController>();
            if (networkVehicleController == null)
            {
                Debug.LogWarning("No NetworkVehicleController found on this GameObject");
            }
        }


    }


    private void Update()
    {
        if (networkVehicleController == null || indicatorAudioSource == null) return;

        // Handle indicator sounds
        UpdateIndicatorSounds();

    }

    private void UpdateIndicatorSounds()
    {        
        // Get current indicator state
        bool leftOn, rightOn;
        networkVehicleController.GetIndicatorState(out leftOn, out rightOn);
        
        // Indicators are active if either left or right is on
        bool indicatorActive = leftOn || rightOn;
        
        // Debug
        if (leftIndicatorActive != leftOn || rightIndicatorActive != rightOn)
        {
            Debug.Log($"Indicator state changed: Left: {leftOn}, Right: {rightOn}");
        }

        // Check if indicator status changed
        if (indicatorActive && !indicatorSoundIsPlaying)
        {
            // Indicators turned on - start sound
            StartIndicatorSound();
        }
        else if (!indicatorActive && indicatorSoundIsPlaying)
        {
            // Indicators turned off - stop sound
            StopIndicatorSound();
        }
        
        // Store current state for next update
        leftIndicatorActive = leftOn;
        rightIndicatorActive = rightOn;
    }

    private void StartIndicatorSound()
    {
        if (indicatorAudioSource != null && indicatorSoundClip != null)
        {
            // Set up the sound
            indicatorAudioSource.clip = indicatorSoundClip;
            indicatorAudioSource.loop = true; // Important! Make it loop
            indicatorAudioSource.volume = indicatorVolume;
            indicatorAudioSource.Play();
            indicatorSoundIsPlaying = true;
            
            Debug.Log("Started playing indicator sound");
        }
    }
    
    private void StopIndicatorSound()
    {
        if (indicatorAudioSource != null)
        {
            indicatorAudioSource.volume = 0f; // Mute the sound
            indicatorAudioSource.Play();
            indicatorSoundIsPlaying = false;
            
            Debug.Log("Stopped playing indicator sound");
        }
    }

}