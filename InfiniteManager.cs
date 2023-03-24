using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.EventSystems;

//line for a word: contains references to list of letters and line object
[Serializable]
public class Line {
    public GameObject lineGameObject;
    public RectTransform lineTransform;
    public List<GameObject> boxes;

    public Line(GameObject g) {
        lineGameObject = g;
        lineTransform = lineGameObject.GetComponent<RectTransform>();
        boxes = new List<GameObject>();
    }   
}

//structure for generating deterministic random indices for a string array (dictionary) to get random words
public class RandomNumberGenerator
{
    private System.Random random;
    private string[] options;
    private DateTimeOffset lastGeneratedDate;
    private int lastGeneratedIndex;

    public RandomNumberGenerator(string[] options)
    {
        // Use the current date and time as a seed for the random number generator
        DateTimeOffset currentDate = DateTimeOffset.Now;
        TimeSpan cstOffset = TimeSpan.FromHours(-6); // CST is UTC-6
        currentDate = currentDate.ToOffset(cstOffset);
        long seed = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day).Ticks;
        this.random = new System.Random((int)(seed & 0xffffffffL) | (int)(seed >> 32));
        this.options = options;

        //get last generated date for random index (used for checking if it is a new day)
        string lastGeneratedDateString = PlayerPrefs.GetString("LastGeneratedDate", "");
        this.lastGeneratedIndex = PlayerPrefs.GetInt("LastGeneratedIndex", -1);
        if (!string.IsNullOrEmpty(lastGeneratedDateString)) {
            this.lastGeneratedDate = DateTimeOffset.Parse(lastGeneratedDateString);
        }
        else {
            this.lastGeneratedDate = DateTimeOffset.MinValue;
        }
    }

    public int GenerateRandomIndex() {
        // Get the current date and time in the specified time zone
        DateTimeOffset currentDate = DateTimeOffset.Now;
        TimeSpan cstOffset = TimeSpan.FromHours(-6); // CST is UTC-6
        currentDate = currentDate.ToOffset(cstOffset);

        // Check if a number has already been generated for today
        if (this.lastGeneratedDate.Date == currentDate.Date) {
            // A number has already been generated for today, so return the same number
            return this.lastGeneratedIndex;
        }
        else {
            // Generate a new random number and update the lastGeneratedDate and lastGeneratedIndex fields
            int ind = this.random.Next(this.options.Length);

            // Save the last generated date and index to PlayerPrefs
            this.lastGeneratedIndex = ind;
            return ind;
        }
    }

    public string GenerateRandomOption()
    {
        // Generate and return a random string from the options array
        int index;
        string thisword;
        int i = 0;
        //keep getting a new random word until finding one that fulfills the proper conditions for a moredle word
        //upper limit of 500 iterations is set to prevent ultra-rare freezes
        do {
            index = GenerateRandomIndex();
            thisword = this.options[index];
            i++;
        } while((thisword.Length < 4 || thisword.Length > 8) && i < 500);
        DateTimeOffset currentDate = DateTimeOffset.Now;
        TimeSpan cstOffset = TimeSpan.FromHours(-6); // CST is UTC-6
        currentDate = currentDate.ToOffset(cstOffset);
        this.lastGeneratedDate = currentDate;
        if(i >= 500) {
            //too many iterations have passed, a predetermined word will be returned
            Debug.Log("failed to get daily word");
            PlayerPrefs.SetString("LastGeneratedDate", this.lastGeneratedDate.ToString());
            PlayerPrefs.SetInt("LastGeneratedIndex", 2);
            return this.options[2];
        } else {
            //return the random word
            PlayerPrefs.SetString("LastGeneratedDate", this.lastGeneratedDate.ToString());
            PlayerPrefs.SetInt("LastGeneratedIndex", this.lastGeneratedIndex);
            return thisword;
        }
    }
}

public class InfiniteManager : MonoBehaviour
{
    //object references
    public RectTransform board;
    public RectTransform boardbg;
    public GameObject boxPrefab;
    public GameObject linePrefab;

    //word info
    [SerializeField]List<Line> boardlayout = new List<Line>();
    [SerializeField]string word = "";
    [SerializeField]List<string> usedwords = new List<string>();

    KeyCode[] letterKeyCodes = {
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E,
        KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J,
        KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O,
        KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T,
        KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z
    };
    char[] alphabet = {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 
        'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
        'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 
        'y', 'z'
    };
    //list of celebratory phrases for winning
    public string[] wintextlist = {
        "Great Job!",
        "Nice!",
        "Nice Work!",
        "Great Work!",
        "Wow!"
    };

    //other references
    public Color darkgraycolor;
    public Color graycolor;
    public Color greencolor;
    public Color yellowcolor;

    public RectTransform scrollView;
    public float minscroll = 0f;
    public float maxscroll = 200f;
    public float scrollRate = 3f;
    public bool bigmode = true;

    //UI references
    public GameObject endpanel;
    public GameObject endpaneltitle;
    public TMP_Text endpanelword;
    public TMP_Text endpanelwintext;
    public GameObject statsbutton;
    public TMP_Text gamesplayedtext;
    public TMP_Text winratetext;
    public RectTransform keyboardtransform;
    public Button showkeyboardbutton;
    public float keyboardanimatetime = 0.5f;
    public GameObject scrollupbutton;
    public GameObject scrolldownbutton;
    public RectTransform helppanel;
    public Color wincolor;
    public Color losecolor;

    Dictionary dict;
    Dictionary profanity;

    public bool playing = true;

    public string unknownword = "_";

    public Slider[] guessbars;
    public TMP_Text[] guessbarlabels;
    public Image[] keyboard;

    public Tween shakeLineTween;
    public float lineShakeDuration = 0.2f;
    public float lineShakeStrength = 0.2f;
    public int lineShakeVibrato = 10;
    public float lineShakeRandomness = 0.5f;

    public RectTransform popup;
    public TMP_Text popuptext;
    float popupstaytime = 1f;
    public Tween shakePopupTween;

    public bool acceptingInput = true;

    bool helppanelhasbeenopened = false;
    public Image example1;
    public Image example2;
    public Image example3;

    public RectTransform scenetransition;

    public Sprite neumorphicbox;
    public Sprite neumorphiccircle;
    public Sprite testboxsliced;
    public Sprite neumorphicboxdark;
    public Sprite neumorphiccircledark;
    public Sprite testboxsliceddark;
    bool darkmode;
    public Camera cam;
    public Color darkmodebg;
    public Color lightmodebg;
    public Color darkmodeboxcolor;
    public Image endpanelclose;
    public Image endpanelrestart;
    public Image helppanelclose;
    public Image homeiconwhite;
    public Image questioniconwhite;
    public Image upiconwhite;
    public Image downiconwhite;
    public Image keyboardiconwhite;
    public Image statsiconwhite;
    public Image entericonwhite;
    public Image backspaceiconwhite;
    [Space]
    public bool dailyMode = false;
    public RandomNumberGenerator gen;
    public DateTimeOffset lastCompletedDate = DateTimeOffset.MinValue;
    public DateTimeOffset currentDate;
    public bool recordStats = true;

    public PlayFabManager playFabManager;
    public GameObject endgameinfopanel;
    public GameObject endgameleaderboardpanel;
    public TMP_InputField usernameinput;
    public GameObject submitscorepanel;
    public TMP_Text leaderboardinfotext;
    string dateusername;
    string[] leaderboardusernames = new string[10];
    int[] leaderboardguesses = new int[10];
    bool onleaderboard = false;
    bool won = false;
    public TMP_Text[] leaderboardUIusernames;
    public TMP_Text[] leaderboardUIguesses;
    public List<TMP_Text> keyboardletters = new List<TMP_Text>();
    public RectTransform endpaneltransform;
    public Image popupimage;
    float lowestliney = 1;
    float keyboardtopy = 0;
    public RectTransform keyboardtop;
    bool escaped = false;

    // Start is called before the first frame update
    void Start() {
        //assign references
        popupimage = popup.GetComponent<Image>();
        endpaneltransform = endpanel.GetComponent<RectTransform>();
        foreach(Image i in keyboard) {
            keyboardletters.Add(i.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>());
        }
        if(dailyMode) {
            playFabManager = GetComponent<PlayFabManager>();
        }
        currentDate = DateTimeOffset.Now;
        TimeSpan cstOffset = TimeSpan.FromHours(-6); // CST is UTC-6
        currentDate = currentDate.ToOffset(cstOffset);

        //set settings values
        bigmode = PlayerPrefs.GetInt("bigmode", 1) == 1;
        if(!bigmode) {
            //small mode
            scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, -275f);
            scrollView.localScale = new Vector3(0.7f, 0.7f, 1f);
            scrollupbutton.SetActive(false);
            scrolldownbutton.SetActive(false);
        } else {
            //big mode
            scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, -375f);
            scrollView.localScale = new Vector3(1f, 1f, 1f);
            scrollupbutton.SetActive(true);
            scrolldownbutton.SetActive(true);
        }

        darkmode = PlayerPrefs.GetInt("darkmode", 0) == 1;
        FormatDarkMode();

        //load word lists
        dict = new Dictionary((TextAsset)Resources.Load("dictionary"));
        profanity = new Dictionary((TextAsset)Resources.Load("profanity"));
        if(dailyMode) {
            //check if player has already done today's moredle
            gen = new RandomNumberGenerator(dict.GetDictionary());
            SetDailyWord();
            endpanelrestart.gameObject.SetActive(false);
            string lastCompletedDateString = PlayerPrefs.GetString("LastCompletedDate", "");
            if(lastCompletedDateString == "") {
                lastCompletedDate = DateTimeOffset.MinValue;
            } else {
                lastCompletedDate = DateTimeOffset.Parse(lastCompletedDateString);
            }
            usernameinput.text = PlayerPrefs.GetString("username", "");
            if(currentDate.Date == lastCompletedDate.Date) {
                //already did todays moredle, recall previous entries and disable score/stats submission
                recordStats = false;
                submitscorepanel.SetActive(false);
                StartCoroutine(GetLeaderboard());
                StartCoroutine(RecallEntries());
            } else
            {
                //havent completed todays moredle
                submitscorepanel.SetActive(true);
                StartCoroutine(UpdateLeaderboardDay());
                string lastplayeddatestring = PlayerPrefs.GetString("LastPlayedDate", "");
                if (lastplayeddatestring != "") {
                    if (currentDate.Date == DateTimeOffset.Parse(lastplayeddatestring).Date)
                    {
                        //attempted todays moredle before, recall previous entries
                        StartCoroutine(RecallEntries());
                    } else {
                        //new day, clear yesterdays saved lines
                        DeleteSavedEntries();
                    }
                } else {
                    //never played before, but clear saved lines just in case
                    DeleteSavedEntries();
                }
            }
        } else {
            //infinite mode, set random word
            SetRandomWord();
        }

        //play scene in transition
        scenetransition.localScale = new Vector2(10f, 2.5f);
        scenetransition.DOScaleY(0f, 1f).SetEase(Ease.InOutExpo);
    }

    void DeleteSavedEntries() {
        for (int i = 1; i <= 10; i++) {
            PlayerPrefs.DeleteKey("line" + i.ToString());
        }
    }

    IEnumerator RecallEntries() {
        for (int i = 1; i <= 10; i++) {
            //for each line, recall its letters and add them back
            string line = PlayerPrefs.GetString("line" + i.ToString(), "");
            if(line == "") {
                //nothing left to recall
                break;
            }
            if(boardlayout.Count == 0) {
                AddLine(true);
            }
            for (int j = 0; j < line.Length; j++) {
                AddBox(line[j].ToString());
                boardlayout[boardlayout.Count - 1].boxes[j].transform.localScale = Vector3.one;
            }
            if(boardlayout.Count == 10) {
                //last guess has been entered
                AddLine(false);
            } else {
                AddLine(true);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void FormatDarkMode() {
        //get references to all affected objects/UI elements
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("neumorphicbox");
        GameObject[] circles = GameObject.FindGameObjectsWithTag("neumorphiccircle");
        GameObject[] sliced = GameObject.FindGameObjectsWithTag("testboxsliced");
        if(darkmode) {
            //set all affected objects to dark color.
            foreach(GameObject g in boxes) {
                g.GetComponent<Image>().color = darkmodeboxcolor;
            }
            foreach(GameObject g in circles) {
                g.GetComponent<Image>().color = darkmodeboxcolor;
            }
            foreach(GameObject g in sliced) {
                g.GetComponent<Image>().color = darkmodeboxcolor;
            }
            cam.backgroundColor = darkmodebg;
            foreach(TMP_Text t in keyboardletters) {
                t.color = Color.white;
            }
            helppanelclose.color = darkmodeboxcolor;
            //set icon colors to white
            helppanelclose.transform.GetChild(0).gameObject.GetComponent<Image>().color = Color.white;
            homeiconwhite.color = Color.white;
            questioniconwhite.color = Color.white;
            upiconwhite.color = Color.white;
            downiconwhite.color = Color.white;
            keyboardiconwhite.color = Color.white;
            statsiconwhite.color = Color.white;
            entericonwhite.color = Color.white;
            backspaceiconwhite.color = Color.white;
        }
        //objects are in light mode by default, so do nothing if light mode selected
    }

    public void SetRandomWord() {
        System.Random rand = new System.Random();
        //keep generating a new random word until all word conditions are met
        do {
            unknownword = dict.GetDictionary()[rand.Next(dict.Length())];
        } while(unknownword.Length < 4 || unknownword.Length > 8);
        Debug.Log("secret word: " + unknownword);
    }

    public void SetDailyWord() {
        unknownword = gen.GenerateRandomOption();
    }

    //resets moredle for infinite mode
    public void Reset() {
        CloseEndPanel();
        if(!dailyMode) {
            //generate a new random word
            SetRandomWord();
        }
        statsbutton.SetActive(false);
        //remove all previous guesses
        foreach(Line line in boardlayout) {
            line.boxes.Clear();
            line.lineGameObject = null;
            line.lineTransform = null;
        }
        boardlayout.Clear();
        foreach(Transform line in board) {
            Destroy(line.gameObject);
        }
        for (int i = 0; i < keyboard.Length; i++) {
            if (darkmode) {
                keyboard[i].color = darkmodeboxcolor;
                keyboardletters[i].color = Color.white;
            } else {
                keyboard[i].color = Color.white;
            }
        }
        word = "";
        usedwords.Clear();
        playing = true;
        acceptingInput = true;
    }

    // Update is called once per frame
    void Update()
    {
        //update normalized screen positions for keyboard and lowest line to determine whether to auto-scroll or not
        if(boardlayout.Count > 0) {
            lowestliney = cam.ScreenToViewportPoint(boardlayout[boardlayout.Count-1].lineGameObject.transform.position).y;
            keyboardtopy = cam.ScreenToViewportPoint(keyboardtop.position).y;
        }
        if(Input.anyKeyDown && playing && acceptingInput) {
            //user pressed a key
            foreach (KeyCode keyCode in letterKeyCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    //if there is a line, add to it, else make a new line and add to that
                    if(boardlayout.Count == 0) {
                        AddLine(true);
                    }

                    //if max letters, dont input anything
                    if(boardlayout[boardlayout.Count-1].boxes.Count >= 8) {
                        SetPopup("Too Many Letters");
                        ShakeLine();
                        break;
                    }

                    PlaySound("Click");
                    AddBox(keyCode.ToString());
                }
            }

            if(Input.GetKeyDown(KeyCode.Return)) {
                //only add a line if not at max line amount (10) and current line isnt less than 4 letters;
                if(boardlayout.Count == 0) {
                    AddLine(true);
                } else if(boardlayout.Count == 10) {
                    //last guess has been entered
                    PlaySound("AddLine");
                    AddLine(false);
                } else if(boardlayout.Count < 10 && boardlayout[boardlayout.Count-1].boxes.Count >= 4) {
                    PlaySound("AddLine");
                    AddLine(true);
                } else {
                    //less than 4 letters
                    ShakeLine();
                    SetPopup("Not Enough Letters");
                }
            }

            if(Input.GetKeyDown(KeyCode.Backspace)) {
                //user pressed backspace
                if(boardlayout.Count != 0) {
                    if(boardlayout[boardlayout.Count-1].boxes.Count != 0) {
                        //there exists letters that can be backspaced
                        PlaySound("Cancel");
                        DeleteBox();
                    }
                }
            }
        }

        //scrolling logic from mouse scroll or arrow keys
        if(bigmode) {
            if(Input.mouseScrollDelta.y < 0 || Input.GetKey(KeyCode.DownArrow)) {
                ScrollDown();
            } else if(Input.mouseScrollDelta.y > 0 || Input.GetKey(KeyCode.UpArrow)) {
                ScrollUp();
            }
        }

        if(!escaped && Input.GetKeyDown(KeyCode.Escape)) {
            //if user presses escape and they havent done it before, go back to main menu
            escaped = true;
            SwitchScene("Menu");
        }
    }

    public void StartScrollDown() {
        EndScrollUp();
        StartCoroutine(ScrollDownRoutine());
    }

    public void EndScrollDown() {
        StopCoroutine(ScrollDownRoutine());
    }

    public void StartScrollUp() {
        EndScrollDown();
        StartCoroutine(ScrollUpRoutine());
    }

    public void EndScrollUp() {
        StopCoroutine(ScrollUpRoutine());
    }

    IEnumerator ScrollDownRoutine() {
        while(true) {
            EndScrollUp();
            ScrollDown();
            yield return null;
        }
    }

    IEnumerator ScrollUpRoutine() {
        while(true) {
            EndScrollDown();
            ScrollUp();
            yield return null;
        }
    }

    public void ScrollDown() {
        StopCoroutine("ScrollToKeyboard");
        scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, Mathf.Clamp(scrollView.anchoredPosition.y + scrollRate * Time.deltaTime, minscroll, maxscroll));
    }

    public void ScrollUp() {
        StopCoroutine("ScrollToKeyboard");
        scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, Mathf.Clamp(scrollView.anchoredPosition.y - scrollRate * Time.deltaTime, minscroll, maxscroll));
    }

    //auto scroll to keep bottom line in view
    IEnumerator ScrollToKeyboard() {
        while(lowestliney < keyboardtopy+0.07f) {
            scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, scrollView.anchoredPosition.y + 18f);
            lowestliney = cam.ScreenToViewportPoint(boardlayout[boardlayout.Count-1].lineGameObject.transform.position).y;
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    void AddLine(bool makeNewLine) {
        if(boardlayout.Count == 0) {
            word = "";
            if(makeNewLine) {
                GameObject line = Instantiate(linePrefab, board);
                boardlayout.Add(new Line(line));
            }
            return;
        }
        //make sure entered word is an actual word
        word = word.ToLower();
        if(word != "") {
            if(!dict.Contains(word)) {
                //not a word
                ShakeLine();
                SetPopup("Not In Word List");
                return;
            }
        }

        //if word has already been used, dont allow using it again
        if(usedwords.Contains(word)) {
            SetPopup("Duplicate Word");
            ShakeLine();
            return;
        }

        //save this line if in daily mode
        if(dailyMode) {
            PlayerPrefs.SetString("line" + (boardlayout.Count).ToString(), word.ToUpper());
            PlayerPrefs.SetString("LastPlayedDate", currentDate.ToString());
        }

        if(word == unknownword) {
            //guessed it correctly
            playing = false;
            acceptingInput = false;
            won = true;
            if(dailyMode && recordStats) {
                PlayerPrefs.SetString("LastCompletedDate", currentDate.ToString());
            }
        } else if(boardlayout.Count >= 10) {
            //ran out of guesses so lost
            playing = false;
            acceptingInput = false;
            if(dailyMode && recordStats) {
                PlayerPrefs.SetString("LastCompletedDate", currentDate.ToString());
            }
        }

        usedwords.Add(word);
        if(boardlayout.Count == 0) {
            AddLinePart2(makeNewLine, word, unknownword, boardlayout.Count);
        } else {
            //grade the word and color letters
            Grade(makeNewLine);
        }

        //reset guess and create new line
        word = "";
        if(makeNewLine && playing) {
            GameObject line = Instantiate(linePrefab, board);
            boardlayout.Add(new Line(line));
        }
        if(lowestliney < keyboardtopy+0.07f) {
            //new line is below keyboard, scroll down
            StartCoroutine(ScrollToKeyboard());
        }
    }

    //end the game if necessary
    void AddLinePart2(bool makeNewLine, string w, string unknown, int numlines) {
        if(w == unknown) {
            EndGame(true);
        } else if(numlines >= 10) {
            EndGame(false);
        }
    }

    void EndGame(bool win) {
        if(win) {
            PlaySound("Success");
            if(helppanel.gameObject.activeInHierarchy) {
                //close help panel if it is open
                helppanel.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                    helppanel.gameObject.SetActive(false);
                });
                helppanel.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                    helppanel.rotation = Quaternion.identity;
                });
            }
            if (dailyMode) {
                if ((boardlayout[boardlayout.Count - 1].boxes.Count == 0 ? boardlayout.Count - 1 : boardlayout.Count) <= leaderboardguesses[9] || leaderboardguesses[9] == -1) {
                    //made top 10 in daily leaderboard
                    leaderboardinfotext.text = "You placed on the leaderboard!";
                    if (!onleaderboard) {
                        //havent already submitted to leaderboard, so allow submission
                        submitscorepanel.SetActive(true);
                    } else {
                        submitscorepanel.SetActive(false);
                    }
                }
                else {
                    leaderboardinfotext.text = "You didn't place on the leaderboard";
                    submitscorepanel.SetActive(false);
                }
            }
            //show game over panel with appropriate text
            endpanel.SetActive(true);
            endpaneltransform.localScale = Vector3.zero;
            endpaneltransform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutBounce);
            endpanel.GetComponent<Image>().color = wincolor;
            endpanelword.text = unknownword;
            endpaneltitle.SetActive(false);
            endpanelword.gameObject.SetActive(false);
            endpanelwintext.gameObject.SetActive(true);
            System.Random rand = new System.Random();
            endpanelwintext.text = wintextlist[rand.Next(wintextlist.Length)];
            statsbutton.SetActive(true);
            if(darkmode) {
                statsbutton.GetComponent<Image>().color = darkmodeboxcolor;
            }
            //update stats
            UpdateStats(true, boardlayout[boardlayout.Count-1].boxes.Count == 0 ? boardlayout.Count-1 : boardlayout.Count);
        } else {
            PlaySound("Fail");
            if(helppanel.gameObject.activeInHierarchy) {
                //close help panel if it is open
                helppanel.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                    helppanel.gameObject.SetActive(false);
                });
                helppanel.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                    helppanel.rotation = Quaternion.identity;
                });
            }
            if(dailyMode) {
                //lost the game, so disable leaderboard submission
                submitscorepanel.SetActive(false);
                leaderboardinfotext.text = "You didn't place on the leaderboard";
            }
            //show game over panel with appropriate text
            endpanel.SetActive(true);
            endpaneltransform.localScale = Vector3.zero;
            endpaneltransform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutBounce);
            endpanel.GetComponent<Image>().color = losecolor;
            endpanelword.text = unknownword;
            endpaneltitle.SetActive(true);
            endpanelword.gameObject.SetActive(true);
            endpanelwintext.gameObject.SetActive(false);
            statsbutton.SetActive(true);
            //update stats
            UpdateStats(false, boardlayout[boardlayout.Count-1].boxes.Count == 0 ? boardlayout.Count-1 : boardlayout.Count);
        }
    }

    void Grade(bool makeNewLine) {
        int lineInd = boardlayout.Count - 1;
        string w = word;
        string unknown = unknownword;
        //toggle off help panel if open
        if(helppanel.gameObject.activeInHierarchy) {
            ToggleHelpPanel();
        }
        char[] target = unknownword.ToCharArray();
        char[] currentword = word.ToCharArray();
        char[] colors = new char[currentword.Length];
        for(int i = 0; i < colors.Length; i++) {
            //initialize colors array to _ to represent unknown/not matching colors
            colors[i] = '_';
        }
        //set green colors
        for(int i = 0; i < Mathf.Min(currentword.Length, target.Length); i++) {
            if(currentword[i] == target[i]) {
                //letter matches: remove letter from word so it cant be matched again and mark it green
                currentword[i] = '_';
                colors[i] = 'G';
                target[i] = ' '; 
            }
        }
        //set yellow
        for(int i = 0; i < currentword.Length; i++) {
            int index = Array.IndexOf(target, currentword[i]);
            if(index > -1) {
                //letter is contained in word: remove letter from word so it cant be matched again and mark it yellow
                currentword[i] = '_';
                colors[i] = 'Y';
                target[index] = ' ';
            }
        }
        //set keyboard colors
        currentword = word.ToCharArray();
        for(int i = 0; i < currentword.Length; i++) {
            Color targetcol = colors[i] == '_' ? darkgraycolor : colors[i] == 'Y' ? yellowcolor : greencolor;
            int keyboardInd = Array.IndexOf(alphabet, currentword[i]);
            if(darkmode) {
                if(keyboard[keyboardInd].color == darkmodeboxcolor) {
                    //this letter of keyboard has not been set, it is blank
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                    keyboardletters[keyboardInd].DOColor(Color.black, 0.1f);
                } else if ((keyboard[keyboardInd].color == darkgraycolor || keyboard[keyboardInd].color == yellowcolor) && targetcol == greencolor) {
                    //green takes precedence over yellow and gray
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                    keyboardletters[keyboardInd].DOColor(Color.black, 0.1f);
                } else if(keyboard[keyboardInd].color == darkmodeboxcolor && targetcol == yellowcolor) {
                    //yellow takes precedence over gray
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                    keyboardletters[keyboardInd].DOColor(Color.black, 0.1f);
                }
            } else {
                if(keyboard[keyboardInd].color == Color.white) {
                    //this letter of keyboard has not been set, it is blank
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                } else if ((keyboard[keyboardInd].color == darkgraycolor || keyboard[keyboardInd].color == yellowcolor) && targetcol == greencolor) {
                    //green takes precedence over yellow and gray
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                } else if(keyboard[keyboardInd].color == Color.white && targetcol == yellowcolor) {
                    //yellow takes precedence over gray
                    keyboard[keyboardInd].DOColor(targetcol, 0.1f);
                }
            }
        }
        //animate in letter colors
        acceptingInput = false;
        Sequence colorsequence = DOTween.Sequence();
        Sequence scalesequence = DOTween.Sequence();
        Sequence textsequence = DOTween.Sequence();
        for (int i = 0; i < currentword.Length; i++) {
            textsequence.Append(boardlayout[lineInd].boxes[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().DOColor(Color.black, 0.2f));// = Color.black;
            if(darkmode) {
                colorsequence.Append(boardlayout[lineInd].boxes[i].GetComponent<Image>().DOColor(colors[i] == '_' ? darkgraycolor : colors[i] == 'Y' ? yellowcolor : greencolor, 0.2f));
            } else {
                colorsequence.Append(boardlayout[lineInd].boxes[i].GetComponent<Image>().DOColor(colors[i] == '_' ? graycolor : colors[i] == 'Y' ? yellowcolor : greencolor, 0.2f));
            }
            scalesequence.Append(boardlayout[lineInd].boxes[i].GetComponent<RectTransform>().DOPunchScale(new Vector3(0.3f,0.3f,0.3f), 0.2f));
        }
        colorsequence.OnComplete(() => { 
            AddLinePart2(makeNewLine, w, unknown, lineInd+1);
            acceptingInput = true;
        });
    }

    public static string DateTimeOffsetToString(DateTimeOffset date) {
        return date.ToString("MMddyyyy");
    }

    public static DateTimeOffset StringToDateTimeOffset(string str) {
        return DateTimeOffset.ParseExact(str, "MMddyyyy", System.Globalization.CultureInfo.InvariantCulture);
    }

    //return the digits contained in a string
    string onlyDigits(string s) {
        string output = "";
        foreach (char c in s) {
            if (char.IsDigit(c)) {
                output += c;
            }
        }
        return output;
    }

    //return the letters contained in a string
    string onlyLetters(string s) {
        string output = "";
        foreach (char c in s) {
            if (!char.IsDigit(c)) {
                output += c;
            }
        }
        return output;
    }

    IEnumerator UpdateLeaderboardDay() {
        //wait until client is logged in to API
        yield return new WaitUntil(() => playFabManager.loggedIn);
        playFabManager.GetLeaderboard("Date", 1);
        yield return new WaitUntil(() => playFabManager.returned);
        playFabManager.returned = false;
        bool ClearLeaderboardFlag = false;
        //username field of API is used to simultaneously store username and date of solve
        dateusername = "";
        if(playFabManager.returnedLeaderboard.Count == 0) {
            //this is the first time the leaderboards are being accessed/modified
            ClearLeaderboardFlag = true;
            Debug.Log("cleared leaderboard because there is nothing in date");
        } else {
            //get date of leaderboard
            string filtered = onlyDigits(playFabManager.returnedLeaderboard[0].DisplayName);
            if(filtered != "") {
                if(StringToDateTimeOffset(filtered).Date != currentDate.Date) {
                    //last time scores were added was yesterday, this leaderboard is outdated and can be cleared safely
                    ClearLeaderboardFlag = true;
                    Debug.Log("cleared leaderboard because last leaderboard date was yesterday");
                } else {
                    dateusername = filtered;
                }
            } else {
                //idk
                ClearLeaderboardFlag = true;
                Debug.Log("cleared leaderboard because date username was empty");
            }
        }
        if(ClearLeaderboardFlag) {
            //clear the leadeboard if necessary
            playFabManager.ClearLeaderboard("Date");
            playFabManager.ClearLeaderboard("Guesses");
            yield return new WaitUntil(() => playFabManager.cleared);
            playFabManager.cleared = false;
            playFabManager.AddLeaderboard("Date", 1);
            yield return new WaitUntil(() => playFabManager.added);
            playFabManager.added = false;
        }
        //set username to date + desired username
        dateusername = DateTimeOffsetToString(currentDate);
        playFabManager.SetDisplayNameForUser(dateusername+usernameinput.text);
        yield return new WaitUntil(() => playFabManager.setusername);
        playFabManager.setusername = false;

        StartCoroutine(GetLeaderboard());
    }

    IEnumerator GetLeaderboard() {
        //wait until client is logged in to API
        yield return new WaitUntil(() => playFabManager.loggedIn);
        playFabManager.GetLeaderboard("Guesses", 10);
        yield return new WaitUntil(() => playFabManager.returned);
        playFabManager.returned = false;
        for (int i = 0; i < 10; i++) {
            if(i >= playFabManager.returnedLeaderboard.Count) {
                //blank leaderboard spot
                leaderboardusernames[i] = "";
                leaderboardguesses[i] = -1;
            } else {
                leaderboardusernames[i] = onlyLetters(playFabManager.returnedLeaderboard[i].DisplayName);
                leaderboardguesses[i] = 10-playFabManager.returnedLeaderboard[i].StatValue;
            }
        }
        //set leaderboard list UI
        for (int i = 0; i < 10; i++) {
            if(leaderboardusernames[i] == "") {
                //no score for this position
                leaderboardUIusernames[i].text = "";
                leaderboardUIguesses[i].text = "";
            } else {
                leaderboardUIusernames[i].text = leaderboardusernames[i];
                leaderboardUIguesses[i].text = leaderboardguesses[i].ToString();
            }
        }
        //mark username if user is on leaderboard
        onleaderboard = Array.Exists(leaderboardusernames, x => x == PlayerPrefs.GetString("username", "-"));
        if(onleaderboard) {
            int ind = IndexOf(leaderboardusernames, PlayerPrefs.GetString("username", "-"));
            if(ind != -1) {
                leaderboardUIusernames[ind].fontStyle = FontStyles.Underline;
            } else {
                foreach(TMP_Text t in leaderboardUIusernames) {
                    t.fontStyle = FontStyles.Normal;
                }
            }
        } else {
            foreach(TMP_Text t in leaderboardUIusernames) {
                t.fontStyle = FontStyles.Normal;
            }
        }
    }

    int IndexOf(string[] arr, string val) {
        for (int i = 0; i < arr.Length; i++) {
            if(arr[i] == val)
                return i;
        }
        return -1;
    }

    public void UpdateStats(bool won, int guesses) {
        if (recordStats) {
            if (PlayerPrefs.HasKey("gamesplayed")) {
                PlayerPrefs.SetInt("gamesplayed", PlayerPrefs.GetInt("gamesplayed") + 1);
            }
            else {
                PlayerPrefs.SetInt("gamesplayed", 1);
            }
            if (won) {
                if (PlayerPrefs.HasKey("wins")) {
                    PlayerPrefs.SetInt("wins", PlayerPrefs.GetInt("wins") + 1);
                }
                else {
                    PlayerPrefs.SetInt("wins", 1);
                }
            }
        }
        //update stats text UI
        gamesplayedtext.text = PlayerPrefs.GetInt("gamesplayed") + " Played";
        winratetext.text = Mathf.RoundToInt(((float)PlayerPrefs.GetInt("wins", 0)/PlayerPrefs.GetInt("gamesplayed", 1))*100f) + "% Winrate";
        if (recordStats && won) {
            //save stats
            if (PlayerPrefs.HasKey(guesses.ToString())) {
                PlayerPrefs.SetInt(guesses.ToString(), PlayerPrefs.GetInt(guesses.ToString()) + 1);
            } else {
                PlayerPrefs.SetInt(guesses.ToString(), 1);
            }
        }
        //update stats bars
        for(int i = 0; i < 10; i++) {
            Slider s = guessbars[i];
            s.value = 0f;
            float sliderTarget = (((float)PlayerPrefs.GetInt((i+1).ToString(), 0)/PlayerPrefs.GetInt("gamesplayed", 1))*100f);
            s.DOValue(sliderTarget, 0.5f).SetEase(Ease.OutQuint).SetDelay(0.5f);
            guessbarlabels[i].text = PlayerPrefs.GetInt((i+1).ToString(), 0).ToString();
        }
    }

    public void CloseEndPanel() {
        ToggleEndPanel();
    }

    public void ToggleEndPanel() {
        if(helppanel.gameObject.activeInHierarchy) {
            //close help panel if open
            helppanel.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                helppanel.gameObject.SetActive(false);
            });
            helppanel.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                helppanel.rotation = Quaternion.identity;
            });
        }
        if (!endpanel.activeInHierarchy)
        {
            //open
            if (dailyMode) {
                if (((boardlayout[boardlayout.Count - 1].boxes.Count == 0 ? boardlayout.Count - 1 : boardlayout.Count) <= leaderboardguesses[9] || leaderboardguesses[9] == -1) && won) {
                    //made top 10
                    leaderboardinfotext.text = "You placed on the leaderboard!";
                    if (!onleaderboard) {
                        //havent already submitted to leaderboard
                        submitscorepanel.SetActive(true);
                    } else {
                        submitscorepanel.SetActive(false);
                    }
                }
                else {
                    leaderboardinfotext.text = "You didn't place on the leaderboard";
                    submitscorepanel.SetActive(false);
                }
            }
            endpanel.SetActive(true);
            endpaneltransform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutBounce);
            if(endpaneltransform.localScale.x == 0) {
                endpaneltransform.rotation = Quaternion.Euler(0f,0f,0f);
            } else {
                endpaneltransform.DORotate(new Vector3(0, 0, 0f - endpaneltransform.rotation.eulerAngles.z), 0.5f, RotateMode.WorldAxisAdd).SetEase(Ease.OutBounce);
            }
            //animate in stats bars
            for(int i = 0; i < 10; i++) {
                Slider s = guessbars[i];
                s.value = 0f;
                float sliderTarget = (((float)PlayerPrefs.GetInt((i+1).ToString(), 0)/PlayerPrefs.GetInt("gamesplayed"))*100f);
                s.DOValue(sliderTarget, 0.5f).SetEase(Ease.OutQuint).SetDelay(0.5f);
                guessbarlabels[i].text = PlayerPrefs.GetInt((i+1).ToString(), 0).ToString();
            }
        } else {
            //close panel
            endpaneltransform.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                endpanel.SetActive(false);
            });
            endpaneltransform.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                endpaneltransform.rotation = Quaternion.identity;
            });
        }
    }

    public void ToggleHelpPanel() {
        //close game over panel if open
        if(endpanel.activeInHierarchy) {
            endpaneltransform.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                endpanel.SetActive(false);
            });
            endpaneltransform.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                endpaneltransform.rotation = Quaternion.identity;
            });
        }
        if (!helppanel.gameObject.activeInHierarchy)
        {
            //open
            helppanel.gameObject.SetActive(true);
            if(!helppanelhasbeenopened) {
                //first time opening
                helppanelhasbeenopened = true;
                helppanel.localScale = Vector3.zero;
                //format help panel to follow color scheme.
                if(darkmode) {
                    foreach(Transform t in example1.gameObject.transform.parent) {
                        t.gameObject.GetComponent<Image>().color = darkmodeboxcolor;
                        t.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                    }
                    foreach(Transform t in example2.gameObject.transform.parent) {
                        t.gameObject.GetComponent<Image>().color = darkmodeboxcolor;
                        t.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                    }
                    foreach(Transform t in example3.gameObject.transform.parent) {
                        t.gameObject.GetComponent<Image>().color = darkmodeboxcolor;
                        t.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                    }
                }
            }
            helppanel.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutBounce);
            if(helppanel.localScale.x == 0) {
                helppanel.rotation = Quaternion.Euler(0f,0f,0f);
            } else {
                helppanel.DORotate(new Vector3(0, 0, 0f - helppanel.rotation.eulerAngles.z), 0.5f, RotateMode.WorldAxisAdd).SetEase(Ease.OutBounce);
            }
            //more color formatting
            if(darkmode) {
                example1.color = darkmodeboxcolor;
                example1.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                example2.color = darkmodeboxcolor;
                example2.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                example3.color = darkmodeboxcolor;
                example3.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().color = Color.white;
                example1.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().DOColor(Color.black, 0.2f).SetDelay(0.6f);
                example2.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().DOColor(Color.black, 0.2f).SetDelay(0.6f);
                example3.gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().DOColor(Color.black, 0.2f).SetDelay(0.6f);
            } else {
                example1.color = Color.white;
                example2.color = Color.white;
                example3.color = Color.white;
            }
            example1.DOColor(greencolor, 0.2f).SetDelay(0.6f);
            example1.gameObject.GetComponent<RectTransform>().DOPunchScale(new Vector3(0.15f,0.15f,0.15f), 0.2f).SetDelay(0.6f);
            example2.DOColor(yellowcolor, 0.2f).SetDelay(0.6f);
            example2.gameObject.GetComponent<RectTransform>().DOPunchScale(new Vector3(0.15f,0.15f,0.15f), 0.2f).SetDelay(0.6f);
            example3.DOColor(darkmode ? darkgraycolor : graycolor, 0.2f).SetDelay(0.6f);
            example3.gameObject.GetComponent<RectTransform>().DOPunchScale(new Vector3(0.15f,0.15f,0.15f), 0.2f).SetDelay(0.6f);
        } else {
            //close help panel
            helppanel.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InOutSine).OnComplete(() => {
                helppanel.gameObject.SetActive(false);
            });
            helppanel.DORotate(new Vector3(0, 0, 90f), 0.2f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine).OnComplete(() => {
                helppanel.rotation = Quaternion.identity;
            });
        }
    }

    void AddBox(string letter) {
        //add letter
        word += letter;
        GameObject box = Instantiate(boxPrefab, boardlayout[boardlayout.Count-1].lineTransform);
        if(darkmode) {
            box.GetComponent<Image>().color = darkmodeboxcolor;
        }
        RectTransform t = box.GetComponent<RectTransform>();
        t.localScale = Vector3.zero;
        t.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBounce);
        TMP_Text child = box.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        child.text = letter;
        if(darkmode) {
            child.color = Color.white;
        }
        boardlayout[boardlayout.Count-1].boxes.Add(box);
    }

    void DeleteBox() {
        //get last box and remove it (backspace)
        word = word.Remove(word.Length-1,1);
        GameObject box = boardlayout[boardlayout.Count-1].boxes[boardlayout[boardlayout.Count-1].boxes.Count-1];
        boardlayout[boardlayout.Count-1].boxes.RemoveAt(boardlayout[boardlayout.Count-1].boxes.Count-1);
        Destroy(box);
    }

    public void PressKey(string key) {
        //simulates letter key press. called when keyboard keys are pressed manually
        if(playing && acceptingInput) {
            if(boardlayout.Count == 0) {
                AddLine(true);
            }

            //if max letters, dont input anything
            if(boardlayout[boardlayout.Count-1].boxes.Count >= 8) {
                ShakeLine();
                SetPopup("Too Many Letters");
                return;
            }

            AddBox(key);
        }
    }

    public void PressBackspace() {
        //simulates backspace press. called when keyboard backspace is pressed manually
        if(boardlayout.Count != 0 && playing && acceptingInput) {
            if(boardlayout[boardlayout.Count-1].boxes.Count != 0) {
                //there exists letters that can be backspaced
                DeleteBox();
            }
        }
    }

    public void PressEnter() {
        //simulates enter press. called when keyboard enter is pressed manually
        if(playing && acceptingInput) {
            if(boardlayout.Count == 0) {
                AddLine(true);
            } else if(boardlayout.Count == 10) {
                //last guess has been entered
                AddLine(false);
            } else if(boardlayout.Count < 10 && boardlayout[boardlayout.Count-1].boxes.Count >= 4) {
                AddLine(true);
            } else {
                //less than 4 letters
                SetPopup("Not Enough Letters");
                ShakeLine();
            }
        }
    }

    //switch scene after playing scene transition
    public void SwitchScene(string name) {
        scenetransition.DOScaleY(2.5f, 1f).SetEase(Ease.InOutExpo).OnComplete(() => {
            SceneManager.LoadScene(name);
        });
    }

    public void ToggleKeyboard() {
        if(keyboardtransform.anchoredPosition.y < 0f) {
            //in down position, move up
            keyboardtransform.DOAnchorPosY(95f, keyboardanimatetime).SetEase(Ease.InOutSine);
        } else if(keyboardtransform.anchoredPosition.y > 0f) {
            //in up position, move down
            keyboardtransform.DOAnchorPosY(-95f, keyboardanimatetime).SetEase(Ease.InOutSine);
        }
    }

    void ShakeLine() {
        RectTransform t = boardlayout[boardlayout.Count - 1].lineGameObject.GetComponent<RectTransform>();
        RectTransform tbg = (RectTransform)boardbg.GetChild(boardlayout.Count - 1);
        if(shakeLineTween == null) {
            shakeLineTween = t.DOShakeAnchorPos(lineShakeDuration, new Vector3(lineShakeStrength,0,0), lineShakeVibrato, lineShakeRandomness, false, true);
            tbg.DOShakeAnchorPos(lineShakeDuration, new Vector3(lineShakeStrength,0,0), lineShakeVibrato, lineShakeRandomness, false, true);
            return;
        }
        if(!shakeLineTween.IsPlaying()) {
            shakeLineTween = t.DOShakeAnchorPos(lineShakeDuration, new Vector3(lineShakeStrength,0,0), lineShakeVibrato, lineShakeRandomness, false, true);
            tbg.DOShakeAnchorPos(lineShakeDuration, new Vector3(lineShakeStrength,0,0), lineShakeVibrato, lineShakeRandomness, false, true);
            return;
        }
    }

    //displays popup on screen with a message
    void SetPopup(string text) {
        StopCoroutine("fadepopup");
        PlaySound("Error");
        popupstaytime = Time.time + 1f;
        popup.sizeDelta = new Vector2(text.Length*10+35, popup.sizeDelta.y);
        popupimage.color = new Color(popupimage.color.r, popupimage.color.g, popupimage.color.b, 1f);
        popuptext.color = new Color(popuptext.color.r, popuptext.color.g, popuptext.color.b, 1f);
        popuptext.text = text;
        StartCoroutine("fadepopup");

        if(shakePopupTween == null) {
            shakePopupTween = popup.DOShakeAnchorPos(lineShakeDuration*0.5f, new Vector3(lineShakeStrength/2f,lineShakeStrength/2f,lineShakeStrength/2f), lineShakeVibrato, lineShakeRandomness, false, true);
            return;
        }
        if(!shakePopupTween.IsPlaying()) {
            shakePopupTween = popup.DOShakeAnchorPos(lineShakeDuration*0.5f, new Vector3(lineShakeStrength/2f,lineShakeStrength/2f,lineShakeStrength/2f), lineShakeVibrato, lineShakeRandomness, false, true);
            return;
        }
    }

    //fade out popup after a set amount of time
    IEnumerator fadepopup() {
        yield return new WaitUntil(() => Time.time >= popupstaytime);
        while(popupimage.color.a > 0f) {
            popupimage.color = new Color(popupimage.color.r, popupimage.color.g, popupimage.color.b, popupimage.color.a-0.04f);
            popuptext.color = new Color(popuptext.color.r, popuptext.color.g, popuptext.color.b, popuptext.color.a-0.04f);
            yield return new WaitForSeconds(0.02f);
        }
    }

    //reset first time flag on application quit
    private void OnApplicationQuit() {
        PlayerPrefs.DeleteKey("firsttime");
    }

    //switches between leaderboard and stats screens of end game panel
    public void ToggleLeaderboardPanel() {
        if(endgameleaderboardpanel.activeInHierarchy) {
            endgameleaderboardpanel.SetActive(false);
            endgameinfopanel.SetActive(true);
        } else {
            endgameinfopanel.SetActive(false);
            endgameleaderboardpanel.SetActive(true);
        }
    }

    public void SubmitToLeaderboard() {
        //add guesses to leaderboard if username is valid
        bool validusername = CheckUsername(usernameinput.text);
        if(!validusername) {
            return;
        }
        StartCoroutine(SubmitToLeaderboardRoutine());
    }

    IEnumerator SubmitToLeaderboardRoutine() {
        //set desired username
        playFabManager.SetDisplayNameForUser(dateusername+usernameinput.text);
        PlayerPrefs.SetString("username", usernameinput.text);
        yield return new WaitUntil(() => playFabManager.setusername);
        playFabManager.setusername = false;
        StartCoroutine(GetLeaderboard());
        submitscorepanel.SetActive(false);
        onleaderboard = true;
        //submit to leaderboard and update leaderboard display UI
        playFabManager.AddLeaderboard("Guesses", 10-(boardlayout[boardlayout.Count-1].boxes.Count == 0 ? boardlayout.Count-1 : boardlayout.Count));
        yield return new WaitUntil(() => playFabManager.added);
        playFabManager.added = false;
    }

    bool CheckUsername(string username) {
        //check if username is valid
        if(username == "") {
            SetPopup("Username Cannot Be Empty");
            return false;
        } else if(username.Length < 3) {
            SetPopup("Username Is Too Short");
            return false;
        } else if(onlyDigits(username) != "") {
            SetPopup("Username Cannot Contain Numbers");
            return false;
        } else if(profanity.Contains(username)) {
            SetPopup("Username Cannot Contain Profanity");
            return false;
        } else if(Array.Exists(leaderboardusernames, x => x == username)) {
            SetPopup("Username Is Taken");
            return false;
        }
        return true;
    }

    public void RefreshLeaderboard() {
        StartCoroutine(GetLeaderboard());
    }

    public void PlaySound(string name) {
        AudioManager.instance.PlayOneShot(name);
    }
}
