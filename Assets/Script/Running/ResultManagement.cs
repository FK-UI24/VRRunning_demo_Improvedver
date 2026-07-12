using TMPro;
using UnityEngine;

public class ResultManagement : MonoBehaviour
{
    [Header("時間表示テキスト")]
    [SerializeField] private GameObject TimeText;
    [Header("距離表示テキスト")]
    [SerializeField] private GameObject DistanceText;
    [Header("消費カロリー表示テキスト")]
    [SerializeField] private GameObject CalorieText;
    [Header("平均速度")]
    [SerializeField] private GameObject AvgSpeedText;
    [Header("平均傾斜")]
    [SerializeField] private GameObject AvgInclineText;

    void Start()
    {
        int h = (int)(Running_Management.goalTime / 3600);
        int m = (int)(Running_Management.goalTime / 60) % 60;
        int s = (int)(Running_Management.goalTime % 60);



        TimeText.GetComponentInChildren<TMP_Text>().text =
            string.Format("{0:00}:{1:00}:{2:00}", h, m, s);

        DistanceText.GetComponentInChildren<TMP_Text>().text =
            Running_Management.goalDistance.ToString("F1") + "m";

        CalorieText.GetComponentInChildren<TMP_Text>().text =
            Running_Management.goalCalorie.ToString("F1") + "kcal";

        AvgSpeedText.GetComponentInChildren<TMP_Text>().text =
            Running_Management.avgSpeed.ToString("F1") + "km/h";

        AvgInclineText.GetComponentInChildren<TMP_Text>().text =
            Running_Management.avgIncline.ToString("F1") + "°";

    }
}
