using System.Collections;
using UnityEngine;

public class Script_PictAnimationManager : MonoBehaviour
{
    [Header("ランニング画像")]
    [SerializeField] private GameObject runningImage;

    [Header("ゴール画像")]
    [SerializeField] private GameObject goalImage;

    [Header("ゴール画像の座標")]
    [SerializeField] private Vector3 targetPosition;

    [Header("ランニング画像の速さ")]
    [SerializeField] private float runningSpeed = 0f;

    [Header("ランニング画像が動き出すまでの時間")]
    [SerializeField] private float stopTime = 0f;

    //ランニング画像が移動してよいかを判断するフラグ
    private bool canMove = false;

    //目的地に到着したかを判断するフラグ
    private bool isArrived = false;


    void Start()
    {
        //最初はゴール画像を表示しない
        if (goalImage != null) goalImage.SetActive(false);

        //最初はランニング画像のみを表示する
        if (runningImage != null) runningImage.SetActive(true);

        //指定した秒数停止する用のコルーチン
        StartCoroutine(WaitStart());
    }

    //指定した秒数停止するコルーチン
    private IEnumerator WaitStart()
    {
        //指定した秒数停止
        yield return new WaitForSeconds(stopTime);

        //指定した秒数が過ぎたら、移動を許可するフラグを入れる
        canMove = true;
    }

    void Update()
    {
        //もし指定した秒数の待機時間が終了していないか、既に目的地に到着している場合は処理をスキップする
        if (canMove == false || isArrived == true) return;

        //ランニング画像の位置を格納する用変数
        RectTransform runningRect = runningImage.GetComponent<RectTransform>();

        //現在の画像の座標を記録するための変数
        Vector3 currentPos;

        //ランニング画像の座標を取得できていたらローカル座標を取得する
        if (runningRect != null)
        {
            //Canvasの中にあるRawImageのlocalpositionの取得
            currentPos = runningRect.localPosition;
        }
        else
        {
            //もしUI以外にアタッチされていた場合、通常の座標を取得する
            currentPos = runningImage.transform.position;
        }

        //現在の画像の位置から、インスペクター側から設定した座標までの距離を計算する
        float distance = Vector3.Distance(currentPos, targetPosition);

        //残りの距離が１マス未満かを判定する
        if (distance < 10f)
        {
            //もし1マス未満なら着いたとする
            isArrived = true;

            //ランニング画像を消す
            runningImage.SetActive(false);

            //もしゴール画像があったらゴール画像を表示する
            goalImage.SetActive(true);
        }
        //もし１マス未満でなかったら
        else
        {
            //手打ちした座標に向かって」毎フレームずつ少しずつ移動させる
            Vector3 nextPos = Vector3.MoveTowards(currentPos, targetPosition, runningSpeed * Time.deltaTime);

            //もしランニング画像の座標があったら
            if (runningRect != null)
            {
                //計算された移動先の座標を、ランニング画像のRectTransformに反映する
                runningRect.localPosition = nextPos;
            }
        }
    }
}
