using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
   #region Singleton

   private static GameFlowManager _instance = null;

   public static GameFlowManager Instance
   {
       get{
           if(_instance == null)
           {
                _instance = FindObjectOfType<GameFlowManager>();
                if(_instance == null) Debug.LogError("Fatal Error: GameFlowManager not Found!");
           } 

           return _instance;
       }
   }
   #endregion


    [Header("UI")]
    public UIGameOver GameOverUI;
    public bool IsGO {get { return isGO;}}
   private bool isGO = false;

    private void Start()
    {
        isGO = false;
    }

    public void GO()
    {
        isGO = true;
        ScoreManager.Instance.SetHighScore();
        GameOverUI.Show();
    }



}
