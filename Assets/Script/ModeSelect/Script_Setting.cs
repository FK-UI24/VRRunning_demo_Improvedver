using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static OVRHaptics;

public class Script_Setting : MonoBehaviour
{
    [Header("ステータス表示テキスト")]
    [SerializeField] private TMP_Text statusText;

    [Header("接続テストボタン")]
    [SerializeField] private GameObject connectTestButton;

    [Header("キャリブレーションボタン")]
    [SerializeField] private GameObject calibrationButton;

    [Header("傾斜測定ボタン")]
    [SerializeField] private GameObject measureInclineButton;

    [Header("ルートシーン遷移ボタン")]
    [SerializeField] private GameObject routeButton;

    [Header("キャリブレーション：既定速度")]
    [SerializeField] private float targetSpeed;

    [Header("キャリブレーション：閾値")]
    [SerializeField] private float stabilityThreshold;

    [Header("キャリブレーション：Unityでの数値更新間隔\n(これはあくまでUnity側での表示間隔。実際の間隔はサーバー側に記入)")]
    [SerializeField] private float calibrationStatusInterval;

    [Header("キャリブレーション：最後にキャリブレーションした日を表示するテキスト")]
    [SerializeField] private TMP_Text currentCalibrationDataText;

    [Header("傾斜測定：Unityでの数値更新間隔")]
    [SerializeField] private float measureInclineInterval;


    //何かしらの機能が動いているかを判断する用変数
    private bool isTesting = false;

    //IPアドレスとポート番号を参照する用変数
    private Script_IP ipConfig;

    //URLのベース部分を格納する変数
    private string baseUrl;

    //SEを格納する用変数
    private AudioSource SE;

    //キャリブレーションが正しく終わったかの判定用変数
    private bool isCalibrationNormalEnd = false;

    //キャリブレーションのコルーチン保持用変数
    private Coroutine calibrationCoroutine;

    //キャリブレーションの実行中判断する用変数
    private bool isCalibration = false;

    //コルーチン保持用
    private Coroutine measureCoroutine;


    private void Start()
    {
        //ステータスメッセージと接続テストボタンの初期設定
        statusText.text = "ここに右のボタンの結果とかが表示されるよ！";
        connectTestButton.GetComponentInChildren<TMP_Text>().text = "Connection Check";

        //ipConfigにIPConfigを参照する
        ipConfig = Resources.Load<Script_IP>("IPConfig");

        //ベースURLを格納する（http:// IPアドレス : 5000）
        baseUrl = "http://" + ipConfig.ipaddress + ":" + ipConfig.port;

        //SEを格納する
        SE = GetComponent<AudioSource>();

        //キャリブレーションのデータがあるかを確認する
        if (PlayerPrefs.HasKey("CalibrationDate"))
        {
            currentCalibrationDataText.text = "最後にキャリブレーションをした日：" + PlayerPrefs.GetString("CalibrationDate");
            routeButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            currentCalibrationDataText.text = "「Calibration」を押して設定して！";
            routeButton.GetComponent<Button>().interactable = false;
        }


    }

    //接続テスト関数
    public void OnConnectTest()
    {
        //もしなにも機能が動いていなかったら
        if (isTesting == false)
        {
            //SEを鳴らす
            SE.Play();

            //実行したら他のボタンを押せないようにする
            falseOtherButton(1);

            StartCoroutine(ConnectTestCoroutine());

        }
    }

    //接続テスト用コルーチン
    private IEnumerator ConnectTestCoroutine()
    {
        //実行を始めたので切り替える
        isTesting = true;

        //URLを作る
        string url = baseUrl + "/connecttest";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            statusText.text = "接続中...";

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                statusText.text = "接続できているよ！";
            }
            else
            {
                statusText.text = "エラー：" + www.error;
            }

            //接続テストが終わったらボタンを有効化する
            trueOtherButton(1);

            //実行が終わったので切り替える
            isTesting = false;
        }
    }

    //キャリブレーション用関数
    public void OnCalibration()
    {
        //もしなにも機能が動いていないかつキャリブレーション中でなかったら
        if (isTesting == false && isCalibration == false)
        {
            //SEを鳴らす
            SE.Play();

            //ステータステキストとキャリブレーションボタンの初期設定
            statusText.text = "接続中...";
            calibrationButton.GetComponentInChildren<TMP_Text>().text = "Calibration";

            //キャリブレーション情報保存用ファイルパス
            string calibrationFile = Path.Combine(Application.persistentDataPath, "calibrationSetting.json");

            //キャリブレーション情報保存用ファイルを確認してなければ生成する
            if (!File.Exists(calibrationFile))
            {
                File.Create(calibrationFile);
                Debug.Log("キャリブレーション情報保存用ファイルを生成した");
            }
            else Debug.Log("キャリブレーション情報保存用ファイルはすでに存在する");

            //実行したらボタンの文字を「Abort」にする
            calibrationButton.GetComponentInChildren<TMP_Text>().text = "Abort";

            //実行したら他のボタンを押せないようにする
            falseOtherButton(2);

            //コルーチンを開始する
            calibrationCoroutine = StartCoroutine(StartCalibrationCoroutine());
            StartCoroutine(ResultCalibrationCoroutine());
        }
        //もしキャリブレーションを実行していたら
        else if (isCalibration == true)
        {
            //もしコルーチンが実行していたら
            if (calibrationCoroutine != null)
            {
                //SEを鳴らす
                SE.Play();

                //停止要求を送る。StopCoroutineは行わずにサーバーの完了を待つ
                StartCoroutine(StopCalibrationCoroutine());
            }
            //ステータステキストの更新
            statusText.text = "キャリブレーションを中断したよ！";

            //ボタンを有効化する
            trueOtherButton(2);
        }
    }

    //キャリブレーション開始用コルーチン
    private IEnumerator StartCalibrationCoroutine()
    {
        //キャリブレーションを始めるので切り替える
        isCalibration = true;

        //フォームを作り、既定速度と閾値を格納する
        WWWForm form = new WWWForm();
        form.AddField("target_speed", targetSpeed.ToString());
        form.AddField("stability_threshold", stabilityThreshold.ToString());

        //URLを作る
        string starturl = baseUrl + "/start_calibration";

        //POST送信
        using (UnityWebRequest StartReq = UnityWebRequest.Post(starturl, form))
        {
            //サーバーからの応答を待つ
            yield return StartReq.SendWebRequest();

            //もしステータスコードが200でないなら
            if (StartReq.result != UnityWebRequest.Result.Success)
            {
                //ボタンの文字を戻す
                calibrationButton.GetComponentInChildren<TMP_Text>().text = "Calibration";

                //キャリブレーション中に無効化したボタンを戻す
                trueOtherButton(2);

                //ステータステキストの更新をする
                statusText.text = "通信エラー：" + StartReq.error;

                //キャリブレーションが終わったので切り替える
                isCalibration = false;

                //おわり
                yield break;
            }

            //キャリブレーション中の状態を定期取得する
            while (true)
            {
                //URLを作る
                string statusURL = baseUrl + "/calibration_status";

                //GETで受け取る
                UnityWebRequest statusReq = UnityWebRequest.Get(statusURL);

                //サーバーからの応答を待つ
                yield return statusReq.SendWebRequest();

                //もしステータスコードが200なら
                if (statusReq.result == UnityWebRequest.Result.Success)
                {
                    //受け取った結果をステータステキストに入れる
                    string currentStatus = statusReq.downloadHandler.text;
                    statusText.text = currentStatus;

                    //もし結果に「終了」が含まれていたら
                    if (currentStatus.Contains("終了"))
                    {
                        //ボタンの文字を戻す
                        calibrationButton.GetComponentInChildren<TMP_Text>().text = "Calibration";

                        //ボタンを有効にする
                        trueOtherButton(2);

                        //正常終了なので切りかえる
                        isCalibrationNormalEnd = true;

                        //正常終了のタイミングを保存する
                        string today = DateTime.Now.ToString("yyyy/MM/dd");
                        PlayerPrefs.SetString("CalibrationDate", today);
                        PlayerPrefs.Save();

                        //最新キャリブレーション日の表示を更新する
                        if (PlayerPrefs.HasKey("CalibrationDate"))
                        {
                            currentCalibrationDataText.text = "最後にキャリブレーションをした日：" + PlayerPrefs.GetString("CalibrationDate");
                            routeButton.GetComponent<Button>().interactable = true;
                        }
                        else
                        {
                            currentCalibrationDataText.text = "「Calibration」を押して設定して！";
                            routeButton.GetComponent<Button>().interactable = false;
                        }


                        break;
                    }
                    else if (currentStatus.Contains("中断"))
                    {
                        //ボタンの文字を戻す
                        calibrationButton.GetComponentInChildren<TMP_Text>().text = "Calibration";

                        //ボタンを有効にする
                        trueOtherButton(2);

                        //キャリブレーション中断
                        break;
                    }
                }
                else
                {
                    //ボタンの文字を戻す
                    calibrationButton.GetComponentInChildren<TMP_Text>().text = "Calibration";

                    //ボタンを有効にする
                    trueOtherButton(2);

                    //ステータス更新
                    statusText.text = "通信エラー：" + statusReq.error;
                }

                //設定した時間停止
                yield return new WaitForSeconds(calibrationStatusInterval);
            }
            //終わったのでフラグとコルーチンを戻す
            isCalibration = false;
            calibrationCoroutine = null;

        }
    }

    //キャリブレーション停止用コルーチン
    private IEnumerator StopCalibrationCoroutine()
    {
        //URLを作る
        string url = baseUrl + "/stop_calibration";

        //フォーム作成
        WWWForm emptyForm = new WWWForm();
        //POST送信
        using (UnityWebRequest stopReq = UnityWebRequest.Post(url, emptyForm))
        {
            yield return stopReq.SendWebRequest();

            //もしステータスコードが200でないなら
            if (stopReq.result != UnityWebRequest.Result.Success)
            {
                //ログにエラーを表示する
                Debug.Log("停止リクエスト失敗：" + stopReq.error);
            }
            else
            {
                //ログに成功を表示する
                Debug.Log("停止リクエスト成功：" + stopReq.downloadHandler.text);
            }

        }

    }

    //キャリブレーション結果を受け取る用リクエスト
    private IEnumerator ResultCalibrationCoroutine()
    {
        //キャリブレーション中は待ち続ける
        while (isCalibration)
        {
            yield return null;
        }

        //もしキャリブレーションが正常終了したら
        if (isCalibrationNormalEnd)
        {
            //URLを作る
            string url = baseUrl + "/calibration_result";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    //結果を格納する
                    string avg_steps = req.downloadHandler.text;
                    Debug.Log(targetSpeed + ":" + avg_steps);

                    //キャリブレーション情報保存用ファイルパス
                    //順当に行くと既にファイルは存在してるので確認はしない
                    string calibrationFile = Path.Combine(Application.persistentDataPath, "calibrationSetting.json");

                    //target_speedをキーにしたavg_stepsを値にしたjsonの定型文を作る
                    string saveJson = "{ \"" + targetSpeed + "\": " + avg_steps + " }";

                    //ファイルに書き込み（毎回完全上書きするので事前に中身の参照をしない）
                    File.WriteAllText(calibrationFile, saveJson);
                    Debug.Log("基準速度をキーとしたときの平均ステップ数を保存した");

                }
                else
                {
                    Debug.LogError(req.error);
                }

                //正常に終了したフラグを戻す
                isCalibrationNormalEnd = false;


            }
        }
    }

    //傾斜測定用関数
    public void OnMeasureIncline()
    {
        if (isTesting == false)
        {
            //SEを鳴らす
            SE.Play();

            //ステータステキストと計測ボタンの初期設定
            statusText.text = "接続中";
            measureInclineButton.GetComponentInChildren<TMP_Text>().text = "Measure Incline";

            //実行したらボタンの文字を「Abort」に変更する
            measureInclineButton.GetComponentInChildren<TMP_Text>().text = "Abort";

            //他のボタンを無効化する
            falseOtherButton(3);

            //コルーチンを開始
            measureCoroutine = StartCoroutine(StartMeasure());
        }
        else
        {
            if (measureCoroutine != null)
            {
                //SEを鳴らす
                SE.Play();

                //Flaskに終了要求を送る
                StartCoroutine(StopMeasure());
                //既に動いているコルーチンを停止する
                StopCoroutine(measureCoroutine);
                //既に動いていたコルーチンをnullにする
            }
            //ステータスを更新する
            statusText.text = "計測を終了しました";
            //計測中のフラグを戻す
            isTesting = false;
            //ボタンの文字を戻す
            measureInclineButton.GetComponentInChildren<TMP_Text>().text = "Measure Incline";

            //計測中に無効化したボタンを有効化する
            trueOtherButton(3);
        }
    }

    private IEnumerator StartMeasure()
    {
        //切り替える
        isTesting = true;

        //フォーム作成
        WWWForm form = new WWWForm();

        //URLを作る
        string startURL = baseUrl + "/start_mpu6050_get_pitch";


        //POST送信
        using (UnityWebRequest startReq = UnityWebRequest.Post(startURL, form))
        {
            //サーバーからの応答を待つ
            yield return startReq.SendWebRequest();

            //もしステータスコードが200でないなら
            if (startReq.result != UnityWebRequest.Result.Success)
            {
                //ボタンの文字を戻す
                measureInclineButton.GetComponentInChildren<TMP_Text>().text = "Measure Incline";
                //計測中に無効化したボタンを有効化する
                trueOtherButton(3);

                //ステータス更新
                statusText.text = "通信エラー：" + startReq.error;
                isTesting = false;
                //終了
                yield break;
            }
            //計測中の値を定期取得する
            while (true)
            {
                //URLを作る
                string statusURL = baseUrl + "/mpu6050_get_pitch_status";

                //GETで受け取る
                using (UnityWebRequest statusReq = UnityWebRequest.Get(statusURL))
                {
                    //サーバーからの応答を待つ
                    yield return statusReq.SendWebRequest();

                    //もしステータスコードが200なら
                    if (statusReq.result == UnityWebRequest.Result.Success)
                    {
                        //受け取った結果をステータステキストに代入する
                        string cuurentStatus = statusReq.downloadHandler.text;
                        statusText.text = cuurentStatus;
                    }
                    else
                    {
                        //ボタンの文字を戻す
                        measureInclineButton.GetComponentInChildren<TMP_Text>().text = "Measure Incline";
                        //計測中に無効化したボタンを有効化する
                        trueOtherButton(3);
                        //ステータス更新
                        statusText.text = "通信エラー：" + statusReq.error;
                    }
                    //設定した時間停止
                    yield return new WaitForSeconds(measureInclineInterval);

                }

            }
        }
    }

    private IEnumerator StopMeasure()
    {
        //URLを作る
        string url = baseUrl + "/stop_mpu6050_get_pitch";

        //フォーム作成
        WWWForm emptyForm = new WWWForm();
        //POST送信
        using (UnityWebRequest stopReq = UnityWebRequest.Post(url, emptyForm))
        {

            //サーバーからの応答を待つ
            yield return stopReq.SendWebRequest();

            //もしステータスコードが200でないなら
            if (stopReq.result != UnityWebRequest.Result.Success)
            {
                //ログにエラーを表示する
                Debug.Log("終了リクエスト失敗：" + stopReq.error);
            }
            else
            {
                //ログに成功を表示する
                Debug.Log("終了リクエスト成功：" + stopReq.downloadHandler.text);
            }

        }


    }

    //引数の番号以外のボタンを無効化する関数
    //１：接続テスト、２：キャリブレーション、３：傾斜測定
    private void falseOtherButton(int num)
    {
        //引数が１なら接続テストボタン以外を無効化する
        if (num == 1)
        {
            calibrationButton.GetComponent<Button>().interactable = false;
            measureInclineButton.GetComponent<Button>().interactable = false;

        }
        //引数が２ならキャリブレーションボタン以外を無効化する
        else if (num == 2)
        {
            connectTestButton.GetComponent<Button>().interactable = false;
            measureInclineButton.GetComponent<Button>().interactable = false;
        }
        //引数が３なら傾斜測定ボタン以外を無効化する
        else if (num == 3)
        {
            connectTestButton.GetComponent<Button>().interactable = false;
            calibrationButton.GetComponent<Button>().interactable = false;
        }

        //引数が指定外なら何もしない
        else return;
    }

    //引数の番号以外のボタンを有効化する関数
    //１：接続テスト、２：キャリブレーション、３：傾斜測定
    private void trueOtherButton(int num)
    {
        //引数が１なら接続テストボタン以外を有効化する
        if (num == 1)
        {
            calibrationButton.GetComponent<Button>().interactable = true;
            measureInclineButton.GetComponent<Button>().interactable = true;
        }
        //引数が２ならキャリブレーションボタン以外を有効化する
        else if (num == 2)
        {
            connectTestButton.GetComponent<Button>().interactable = true;
            measureInclineButton.GetComponent<Button>().interactable = true;
        }
        //引数が３なら傾斜測定ボタン以外を有効化する
        else if (num == 3)
        {
            connectTestButton.GetComponent<Button>().interactable = true;
            calibrationButton.GetComponent<Button>().interactable = true;
        }
        //引数が指定外なら何もしない
        else return;
    }


}
