using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITime : MonoBehaviour
{
   public Text time;

   private void update()
   {
       time.text = GetTimeString(TimeManager.Instance.GetRemainingTime() +1);
   }

   public void Show()
   {
       gameObject.SetActive(true);
   }

   public void Hide()
   {
       gameObject.SetActive(false);
   }

   private string GetTimeString(float timeRemain)
   {
       int min = Mathf.FloorToInt(timeRemain / 60);
       int sec = Mathf.FloorToInt(timeRemain % 60);

       return string.Format("{0} : {1}",min.ToString(),sec.ToString());
   }
}
