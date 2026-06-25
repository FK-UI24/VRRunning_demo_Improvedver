using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Script_SettingManagement : MonoBehaviour
{
    [Header("合計距離表示テキスト")]
    [SerializeField] private TMP_Text totalDistanceText;

    [Header("ウェイポイント数表示テキスト")]
    [SerializeField] private TMP_Text waypointText;

    [Header("1秒間に回転する角度を決めるスライダー")]
    [SerializeField] private Slider smoothSlider;

    [Header("1秒間に回転する角度表示テキスト")]
    [SerializeField] private TMP_Text smoothText;

    //ランニングシーンで参照し、設定を反映させるための変数
    //このシーンではトグルは全て「初期値はFalse」にしておく

    //センターマーカー表示（true=あり、false=なし）
    public static bool UseCenterMarker = false;

    //フレーム表示（true=あり、false=なし）
    public static bool UseFlame = false;

    //カメラを滑らかに回転させるか（true=滑らかに回転、false=一瞬で切り替わる）
    public static bool UseSmooth = false;

    //１秒間に回転する角度を入れる変数
    public static int SmoothValue = 130;

    private void Start()
    {
        //合計距離を反映する
        totalDistanceText.text = Script_WaypointManagement.totalDistance.ToString("F1") + "m";


        //ルートを保存するフォルダがあるかの確認
        string routeDirectory = Path.Combine(Application.persistentDataPath, "RouteData");
        //「RouteData」フォルダがなければ生成する
        if (!Directory.Exists(routeDirectory)) Directory.CreateDirectory(routeDirectory);

        //ファイルのパスを一時的に格納する変数
        string RouteJsonFile;

        //上記のフォルダ内にルートを保存するためのJsonファイルがあるか確認
        RouteJsonFile = Path.Combine(routeDirectory, "routeData.json");
        //「routeData.json」ファイルがなければ生成する
        //usingを使わないとファイルハンドリングが解放されず例外が起きる
        if (!File.Exists(RouteJsonFile)) using (File.Create(RouteJsonFile)) { }

        //JSONを文字列で読み込む
        string json = File.ReadAllText(RouteJsonFile);

        //ウェイポイント数を格納する変数
        int count = 0;

        if (!string.IsNullOrEmpty(json)) count = json.Split('"').Length / 2;

        waypointText.text = count.ToString() + "個";


        //角度スライダーのint変換値をテキストに反映する
        smoothText.GetComponentInChildren<TMP_Text>().text = ((int)smoothSlider.value).ToString();
    }

    //それぞれのトグルやスライダーの値を変数に反映する関数群
    public void OnCenterMarkarToggle()
    {
        UseCenterMarker = !UseCenterMarker;
    }
    public void OnFlameToggle()
    {
        UseFlame = !UseFlame;
    }
    public void OnSmoothToggle()
    {
        UseSmooth = !UseSmooth;
    }
    public void changeSmoothSlider()
    {
        SmoothValue = (int)smoothSlider.value;
        smoothText.text = SmoothValue.ToString();
    }

    //「次に」ボタンを押したときに念のためデバッグ文を表示する
    public void OnNext()
    {
        Debug.Log("\nCenter:" + UseCenterMarker + "\nFrame:" + UseFlame + "\nSmooth:" + UseSmooth + "\nSmoothValue:" + SmoothValue);
    }

}
