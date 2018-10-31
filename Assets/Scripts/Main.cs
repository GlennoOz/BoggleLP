using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
//using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;


public class Main : MonoBehaviour

{


    static string theLetters = "";
    static List<string> theDictionary = new List<string>();
    static List<string> theDiceLetterSets = new List<string>();
    static List<string> solvedLegalWords = new List<string>();
    static Dice[,] puzzleBoard = new Dice[4, 4];
    static List<Dice> diceStorage = new List<Dice>();
    static List<Transform> dicePreFabStorage = new List<Transform>();

    public GameObject diceTray;
    public GameObject TimeField;
    public GameObject ResultsField;
    public GameObject TheInputField;

    public GameObject startButton;
    public GameObject restartButton;

    static int gameTime; //game time in minutes
    static Stopwatch gameTimer = new Stopwatch();
    static DateTime startTime;
    static DateTime endTime;
    static TimeSpan timeLeft;
    public bool gameStarted = false;

    // Use this for initialization
    void Start()
    {
        int width = 800; 
        int height = 600; 
        bool isFullScreen = false; 
        int desiredFPS = 60; 

        Screen.SetResolution(width, height, isFullScreen, desiredFPS);
    }

    // Update is called once per frame
    void Update()
    {
        if (gameStarted)
        {
            displayTimer();
            checkTimeLeft();
        }
    }




    //static void Start(string[] args)
    public void StartGame(int aTimeInMinutes)
    {
        startButton.SetActive(false);

        gameTime = aTimeInMinutes;

        //Initialisation stage
        SuckInDiceLetterSets();

        InitialiseDice();

        SuckInDictionaryFile();


        //setup UI
        gameStarted = true;
        startTimer(gameTime);


        //solve which words are present
        InPlayCheckWords();
    }

    public void ReplayGame(int aTimeInMinutes)
    {
        solvedLegalWords = new List<string>();
        theDictionary = new List<string>();

        InitialiseDice();
        SuckInDictionaryFile();

        //re-setup UI
        playAgain();

        gameStarted = true;
        startTimer(gameTime);

        //solve which words are present
        InPlayCheckWords();
    }

    public void quitGame()
    {
        Application.Quit();
    }

    public void checkTimeLeft()
    {
        timeLeft = endTime - DateTime.Now;
        if (timeLeft < TimeSpan.Zero)
        {
            gameStarted = false;
            scoreGame();
        }
    }


    static void startTimer(int aGameTime)
    {

        startTime = DateTime.Now;

        endTime = startTime.AddMinutes(aGameTime);
    }

    public void displayTimer()
    {
        timeLeft = endTime - DateTime.Now;

        string displayTime = String.Format("{0:00}:{1:00}",
        timeLeft.Minutes,
        timeLeft.Seconds);

        //LogSomething("Time left: " + timeLeft);
        TimeField.GetComponent<UnityEngine.UI.Text>().text = "Time left: " + displayTime;
    }

    static void stopTimer()
    {
        gameTimer.Stop();
    }

    static void LogSomething(string aString)
    {
        //disabled logging fundtion
        //UnityEngine.Debug.Log(aString);
    }

    static void LogSomething()
    {
        //disabled logging fundtion
        // UnityEngine.Debug.Log(".");
    }

    static void InPlayCheckWords()
    {

        //Stopwatch stopwatch = new Stopwatch();
        //stopwatch.Start();

        foreach (string aPossibleWord in theDictionary)
        {
            if (checkWordVSGrid(aPossibleWord))
            {
                //LogSomething("FOUND " + aPossibleWord);
                solvedLegalWords.Add(aPossibleWord);
                LogSomething();
            }
        }

        //stopwatch.Stop();
        //long elapsed = stopwatch.ElapsedMilliseconds;
        //LogSomething("The time to check for words " + elapsed + " milliseconds");

    }

    static bool checkWordVSGrid(string wordToCheck)
    {
        //Checking for initial letter of word in a top left to bottom right pattern
        //LogSomething("Checking " + wordToCheck + " ");
        for (int y = 0; y <= 3; y++)
        {
            for (int x = 0; x <= 3; x++)
            {

                if (puzzleBoard[x, y].myLetter.ToLower() == wordToCheck[0].ToString())
                {
                    //Once the initial letter of the word is matched, then send it off to be recursively checked, relative to the x,y grid location
                    puzzleBoard[x, y].usedThisDice();
                    if (recursiveCheckWord(wordToCheck.ToLower(), 1, x, y) == true)
                    {
                        //if the recursive check returns true at this stage, then all the letters of the word were found
                        resetDice();
                        return true;
                    }
                    else
                    {
                        //if its false at this stage, then the word is not an acceptable word
                        resetDice();  //simply resetting the 'alredayUsed' flag for each dice
                    }

                }
            }
        }
        //if the first letter was not found in the grid
        LogSomething();
        return false;
    }

    static bool recursiveCheckWord(string aWordCandidate, int currentIndex, int currentX, int currentY)
    {


        string nextCharacter = aWordCandidate[currentIndex].ToString();

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int tempXCoord = currentX + x;
                int tempYCoord = currentY + y;
                if ((tempXCoord >= 0 & tempXCoord < 4) & (tempYCoord >= 0 & tempYCoord < 4) & (tempXCoord != currentX || tempYCoord != currentY))
                {
                    string tString = puzzleBoard[tempXCoord, tempYCoord].myLetter.ToLower();

                    if ((puzzleBoard[tempXCoord, tempYCoord].myLetter.ToLower() == nextCharacter) & (puzzleBoard[tempXCoord, tempYCoord].checkUsed() == false))
                    {
                        //ok to carry on
                        puzzleBoard[tempXCoord, tempYCoord].usedThisDice();
                        if (currentIndex + 1 == aWordCandidate.Length)
                        {
                            //reached the end of the word, so its a goer
                            return true;
                        }
                        else
                        {
                            //found the current letter(which is not the last letter), so keep recursively checking
                            currentIndex++;
                            return (recursiveCheckWord(aWordCandidate, currentIndex, tempXCoord, tempYCoord));
                        }

                    }
                }
            }
        }
        //didnt match the current letter of the word in any of the surrounding grid cells
        return false;
    }


    static void resetDice()
    {
        foreach (Dice aDice in diceStorage)
        {
            aDice.resetDice();
        }
    }

    public void scoreGame()
    {
        int score = 0;

        string theUsersWords = TheInputField.GetComponent<UnityEngine.UI.InputField>().text;
        string[] userWordsList = theUsersWords.Split('\n');

        //check for words in solutions list
        foreach (string solutionWord in solvedLegalWords)
        {

            int pos = Array.IndexOf(userWordsList, solutionWord);
            if (pos > -1)
            {
                score = score + wordScore(solutionWord);
                userWordsList[pos] = "<color=green>" + solutionWord + "</color>";
            }
        }


        //display colourized user words/results
        TheInputField.GetComponent<UnityEngine.UI.InputField>().text = "";
        string resultsWords = "";
        foreach (string aString in userWordsList)
        {
            resultsWords = resultsWords + aString + "\n";
        }
        TheInputField.GetComponent<UnityEngine.UI.InputField>().text = resultsWords;
        ResultsField.GetComponent<UnityEngine.UI.Text>().text = "Well done, your score is:" + score;

        restartButton.SetActive(true);
    }

    public void playAgain()
    {
        ResultsField.GetComponent<UnityEngine.UI.Text>().text = "";
        TheInputField.GetComponent<UnityEngine.UI.InputField>().text = "";
    }

    public int wordScore(string aCorrectWord)
    {

        //scroring
        //3 Letters: 1 point
        //4 Letters: 1 point
        //5 Letters: 2 points
        //6 Letters: 3 points
        //7 Letters: 4 points
        //8 or More Letters: 11 points
        LogSomething(aCorrectWord);
        switch (aCorrectWord.Length)
        {
            case 3:
                return 1;
                break;
            case 4:
                return 1;
                break;
            case 5:
                return 2;
                break;
            case 6:
                return 3;
                break;
            case 7:
                return 4;
                break;
            default:
                return 11;
                break;
        }
    }


    static void SuckInDiceLetterSets()
    {
        string[] stringSeparators = new string[] { "\r\n" };
        TextAsset diceTextAsset = Resources.Load("diceLetterSets") as TextAsset;

        string[] diceLetterSets = diceTextAsset.text.Split(stringSeparators, StringSplitOptions.None);
        foreach (string aDiceSet in diceLetterSets)
        {
            theDiceLetterSets.Add(aDiceSet);
        }

    }

    static void SuckInDictionaryFile()
    {
        string[] stringSeparators = new string[] { "\r\n" };
        TextAsset theWholeDictionary = Resources.Load("dictionary") as TextAsset;

        string[] dictionaryWords = theWholeDictionary.text.Split(stringSeparators, StringSplitOptions.None);
        int totalInitialWordCount = 0;

        foreach (string aWord in dictionaryWords)
        {

            totalInitialWordCount++;
            //checking if its even a valid word to be checking
            if (checkDictWordViable(aWord))
            {
                //add it to our dictionary list if it passed all checks
                theDictionary.Add(aWord);
            }
        }

        LogSomething("The INITIAL number of words in our dictionary is " + totalInitialWordCount);
        LogSomething("The FINAL number of words in our dictionary is " + theDictionary.Count);

    }



    void InitialiseDice()
    {

        //diceTray
        // GameObject dicePrefab = Instantiate(Resources.Load("DicePrefab"), new Vector3(100,100,100), Quaternion.identity) as GameObject;
        GameObject DiceTrayGO = GameObject.Find("DiceTray");

        int simpleCounter = 0;
        Transform dicePrefab;
        theLetters = "";

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {

                if (dicePreFabStorage.Count != 16)
                {
                    Dice aTempDice = new Dice();
                    puzzleBoard[x, y] = aTempDice;
                    diceStorage.Add(aTempDice);
                    

                    //create an instance of the DicePrefab resource
                    dicePrefab = Instantiate((Resources.Load("DicePrefab") as GameObject).transform, new Vector3(0, 0, 0), Quaternion.identity);

                    dicePrefab.transform.parent = DiceTrayGO.transform;
                    dicePrefab.localPosition = new Vector3((x * 1.5f), 0, (y * -1.5f));
                    dicePreFabStorage.Add(dicePrefab);

                }
                else
                {
                    dicePrefab = dicePreFabStorage[simpleCounter];
                    //puzzleBoard[x, y].setLetterSet();
                }
                theLetters = theLetters + puzzleBoard[x, y].setLetterSet(theDiceLetterSets[simpleCounter]);
                dicePrefab.GetComponentInChildren<TextMesh>().text = puzzleBoard[x, y].myLetter;
                simpleCounter++;
            }
            LogSomething();
        }

    }

    static bool checkDictWordViable(string theWord)
    {
        //initial check to remove short words from our dictionary
        //LogSomething("checkDictWordViable checking " + theWord);
        if (theWord.Length < 3)
        {
            //too short
            return false;
        }
        else
        {
            for (int i = 0; i < theWord.Length; i++)
            {
                if (theLetters.ToLower().Contains(theWord[i]) == false)
                {
                    //the word contains a letter which is NOT a character/letter in our puzzle");
                    return false;
                }
            }
            //LogSomething("at least one of the letters " + theWord + "are in our puzzle, and its 3 or longer in length");
            return true;
        }


        //started out checking if the dictionaries word contained at least one of the letters in our puzzle//
        //the new way, checking the letters in the word for if they are in the puzzle letters. (ie. the other way round//
        //also allows us to get rid of some words that contain rubbish/corrupt/unknown characters//

        //if (theWord.ToLower().Contains("!"))
        //{
        //    LogSomething("The word " + theWord + " contains illegal characters");
        //    return false;
        //}

        //for (int i = 0; i < theLetters.Length; i++)
        //{
        //    //LogSomething("The index = "+i);
        //    if (theWord.ToLower().Contains(theLetters[i]) != false) {
        //        //LogSomething("The letter " + theLetters[i] + " IS in the word " + theWord+ i);
        //        return true;
        //    }
        //}
        //LogSomething("NONE of theLetters are in the word "+theWord);
        //return false;

    }

    class Dice
    {
        private String myLetters;
        public String myLetter;
        private bool alreadyUsed = false;



        public string setLetterSet(String someLetters)
        {
            myLetters = someLetters;
            getRandomletter();
            return myLetter;
        }

        public string setLetterSet()
        {
            getRandomletter();
            return myLetter;
        }

        private void getRandomletter()
        {
            UnityEngine.Random rnd = new UnityEngine.Random();
            int theIndex = UnityEngine.Random.Range(0, myLetters.Length);
            myLetter = myLetters[theIndex].ToString();
        }

        public bool checkUsed()
        {
            return alreadyUsed;
        }

        public void usedThisDice()
        {
            alreadyUsed = true;
        }

        public void resetDice()
        {
            alreadyUsed = false;
        }

    }


}
