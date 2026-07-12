using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Running_Management : MonoBehaviour
{
    [Header("メインカメラ")]
    [SerializeField] private GameObject cameraObject;
    [Header("センターアイアンカー\n線の更新に使用する")]
    [SerializeField] private GameObject centerEyeAnchorObject;

    [Header("CurrentSttus.csをアタッチしたオブジェクト")]
    [SerializeField] private CurrentStatus cStatus;
    [Header("StartUP_Management.csをアタッチしたオブジェクト")]
    [SerializeField]private StartUP_Management sManagement;

    [Header("速度表示テキスト")]
    [SerializeField] private GameObject speedText;
    [Header("傾斜表示テキスト")]
    [SerializeField] private GameObject inclineText;

    [Header("移動距離表示テキスト")]
    [SerializeField] private GameObject distanceText;
    //手動では7km/hで0.282になった（多少のずれあり）
    [Header("スケール補正係数\n増やすと距離の経過が速くなる\n減らすと距離の経過が遅くなる")]
    [SerializeField] private float distanceScale = 1.0f;

    [Header("経過時間表示テキスト")]
    [SerializeField] private GameObject timerText;

    [Header("カロリー表示テキスト")]
    [SerializeField] private GameObject calorieText;

    [Header("左手のパネル")]
    [SerializeField] private GameObject timerAndCaloriePanel;
    [Header("右手のパネル")]
    [SerializeField] private GameObject rightHansPanel;
    [Header("右手のパネルのPauseテキスト1")]
    [SerializeField] private GameObject pauseText1;

    [Header("速度取得インターバル")]
    [SerializeField] private float getSpeedInterval;
    [Header("速度変化の滑らかさ")]
    [Range(0.01f, 10f)] 
    [SerializeField] private float speedSmoothing = 5f;

    [Header("傾斜取得インターバル")]
    [SerializeField] private float getInclineInterval;
    [Header("速度変化の滑らかさ")]
    [Range(0.01f, 10f)]
    [SerializeField] private float inclineSmoothing = 5f;

    [Header("カメラの回転の有無をインスペクター設定にしないか")]
    [SerializeField] private bool useChoppyRotation;
    [Header("カメラの回転を無効化するか")]
    [SerializeField] private bool ischoppyRotation;
    [Header("1秒あたりに曲がる角度をインスペクター設定にしないか")]
    [SerializeField] private bool useInspectorSmooth;
    [Header("1秒あたりに曲がる角度")]
    [SerializeField] private float maxRotationDPS;
    [Header("カメラの回転の滑らかさ")]
    [Range(0.01f,10f)]
    [SerializeField] private float rotationSmoothing = 5f;

    [Header("StartUpパネル")]
    [SerializeField] private GameObject startUpPanel;
    [Header("Runningパネル")]
    [SerializeField] private GameObject runningPanel;
    [Header("PAUSEパネル")]
    [SerializeField] private GameObject pausePanel;
    [Header("STOPパネル")]
    [SerializeField] private GameObject stopPanel;

    [Header("停止時タイトルテキスト")]
    [SerializeField] private GameObject stopTitleText;
    [Header("停止までのカウントダウン秒数")]
    [SerializeField] private float stopCountdown;
    [Header("停止時カウントダウンテキスト")]
    [SerializeField] private GameObject stopCountdownText;
    [Header("停止時警告テキスト")]
    [SerializeField]private GameObject stopWarningText;
    [Header("リタイア時遷移シーン名")]
    [SerializeField] private string retireScene;

    [Header("ゴール時遷移シーン名")]
    [SerializeField] private string goalScene;

    [Header("開発モード速度")]
    [SerializeField] private float debugSpeed;
    [Header("開発モード傾斜角")]
    [SerializeField] private float debugIncline;

    //カメラのRigidBody格納用変数
    private Rigidbody cameraRb;

    //IPアドレス参照用変数
    private Script_IP config;
    //速度/傾斜取得URL格納用変数
    private string getSpeedURL;
    private string getInclineURL;

    //速度取得コルーチンと傾斜取得コルーチンを格納する用変数
    //コルーチンを停止するときとかに使う
    private Coroutine getSpeedCoroutine;
    private Coroutine getInclineCoroutine;

    // 現在カメラに適用されている速度
    private float currentSpeed;
    // サーバーから取得した実際の速度
    private float targetSpeed;
    //スタート直後に前に一気に進まないようにするためのフラグ
    private bool ignoreFirstSpeedReading = true;

    //現在表示されている傾斜角
    private float currentIncline;
    //サーバーから取得した実際の速度
    private float targetIncline;
    //スタート直後の値は無視する用にいするためのフラグ
    private bool ignoreFirstInclineReading = true;

    //合計走行距離を格納する変数
    private float totalDistance;
    //１フレーム前の座標を格納する変数
    private Vector3 lastPosition;
    //現在のフレームの座標を格納する用変数
    private Vector3 currentPosition;
    //前のフレームと今のフレームの座標の差を記録する
    private float distance;
    //最初のフレームで初期位置を記録するのでそれ用のフラグ
    private bool isDistanceFisrstUpdate = true;

    //経過時間
    private float totalTime;

    //総カロリーを格納する用変数
    private float totalCalorie;
    //プレイヤーの体重格納用変数
    private float UserWeight;

    //線の更新を行う線を格納する用変数
    private LineRenderer lineRenderer;
    //ウェイポイントの座標を格納する用リスト変数
    private List<Vector3> waypointsPos = new List<Vector3>();
    //次の目的地のウェイポイントの座標を保存する用変数
    private Vector3 nextWayPointPos;
    //何個のワイポイントを通過したかのカウント用変数
    private int passWayPointCount = 0;
    //カメラの回転を格納する用変数
    private Quaternion targetRotation;

    //ユーザーの身長を格納する変数
    private float UserHegiht;
    //線の更新を身長から地面の高さ1.0mにして格納する用変数
    private float updateLineHeight;

    //PAUSEパネルをだしているか（PAUSE状態か）のフラグ
    //リタイア処理においてはPAUSEパネルがなくてもtrueのままにする
    //StartUPManagementでPauseパネルとStartUpパネルの交互表示で使うのでpublic
    [HideInInspector]
    public bool isPause = false;

    // 以下は走行中エラー発生時に使う
    // 再接続の選択肢UIを表示したかどうかを管理するフラグ
    private bool isReconnectProcessStarted = false;
    // 現在、再接続を試みている最中かどうかを管理するフラグ
    private bool isAttemptingReconnect = false;

    //ゴール後に完全に停止したかのフラグ
    private bool isGoaledStop = false;

    //平均傾斜を計算する用
    //合計平均傾斜
    private float totalIncline;
    //何回傾斜を取得したかのカウント用
    private int getInclineCount = 0;
    //傾斜を取得するタイミング計算用
    private float getInclineTimer = 0f;

    //結果表示参照用変数
    //合計を取るものはゴール時に一気にとる
    [HideInInspector]
    public static float goalTime = 0f;
    [HideInInspector]
    public static float goalDistance = 0f;
    [HideInInspector]
    public  static float goalCalorie = 0f;
    [HideInInspector]
    public static float avgSpeed= 0f;
    //傾斜を求めるものは常に動かしておく
    [HideInInspector]
    public static float avgIncline = 0f;

    private void Start()
    {
        //速度/傾斜取得用のアドレス等格納
        config = Resources.Load<Script_IP>("IPConfig");
        getSpeedURL = "http://" + config.ipaddress + ":" + config.port + "/get_rotary_speed";
        getInclineURL = "http://" + config.ipaddress + ":" + config.port + "/mpu6050_get_pitch_result";

        //カメラのRigidBodyを格納する
        cameraRb = cameraObject.GetComponent<Rigidbody>();

        //カメラの回転の有無をインスペクターの設定を使うか準備画面で設定したものを使うか
        //回転の設定箇所のスクリプトの関係で判定を反転させる
        if (useChoppyRotation)
        {
            ischoppyRotation = !Script_SettingManagement.UseSmooth;
            Debug.Log("カメラの回転の有無を準備画面で設定した" + Script_SettingManagement.UseSmooth + "に設定した");
        }

        //1秒間に曲がる角度をインスペクターの設定を使うか準備画面で設定したものを使うか
        if (useInspectorSmooth)
        {
            maxRotationDPS = Script_SettingManagement.SmoothValue;
            Debug.Log("1秒間に曲がる角度に準備画面で設定した" + Script_SettingManagement.SmoothValue + "°を使用する");
        }

        //合計走行距離を初期化
        totalDistance = 0f;
        //一応現在のフレームの座標を扱う変数を初期化する
        currentPosition = Vector3.zero;
        //前のフレームと現在のフレームの座標の差を記録する変数の初期か
        distance = 0f;

        //合計時間の初期化
        totalTime = 0f;

        //総カロリーを格納する変数の初期化
        totalCalorie = 0f;
        //プレイヤーの体重を格納する
        UserWeightSet();

        //左右の手パネルを非表示にする
        timerAndCaloriePanel.SetActive(false);
        rightHansPanel.SetActive(false);

        //PAUSEパネルを非表示にする
        pausePanel.SetActive(false);

        //stopパネルと停止用カウントダウンテキストと警告文を無効化する
        stopPanel.SetActive(false);
        stopCountdownText.SetActive(false);
        stopWarningText.SetActive(false);
        stopTitleText.SetActive(false);

        //線の格納をする
        lineRenderer = sManagement.lineRenderer;

        //最初の目的地のウェイポイントの座標を格納する
        nextWaypointPosSet();
        //ユーザーの身長から更新する線の高さを求める
        UserHeightSet();

        if (cStatus.developStatus() == 100)
        {
            // 速度を格納する
            targetSpeed = debugSpeed;
            //傾斜角を格納する
            targetIncline = debugIncline;
        }

        //コルーチンを変数に格納しつつ、速度取得コルーチンを開始する
        getSpeedCoroutine = StartCoroutine(GetSpeed());
        //コルーチンを変数に格納しつつ、傾斜取得コルーチンを開始する
        getInclineCoroutine = StartCoroutine(GetIncline());
    }

    private void Update()
    {
        //速度取得と傾斜取得
        speedUpdate();
        inclineUpdate();

        //傾斜の平均を取得する
        AvgIncline();

        //走行距離の取得
        GetDistance();
        //時間の取得
        GetTime();
        //カロリーの取得
        GetCalorie();

        //左アナログスティックの押し込み判定を行う
        OnLThumbStick();
        //右アナログスティックの押し込み判定を行う
        OnRThumbStick();
        RightPanelControll();

        //線の更新とウェイポイントについたときの処理
        LineUpdate();
        checkWaypoint();

        //PAUSEパネルの制御
        OnMenu();

        //リタイア時にトリガーを押すと遷移する関数
        RetireTorriger();

        //エラー発生時に復帰する用の関数
        ReConnect();

        //開発用pキーでコルーチン全終了
        StopAllCoroutineKey();
    }

    //カメラの速度を変えて、速度表示を切り替える関数
    private void speedUpdate()
    {
        //もし開発モードかつStartUp状態なら何もしない
        //またもしisStopedがtrueなら更新はやめる（エラーやリタイア選択などで停止が選択されたため、速度の更新をやめるため）
        //もしここのretuenに引っかかるとその下の３行が実行されないため速度は変わらない
        //（もし１フレーム前に実行していたらその時の速度で定速に進む）
        //なのでtargetSpeedはコルーチン内で更新され続けていて大丈夫
        //つまりコルーチンは動いていても大丈夫
        if ((cStatus.developStatus() == 100 && cStatus.isStartUp) || cStatus.isStoped) 
        {
            return;
        }
        // 現在の速度(currentSpeed)を目標速度(targetSpeed)に滑らかに近づける
        // 第3引数の値が小さいほど滑らかになる (speedSmoothingが大きいほど速く追従する)
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedSmoothing);

        // 速度分進む
        cameraRb.linearVelocity = cameraObject.transform.forward * currentSpeed;
        //速度表示の切り替え
        speedText.GetComponentInChildren<TMP_Text>().text = currentSpeed.ToString("F0") + "km/h";

    }

    //実際に速度を取得するコルーチン
    IEnumerator GetSpeed()
    {
        // 開発モードの場合は何もしない
        if (cStatus.developStatus() == 100)
        {
            // コルーチンをここで終了
            yield break;
        }

        //while(true)でループし、内部で状態をチェックする
        while (true)
        {
            // isRunningとisGoaledでないなら、次のフレームまで待機してループを続ける
            if (!cStatus.isRunning && !cStatus.isGoaled)
            {
                //ここで最初のフレームを無視するフラグを戻し続ける
                //これにより何度状態が変わっても、変わった瞬間一気に進まなくなる
                ignoreFirstSpeedReading = true;
                // 1フレームだけ待つ
                yield return null;
                // ループの先頭に戻る
                continue;
            }

            using (UnityWebRequest getSpeedReq = UnityWebRequest.Get(getSpeedURL))
            {
                // タイムアウトを2秒に設定
                getSpeedReq.timeout = 2;
                yield return getSpeedReq.SendWebRequest();

                if (getSpeedReq.result == UnityWebRequest.Result.Success)
                {
                    //最初のデータで現在速度と目標速度を両方初期化する
                    if (ignoreFirstSpeedReading)
                    {
                        Debug.Log("速度取得の開始（初回データで初期化）");
                        ignoreFirstSpeedReading = false;
                    }
                    // 2回目以降は目標速度だけを更新する
                    else
                    {
                        targetSpeed = float.Parse(getSpeedReq.downloadHandler.text);
                    }
                }
                // 通信失敗時は目標速度を0にし、次に備えてignoreフラグをtrueに戻す
                //また停止処理をする
                else
                {
                    targetSpeed = 0;
                    ignoreFirstSpeedReading = true;
                    Debug.LogError("速度取得に失敗: " + getSpeedReq.error);

                    //ここから停止処理に移る
                    //複数回停止処理を呼ばれることを防ぐ
                    if (!cStatus.isStoped)
                    {
                        cStatus.isErrored = true;
                        Debug.Log("通信エラー発生のためisErrorをtrueにした\n" + cStatus.currentStatus());

                        Debug.Log("Pauseパネルを非表示にする");
                        pausePanel.SetActive(false);

                        Debug.Log("速度取得失敗のため停止処理を行う");
                        StartCoroutine(StopProces());

                    }
                }
            }
            yield return new WaitForSeconds(getSpeedInterval);
        }
    }

    //傾斜角の表示を切り替える関数
    private void inclineUpdate()
    {
        //もし開発モードかつStartUp状態なら何もしない
        //またもしisStopedがtrueなら更新はやめる（エラーやリタイア選択などで停止が選択されたため、速度の更新をやめるため）
        //もしここのretuenに引っかかるとその下の３行が実行されないため傾斜は変わらない
        //（もし１フレーム前に実行していたらその時の傾斜で一定に進む）
        //なのでtargetInclineはコルーチン内で更新され続けていて大丈夫
        //つまりコルーチンは動いていても大丈夫
        if ((cStatus.developStatus() == 100 && cStatus.isStartUp) || cStatus.isStoped)
        {
            return;
        }
        //現在の傾斜を目標傾斜に滑らかに近づける
        //第3引数の値が小さいほど滑らかになる（inclineSmoothhingが大きいほど深く追従する）
        currentIncline = Mathf.Lerp(currentIncline, targetIncline, Time.deltaTime * inclineSmoothing);

        //傾斜表示の切り替え
        inclineText.GetComponentInChildren<TMP_Text>().text = currentIncline.ToString("F0") + "°";
    }

    //実際に速度を取得するコルーチン
    IEnumerator GetIncline()
    {
        //開発モードなら何もしない
        if (cStatus.developStatus() == 100)
        {
            yield break;
        }

        //while(true)でループし、内部で状態をチェックする
        while (true)
        {
            //isRunningとisGoaledでないなら、次のフレームまで待機してループを続ける
            if (!cStatus.isRunning && !cStatus.isGoaled)
            {
                //ここで最初のフレームを無視するフラグを戻し続ける
                ignoreFirstInclineReading = true; ;
                //1フレームだけ待機する
                yield return null;
                //ループの先頭に戻る
                continue;
            }

            using (UnityWebRequest getInclineReq = UnityWebRequest.Get(getInclineURL))
            {
                //タイムアウトを２秒で固定する
                getInclineReq.timeout = 2;
                yield return getInclineReq.SendWebRequest();

                if (getInclineReq.result == UnityWebRequest.Result.Success)
                {
                    //最初のデータは無視するようにする
                    if (ignoreFirstInclineReading)
                    {
                        Debug.Log("傾斜取得の開始（初回データで初期化）");
                        ignoreFirstInclineReading = false;
                    }
                    else
                    {
                        //もし取得したテキストを正しくfloatにできたら
                        if (float.TryParse(getInclineReq.downloadHandler.text, out targetIncline))
                        {
                            //取得した値を最も値の近い5の倍数に丸める
                            float roundedIncline = Mathf.Round(targetIncline / 5.0f) * 5.0f;
                            //targetInclineの値をセンサーから取得した値を5の倍数に丸めたものにする
                            targetIncline = roundedIncline;
                        }
                        //変換に失敗した場合
                        else
                        {
                            //フラグを戻す
                            ignoreFirstInclineReading = true;
                            //targetInclineを0にする
                            targetIncline = 0;
                        }
                    }
                }
                //通信失敗時は目標傾斜を0にし、次に備えてignoreフラグをtrueに戻す
                else
                {
                    targetIncline = 0;
                    ignoreFirstInclineReading = true;
                    Debug.Log("傾斜取得に失敗: " + getInclineReq.error);

                    //ここから停止処理に移る
                    //複数回停止処理を呼ばれることを防ぐ
                    if (!cStatus.isStoped)
                    {
                        cStatus.isErrored = true;
                        Debug.Log("通信エラー発生のためisErrorをtrueにした\n" + cStatus.currentStatus());

                        Debug.Log("Pauseパネルを非表示にする");
                        pausePanel.SetActive(false);

                        Debug.Log("傾斜取得失敗のため停止処理を行う");
                        StartCoroutine(StopProces());

                    }
                }
            }
            yield return new WaitForSeconds(getInclineInterval);
        }
    }

    //距離の取得を行う関数
    //距離の取得はcameraObject基準で行う
    private void GetDistance()
    {
        //ランニング状態のときのみ行う
        //他動く状態があれば行うようにする
        //Goal状態では動かないようにする
        if (cStatus.isRunning)
        {
            //最初のフレームで初期位置を記録する
            if (isDistanceFisrstUpdate)
            {
                lastPosition = cameraObject.transform.position;
                isDistanceFisrstUpdate = false;
            }
            //もしオブジェクトがなかったらreturnする
            if (cameraObject == null) return;

            //今のカメラの座標を記録する
            currentPosition = cameraObject.transform.position;
            //距離を計算する
            distance = Vector3.Distance(currentPosition, lastPosition);
            //スケール補正を乗算して距離をp補正して追加する
            totalDistance += distance * distanceScale;

            //テキストを更新する
            distanceText.GetComponentInChildren<TMP_Text>().text = totalDistance.ToString("F0") + "m";

            //次のフレームのために位置を更新する
            lastPosition = currentPosition;

        }
    }

    //タイマーを制御する関数
    //最初のif文で走っているときだけに制限すると勝手に計測が止まる
    private void GetTime()
    {
        //ランニング状態のときのみ行う
        //他動く状態があれば行うようにする
        //Goal状態では動かないようにする
        if (cStatus.isRunning) 
        {
            totalTime += Time.deltaTime;

            int h = (int)(totalTime / 3600);
            int m = (int)(totalTime / 60) % 60;
            int s = (int)(totalTime % 60);

            timerText.GetComponentInChildren<TMP_Text>().text = string.Format("{0:00}:{1:00}:{2:00}", h, m, s);

        }
    }

    //カロリー表示を行う関数
    private void GetCalorie()
    {
        //ランニング状態のときのみ行う
        //他動く状態があれば行うようにする
        //Goal状態では動かないようにする
        if (cStatus.isRunning)
        {
            //現在の速度を取得する
            float speed = currentSpeed;
            //現在の傾斜を取得する
            float incline = currentIncline;

            //速度から基本METs値を取得する
            float baseMETs = METsList(speed);
            //傾斜から追加METsを取得する
            float inclineBonusMETs = GetInclineBonusMets(speed, incline);
            //合計METsを計算する
            float totalMETs = baseMETs + inclineBonusMETs;

            //今のフレームで消費したカロリーを計算する
            //消費カロリー(kcal)=METs*体重(kg)*運動時間*1.05
            //運動時間(h)=deltaTime(秒)/3600
            float frameCalorie = totalMETs * UserWeight * (Time.deltaTime / 3600f) * 1.05f;

            //総消費カロリーに加算する
            totalCalorie += frameCalorie;

            //テキストを更新する
            calorieText.GetComponent<TMP_Text>().text = totalCalorie.ToString("F1") + "kcal";


        }
    }

    //速度から基本METs値を返す関数
    //速度とMETsの対応を片っ端から準備する
    //範囲外の箇所とかは線形補完を使っておおよその値を返すようにしている
    private float METsList(float currentSpeedKmh)
    {
        // METs表のデータに基づく分岐処理
        if (currentSpeedKmh <= 0.5) return 0.0f;
        if (currentSpeedKmh <= 1.6f) return 2.0f;
        if (currentSpeedKmh < 4.2f) return LinearInterpolate(currentSpeedKmh, 1.6f, 2.0f, 4.2f, 3.3f);
        if (currentSpeedKmh <= 6.0f) return 3.3f;
        if (currentSpeedKmh < 6.5f) return LinearInterpolate(currentSpeedKmh, 6.0f, 3.3f, 6.5f, 6.5f);
        if (currentSpeedKmh <= 6.8f) return 6.5f;
        if (currentSpeedKmh < 7.0f) return LinearInterpolate(currentSpeedKmh, 6.8f, 6.5f, 7.0f, 7.8f);
        if (currentSpeedKmh <= 7.8f) return 7.8f;
        if (currentSpeedKmh < 8.1f) return LinearInterpolate(currentSpeedKmh, 7.8f, 7.8f, 8.1f, 8.5f);
        if (currentSpeedKmh <= 8.4f) return 8.5f;
        if (currentSpeedKmh < 8.9f) return LinearInterpolate(currentSpeedKmh, 8.4f, 8.5f, 8.9f, 9.0f);
        if (currentSpeedKmh <= 9.4f) return 9.0f;
        if (currentSpeedKmh < 9.7f) return LinearInterpolate(currentSpeedKmh, 9.4f, 9.0f, 9.7f, 9.3f);
        if (currentSpeedKmh <= 10.2f) return 9.3f;
        if (currentSpeedKmh < 10.9f) return LinearInterpolate(currentSpeedKmh, 10.2f, 9.3f, 10.9f, 10.5f);
        if (currentSpeedKmh < 11.3f) return LinearInterpolate(currentSpeedKmh, 10.9f, 10.5f, 11.3f, 11.0f);
        if (currentSpeedKmh < 12.2f) return LinearInterpolate(currentSpeedKmh, 11.3f, 11.0f, 12.2f, 11.8f);
        if (currentSpeedKmh < 13.0f) return LinearInterpolate(currentSpeedKmh, 12.2f, 11.8f, 13.0f, 12.0f);
        if (currentSpeedKmh < 13.9f) return LinearInterpolate(currentSpeedKmh, 13.0f, 12.0f, 13.9f, 12.5f);
        if (currentSpeedKmh < 14.6f) return LinearInterpolate(currentSpeedKmh, 13.9f, 12.5f, 14.6f, 13.0f);
        if (currentSpeedKmh < 15.1f) return LinearInterpolate(currentSpeedKmh, 14.6f, 13.0f, 15.1f, 14.8f);
        if (currentSpeedKmh <= 15.6f) return 14.8f;
        if (currentSpeedKmh < 16.2f) return LinearInterpolate(currentSpeedKmh, 15.6f, 14.8f, 16.2f, 14.8f);
        if (currentSpeedKmh < 17.8f) return LinearInterpolate(currentSpeedKmh, 16.2f, 14.8f, 17.8f, 16.8f);
        if (currentSpeedKmh < 19.4f) return LinearInterpolate(currentSpeedKmh, 17.8f, 16.8f, 19.4f, 18.5f);
        if (currentSpeedKmh < 21.1f) return LinearInterpolate(currentSpeedKmh, 19.4f, 18.5f, 21.1f, 19.8f);
        if (currentSpeedKmh < 22.7f) return LinearInterpolate(currentSpeedKmh, 21.1f, 19.8f, 22.7f, 23.0f);

        // METs表の上限を超える場合は、最高値で固定
        return 23.0f;

    }

    //基本METs値を返すのに使用する
    //2点間の値を線形補間するための補助関数
    //求めたい点のX座標 (現在の速度)
    //始点のX座標 (区間の下の速度)
    //始点のY座標 (区間の下のMETs)
    //終点のX座標 (区間の上の速度)
    //終点のY座標 (区間の上のMETs)
    //補間されたY座標 (計算されたMETs)
    private float LinearInterpolate(float x, float x0, float y0, float x1, float y1)
    {
        // 0除算を防ぐ
        if ((x1 - x0) == 0)
        {
            return y0;
        }
        // 線形補間の計算式
        return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
    }

    //現在の速度と傾斜から追加のMETsを返す
    private float GetInclineBonusMets(float currentSpeedKmh, float inclineDegrees)
    {
        //動いていない、または下り坂の場合は追加METsは0
        if (currentSpeedKmh <= 0.1f || inclineDegrees <= 0)
        {
            return 0f;
        }

        //速度を 時速(km/h) から 分速(m/min) に変換する
        float speedMetersPerMinute = currentSpeedKmh * 1000f / 60f;
        //傾斜を 角度(°) から 勾配(%)の小数表現に変換する
        float grade = Mathf.Tan(inclineDegrees * Mathf.Deg2Rad);

        //傾斜によって追加される酸素摂取量(VO2)を計算
        float inclineVo2;
        if (currentSpeedKmh < 5.0f)
        {
            //歩行時の傾斜成分
            inclineVo2 = 1.8f * speedMetersPerMinute * grade;
        }
        else
        {
            //走行時の傾斜成分
            inclineVo2 = 0.9f * speedMetersPerMinute * grade;
        }

        //追加の酸素摂取量をMETsに変換して返す (追加VO2 / 3.5)
        return inclineVo2 / 3.5f;
    }

    //開発用全コルーチン停止ボタン
    //使用する場合はシミュレーターやMetaとの接続をしていない状態でPを押す
    //なにかしらのVR機器と接続していると入力の権限かなんかがそっちにいってキーボードが使えなくなる
    private void StopAllCoroutineKey()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StopAllRunningTasks();
            Debug.LogError("Pキーが押された");
        }
    }

    //このスクリプト上で動いている全てのコルーチンを止める関数
    //ゴールしたときとかリタイアするときに呼び出す
    public void StopAllRunningTasks()
    {
        StopAllCoroutines();
        Debug.Log("Running_Management.csのコルーチンを全て停止した");
    }

    //waypointPosの初期値を設定する
    //またnextWayPointPosの初期値も設定する
    //スタートで１回だけ実行する
    private void nextWaypointPosSet()
    {
        //StartUP_Management.csのウェイポイントの座標をまとめたリストを参照する
        waypointsPos = sManagement.wayPointsPos;
        //0番目のウェイポイントは既に通過した扱いなので1番目のウェイポイントから格納するようにするために加算する
        passWayPointCount++;

        //最初の目的地を格納する（１番目の座標）
        nextWayPointPos = waypointsPos[passWayPointCount];
    }

    //ユーザーの身長から地面の高さ1.0mを取得する
    //これにより地面からではなく、頭を基準に地面から1.0mを取得できる
    //スタートで１回だけ実行する
    private void UserHeightSet()
    {
        UserHegiht = 1.7f;
        Debug.Log("ユーザー身長を1.7m固定でセットする");

        //頭の高さから何メートルが地面から1メートルかを計算する
        updateLineHeight = UserHegiht - 1;
    }

    //ユーザーの体重を格納する
    //スタートで1回だけ実行する
    private void UserWeightSet()
    {
        UserWeight = 60f;
        Debug.Log("ユーザー体重を60kg固定でセットする");
    }

    //線の描画を更新する関数
    //仕組みとしてはカメラの位置を線の始点に置き換えていく感じ
    //更新されていく線の高さが開発段階だと合わないかもしれないが、
    //それはシミュレータｍｐ高さが1.7mでユーザーの設定した身長があっていないだけで
    //多少誤差はあれど高さは合う
    private void LineUpdate()
    {
        Vector3 linePos = new Vector3(centerEyeAnchorObject.transform.position.x, centerEyeAnchorObject.transform.position.y - updateLineHeight,
            centerEyeAnchorObject.transform.position.z);
        lineRenderer.SetPosition(0, linePos);
    }

    //ウェイポイントにたどり着いたら行う関数
    //通常はUpdateで行っていていい
    //ゴール判定はこの中
    private void checkWaypoint()
    {
        //もし走行状態だったら
        if (cStatus.isRunning && !cStatus.isStoped)
        {

            //これは理想のカメラのポジション
            //実際はyが0だけどそこに+1の補正をかけると理想になる
            Vector3 cameraPos = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y + 1.0f, cameraObject.transform.position.z);

            //理想のカメラ座標と次のウェイポイントの座標の距離
            Vector3 direction = (nextWayPointPos - cameraPos).normalized;

            //もし理想のカメラ座標と次のウェイポイントの座標の差が0でなければ
            if (direction != Vector3.zero)
            {
                //directionの方向を向くような回転を計算する
                targetRotation = Quaternion.LookRotation(direction);
            }

            //スムーズに回転するならこっち
            if (!ischoppyRotation)
            {
                //回転の滑らかさを反映する
                float adjustedDPS = maxRotationDPS * rotationSmoothing;

                // Rigidbodyを使って回転
                float maxStep = adjustedDPS * Time.fixedDeltaTime;
                Quaternion newRotation = Quaternion.RotateTowards(cameraRb.rotation, targetRotation, maxStep);
                cameraRb.MoveRotation(newRotation);

                //カメラの高さ制限
                //近い距離で高いところから低いところに行くと床にめり込んで裏世界見えるから対処
                float minHeight = 0.001f;
                Vector3 camHeightPos = cameraObject.transform.position;
                if (camHeightPos.y < minHeight)
                {
                    camHeightPos.y = minHeight;
                    cameraObject.transform.position = camHeightPos;
                }
            }
            // ぱっぱと切り替えるならこっち (ischoppyRotation == true)
            else
            {
                //ターゲットの回転へ瞬時に切り替える
                cameraObject.transform.rotation = targetRotation;
            }

            //カメラの高さとウェイポイントの高さの基準が異なる（カメラ→0、次のウェイポイント→1）
            //なのでこのままだとこの次の条件文に引っかからない。のでy軸だけ+1で補正する。
            Vector3 cameraCorrectionPos = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y + 1.0f,
                cameraObject.transform.position.z);

            if (Vector3.Distance(cameraCorrectionPos, nextWayPointPos) < 0.5f)
            {

                //通過したウェイポイントのカウントを加算する
                passWayPointCount++;
                //もし通過したウェイポイントの数がウェイポイント全体の数以上ならゴール判定をする
                if (passWayPointCount >= waypointsPos.Count)
                {
                    //ゴールしたのとPause状態を判定するフラグを変える
                    isPause = false;
                    pausePanel.SetActive(false);
                    cStatus.isGoaled = true;
                    //結果を格納する
                    resultSet();
                    //停止プロセスを行う
                    //isGoaledがtrueになっているので自動でシーン遷移まで行う
                    StartCoroutine(StopProces());
                    Debug.Log("ゴールした！！！");
                    return;
                }
                //次の目的地の座標を更新する
                nextWayPointPos = waypointsPos[passWayPointCount];

                //線の配列の更新
                lineIndexUpdate();
            }
        }
    }

    //ウェイポイントにたどり着いたときに呼び出して頂点を更新する関数
    //これによってウェイポイントに達しても視点を常にカメラの位置に更新できる
    //これをウェイポイントに到達した時に使用する
    private void lineIndexUpdate()
    {
        //今のlineRendererの頂点数を取得する
        int count = lineRenderer.positionCount;

        //先頭から何個詰めるか（今回は１個）
        int removeCount = 1;

        //削除数が現在の頂点数以上なら、線を消して終わり
        if (removeCount >= count)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        //新しい頂点配列を用意する（古い頂点数からremoveCountを引いた長さ）
        Vector3[] newPositions = new Vector3[count - removeCount];

        //oldPositionの（i+removeCount）をnewPositions[i]に詰める
        //つまり、先頭removeCount個分飛ばして残りを前に詰める処理
        for(int i = 0; i < newPositions.Length; i++)
        {
            newPositions[i] = lineRenderer.GetPosition(i + removeCount);
        }

        //lineRendererの頂点数を新しい長さに設定する
        lineRenderer.positionCount = newPositions.Length;

        //配列で一括設定
        lineRenderer.SetPositions(newPositions);
    }

    //左アナログスティックを押し込むとタイマーテキストとカロリーテキストを表示する
    private void OnLThumbStick()
    {
        //左アナログスティックを押し込んだら表示
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstick))
        {
            Debug.Log("左アナログスティックを押し込んだ");
            timerAndCaloriePanel.SetActive(true);
        }

        //左アナログスティックを離したら非表示
        if (OVRInput.GetUp(OVRInput.RawButton.LThumbstick))
        {
            Debug.Log("左アナログスティックを離した");
            timerAndCaloriePanel.SetActive(false);
        }

    }

    //操作マニュアルを表示する
    private void OnRThumbStick()
    {
        //右アナログスティックを押したら表示する
        if (OVRInput.GetDown(OVRInput.RawButton.RThumbstick))
        {
            Debug.Log("右アナログスティックを押し込んだ");
            rightHansPanel.SetActive(true);
        }

        //左アナログスティックを離したら非表示
        if (OVRInput.GetUp(OVRInput.RawButton.RThumbstick))
        {
            Debug.Log("右アナログスティックを離した");
            rightHansPanel.SetActive(false);
        }

    }

    //右手のパネル内容制御
    private void RightPanelControll()
    {
        //goal状態のときにpause関連のテキストを薄くする
        if (cStatus.isGoaled)
        {
            Color c1 = pauseText1.GetComponentInChildren<TMP_Text>().color;
            c1.a = 0.6f;
            pauseText1.GetComponentInChildren<TMP_Text>().color = c1;
        }
        else
        {
            Color c1 = pauseText1.GetComponentInChildren<TMP_Text>().color;
            c1.a = 1f;
            pauseText1.GetComponentInChildren<TMP_Text>().color = c1;

        }
    }

    //Pause関連の処理
    //PAUSE状態でなく、メニューボタンを押したらPAUSEパネルの表示とフラグの切り替え
    //またPAUSE状態かつ、Bボタンが押されたらPAUSEパネルの非表示とフラグの切り替え
    private void OnMenu()
    {
        //PAUSE状態でないかつisErroredじゃないかつゴール状態ではないかつメニューボタンを押されたらPAUSEパネル表示
        //メニューボタンは不安定らしい
        if (!isPause && !cStatus.isErrored && !cStatus.isGoaled && OVRInput.GetDown(OVRInput.RawButton.Start)) 
        {
            //もしStartUp状態でPause状態になったらStartUpパネルを非表示にする
            if (cStatus.isStartUp)
            {
                startUpPanel.SetActive(false);
            }
            Debug.Log("isPauseをtrueにした");
            pausePanel.SetActive(true);
            isPause = true;
        }
        //PAUSE状態かつBボタンが押されたらPAUSEパネル非表示
        if (isPause && OVRInput.GetDown(OVRInput.RawButton.B))
        {
            //もしStartUp状態でPauseパネルが表示されていたらStartUpパンるを表示する
            if (cStatus.isStartUp)
            {
                startUpPanel.SetActive(true);
            }
            Debug.Log("isPauseをfalseにした");
            pausePanel.SetActive(false);
            isPause = false;
        }

        //PAUSE状態かつisStopedがfalse(まだ停止プロセスでない)かつ
        //4つのトリガーとX,Aを押したらリタイア処理をする
        //PauseをfalseにしてisStopedをtrueにする
        if (isPause && !cStatus.isStoped && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) &&
            OVRInput.Get(OVRInput.RawButton.RHandTrigger) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) &&
            OVRInput.Get(OVRInput.RawButton.LHandTrigger) && OVRInput.Get(OVRInput.RawButton.A) &&
            OVRInput.Get(OVRInput.RawButton.X)) 
        {

            //PAUSEパネルをオフにする
            //リタイア処理の場合はisPauseをtrueのままにする
            //ここがtrueかどうかでリタイア処理が変わるから
            Debug.Log("リタイアが選択されたた");
            pausePanel.SetActive(false);

            //ここからリタイア処理に移る
            StartCoroutine(StopProces());
        }
    }

    //呼ばれたら停止処理とisPauseがtrueならリタイア処理をするコルーチン
    //エラー時とリタイアが決定されたときにこれを呼び出す
    IEnumerator StopProces()
    {
        Debug.Log("停止プロセスを開始");

        //もしStartUp状態でリタイアをした場合、カウントやトリガーなどはなしですぐにシーンを切り替える
        if (cStatus.isStartUp)
        {
            Debug.Log("StartUp状態でのリタイア処理を実行する");
            StopAllRunningTasks();
            SceneManager.LoadScene(retireScene);
            yield break;
        }

        //isStopedをtrueにする
        //ここを変えるとspeedUpdateとinclineUpdateの更新が止まり、current変数に１フレーム前の速度が残る
        //そのため減速がしたいのに、勝手に更新されるのを防ぐこともできる
        //傾斜は停止中や停止プロセス中には見えないようにする
        cStatus.isStoped = true;
        Debug.Log("isStopedをtrueにした");
        //停止直前の速度と傾斜を取得する
        float speed = currentSpeed;

        //カウントダウンテキストにカウントダウンの初期値を入れる
        stopCountdownText.GetComponentInChildren<TMP_Text>().text = stopCountdown.ToString();
        //パネルの表示とカウントダウンと警告文の表示
        stopPanel.SetActive(true);
        stopCountdownText.SetActive(true);
        //フォントサイズを途中変えているので念のため初期化
        stopCountdownText.GetComponent<TMP_Text>().fontSize = 0.28f;
        stopWarningText.SetActive(true);
        //エラー発生時はstopTitleTextをエラー発生に変える
        if (cStatus.isErrored)
        {
            stopTitleText.GetComponentInChildren<TMP_Text>().text = "エラー発生!!!";
            stopTitleText.GetComponentInChildren<TMP_Text>().color = Color.red;
        }
        else if (cStatus.isGoaled)
        {
            stopTitleText.GetComponentInChildren<TMP_Text>().text = "ゴール処理中";
            stopTitleText.GetComponentInChildren<TMP_Text>().color = Color.white;
        }
        stopTitleText.SetActive(true);

        //Runningパネルを非表示にする
        runningPanel.SetActive(false);

        //3秒待機
        Debug.Log("停止カウントダウン開始までの停止開始");
        yield return new WaitForSeconds(5f);

        //カウントダウンと減速を行う
        Debug.Log("停止カウントダウンと画面の停止開始");
        //経過時間の初期化
        float elapsedTime = 0f;
        //カウントダウンの秒数を参照する
        float countdown = stopCountdown;

        while (elapsedTime < stopCountdown) 
        {
            //経過時間を加算する
            elapsedTime += Time.deltaTime;
            //残り時間の計算をする
            float remainingTime = countdown - elapsedTime;
            //カウントダウンテキストを更新する
            stopCountdownText.GetComponentInChildren<TMP_Text>().text = Mathf.CeilToInt(remainingTime).ToString();
            //経過時間の割合(0.0～1.0)を計算する
            float t = elapsedTime / stopCountdown;
            //速度を滑らかに0に近づける
            float stopCurrentSpeed = Mathf.Lerp(speed, 0f, t);
            //計算した速度をカメラの動きに反映する
            cameraRb.linearVelocity = cameraObject.transform.forward * stopCurrentSpeed;
            //次のフレームまで待機する
            yield return null;
        }

        Debug.Log("カウントダウンと画面の停止終了");

        //停止が完了したのでisRunningをfalseにする
        cStatus.isRunning = false;
        Debug.Log("isRunningをfalseにした\n"+cStatus.currentStatus());

        if (isPause)
        {
            Debug.Log("リタイアなのでシーン遷移を待機する");
            stopCountdownText.GetComponent<TMP_Text>().fontSize = 0.07f;
            stopCountdownText.GetComponent<TMP_Text>().text = "人差し指トリガーを押して\nメイン画面に移動してください";
            StopAllRunningTasks();
        }
        else if (cStatus.isGoaled)
        {
            //フラグを変えて別の関数からシーン遷移できるようにする
            Debug.Log("ゴール後に完全に停止したのでシーンを待機する");
            stopCountdownText.GetComponent<TMP_Text>().fontSize = 0.065f;
            stopCountdownText.GetComponent<TMP_Text>().text = "人差し指トリガーを押して\n結果表示画面に移動してください";
            isGoaledStop = true;
            StopAllRunningTasks();
        }
        else
        {
            Debug.Log("停止中...");
        }
    }

    //リタイア時に人差し指トリガーを押すとシーン遷移する
    //またゴール後に人差し指トリガーを押すとシーンを遷移する
    private void RetireTorriger()
    {
        //走行中ではなくPause状態かつStop状態の時
        if ((!cStatus.isRunning && isPause && cStatus.isStoped) || (cStatus.isGoaled && isGoaledStop))  
        {
            //もし左右どちらかの人差し指トリガーが押されたら
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) || OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
            {
                //シーン遷移の実行
                if (!cStatus.isRunning && isPause && cStatus.isStoped)
                {
                    Debug.Log("リタイアによるシーン遷移を実行する");
                    SceneManager.LoadScene(retireScene);

                }
                else if ((cStatus.isGoaled && isGoaledStop)) 
                {
                    Debug.Log("ゴールによるシーン遷移を実行する");
                    SceneManager.LoadScene(goalScene);

                }
            }
        }
    }

    //走行中エラー発生時に画面が完全に止まった後に復帰するまでの処理
    //エラー発生時画面完全停止→isStoped,isErrored,!isRunning
    //pause画面の非表示はisErroredがtrueになった時点で行っている
    private void ReConnect()
    {
        // 走行中エラーで完全に停止している場合のみ処理を行う
        if (!cStatus.isRunning && cStatus.isStoped && cStatus.isErrored)
        {
            // 最初の1フレームだけ、選択肢UIのセットアップを行う
            if (!isReconnectProcessStarted)
            {
                Debug.Log("エラー発生、画面完全停止完了。再接続処理を開始します。");

                // カウントダウンテキストを操作指示UIに変更する
                stopCountdownText.GetComponent<TMP_Text>().fontSize = 0.0584f;
                stopCountdownText.GetComponent<TMP_Text>().text = "RELOAD: B + Y\nRETIRE: 4つのトリガー同時押し + A + X";

                // セットアップが完了したことを記録する
                isReconnectProcessStarted = true;
            }

            // 再接続処理中でなければ、コントローラーからの入力を受け付ける
            if (!isAttemptingReconnect)
            {
                // リタイアが選択された場合
                if (OVRInput.Get(OVRInput.RawButton.RIndexTrigger) &&
                    OVRInput.Get(OVRInput.RawButton.RHandTrigger) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) &&
                    OVRInput.Get(OVRInput.RawButton.LHandTrigger) && OVRInput.Get(OVRInput.RawButton.A) &&
                    OVRInput.Get(OVRInput.RawButton.X))
                {
                    // 他の入力が重複しないようにフラグを立てる
                    isAttemptingReconnect = true; 
                    Debug.Log("エラー状態でリタイアを選択したためシーン遷移を行います");
                    SceneManager.LoadScene(retireScene);
                }
                // 再接続(RELOAD)が選択された場合
                else if (OVRInput.Get(OVRInput.RawButton.B) && OVRInput.Get(OVRInput.RawButton.Y))
                {
                    Debug.Log("再接続を開始します");
                    // 再接続処理中のフラグを立てる
                    isAttemptingReconnect = true;
                    // 再接続処理コルーチンを開始
                    StartCoroutine(AttemptReconnectCoroutine()); 
                }
            }
        }
    }

    //実際に走行中エラー発生時から復帰するときの処理
    IEnumerator AttemptReconnectCoroutine()
    {
        // UIを「再接続中...」というフィードバックに変更
        stopCountdownText.GetComponent<TMP_Text>().text = "再接続を試みています...";

        // サーバーに疎通確認のリクエストを送信 (GetSpeedのエンドポイントをテストに利用)
        using (UnityWebRequest reconnectReq = UnityWebRequest.Get(getSpeedURL))
        {
            reconnectReq.timeout = 5; // タイムアウトを5秒に設定
            yield return reconnectReq.SendWebRequest();

            // 再接続に成功した場合
            if (reconnectReq.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("再接続に成功しました。ゲームを再開します。");

                // 警告テキストを「再スタート」用に変更
                stopWarningText.GetComponentInChildren<TMP_Text>().text = "再スタートまであと";
                stopWarningText.GetComponentInChildren<TMP_Text>().fontSize = 0.22f;

                // カウントダウンテキストのフォントサイズを通常に戻す
                stopCountdownText.GetComponent<TMP_Text>().fontSize = 0.28f;

                // カウントダウン処理を開始
                float elapsedTime = 0f;
                float countdown = stopCountdown; // 停止時と同じ秒数を使用

                while (elapsedTime < countdown)
                {
                    elapsedTime += Time.deltaTime;
                    float remainingTime = countdown - elapsedTime;
                    stopCountdownText.GetComponentInChildren<TMP_Text>().text = Mathf.CeilToInt(remainingTime).ToString();
                    yield return null;
                }

                // 各種ステータスフラグを走行中に戻す
                cStatus.isErrored = false;
                cStatus.isStoped = false;
                cStatus.isRunning = true;

                // UIを走行中の状態に戻す
                stopPanel.SetActive(false);
                runningPanel.SetActive(true);

                // StopProcesが呼ばれた際に正しく設定されるよう、警告テキストを元の内容に戻しておく
                stopWarningText.GetComponentInChildren<TMP_Text>().text = "走り続けて!!!";
                stopWarningText.GetComponentInChildren<TMP_Text>().fontSize = 0.3f;

                //stopTitleTextを元の内容に戻しておく
                stopTitleText.GetComponentInChildren<TMP_Text>().text = "停止プロセス中";
                stopTitleText.GetComponentInChildren<TMP_Text>().color = Color.white;

                // 再接続処理用のフラグを初期状態に戻す
                isReconnectProcessStarted = false;
                isAttemptingReconnect = false;

                // 通信コルーチン内の初回読み込みフラグをリセットし、再開直後に値が飛ぶのを防ぐ
                ignoreFirstSpeedReading = true;
                ignoreFirstInclineReading = true;

                Debug.Log("ゲーム再開。現在の状態: " + cStatus.currentStatus());
            }
            // 再接続に失敗した場合
            else
            {
                Debug.LogError("再接続に失敗しました: " + reconnectReq.error);

                // UIで失敗したことをユーザーに伝える
                stopCountdownText.GetComponent<TMP_Text>().text = "再接続に失敗しました";

                // 2秒間、失敗表示を見せた後、再度選択肢UIに戻す
                yield return new WaitForSeconds(2f);
                stopCountdownText.GetComponent<TMP_Text>().text = "RELOAD: B + Y\nRETIRE: 4つのトリガー同時押し + A + X";

                // 再度、入力を受け付けるためにフラグをfalseに戻す
                isAttemptingReconnect = false;
            }
        }
    }

    //平均傾斜を計算し続ける関数
    private void AvgIncline()
    {
        if (!cStatus.isRunning)
        {
            return;
        }

        getInclineTimer += Time.deltaTime;

        //5秒ごとに傾斜を取得する
        if (getInclineTimer >= 5f)
        {
            totalIncline += currentIncline;
            getInclineCount++;
            getInclineTimer = 0f;
        }

    }

    //ゴールしたときに呼び出してゴール時の情報を格納する
    private void resultSet()
    {
        goalTime = totalTime;
        goalDistance = totalDistance;
        goalCalorie = totalCalorie;
        //平均速度は距離と時間から求める
        //単位は(km/h)
        avgSpeed = (goalDistance / goalTime) * 3.6f;
        //平均傾斜を計算する
        avgIncline = totalIncline / getInclineCount;

        Debug.Log(goalTime + "\n" + goalDistance + "\n" + goalCalorie + "\n" + avgSpeed + "\n" + avgIncline);
    }

}