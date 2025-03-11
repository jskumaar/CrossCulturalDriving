using UnityEngine;

public class EngineSoundController : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource engineAudioSource; // Assign in Unity Inspector

    public AudioSource indicatorAudioSource; // Assign in Unity Inspector or will be created at runtime

    public AudioSource tireAudioSource; // Assign in Unity Inspector or will be created at runtime

    public AudioClip engineSoundClip; // Assign in Unity Inspector
    public AudioClip accelerationSoundClip; // Assign in Unity Inspector

    public AudioClip ignitionSoundClip; // Assign in Unity Inspector

    public AudioClip indicatorSoundClip; // Assign in Unity Inspector - the "tick" sound for indicators

    public AudioClip tireScreechClip; // Assign in Unity Inspector - the sound for tire screeching



    [Header("Vehicle Reference")]
    public SC_AVFollowSplineEgo vehicleController; // Assign the script in Unity
    public NetworkVehicleController networkVehicleController; // Assign the Network Vehicle Controller


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


    private float buttonPressTime = 0f; // Time tracking

    [Header("Indicator Sound Settings")]
    public float indicatorVolume = 0.9f;
    private bool leftIndicatorActive = false;
    private bool rightIndicatorActive = false;
    private bool indicatorSoundIsPlaying = false;

    // Add this to completely prevent indicator playback until we're ready
    private bool allowIndicatorSounds = false;
    private float startupDelay = 0.5f;

    [Header("Tire Screeching Settings")]
    public float minTurnAngleForScreech = 20f; // Minimum steering angle to trigger screeching
    public float minSpeedForScreech = 5f; // Minimum speed needed for screeching
    public float screechVolume = 0.8f; // Volume for tire screeching sound
    private bool tireScreechActive = false;

    private float previousThrottle; // For detecting acceleration/deceleration
    public bool isAccelerating = false;
    public bool isCarStarted = false; // Track if the car is started

    private bool ignitionButtonPressed = false; // Buffer for button press

    private bool ignitionSoundActive = false; // Buffer for ignition sound

    void Awake()
    {
        // CRITICAL: Force stop any playing indicator sounds immediately
        if (indicatorAudioSource != null)
        {
            indicatorAudioSource.Stop();
            indicatorAudioSource.enabled = false; // Disable the component entirely
        }

        if (tireAudioSource != null)
        {
            tireAudioSource.Stop();
            tireAudioSource.enabled = false; // Disable the component entirely
        }
    }

    void Start()
    {
        if (engineAudioSource != null && engineSoundClip != null)
        {
            engineAudioSource.clip = engineSoundClip;
            engineAudioSource.loop = true;
            engineAudioSource.volume = 0f; // Start muted
            // engineAudioSource.Play();
        }

        // Configure the indicator audio source
        if (indicatorAudioSource != null)
        {
            // IMPORTANT: Stop any audio that might be playing initially
            indicatorAudioSource.enabled = false; // Keep disabled until needed
            indicatorAudioSource.Stop();
            
            indicatorAudioSource.playOnAwake = false;
            indicatorAudioSource.loop = true; // Important: Loop the indicator sound since it has intervals
            indicatorAudioSource.volume = 0f;
            indicatorAudioSource.spatialBlend = 0.5f; // Mix of 2D and 3D sound for better audibility
            indicatorAudioSource.minDistance = 2.0f;
            indicatorAudioSource.maxDistance = 15.0f;
            indicatorAudioSource.priority = 128; // Medium priority
            
            // Set the indicator sound clip
            if (indicatorSoundClip != null)
            {
                indicatorAudioSource.clip = indicatorSoundClip;
                indicatorAudioSource.volume = 0f;
                Debug.Log($"Indicator sound clip configured: {indicatorSoundClip.name}, Length: {indicatorSoundClip.length}s");
            }
            else
            {
                Debug.LogWarning("Indicator sound clip is not assigned!");
            }

        }

        // Configure the tire screech audio source
        if (tireAudioSource != null)
        {
            tireAudioSource.playOnAwake = false;
            tireAudioSource.loop = false;  // Just play the clip once
            tireAudioSource.volume = 0f;
            tireAudioSource.spatialBlend = 0.7f; // More 3D than indicators for realism
            tireAudioSource.minDistance = 1.0f;
            tireAudioSource.maxDistance = 20.0f;
            tireAudioSource.priority = 128;
            
            // Set the tire screech sound clip
            if (tireScreechClip != null)
            {
                tireAudioSource.clip = tireScreechClip;
                Debug.Log($"Tire screech sound clip configured: {tireScreechClip.name}");
            }
        }
        

        if (networkVehicleController == null)
        {
            networkVehicleController = GetComponent<NetworkVehicleController>();
            if (networkVehicleController == null)
            {
                Debug.LogWarning("No NetworkVehicleController found on this GameObject");
            }
        }



        // Delay enabling indicators to ensure they don't play at start
        Invoke("EnableIndicatorSounds", startupDelay);
    }

    // This will be called after a delay to enable indicator functionality
    private void EnableIndicatorSounds()
    {
        if (indicatorAudioSource != null)
        {
            indicatorAudioSource.enabled = true;
            indicatorAudioSource.Stop(); // Stop again just to be sure
            indicatorAudioSource.volume = 0f;
        }
        allowIndicatorSounds = true;
        Debug.Log("Indicator sounds now enabled and ready");
    }

    private void Update()
    {
        if (vehicleController == null || engineAudioSource == null) return;

        // Check if the ignition button is pressed
        if (Input.GetKeyDown(KeyCode.JoystickButton10))
        {
            ignitionButtonPressed = true;
            buttonPressTime = Time.time; // Store time when button is pressed
            Debug.Log("Ignition button pressed!");
        }
        

        // Play ignition sound for when the car is cranked (but not started)
        if (ignitionButtonPressed && !isCarStarted && !ignitionSoundActive){
            StartIgnitionSound();
            buttonPressTime = Time.time; // Store time when button is pressed
            ignitionSoundActive = true;
        }
        else if(!isCarStarted && Time.time - buttonPressTime > 1f){
            ignitionButtonPressed = false;
            engineAudioSource.volume = 0f; // Stop the engine sound
            ignitionSoundActive = false;
        }

        // Check if the car has started moving
        if ((!isCarStarted && vehicleController.currentSpeed > 0.1f))
        {
            isCarStarted = true;
        }

        // check if the car has stopped moving
        if (vehicleController.currentSpeed < 0.1f && isCarStarted && Time.time - buttonPressTime > 1f){
            Debug.Log("Ego car has stopped moving.");
            isCarStarted = false;
            engineAudioSource.volume = 0f; // Stop the engine sound
        }

        // Engine sound for when the car is running
        if (isCarStarted && (Time.time - buttonPressTime) > 1f)
        {
            AdjustEngineSound();
        }

        // Handle indicator sounds - only after enabled
        if (allowIndicatorSounds)
        {
            UpdateIndicatorSounds();
        }
        else if (indicatorAudioSource != null && indicatorAudioSource.isPlaying)
        {
            // Forcefully stop if somehow it's playing when not allowed
            indicatorAudioSource.Stop();
            Debug.Log("Forcefully stopped indicator sound");
        }
    }

    private void UpdateIndicatorSounds()
    {
        // Skip if no NetworkVehicleController or no sound clip or no audio source
        Debug.Log($"NetworkVehicleController: {networkVehicleController}, IndicatorSoundClip: {indicatorSoundClip}, IndicatorAudioSource: {indicatorAudioSource}");
        if (networkVehicleController == null || 
            indicatorSoundClip == null || 
            indicatorAudioSource == null ||
            !indicatorAudioSource.enabled) 
        {
            return;
        }
        
        // Get current indicator state from NetworkVehicleController
        bool leftOn, rightOn;
        networkVehicleController.GetIndicatorState(out leftOn, out rightOn);
        
        bool indicatorActive = leftOn || rightOn;
        
        // For debugging
        Debug.Log($"Indicator state: Left: {leftOn}, Right: {rightOn}, Active: {indicatorActive}, Sound Playing: {indicatorSoundIsPlaying}");

        // Store current state for next update
        leftIndicatorActive = leftOn;
        rightIndicatorActive = rightOn;
        
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
    }
    
    private void StartIndicatorSound()
    {
        if (indicatorAudioSource != null && indicatorSoundClip != null)
        {
            // Make sure the right clip is set
            indicatorAudioSource.clip = indicatorSoundClip;
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
            indicatorAudioSource.Stop();
            indicatorSoundIsPlaying = false;
            
            Debug.Log("Stopped playing indicator sound");
        }
    }

    private void StartIgnitionSound()
    {
        Debug.Log("Starting ignition sound.");
        engineAudioSource.clip = ignitionSoundClip;
        engineAudioSource.volume = maxVolume;
        engineAudioSource.Play();
    }

    public void PlayTireScreech()
    {
        if (tireAudioSource == null || tireScreechClip == null) return;
        
        // Start playing if not already
        if (!tireScreechActive)
        {
            tireAudioSource.clip = tireScreechClip;
            tireAudioSource.volume = 1f;
            tireAudioSource.Play();
            tireScreechActive = true;
            
            Debug.Log($"Started playing tire screech sound");
        }
    }

    // public void StopTireScreech()
    // {
    //     tireScreechActive = false;
    //     tireScreechAudioSource.volume = 0f;
    //     tireScreechAudioSource.Stop();

    //     Debug.Log("Stopping tire screech sound");
    // }

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

        // Detect ignition sound


        // Reset buttonPress if more than 1 second has passed
        if (ignitionButtonPressed && Time.time - buttonPressTime > 1f)
        {
            ignitionButtonPressed = false;
            Debug.Log("Button press reset due to timeout.");
        }

        // If button is pressed and scenario is ready, activate it
        if (ignitionButtonPressed)
        {
            // Switch to ignition clip
            engineAudioSource.clip = ignitionSoundClip;
            engineAudioSource.volume = maxVolume;
            engineAudioSource.Play();

            ignitionSoundActive = true;

        }
        else
        {
            
        ////////////////////////////////////////////////////

            ignitionSoundActive = false;

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
            ////////////////////////////////////////////////////

            // Detect acceleration
            bool currentlyAccelerating = false;
            if (((throttle > previousThrottle + 0.13f) || (throttle > 0.8f)) && (speed < 5 || speed > 6.5))
            {
                currentlyAccelerating = true;
            }

            // Debug.Log(throttle + "...." + previousThrottle + "Currently accelerating? " + currentlyAccelerating + ". isAccelerating? " + isAccelerating);
            if (currentlyAccelerating && !isAccelerating)
            {
                // Switch to acceleration clip
                engineAudioSource.clip = accelerationSoundClip;
                engineAudioSource.Play();
                isAccelerating = true;
                // Debug.Log("Switched to acceleration sound.");
            }
            else if (!currentlyAccelerating && isAccelerating)
            {
                // Switch back to normal engine sound
                engineAudioSource.clip = engineSoundClip;
                engineAudioSource.Play();
                isAccelerating = false;
                // Debug.Log("Switched back to normal engine sound.");
            }
            ////////////////////////////////////////////////////



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
}
