using UltimateReplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NavigationScreenSS : ReplayBehaviour
{
    public enum IconType
    {
        Straight,
        Left,
        LeftClear,
        Right,
        RightClear,
        vagueIcon,
        vagueIconClear,
        warning,
        warningClear,
        stop,
        car,
        carClear,
        pedestrian,
        pedestrianClear,
        gradient,
        gradientClear,
        dropoff,
        dropoffClear,
        microphone,
        blankScreen  // Ensure this is the default until a trigger is hit
    }

    [ReplayVar(false)] public int recordingIconType = (int)IconType.blankScreen;

    public Sprite vagueIconImage, vagueIconClearImage, pedestrianImage, pedestrianClearImage, warningIconImage, warningClearImage, stopIconImage, carImage, carClearImage, straightImage, leftImage, leftClearImage, rightImage, rightClearImage, gradientImage, gradientClearImage, microphoneImage, dropoffImage, dropoffClearImage, blankScreen;
    public Image gpsImagePlane;
    public IconType defaultIconType;
    private AudioSource GpsAudioPlayer;
    public IconType previousIconType = IconType.blankScreen;

    // **NEW: Map trigger IDs to corresponding GPS Icons**
    public Dictionary<string, IconType> triggerIconMap = new Dictionary<string, IconType>()
    {
        { "Trigger_Straight", IconType.Straight },
        { "Listening", IconType.microphone},
        { "Standby", IconType.blankScreen},
        { "TrialDropoff", IconType.dropoff},
        { "TrialDropoffClear", IconType.dropoffClear},
        { "confusion_alert_trigger_1", IconType.pedestrian },
        { "confusion_alert_trigger_2", IconType.Left },
        { "confusion_alert_trigger_3", IconType.warning },
        { "confusion_alert_trigger_4", IconType.car },
        { "surprise_alert_trigger_1", IconType.pedestrian },
        { "surprise_alert_trigger_2", IconType.car },
        { "surprise_alert_trigger_3", IconType.warning },
        { "surprise_alert_trigger_4", IconType.Left },
        { "frustration_alert_trigger_1", IconType.Left },
        { "frustration_alert_trigger_2", IconType.car },
        { "frustration_alert_trigger_3", IconType.gradient },
        { "frustration_alert_trigger_4", IconType.pedestrian },
        { "confusion_alert_trigger_1_action_end", IconType.pedestrianClear },
        { "confusion_alert_trigger_2_action_end", IconType.LeftClear },
        { "confusion_alert_trigger_3_action_end", IconType.warningClear },
        { "confusion_alert_trigger_4_action_end", IconType.carClear },
        { "surprise_alert_trigger_1_action_end", IconType.pedestrianClear },
        { "surprise_alert_trigger_2_action_end", IconType.carClear },
        { "surprise_alert_trigger_3_action_end", IconType.warningClear },
        { "surprise_alert_trigger_4_action_end", IconType.LeftClear },
        { "frustration_alert_trigger_1_action_end", IconType.LeftClear },
        { "frustration_alert_trigger_2_action_end", IconType.carClear },
        { "frustration_alert_trigger_3_action_end", IconType.gradientClear },
        { "frustration_alert_trigger_4_action_end", IconType.pedestrianClear }
    };

    // Event subscription for triggers
    public delegate void TriggerEventHandler(string triggerID);
    public static event TriggerEventHandler OnTriggerDetectedEvent;

    private void Awake()
    {
        OnTriggerDetectedEvent += OnTriggerDetected;
    }

    private void OnDestroy()
    {
        OnTriggerDetectedEvent -= OnTriggerDetected;
    }

    private void Start()
    {
        if (gpsImagePlane != null)
        {
            gpsImagePlane.sprite = spriteForIcon(defaultIconType);
        }
        else
        {
            Debug.LogError("gpsImagePlane is not assigned in the Inspector!");
        }

        GpsAudioPlayer = GetComponent<AudioSource>();
        if (GpsAudioPlayer == null)
        {
            Debug.LogWarning("GpsAudioPlayer not found! Ensure an AudioSource is attached.");
        }

        if (ConnectionAndSpawning.Singleton != null && ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            Image image = GetComponentInChildren<Image>();

            if (canvas != null) canvas.enabled = true;
            if (image != null) image.enabled = true;
        }
    }

    private Sprite spriteForIcon(IconType iconType)
    {
        switch (iconType)
        {
            case IconType.vagueIcon:
                return vagueIconImage;
            case IconType.warning:
                return warningIconImage;
            case IconType.Straight:
                return straightImage;
            case IconType.Left:
                return leftImage;
            case IconType.Right:
                return rightImage;
            case IconType.pedestrian:
                return pedestrianImage;
            case IconType.car:
                return carImage;
            case IconType.stop:
                return stopIconImage;
            case IconType.gradient:
                return gradientImage;
            case IconType.microphone:
                return microphoneImage;
            case IconType.vagueIconClear:
                return vagueIconClearImage;
            case IconType.warningClear:
                return warningClearImage;
            case IconType.LeftClear:
                return leftClearImage;
            case IconType.RightClear:
                return rightClearImage;
            case IconType.pedestrianClear:
                return pedestrianClearImage;
            case IconType.carClear:
                return carClearImage;
            case IconType.gradientClear:
                return gradientClearImage;
            case IconType.dropoff:
                return dropoffImage;
            case IconType.dropoffClear:
                return dropoffClearImage;
            case IconType.blankScreen:
                return blankScreen;
            default:
                return blankScreen;
        }
    }

    public void SetIcon(IconType newIconType)
    {
        if (previousIconType != newIconType)
        {
            previousIconType = newIconType;
            gpsImagePlane.sprite = spriteForIcon(newIconType);
            if (GpsAudioPlayer != null  && newIconType != IconType.blankScreen) GpsAudioPlayer.Play();

            if (ConnectionAndSpawning.Singleton != null && ConnectionAndSpawning.Singleton.ServerisRunning)
            {
                recordingIconType = (int)newIconType;
            }
        }
    }

    public void SetIconByString(string iconName)
    {
        // First check if it's a trigger ID
        if (triggerIconMap.TryGetValue(iconName, out IconType triggerIcon))
        {
            SetIcon(triggerIcon);
            Debug.Log($"Set icon to {triggerIcon} using trigger ID: {iconName}");
            return;
        }
        
        // Handle case where icon name isn't found
        Debug.LogWarning($"Icon name '{iconName}' not recognized. No icon change.");
    }

    // Map trigger IDs to Icons & Update the GPS**
    private void OnTriggerDetected(string triggerID)
    {
        if (triggerIconMap.TryGetValue(triggerID, out IconType newIconType))
        {
            SetIcon(newIconType);
            Debug.Log($"Updated GPS icon to {newIconType} due to trigger: {triggerID}");
        }
        else
        {
            Debug.LogWarning($"Trigger ID '{triggerID}' not found in map. No icon change.");
        }
    }

    // Called by StartleTrigger when a trigger is entered or exited**
    public static void TriggerIconChange(string triggerID)
    {
        OnTriggerDetectedEvent?.Invoke(triggerID);
    }


    public void ClearAlertButtonPress()
    {
        // Set the icon to blank screen
        SetIcon(IconType.blankScreen);
        Debug.Log("Navigation screen set to blank due to steering wheel button press");
    }

}
