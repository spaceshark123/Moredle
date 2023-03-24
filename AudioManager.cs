using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

[Serializable]
public class StringList
{
    public string Label = "";
    public List<string> strList = new List<string>();
}

public class AudioManager : MonoBehaviour
{
    //collection of sounds and groups of sounds
    public Sound[] sounds;
    public List<StringList> Groups = new List<StringList>();

    //singleton reference
    public static AudioManager instance;

    float startTime;
    public bool notplaying = false;

    // Start is called before the first frame update
    void Awake()
    {
        //assign singleton reference
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
            return;
        }
        
        //initialize sounds in collection
        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.playOnAwake = s.playOnAwake;
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.playOnAwake = s.playOnAwake;
            if(s.playOnAwake)
            {
                PlaySound(s.name);
            }
        }
    }

    public bool isPlaying(AudioClip clip)
    {
        if ((Time.time - startTime) >= clip.length)
            return false;
        return true;
    }

    /// <summary>
    /// Disables a group of audio sources.
    /// <para>Groups are defined in the AudioManager inspector.</para>
    /// Groups have a label for the user to recognize but are used in code by their index value.
    /// </summary>
    public void DisableGroup(int groupNum)
    {
        foreach(string str in Groups[groupNum].strList)
        {
            if(GetSound(str).source != null)
                GetSound(str).source.enabled = false;
        }
    }

    /// <summary>
    /// Enables a group of audio sources.
    /// <para>Groups are defined in the AudioManager inspector.</para>
    /// Groups have a label for the user to recognize but are used in code by their index value.
    /// </summary>
    public void EnableGroup(int groupNum)
    {
        foreach (string str in Groups[groupNum].strList)
        {
            if (GetSound(str).source != null)
                GetSound(str).source.enabled = true;
        }
    }
    
    /// <summary>
    /// Returns list of sounds contained in a group specified by a name string.
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// </summary>
    public List<Sound> GetGroup(int groupNum) {
        List<string> sounds = Groups[groupNum].strList;
        List<Sound> group = new List<Sound>();
        for (int i = 0; i < sounds.Count; i++)
        {
            group.Add(GetSound(sounds[i]));
        }
        return group;
    }

    /// <summary>
    /// Plays a sound specified by a name string.
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// Only plays sound if found.
    /// </summary>
    public void PlaySound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found");
            return;
        }
        if(!s.source.enabled)
        {
            //audiosource not enabled
            return;
        }
        startTime = Time.time;
        s.source.Play();
    }

    /// <summary>
    /// Plays a sound specified by a name string.
    /// PlayOneShot() allows for multiple instances of the same sound playing at the same time.
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// Only plays sound if found.
    /// </summary>
    public void PlayOneShot(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found");
            return;
        }
        if (!s.source.enabled)
        {
            //audiosource not enabled
            return;
        }
        startTime = Time.time;
        s.source.PlayOneShot(s.source.clip);
    }

    /// <summary>
    /// Plays a sound specified by a name string IF the origin of the sound is within a certain range of the target
    /// <para>Sounds and their names are defined in the AudioManager inspector. <paramref name="origin"/> and <paramref name="target"/> are Vector3's. Only input the SQUARE of the desired range.</para>
    /// sqrRange parameter is inclusive.
    /// Only plays sound if found.
    /// returns true or false depending on if the sounbd was played or if it was in range
    /// </summary>
    public bool PlaySoundInRange(string name, Vector3 origin, Vector3 target, float sqrRange)
    {
        if((target - origin).sqrMagnitude <= sqrRange)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found");
                return false;
            }
            if (!s.source.enabled)
            {
                //audiosource not enabled
                return false;
            }
            startTime = Time.time;
            s.source.Play();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Plays a sound specified by a name string IF the origin of the sound is within a certain range of the target
    /// PlayOneShot() allows for multiple instances of the same sound playing at the same time.
    /// <para>Sounds and their names are defined in the AudioManager inspector. <paramref name="origin"/> and <paramref name="target"/> are Vector3's. Only input the SQUARE of the desired range.</para>
    /// sqrRange parameter is inclusive.
    /// Only plays sound if found.
    /// returns true or false depending on if the sounbd was played or if it was in range
    /// </summary>
    public bool PlayOneShotInRange(string name, Vector3 origin, Vector3 target, float sqrRange)
    {
        if ((target - origin).sqrMagnitude <= sqrRange)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found");
                return false;
            }
            if (!s.source.enabled)
            {
                //audiosource not enabled
                return false;
            }
            startTime = Time.time;
            s.source.PlayOneShot(s.source.clip);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a sound specified by a name string.
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// Only gets sound if found.
    /// Sounds have properties defined by their sound class
    /// Not recommended to directly change sound values. Instead change <u><strong>Sound.source</strong></u> values.
    /// </summary>
    public Sound GetSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found");
            return new Sound();
        }
        return s;
    }

    /// <summary>
    /// Stops a sound specified by a name string.
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// Only stops sound if found and playing.
    /// </summary>
    public void StopSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found");
            return;
        }
        if(!s.source.isPlaying)
        {
            Debug.LogWarning("Sound: " + name + " isn't playing");
            return;
        }
        s.source.Stop();
    }

    /// <summary>
    /// Stops all currently playing sounds
    /// <para>Sounds and their names are defined in the AudioManager inspector.</para>
    /// </summary>
    public void StopAllSounds()
    {
        foreach(AudioSource src in gameObject.GetComponents<AudioSource>())
        {
            if(src.isPlaying)
            {
                src.Stop();
            }
        }
    }

    //loop music after a brief break after each iteration
    private void Update() {
        if(!GetSound("Music").source.isPlaying && (!notplaying)) {
            notplaying = true;
            Invoke("playmusic", UnityEngine.Random.Range(5f,10f));
        }
    }

    void playmusic() {
        PlaySound("Music");
    }
}
