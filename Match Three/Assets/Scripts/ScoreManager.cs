using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
   private static int hScore;

   #region Singleton

   private static ScoreManager _instance = null;
   
   public static ScoreManager Instance
   {
       get{
           if(_instance == null)
           {
                _instance = FindObjectOfType<ScoreManager>();

                 if(_instance == null) 
                 {
                     Debug.LogError("Fatal Error: ScoreManager not Found!");
                 }
           } 
           return _instance;
       }
   }

   #endregion

   public int tileRatio;
   public int comboRatio;

   public int HighScore {get {return hScore;}}
    public int CurrentScore{get {return currScore;}}

    private int currScore;

    public void ResetCurrScore()
    {
        currScore = 0;
    }

    public void IncrementCurrScore(int tileCount, int comboCount)
    {
        currScore +=(tileCount * tileRatio) * (comboCount * comboRatio);

        SoundManager.Instance.PlayScore(comboCount > 1);
    }

    public void SetHighScore()
    {
        hScore = currScore;
    }

    private void Start()
    {
        ResetCurrScore();
    }

}
