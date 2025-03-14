using UnityEngine;

public class TireSoundController : MonoBehaviour
{
    [Header("Audio ")]
    public AudioSource tireAudioSource; // Assign in Unity Inspector or will be created at runtime
    public AudioClip tireScreechClip; // Assign in Unity Inspector - the sound for tire screeching

    public float tireScreechVolume = 0.7f; // Volume for tire screech sound
    
    void Start()
    {
        // Configure the tire screech audio source
        if (tireAudioSource != null && tireScreechClip != null)
        {
            tireAudioSource.clip = tireScreechClip;
            tireAudioSource.loop = false; // Just play the clip once
            tireAudioSource.volume = 0f; // Start muted
        }
    }


    private void Update()
    {
        if (tireAudioSource == null) return;

    }

    public void PlayTireScreech()
    {
        tireAudioSource.clip = tireScreechClip;
        tireAudioSource.volume = tireScreechVolume;
        tireAudioSource.loop = false; // Just play the clip once
        tireAudioSource.Play();
        
        Debug.Log($"Started playing tire screech sound");
    }

}
