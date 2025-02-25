using UnityEngine;

public class EngineSoundController : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource engineAudioSource; // Assign in Unity Inspector
    public AudioClip engineSoundClip; // Assign in Unity Inspector
    public AudioClip accelerationSoundClip; // Assign in Unity Inspector

    [Header("Vehicle Reference")]
    public SC_AVFollowSplineEgo vehicleController; // Assign the script in Unity

    [Header("Engine Sound Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 2.5f;
    public float minVolume = 0.3f;
    public float maxVolume = 1.0f;
    private float maxSpeed = 20f; // Maximum speed of the vehicle for normalization

    public float targetPitch;
    public float targetVolume;

    [Header("Sporty Mode Settings")]
    public float sportyPitchMultiplier = 1.5f; // Increase pitch in sporty mode
    public float sportyVolumeMultiplier = 1.5f; // Increase volume in sporty mode

    private float previousThrottle; // For detecting acceleration/deceleration
    public bool isAccelerating = false;
    public bool isCarStarted = false; // Track if the car is started


    void Start()
    {
        if (engineAudioSource != null && engineSoundClip != null)
        {
            engineAudioSource.clip = engineSoundClip;
            engineAudioSource.loop = true;
            engineAudioSource.volume = 0f; // Start muted
            // engineAudioSource.Play();
        }
    }

    private void Update()
    {
        if (vehicleController == null || engineAudioSource == null) return;

        // Check if the car has started moving
        if (!isCarStarted && vehicleController.currentSpeed > 0.1f)
        {
            isCarStarted = true;
            StartEngineSound();
        }

        if (vehicleController.currentSpeed < 0.1f && isCarStarted){
            isCarStarted = false;
            engineAudioSource.volume = 0f; // Stop the engine sound
        }

        if (isCarStarted)
        {
            AdjustEngineSound();
        }
    }

    private void StartEngineSound()
    {
        engineAudioSource.clip = engineSoundClip;
        engineAudioSource.volume = minVolume;
        engineAudioSource.Play();
    }

    private void AdjustEngineSound()
    {
        // Get values from vehicle script
        float speed = vehicleController.currentSpeed;
        float throttle = vehicleController.throttleControl; // Between -1 and 1
        string driveMode = vehicleController.driveMode; // "normal", "sporty", "eco", etc.

        // Normalize speed and throttle values
        float speedFactor = Mathf.Clamp01(speed / maxSpeed);
        float throttleFactor = Mathf.Abs(throttle); // Ignore sign for intensity

        // Adjust pitch based on speed
        targetPitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);

        // Adjust volume based on throttle and speed
        targetVolume = Mathf.Lerp(minVolume, maxVolume, speedFactor * throttleFactor);

        // Exaggerate pitch and volume if in sporty mode
        if (driveMode == "sporty")
        {
            targetPitch *= sportyPitchMultiplier;
            targetVolume *= sportyVolumeMultiplier;
        }

        // Detect acceleration
        bool currentlyAccelerating = throttle > previousThrottle + 0.2f;

        if (currentlyAccelerating && !isAccelerating)
        {
            // Switch to acceleration clip
            engineAudioSource.clip = accelerationSoundClip;
            engineAudioSource.Play();
            isAccelerating = true;
        }
        else if (!currentlyAccelerating && isAccelerating)
        {
            // Switch back to normal engine sound
            engineAudioSource.clip = engineSoundClip;
            engineAudioSource.Play();
            isAccelerating = false;
        }

        // Apply pitch and volume
        engineAudioSource.pitch = targetPitch;
        engineAudioSource.volume = Mathf.Clamp(targetVolume, minVolume, maxVolume * sportyVolumeMultiplier);

        // Detect sudden deceleration (engine braking)
        if (throttle < previousThrottle - 0.2f)
        {
            engineAudioSource.pitch *= 0.7f; // Slightly drop pitch for engine braking effect
        }

        previousThrottle = throttle; // Store last throttle value
    }
}
