using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    // UI elements
    public Toggle bigmode;
    public Toggle darkmode;
    public RectTransform scenetransition;
    public RectTransform bg1;
    public RectTransform bg2;
    public GameObject menupanel;
    public GameObject settingspanel;
    public GameObject quitbutton;
    public Slider sfxslider;
    public Slider musicslider;
    public Image sfxsliderhandle;
    public Image sfxsliderfill;
    public Image musicsliderhandle;
    public Image musicsliderfill;
    public Image darkmodecheckmark;
    public Image bigmodecheckmark;

    // Other variables
    public Color greencolor;
    public Color yellowcolor;
    public Color graycolor;
    public Color darkmodeboxcolor;
    public Color darkmodecamcolor;
    public Camera cam;
    char[] alphabet = {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'};
    GameObject[] boxes;
    Image[] boxesimg;
    GameObject[] circles;
    Image[] circlesimg;
    GameObject[] sliced;
    Image[] slicedimg;
    TMP_Text[] text;
    public float slidersoundinterval = 0.1f;
    [SerializeField] bool firsttime = true;
    [SerializeField] float lastslidersoundtime = -10f;
    public List<Image> bg1children = new List<Image>();
    public List<Image> bg2children = new List<Image>();

    void Start()
    {
        // Retrieve values from player prefs
        firsttime = PlayerPrefs.GetInt("firsttime", 1) == 1;
        bigmode.isOn = PlayerPrefs.GetInt("bigmode", 1) == 1;
        darkmode.isOn = PlayerPrefs.GetInt("darkmode", 0) == 1;
        sfxslider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        musicslider.value = PlayerPrefs.GetFloat("MusicVolume", 0.25f);

        // Disable quit button on certain platforms
        if(Application.platform == RuntimePlatform.WebGLPlayer || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) {
            quitbutton.SetActive(false);
        }

        if (!firsttime)
        {
            // Play scene transition animation
            scenetransition.localScale = new Vector2(10f, 2.5f);
            scenetransition.DOScaleY(0f, 1f).SetEase(Ease.InOutExpo);
        }
        else
        {
            // Reset first time flag
            firsttime = false;
            PlayerPrefs.SetInt("firsttime", 0);
        }

        // Format dark mode if enabled
        FormatDarkMode();
    }

    //Formats the UI elements for appropriate color scheme by setting color of various UI elements
    void FormatDarkMode() {
        // If the 'boxes' array has not been initialized yet, initialize it and find all UI elements with specific tags.
        if (boxes == null)
        {
            bool prevmenu = menupanel.activeInHierarchy;
            bool prevsettings = settingspanel.activeInHierarchy;
            menupanel.SetActive(true);
            settingspanel.SetActive(true);
            boxes = GameObject.FindGameObjectsWithTag("neumorphicbox");
            boxesimg = new Image[boxes.Length];
            for (int i = 0; i < boxes.Length; i++) {
                boxesimg[i] = boxes[i].GetComponent<Image>();
            }
            circles = GameObject.FindGameObjectsWithTag("neumorphiccircle");
            circlesimg = new Image[circles.Length];
            for (int i = 0; i < circles.Length; i++) {
                circlesimg[i] = circles[i].GetComponent<Image>();
            }
            sliced = GameObject.FindGameObjectsWithTag("testboxsliced");
            slicedimg = new Image[sliced.Length];
            for (int i = 0; i < slicedimg.Length; i++) {
                slicedimg[i] = sliced[i].GetComponent<Image>();
            }
            text = GameObject.FindObjectsOfType<TMP_Text>();
            menupanel.SetActive(prevmenu);
            settingspanel.SetActive(prevsettings);
        }

        // If 'bg1children' list is empty, add all Image components in the children of 'bg1' to the list.
        if(bg1children.Count == 0) {
            foreach(Image i in bg1.GetComponentsInChildren<Image>()) {
                bg1children.Add(i);
            }
        }
        // If 'bg2children' list is empty, add all Image components in the children of 'bg2' to the list.
        if(bg2children.Count == 0) {
            foreach(Image i in bg2.GetComponentsInChildren<Image>()) {
                bg2children.Add(i);
            }
        }
        // If the 'darkmode' key in PlayerPrefs is set to 1, format UI elements for dark mode, else do light mode
        if(PlayerPrefs.GetInt("darkmode", 0) == 1) {
            // Set colors of slider fills and handles to white.
            sfxsliderfill.color = Color.white;
            sfxsliderhandle.color = Color.white;
            musicsliderfill.color = Color.white;
            musicsliderhandle.color = Color.white;
            // Set colors of UI elements to the dark mode color scheme.
            foreach(Image g in boxesimg) {
                g.color = darkmodeboxcolor;
            }
            foreach(Image g in circlesimg) {
                g.color = darkmodeboxcolor;
            }
            foreach(Image g in slicedimg) {
                g.color = darkmodeboxcolor;
            }
            foreach(Image i in bg1children) {
                i.color = new Color(darkmodeboxcolor.r, darkmodeboxcolor.g, darkmodeboxcolor.b, 0.5f);
            }
            foreach(Image i in bg2children) {
                i.color = new Color(darkmodeboxcolor.r, darkmodeboxcolor.g, darkmodeboxcolor.b, 0.5f);;
            }
            cam.backgroundColor = darkmodecamcolor;
            foreach(TMP_Text t in text) {
                t.color = Color.white;
            }
            darkmodecheckmark.color = Color.white;
            bigmodecheckmark.color = Color.white;
        } else {
            // Set colors of slider fills and handles to dark color.
            sfxsliderfill.color = darkmodeboxcolor;
            sfxsliderhandle.color = darkmodeboxcolor;
            musicsliderfill.color = darkmodeboxcolor;
            musicsliderhandle.color = darkmodeboxcolor;
            // Set colors of UI elements to white
            foreach(Image g in boxesimg) {
                g.color = Color.white;
            }
            foreach(Image g in circlesimg) {
                g.color = Color.white;
            }
            foreach(Image g in slicedimg) {
                g.color = Color.white;
            }
            foreach(Image i in bg1children) {
                i.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);;
            }
            foreach(Image i in bg2children) {
                i.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);;
            }
            cam.backgroundColor = Color.white;
            foreach(TMP_Text t in text) {
                t.color = Color.black;
            }
            darkmodecheckmark.color = darkmodeboxcolor;
            bigmodecheckmark.color = darkmodeboxcolor;
        }
    }

    public void ShowSettings() {
        menupanel.GetComponent<RectTransform>().DOAnchorPosX(-800f, 0.3f).SetEase(Ease.InOutSine);
        settingspanel.GetComponent<RectTransform>().DOAnchorPosX(0f, 0.3f).SetEase(Ease.InOutSine);
    }

    public void ShowMain() {
        menupanel.GetComponent<RectTransform>().DOAnchorPosX(0f, 0.3f).SetEase(Ease.InOutSine);
        settingspanel.GetComponent<RectTransform>().DOAnchorPosX(800f, 0.3f).SetEase(Ease.InOutSine);
    }

    public void ToggleBigMode() {
        if(Time.time > 0.2f) {
            PlayerPrefs.SetInt("bigmode", bigmode.isOn ? 1 : 0);
        }
    }

    public void ToggleDarkMode() {
        if(Time.time > 0.2f) {
            PlayerPrefs.SetInt("darkmode", darkmode.isOn ? 1 : 0);
            FormatDarkMode();
        }
    }

    public void SetSFXVolume() {
        PlayerPrefs.SetFloat("SFXVolume", sfxslider.value);
        List<Sound> sfx = AudioManager.instance.GetGroup(0);
        foreach(Sound s in sfx) {
            s.source.volume = sfxslider.value;
        }
        if(lastslidersoundtime + slidersoundinterval <= Time.time) {
            //enough time has passed to play another sound
            lastslidersoundtime = Time.time;
            PlaySound("Hover");
        }
    }

    public void SetMusicVolume() {
        PlayerPrefs.SetFloat("MusicVolume", musicslider.value);
        AudioManager.instance.GetSound("Music").source.volume = musicslider.value;
        if(lastslidersoundtime + slidersoundinterval <= Time.time) {
            //enough time has passed to play another sound
            lastslidersoundtime = Time.time;
            PlaySound("Hover");
        }
    }

    //Switch scenes after playing the scene transition
    public void SwitchScene(string name) {
        scenetransition.DOScaleY(2.5f, 1f).SetEase(Ease.InOutExpo).OnComplete(() => {
            SceneManager.LoadScene(name);
        });
    }

    public void Quit() {
        Application.Quit();
    }

    //clears all player stats data while keeping everything else
    public void DeleteAllData() {
        //record essential backend saved values
        int firsttimeval = PlayerPrefs.GetInt("firsttime", 0);
        string lastgendate = PlayerPrefs.GetString("LastGeneratedDate", "");
        string lastcompdate = PlayerPrefs.GetString("LastCompletedDate", "");
        int lastgenind = PlayerPrefs.GetInt("LastGeneratedIndex", -1);
        string[] lines = new string[10];
        string username = PlayerPrefs.GetString("username", "");
        for (int i = 0; i < 10; i++) {
            lines[i] = PlayerPrefs.GetString("line" + (i + 1).ToString(), "");
        }
        musicslider.value = AudioManager.instance.GetSound("Music").volume;
        sfxslider.value = AudioManager.instance.GetSound("Click").volume;
        //delete all saved data
        PlayerPrefs.DeleteAll();
        //restore essential backend data
        bigmode.isOn = PlayerPrefs.GetInt("bigmode", 1) == 1;
        darkmode.isOn = PlayerPrefs.GetInt("darkmode", 0) == 1;
        PlayerPrefs.SetInt("firsttime", firsttimeval);
        for (int i = 0; i < 10; i++) {
            if(lines[i] != "") {
                PlayerPrefs.SetString("line" + (i + 1).ToString(), lines[i]);
            }
        }
        if(lastgendate != "") {
            PlayerPrefs.SetString("LastGeneratedDate", lastgendate);
        }
        if(lastcompdate != "") {
            PlayerPrefs.SetString("LastCompletedDate", lastcompdate);
        }
        if (lastgenind != -1) {
            PlayerPrefs.SetInt("LastGeneratedIndex", lastgenind);
        }
        if(username != "") {
            PlayerPrefs.SetString("username", username);
        }
    }

    //ONLY FOR EDITOR TESTING PURPOSES
    // deletes ALL data, even essential values for proper functioning
    public void PERMANENTLYDELETEALLDATA() {
        PlayerPrefs.DeleteAll();
    }

    //resets first time flag on application quit
    private void OnApplicationQuit() {
        PlayerPrefs.DeleteKey("firsttime");
    }

    public void PlaySound(string name) {
        AudioManager.instance.PlayOneShot(name);
    }
}
