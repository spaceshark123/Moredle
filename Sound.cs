using UnityEngine.Audio;
using UnityEngine;

/// <summary>
/// A container for all of the specifications required for a single audio file and the ability to play it.
/// <para>All Sounds are managed by the <u><strong>AudioManager</strong></u> class.</para>
/// </summary>
[System.Serializable]
public class Sound
{
    [Tooltip("Name by which sounds are identified. Not recommended to change at runtime.")]
    public string name;

    [Tooltip("The AudioClip that the sound plays. Not recommended to change at runtime.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("The volume at which the sound is played. Not recommended to change at runtime.")]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    [Tooltip("The pitch at which the sound is played. Not recommended to change at runtime.")]
    public float pitch = 1f;

    [Tooltip("Whether the sound should loop or not. Not recommended to change at runtime.")]
    public bool loop;

    [Tooltip("Whether the sound should play automatically on awake or not. Not recommended to change at runtime.")]
    public bool playOnAwake;

    [HideInInspector]
    [Tooltip("The actual AudioSource associated with the sound. Do NOT change this at runtime. " +
        "Values WITHIN this source CAN be changed.")]
    public AudioSource source;
}