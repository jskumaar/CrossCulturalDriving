using UnityEngine;

public class EngineSoundController : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource engineAudioSource; // Assign in Unity Inspector
    public AudioClip engineSoundClip; // Assign in Unity Inspector
    public AudioClip accelerationSoundClip; // Assign in Unity Inspector
    public AudioClip ignitionSoundClip; // Assign in Unity Inspector


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




    private float previousThrottle; // For detecting acceleration/deceleration
    public bool isAccelerating = false;
    public bool isCarStarted = false; // Track if the car is started

    private bool ignitionButtonPressed = false; // Buffer for button press

    private bool ignitionSoundActive = false; // Buffer for ignition sound


    void Start()
    {
        // Configure the engine audio source
        if (engineAudioSource != null && engineSoundClip != null)
        {
            engineAudioSource.clip = engineSoundClip;
            engineAudioSource.loop = true;
            engineAudioSource.volume = 0f; // Start muted
            // engineAudioSource.Play();
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
        if (vehicleController == null || engineAudioSource == null) return;

        // Check if the ignition button is pressed
        if (Input.GetKeyDown(KeyCode.JoystickButton10))
        // if (Input.GetKeyDown(KeyCode.S))  // For debugging
        {
            ignitionButtonPressed = true;
            buttonPressTime = Time.time; // Store time when button is pressed
            Debug.Log("Ignition button pressed!");
        }
        

        // Play ignition sound for when the car is cranked (but not started)
        if (ignitionButtonPressed && !isCarStarted && !ignitionSoundActive){
            PlayIgnitionSound();
            buttonPressTime = Time.time; // Store time when button is pressed
            ignitionSoundActive = true;
        }
        else if(!isCarStarted && Time.time - buttonPressTime > 1f){
            ignitionButtonPressed = false;
            engineAudioSource.volume = 0f; // Stop the engine sound
            ignitionSoundActive = false;
        }

        if ((!isCarStarted && vehicleController.currentSpeed > 0.1f)){
            isCarStarted = true;
        }

        // Engine sound for when the car is running
        if (isCarStarted && (Time.time - buttonPressTime) > 1f)
        {
            AdjustEngineSound();
        }

    }


    private void PlayIgnitionSound()
    {
        Debug.Log("Starting ignition sound.");
        engineAudioSource.clip = ignitionSoundClip;
        engineAudioSource.volume = maxVolume;
        engineAudioSource.loop = false; // Don't loop the ignition sound
        engineAudioSource.Play();
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
            engineAudioSource.loop = false; // Don't loop the ignition sound
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
                engineAudioSource.volume = targetVolume;
                engineAudioSource.loop = true; // Loop the acceleration sound
                engineAudioSource.Play();
                isAccelerating = true;
                Debug.Log("Switched to acceleration sound.");
            }
            else if (!currentlyAccelerating && isAccelerating)
            {
                // Switch back to normal engine sound
                engineAudioSource.clip = engineSoundClip;
                engineAudioSource.volume = targetVolume;
                engineAudioSource.loop = true; // Loop the engine sound
                engineAudioSource.Play();
                isAccelerating = false;
                // Debug.Log("Switched back to normal engine sound.");
            }
            // // Check if the car has started moving
            // else if ((!isCarStarted && vehicleController.currentSpeed > 0.1f))
            // {
            //     isCarStarted = true;
                
            //     // Make sure we're playing the engine sound, not the ignition sound
            //     engineAudioSource.clip = engineSoundClip;
            //     engineAudioSource.volume = minVolume; // Start with a low volume
            //     engineAudioSource.loop = true;        // Make sure it loops
            //     engineAudioSource.Play();             // Start playing
            // }
            // check if the car has stopped moving (play idling sound)
            else if (vehicleController.currentSpeed < 0.1f && isCarStarted){
                Debug.Log("Ego car has stopped moving.");
                // isCarStarted = false;
                engineAudioSource.volume = minVolume; // Idling engine sound
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
