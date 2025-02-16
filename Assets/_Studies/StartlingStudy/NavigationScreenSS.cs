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
        Right,
        vague_icon,
        warning,
        car,
        pedestrian,
        blankScreen  // Ensure this is the default until a trigger is hit
    }

    [ReplayVar(false)] public int recordingIconType = (int)IconType.blankScreen;

    public Sprite vagueIconImage, pedestrianImage, warningIconImage, carImage, straightImage, leftImage, rightImage, blankScreen;
    public Image gpsImagePlane;
    public IconType defaultIconType;
    private AudioSource GpsAudioPlayer;
    private IconType previousIconType = IconType.blankScreen;

    // **NEW: Map trigger IDs to corresponding GPS Icons**
    private Dictionary<string, IconType> triggerIconMap = new Dictionary<string, IconType>()
    {
        { "Trigger_Straight", IconType.Straight },
        { "confusion_alert_trigger_1", IconType.pedestrian },
        { "confusion_alert_trigger_2", IconType.Right },
        { "confusion_alert_trigger_3", IconType.vague_icon },
        { "surprise_alert_trigger_1", IconType.car },
        { "surprise_alert_trigger_2", IconType.Right },
        { "surprise_alert_trigger_3", IconType.warning },
        { "confusion_alert_trigger_1_action_end", IconType.blankScreen },
        { "confusion_alert_trigger_2_action_end", IconType.blankScreen },
        { "confusion_alert_trigger_3_action_end", IconType.blankScreen },
        { "surprise_alert_trigger_1_action_end", IconType.blankScreen },
        { "surprise_alert_trigger_2_action_end", IconType.blankScreen },
        { "surprise_alert_trigger_3_action_end", IconType.blankScreen }
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
            case IconType.vague_icon:
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
}
