using System.Collections.Generic;
// system.io is to use things inside of windows (system files) this will help make the .txt file
using System.IO;

using UnityEngine;

public class Highscores : MonoBehaviour
{

    // string with file path
    private string filepath;
    // list of top scores, make public to modify it, list of integer values (score is always a whole number)
    public List<int> topScores = new List<int>();

    private void Awake()
    {
        // set file path, persistent data path is in C:\Users\<this user name>\AppData\LocalLow\<company name>
        filepath = Path.Combine(Application.persistentDataPath, "highscores.txt");
        LoadScores();

    }

    // Start is called once before the first execution of update after the MonoBehaviour is created
    // J. Ryan High Score Tutorial in class
    // Start gets called once during update, awake gets called every time 
    void Start()
    {


    }


    // Update is called once per frame
    void Update()
    {


    }

    // Add function to add score to high score table
    //"score" is defined as the score in another script
    public void AddScore(int score)
    {
       // Every score gets added to the high score table
        topScores.Add(score);

        // sort high to low
        topScores.Sort((a,b) => b.CompareTo(a));

        //keep the top 10
        if(topScores.Count > 10)
        {

            topScores = topScores.GetRange(0, 10);
        }

        // Function to save
        SaveScore();
        
    }
    
    private void SaveScore()

    {
        // writes files that aren't a part of Unity itself.  .txt files and .json files need StreamWriter
        using (StreamWriter writer = new StreamWriter(filepath))
        {
            // dealing with just the scores
            foreach(int score in topScores)
            {
                writer.WriteLine(score);
            }


        }
    }

    private void LoadScores()
    {
        topScores.Clear();
        // make sure there is a file to load from

        if (!File.Exists(filepath))
        {
            // if nothing exist then nothing will break
            return;
        }

        //the file exists when the game is saved for the first time and the file can be read
        // string array to separate the lines of scores to distinguish them, if just a string it'd turn it into one long number
        // array " [] " makes a list
        string[] lines = File.ReadAllLines(filepath);

        // convert from string array to list to be used in other places
        foreach (string line in lines)
        {
            if (int.TryParse(line, out int score))
            {
                topScores.Add(score);
            }
        }

        topScores.Sort((a, b) => b.CompareTo(a));

        if (topScores.Count > 10)
        {
            topScores = topScores.GetRange(0, 10);
        }

    }

}

