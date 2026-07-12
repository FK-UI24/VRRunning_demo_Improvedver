using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ping = System.Net.NetworkInformation.Ping;
using System.Threading.Tasks;
using System.Net;

public class StartUP_Management : MonoBehaviour
{

    //ウェイポイントと線の設置
    //StartUp時の接続エラー制御（キャリブレーション結果を送信できたか、傾斜を取得できたか）
    //トリガー押してランニング状態への遷移

    [Header("メインカメラオブジェクト")]
    [SerializeField] private GameObject cameraObject;

    [Header("ウェイポイントのオブジェクト")]
    [SerializeField] private GameObject wayPointObject;

    [Header("CurrentSttus.csをアタッチしたオブジェクト")]
    [SerializeField] private CurrentStatus curentStatus;

    [Header("スタートテキスト")]
    [SerializeField] private GameObject startText;

    [Header("リロードテキスト")]
    [SerializeField] private GameObject reloadText;

    [Header("コネクトテキスト")]
    [SerializeField] private GameObject connectText;

    [Header("キャリブレーション結果送信エラーテキスト")]
    [SerializeField] private GameObject calibrationErrorText;

    [Header("傾斜取得エラーテキスト")]
    [SerializeField] private GameObject inclineErrorText;

    [Header("Running_Management.csがアタッチされているオブジェクト")]
    [SerializeField] private GameObject RM;

    [Header("接続エラーテキスト")]
    [SerializeField] private GameObject connectionErrorText;
    [Header("接続監視インターバル(秒)")]
    [SerializeField] private float pingInterval = 1.0f;

    //ルートファイルまでのパスを格納する用変数
    private string RouteJsonFile;
    //ウェイポイントオブジェクトを格納する用リスト
    //座標を保存するリストと対応させるために使用する
    private List<GameObject> wayPoints = new List<GameObject>();
    //ステージ名を格納する用変数
    private string stageName;

    //LineRenderer用変数
    //線の更新で使用するのでpublic
    [HideInInspector]
    public LineRenderer lineRenderer;

    //RunningManagementのウェイポイント到達判定で使用するリスト
    [HideInInspector]
    public List<Vector3> wayPointsPos = new List<Vector3>();


    //SE格納用リスト
    private AudioSource[] SE;

    //IPアドレス参照用変数
    private Script_IP config;
    //キャリブレーション結果送信URL
    private string sendCalibrationURL;
    //傾斜取得リンク
    private string getInclineURL;
    //キャリブレーション結果の送信が成功したかのフラグ
    private bool setCalibrationFlag = false;
    //傾斜取得に成功したかのフラグ
    private bool getInclineFlag = false;
    //キャリブレーション結果送信進行中かのフラグ
    private bool isSendingCalibration = false;
    //傾斜取得進行中のフラグ
    private bool isGettingIncline = false;

    //リロードテキストのTMP_Textを格納する用変数
    private TMP_Text tmpReloadtext;
    //リロードテキストの文字の色を格納する用変数
    private Color reloadColor;

    //Running_Managementのスクリプトの参照用関数
    private Running_Management rm;

    // サーバーとの接続状態を保持するフラグ
    private bool isConnected = true;
    // 接続監視コルーチンを管理する変数
    private Coroutine checkConnectivityCoroutine;


    private void Start()
    {
        //現在の開発モードの確認
        Debug.Log(curentStatus.developStatus());

        //SEを格納する
        SE = GetComponents<AudioSource>();

        //スタートテキストとリロードテキストを非表示にする
        //ラズパイとのやり取りの結果が出るまで両方ともオフにする
        startText.SetActive(false);
        reloadText.SetActive(false);
        //エラーテキストの非表示
        calibrationErrorText.SetActive(false);
        inclineErrorText.SetActive(false);
        connectionErrorText.SetActive(false);

        //リロードテキストのTMP_Tectを格納する
        tmpReloadtext = reloadText.GetComponentInChildren<TMP_Text>();
        //リロードテキストの色を格納する
        reloadColor = tmpReloadtext.color;

        //現在の状態確認
        Debug.Log(curentStatus.currentStatus());
        if (curentStatus.currentStatus() == -1) Debug.LogError("現在の状態の定義がない");

        //接続確認用のアドレス等格納
        config = Resources.Load<Script_IP>("IPConfig");
        //キャリブレーション結果送信URLと傾斜取得URLを格納する
        sendCalibrationURL = "http://" + config.ipaddress + ":" + config.port + "/set_rotary_speed";
        getInclineURL = "http://" + config.ipaddress + ":" + config.port + "/start_mpu6050_get_pitch";

        //Running_Managementの参照をする
        rm = RM.GetComponentInChildren<Running_Management>();

        //ウェイポイントの設置と線の描画
        WayPointInstallation();

        if (curentStatus.developStatus() != 100)
        {
            checkConnectivityCoroutine = StartCoroutine(CheckConnectivity());
        }

        //キャリブレーション結果の送信
        SendCalibration();

        //傾斜取得の確認
        GetIncline();
    }

    //ランニング状態への状態変化、または接続エラー時の制御
    private void Update()
    {
        //接続確認とキャリブレーション結果送信と傾斜取得が成功したらtrueになるフラグ
        bool allChecksPassed = setCalibrationFlag && getInclineFlag && isConnected;


        //もしStartUp状態で、キャリブレーション結果送信と傾斜取得が成功していたら
        if ((curentStatus.currentStatus() == 0 && allChecksPassed))
        {

            // スタートテキストを表示/リロードテキストを非表示にする
            startText.SetActive(true);
            reloadText.SetActive(false);

            //もし左人差し指トリガーか右人差し指トリガーを押したら
            //かつisPauseがfalseのとき
            if ((OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||
                OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger)) &&
                !rm.isPause)
            {
                //SEを鳴らす
                SE[0].Play();

                //通信監視コルーチンを停止する
                if (checkConnectivityCoroutine != null)
                {
                    StopCoroutine(checkConnectivityCoroutine);
                }

                //StartUp状態からランニング状態に変える
                curentStatus.isStartUp = false;
                curentStatus.isRunning = true;
                Debug.Log("StartUp状態からRunning状態に遷移した");
                this.enabled = false;
            }
        }
        //もしどちらか、または両方が失敗していたら
        else if (curentStatus.currentStatus() == 0)  
        {
            startText.SetActive(false);
            reloadText.SetActive(true);

            // トリガー入力でリトライ処理
            //かつisPauseがfalseのとき
            if ((OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||
                OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger)) &&
                !rm.isPause)
            {
                // 実行できるリトライ処理があるか先に確認
                //通信が失敗している場合もリトライ対象にする
                bool canRetry = (!setCalibrationFlag && !isSendingCalibration)
                                || (!getInclineFlag && !isGettingIncline)
                                || !isConnected; 
                // リトライ可能なら、先に音を鳴らす
                if (canRetry)
                {
                    SE[1].Play();
                }

                //通信失敗時は、両方のチェックを再実行する
                if (!isConnected)
                {
                    Debug.Log("接続が切れているため、両方の通信をリトライする");
                    // 進行中でなければ再試行
                    if (!isSendingCalibration) SendCalibration();
                    if (!isGettingIncline) GetIncline();
                }
                else
                {
                    // 失敗している通信だけを再試行する
                    if (!setCalibrationFlag && !isSendingCalibration)
                    {
                        SendCalibration();
                    }
                    if (!getInclineFlag && !isGettingIncline)
                    {
                        GetIncline();
                    }

                }
            }
        }

        //もしisSendingCalibrationかisGettingInclineがtrueなら
        //リロードテキストの文字を薄くする
        //コネクトテキストの表示
        if (isSendingCalibration || isGettingIncline)
        {
            reloadColor.a = 0.4f;
            tmpReloadtext.color = reloadColor;
            connectText.SetActive(true);
            

        }
        //isSendingCalibration/isGettingIncline両方がfalseの場合
        //リロードテキストの文字を戻す
        //コネクトテキストの非表示
        else
        {
            reloadColor.a = 1.0f;
            tmpReloadtext.color = reloadColor;
            connectText.SetActive(false);
        }
    }

    //サーバーとの接続を監視するコルーチン
    IEnumerator CheckConnectivity()
    {
        // StartUp状態の間、ループし続ける
        while (curentStatus.isStartUp)
        {
            // Pingを非同期で実行するタスクを開始
            var pingTask = PingAsync(config.ipaddress);

            // タスクが完了するまで待機する (メインスレッドはブロックしない)
            yield return new WaitUntil(() => pingTask.IsCompleted);

            // タスクの結果を取得
            PingReply reply = pingTask.Result;

            if (reply != null && reply.Status == IPStatus.Success)
            {
                // 接続成功
                if (!isConnected)
                {
                    Debug.Log("サーバーへの接続が回復しました。");
                    isConnected = true;
                    connectionErrorText.SetActive(false);
                }
            }
            else
            {
                // 接続失敗
                if (isConnected)
                {
                    string status = reply != null ? reply.Status.ToString() : "Exception";
                    Debug.LogWarning("サーバーへのPingが失敗しました。接続を確認してください。 Status: " + status);
                    isConnected = false;
                    connectionErrorText.SetActive(true);
                    // 接続が切れたら、他のフラグもリセットして再試行を促す
                    setCalibrationFlag = false;
                    getInclineFlag = false;
                }
            }

            // 指定した間隔で待機
            yield return new WaitForSeconds(pingInterval);
        }
    }
    //Pingを非同期で実行するためのasync Taskメソッド
    private async Task<PingReply> PingAsync(string address)
    {
        try
        {
            // 1. ホスト名からIPアドレスのリストを非同期で取得する
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(address);

            // 2. 取得したリストからIPv4アドレス(InterNetwork)を最初に見つける
            IPAddress ipV4Address = addresses.FirstOrDefault(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            // 3. IPv4アドレスが見つからなかった場合はエラーとして扱う
            if (ipV4Address == null)
            {
                Debug.LogError("指定されたホスト名からIPv4アドレスが見つかりませんでした: " + address);
                return null;
            }

            // 4. 解決したIPv4アドレスを使ってPingを送信する
            Ping pinger = new Ping();
            // SendPingAsyncを呼び出し、結果が返ってくるまで待機する (await)
            return await pinger.SendPingAsync(ipV4Address, 1000); // タイムアウト1秒
        }
        catch (System.Exception ex) // SocketException以外もキャッチできるように変更
        {
            Debug.LogError("Ping送信中に例外が発生しました: " + ex.Message);
            return null; // 例外発生時はnullを返す
        }
    }

    //ウェイポイントの設置とLineInstallationの実行をする関数
    private void WayPointInstallation()
    {
        //ルートファイルまでのパスを格納する
        string routeDirectory = Path.Combine(Application.persistentDataPath, "RouteData");

        //開発モード時はサンプルルートを参照
        if (curentStatus.developStatus() == 100)
        {
            RouteJsonFile = Path.Combine(routeDirectory, "routeData_Sample.json");
        }
        else
        {
            RouteJsonFile = Path.Combine(routeDirectory, "routeData.json");
        }

        //ファイルがあるかを確認する
        //順当にいけば、ファイルはあるので特にエラー文の表示などはしない
        if (!File.Exists(RouteJsonFile))
        {
            return;
        }

        //ファイルの中身を文字列として読み込む
        string jsonText = File.ReadAllText(RouteJsonFile);

        //正規表現で"(x,y,z)"の形を全部見つける
        MatchCollection matches = Regex.Matches(jsonText, @"\(([^)]+)\)");

        //ウェイポイントオブジェクトに対応する番号を数える用変数
        int count = 0;

        //1番のウェイポイントの座標を保存する用変数
        Vector3 startPos = Vector3.zero;

        //2番のウェイポイントオブジェクトを保存する用変数
        GameObject secondWayPoint = null;

        foreach (Match match in matches)
        {
            //"x,y,z"の部分を取得する
            //match.Groups[0]...マッチ全体"(x,y,z)"
            //match.Groups[1]...丸括弧の中身だけ"x,y,z"
            string[] parts = match.Groups[1].Value.Split(',');

            //floatに変換する
            float x = float.Parse(parts[0]);
            float y = float.Parse(parts[1]);
            float z = float.Parse(parts[2]);

            //Vector3への変換とウェイポイントの高さを調整する
            //ここで高さを-1.0fしているのはウェイポイントの位置をカメラの少し下の高さに設置したいからである
            //しかしこのままだとposはカメラ事態の高さにも使用しているので結局カメラとウェイポイント両方の高さが低くなるだけになってしまう
            //そのためこの関数の下部のカメラの初期位置を決めているところで、ここでマイナスした分をプラスして調整している
            Vector3 pos = new Vector3(x, y , z);

            //オブジェクト生成
            GameObject waypoint = Instantiate(wayPointObject, pos, Quaternion.identity);
            wayPoints.Add(waypoint);
            wayPointsPos.Add(waypoint.transform.position);

            //ウェイポイントのカウントを数える
            count += 1;

            //１番だったら座標を保存する
            //またウェイポイントとぶつかると音が鳴るため最初の１個目(Start)ではならないようにする
            //設置したウェイポイントの当たり判定をなくして、その後表示しないようにする
            if (count == 1)
            {
                startPos = pos;

                //最初のウェイポイントの当たり判定をなくして表示させない処理
                Collider col = waypoint.GetComponent<Collider>();
                col.enabled = false;
                waypoint.SetActive(false);
            }
            //2番だったら参照用に保存する
            else if (count == 2)
            {
                secondWayPoint = waypoint;
            }
        }

        //ここで線を結ぶ関数を呼び出す
        LineInstallation(wayPoints);

        Debug.Log("ウェイポイントの設置と線の描画完了");

        //ここでカメラの位置と向きを変える
        //ここでの高さ調整についてはこの関数内のposの格納個所を確認するとわかる
        cameraObject.transform.position = new Vector3(startPos.x, cameraObject.transform.position.y, startPos.z);

        //向く方向はx,zは２番目のウェイポイントでいいが、y（高さ）はカメラの高さにそろえる
        Vector3 lookTarget =
            new Vector3(secondWayPoint.transform.position.x, cameraObject.transform.position.y,
            secondWayPoint.transform.position.z);
        cameraObject.transform.LookAt(lookTarget);

        Debug.Log("メインカメラを初期位置に設置完了");
    }    //線の描画を行う関数
    private void LineInstallation(List<GameObject> wayPoints)
    {
        //このゲームオブジェクトのLineRendererコンポーネントを取得する
        //LineRendererは、複数の点（座標）を順番につないで線を描くコンポーネント
        lineRenderer = gameObject.GetComponent<LineRenderer>();

        //もしLineRendererコンポーネントがなかったら、新たに追加する
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        //positionCountは線に使う点の数を指定するプロパティである
        lineRenderer.positionCount = 0;

        //線の太さを設定する（最初と最後で同じ幅にする）
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth = 0.25f;

        //マテリアルをStandardシェーダーで生成
        //これによりどこから見ても一定の幅に見える
        lineRenderer.material = new Material(Shader.Find("Standard"));

        //線の色を黄色にする
        lineRenderer.material.color = Color.yellow;

        //Emission（自己発光）を有効化して、くすんだ色にならないようにする
        lineRenderer.material.EnableKeyword("_EMISSION");

        //Emissionの色も黄色に設定する
        lineRenderer.material.SetColor("_EmissionColor", Color.yellow);

        //線による影の投影を無効化
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        //線がほかのオブジェクトから影の影響を受けないようにする
        lineRenderer.receiveShadows = false;

        //線の角を丸める
        lineRenderer.numCapVertices = 10;

        //頂点を丸める
        lineRenderer.numCornerVertices = 10;

        //ウェイポイントを線で結ぶ
        if (wayPoints.Count < 2)
        {
            //2点未満なら線なし
            lineRenderer.positionCount = 0;
            return;
        }

        //線を書くのに使う点の数はウェイポイントの数-1であるので、リストのインデックス数と同じである
        lineRenderer.positionCount = wayPoints.Count;
        for (int i = 0; i < wayPoints.Count; i++)
        {

            //いったんまとめる
            Vector3 pos = new Vector3(wayPoints[i].transform.position.x,
                wayPoints[i].transform.position.y, wayPoints[i].transform.position.z);

            //SetPosition(何番目の点を設定するか,設定する座標)
            lineRenderer.SetPosition(i, pos);
        }

    }

    //キャリブレーション結果を送信して、成功したか失敗したかの関数
    private void SendCalibration()
    {
        //もし開発モードならキャリブレーション結果送信成功の体で進める
        if (curentStatus.developStatus() == 100)
        {
            setCalibrationFlag = true;
            return;
        }

        //キャリブレーション結果ファイルの中身を参照する
        string settingFile = Path.Combine(Application.persistentDataPath, "calibrationSetting.json");
        string json = File.ReadAllText(settingFile);
        JObject obj = JObject.Parse(json);
        float targetSpeed = float.Parse(obj.Properties().First().Name);
        float calibrationStep = obj.Properties().First().Value.Value<float>();

        //キャリブレーション結果送信前に送信進行中のフラグをオンにする
        isSendingCalibration = true;

        //コルーチンに渡し、実際にキャリブレーション結果を送信する
        StartCoroutine(SetCalibration(targetSpeed,calibrationStep));
    }
    //実際にキャリブレーション結果を送信する
    IEnumerator SetCalibration(float targetSpeed, float calibrationStep)
    {
        WWWForm form = new WWWForm();
        form.AddField("ref_speed", targetSpeed.ToString());
        form.AddField("ref_steps", calibrationStep.ToString());
        using (UnityWebRequest setSpeedReq = UnityWebRequest.Post(sendCalibrationURL, form))
        {
            // タイムアウトを10秒に設定
            setSpeedReq.timeout = 10;
            //通信終了まで待つ
            yield return setSpeedReq.SendWebRequest();

            //通信が終わったのでキャリブレーション結果送信中のフラグをオフにする
            isSendingCalibration = false;


            //キャリブレーション結果送信失敗時
            if (setSpeedReq.result != UnityWebRequest.Result.Success)
            {
                //フラグはfalseにする
                setCalibrationFlag = false;
                //キャリブレーション結果送信エラーテキストを表示する
                calibrationErrorText.SetActive(true);
                Debug.Log("キャリブレーション結果送信失敗："+ setSpeedReq.error);
            }
            //キャリブレーション結果送信成功時
            else
            {
                //フラグをtrueにする
                setCalibrationFlag = true;
                //キャリブレーション結果送信エラーテキストを非表示にする
                calibrationErrorText.SetActive(false);
                Debug.Log("キャリブレーション結果送信成功");
            }
        }
    }

    //傾斜取得が可能かを確認して、成功したか失敗したかの関数
    private void GetIncline()
    {
        //もし開発モードなら傾斜取得成功の体で進める
        if(curentStatus.developStatus() == 100)
        {
            getInclineFlag = true;
            return;
        }

        //傾斜確認前に確認進行中のフラグをオンにする
        isGettingIncline = true;

        //コルーチンを実行し、実際に傾斜取得を確認する
        StartCoroutine(checkIncline());
    }
    //実際に傾斜取得が可能か確認する
    IEnumerator checkIncline()
    {
        using (UnityWebRequest startInclineReq = UnityWebRequest.Post(getInclineURL, new WWWForm()))
        {
            // タイムアウトを10秒に設定
            startInclineReq.timeout = 10;
            //通信終了まで待つ
            yield return startInclineReq.SendWebRequest();

            //通信が終わったので傾斜取得確認中のフラグをオフにする
            isGettingIncline = false;

            //傾斜取得失敗時
            if (startInclineReq.result != UnityWebRequest.Result.Success)
            {
                //フラグはfalseにする
                getInclineFlag = false;
                //傾斜取得エラーテキストを表示する
                inclineErrorText.SetActive(true);
                Debug.Log("傾斜取得失敗：" + startInclineReq.error);
            }
            //傾斜取得成功時
            else
            {
                //フラグはtrueにする
                getInclineFlag = true;
                //傾斜取得エラーテキストを非表示にする
                inclineErrorText.SetActive(false);
                Debug.Log("傾斜取得成功");
            }
        }
    }

}
