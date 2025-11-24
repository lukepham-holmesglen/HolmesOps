using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class Highscore : MonoBehaviour
{
    private string filePath;
    public List<int> topScores = new List<int>();

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "highscore.text");
        LoadScores();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddScore(int score)
    {
        topScores.Add(score);

        // sort score high to low
        topScores.Sort((a, b) => b.CompareTo(a));

        //keep the top 10
        if(topScores.Count > 10)
        {
            topScores = topScores.GetRange(0, 10);
        }
        SaveScore();
    }
    
    private void SaveScore()
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach(int score in topScores)
            {
                writer.WriteLine(score);
            }
        }
    }

    private void LoadScores()
    {
        topScores.Clear();

        if(!File.Exists(filePath))
        {
            return;
        }
       
        //We know filepath exists
        string[] lines = File.ReadAllLines(filePath);

        foreach(string line in lines)
        {
            if (int.TryParse(line, out int score))
            {
                topScores.Add(score);
            }
        }

        topScores.Sort((a, b) => b.CompareTo(a));

        if(topScores.Count > 10)
        {
            topScores = topScores.GetRange(0, 10);
        }
    }
}
