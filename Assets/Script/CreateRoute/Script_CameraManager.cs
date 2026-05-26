using UnityEngine;

//ルート作成画面でカメラの移動を行うスクリプト
//カメラに直接配置する

public class Script_CameraManager : MonoBehaviour
{
    [Header("ルート作成時のカメラの初期位置：X")]
    [SerializeField]private float initialPositionX;

    [Header("ルート作成時のカメラの初期位置：Y")]
    [SerializeField] private float initialPositionY;

    [Header("ルート作成時のカメラの初期位置：Z")]
    [SerializeField] private float initialPositionZ;

    [Header("ルート作成時のカメラの初期角度：X")]
    [SerializeField] private float initialRotationX;

    [Header("ルート作成維持のカメラの初期角度：Y")]
    [SerializeField] private float initialRotationY;

    [Header("ルート作成時のカメラの初期角度：Z")]
    [SerializeField] private float initialRotationZ;

    [Header("ズーム速度")]
    [SerializeField] private float zoomSpeed;

    [Header("ズーム距離の最小値")]
    [SerializeField] private float minHeight;

    [Header("ズーム距離の最大値")]
    [SerializeField] private float maxHeight;

    [Header("移動速度")]
    [SerializeField] private float moveSpeed;

    //カメラのRigidbodyを格納する用変数
    //ズーム時や移動時にオブジェクトにめり込まないようにするために使う
    private Rigidbody cameraRb;

    //左クリックが押されたときのマウス位置を記録する用変数
    Vector3 lastMousePosition;


    private void Start()
    {
        //カメラを初期位置にする
        gameObject.transform.position = new Vector3(initialPositionX, initialPositionY, initialPositionZ);

        //カメラを初期角度にする
        gameObject.transform.rotation = Quaternion.Euler(initialRotationX, initialRotationY, initialRotationZ);

        //カメラのRigidbodyを格納する
        cameraRb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        //ズームを行う関数
        HandleZoom();

        //移動を行う関数
        HandleDrag();
    }

    //マウスのホイールスクロールで拡大縮小を行う関数
    private void HandleZoom()
    {
        ///Input.GetAxisは、あらかじめ設定された軸の名前(Axis)を指定して、その入力を-1.0～1.0の数値として受け取る
        ///奥に回すとプラスの値、手前に回すとマイナスの値、回していないときは0となる。
        ///通常、１目盛り回すごとに0.1程度の値が返ってくる
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        //scrollの絶対値が0.001より小さければ何もしない（負荷を減らす処理）
        if (Mathf.Abs(scroll) < 0.001f) return;

        //移動量の計算（向き×入力×速さ×時間）
        //時間は１秒間隔で定量を移動させるために使用する
        ///Time.deltaTime...前のフレームから今のフレームにかかった時間
        Vector3 zoomMove = transform.forward * scroll * zoomSpeed * Time.deltaTime;

        //移動先（未来の座標）を計算する
        Vector3 nextPos = cameraRb.position + zoomMove;

        //移動先の高さだけを取り出して保持しておく
        float nextHeight = nextPos.y;

        //現在地から移動方向へ、移動距離方向にカメラから長さ3.0mの見えない光線を飛ばして障害物がないかを確認
        //Physics.Raycastがfalse(何も当たらない)の場合のみ、if文の中の処理をする
        if (!Physics.Raycast(cameraRb.position, zoomMove.normalized, zoomMove.magnitude + 3.0f))
        {
            //移動先の高さが設定した最小の高さから最大の高さの範囲内なら
            if (nextHeight > minHeight && nextHeight < maxHeight)
            {
                //全ての条件をクリアしたらその位置に移動する
                cameraRb.MovePosition(nextPos);
            }
        }
    }

    //左クリックドラッグで逆方向に移動する関数
    private void HandleDrag()
    {
        //左クリックが押されたとき
        if (Input.GetMouseButtonDown(0))
        {
            //現在のマウス位置を記録する
            lastMousePosition = Input.mousePosition;
        }

        //左クリックが押され続けている間
        if (Input.GetMouseButton(0)){

            //現在のマウス位置と前回の位置の差を取得する
            Vector3 delta = Input.mousePosition - lastMousePosition;

            //カメラの右方向を使って左右移動（逆方向なのでマイナス、左に進もうとするとtransform.rightがマイナスになって打ち消しあう）
            Vector3 moveX = -transform.right * delta.x * moveSpeed * Time.deltaTime;

            //カメラの前後移動を計算する（Z方向の移動）
            ///new Vector3(0,0,1)...ワールドの前方向(Z軸)
            ///delta.y...マウスの上下の移動量
            ///マイナスを付けると反対方向に動く
            ///カメラがx=90°回転しているため、transform.forwardを使うとズームしちゃう
            Vector3 moveZ = -new Vector3(0, 0, 1) * delta.y * moveSpeed * Time.deltaTime;

            //X方向とZ方向を合成して最終的な移動量を作る
            Vector3 move = moveX + moveZ;

            //移動先の位置を取得する
            //カメラの当たり判定に移動量を足す
            Vector3 nextPosition = cameraRb.position + move;

            //壁などにぶつかるかをRaycastで確認する
            if (!Physics.Raycast(cameraRb.position, move.normalized, move.magnitude + 0.5f))
            {
                //Rigidbodyを使って位置を更新する
                cameraRb.MovePosition(nextPosition);
            }

            //現在のマウス位置を次フレーム用に保存する
            lastMousePosition = Input.mousePosition;

        }

    }

}
