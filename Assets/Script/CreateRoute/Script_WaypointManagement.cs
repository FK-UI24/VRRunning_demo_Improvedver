using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Script_WaypointManagement : MonoBehaviour
{
    [Header("ウェイポイントのオブジェクト")]
    [SerializeField] private GameObject wayPointObject;

    //ウェイポイント間を繋ぐLineRendere用変数
    private LineRenderer lineRenderer;

    //ルートを保存しているJSONファイルまでのパス用変数
    private string RouteJsonFile;

    //ウェイポイントオブジェクトを格納する用リスト
    //座標を保存するリストと対応させるために使用する
    private List<GameObject> wayPoints = new List<GameObject>();

    //一時的に座標を保存する用のリスト
    private List<Vector3> clickedPositions = new List<Vector3>();

    //ウェイポイント設置時のSE用変数
    private AudioSource SE;

    //Startよりも前にオブジェクトのの追加などを行う
    private void Awake()
    {
        //このオブジェクトにアタッチされているLineReb\ndereを格納する
        lineRenderer = GetComponent<LineRenderer>();

        //もし無かったら、新たに追加する
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

        //positionCountは線に使う点の数を指定する（とりあえず０でいい）
        lineRenderer.positionCount = 0;

        //線の太さを指定する（最初と最後で同じにする）
        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.5f;

        //マテリアルをStandartシェーダーで生成
        //これによりどこから見ても一定の幅に見えるようになる
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

    }

    private void Start()
    {
        //ルートを保存するフォルダがあるかの確認
        string routeDirectory = Path.Combine(Application.persistentDataPath, "RouteData");
        //「RouteData」フォルダがなければ生成する
        if (!Directory.Exists(routeDirectory)) Directory.CreateDirectory(routeDirectory);

        //上記のフォルダ内にルートを保存するためのJsonファイルがあるか確認
        RouteJsonFile = Path.Combine(routeDirectory, "routeData.json");
        //「routeData.json」ファイルがなければ生成する
        //usingを使わないとファイルハンドリングが解放されず例外が起きる
        if(!File.Exists(RouteJsonFile))using (File.Create(RouteJsonFile)) { }

        //SEを格納する
        SE = GetComponent<AudioSource>();

    }

    private void Update()
    {
        //右クリックが押されたらウェイポイントを設置し、座標をリストに追加する
        if (Input.GetMouseButtonDown(1))
        {
            WaypointSet();
        }
    }

    private void WaypointSet()
    {
        //UIの上なら反応しないようにする
        if (EventSystem.current.IsPointerOverGameObject()) return;

        //マウス位置からカメラに向かってRay（見えない線）を飛ばす
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        //Raycast（線）にヒットした情報を格納するための変数
        RaycastHit hit;

        //Rayがオブジェクトに当たったかを判定する
        if (Physics.Raycast(ray, out hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 3f);
            Debug.Log($"Ray origin:{ray.origin} → hit:{hit.point}");

            //当たった場所の座標を取得する
            Vector3 clickPosition = hit.point;

            //clickPosiutionの座標を「リストに保存する」
            clickedPositions.Add(clickPosition);
            Debug.Log(clickPosition + "をリストに追加！！！");

            //ウェイポイントの高さを調整
            clickPosition.y += 2.5f;

            //選択したときのSEを鳴らす
            SE.Play();

            //インスペクター側から設定したオブジェクトを置く
            GameObject waypoint = Instantiate(wayPointObject, clickPosition, Quaternion.identity);

            //生成したクローンをリストに追加する
            wayPoints.Add(waypoint);

            //wayPointの子オブジェクトのTMPを取得して、リストに対応する番号を入れる
            TextMeshProUGUI waypointCounter = waypoint.GetComponentInChildren<TextMeshProUGUI>();
            waypointCounter.text = wayPoints.Count.ToString();

            //線の更新
            UpdateLineRenderer();


        }
    }

    //LineRendererを更新してウェイポイントに順番につなぐ
    private void UpdateLineRenderer()
    {
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
            //SetPosition(何番目の点を設定するか,設定する座標)
            lineRenderer.SetPosition(i, wayPoints[i].transform.position);
        }
    }

    //リストの内容を指定したJsonファイルに保存する関数
    //単純にキーなしで座標のみを保存していく
    //これはボタンから間接的に呼び出す
    public void SavePositionToJson()
    {
        //StringBuilderを使って効率的に文字列を組み立てる準備をする
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        //JSON配列の開始記号「[」を追加する
        //改行を入れて見やすくする
        sb.Append("[\n");

        //clickPositionsリストの要素を１つずつ取り出してループする
        for (int i = 0; i < clickedPositions.Count; i++)
        {
            Vector3 v = clickedPositions[i];

            //最後の要素以外にはコンマと改行を追加してJSON配列の区切りにする
            if (i < clickedPositions.Count - 1)
            {
                sb.Append(",\n");
            }
        }

        //JSON配列の終了記号を追加する
        //改行も入れて整形する
        sb.Append("\n]");

        //完成したJSON文字列をファイルに完全上書きする
        //毎回完全上書きなので、
        //ロード関数を作って１番最初にJSONファイルの中身を取り出しておかないとリセットになる
        File.WriteAllText(RouteJsonFile, sb.ToString());

        Debug.Log("リストを" + RouteJsonFile + "に保存した");
    }


    //リストから最後の座標を削除する関数
    //これはボタンから間接的に呼び出す
    public void ListRemove()
    {
        //リストに保存されている座標が０個より多い場合
        if (clickedPositions.Count > 0)
        {
            //削除されるリストの要素を格納する
            Vector3 RemovePosition = clickedPositions[clickedPositions.Count - 1];

            //RemovAtで指定したインデックス番号の削除をする
            //clickedPositions.Countはリストの要素の数なので
            //そこから１を引くことで最新の要素を指定できる
            clickedPositions.RemoveAt(clickedPositions.Count - 1);
            Debug.Log(RemovePosition + "を削除した！！！");

            //対応するウェイポイントを削除する
            GameObject waypoint = wayPoints[wayPoints.Count - 1];
            wayPoints.RemoveAt(wayPoints.Count - 1);
            Destroy(waypoint);

            //線の更新
            UpdateLineRenderer();

        }
    }


}
