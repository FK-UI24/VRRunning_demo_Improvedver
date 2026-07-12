using UnityEngine;

public class CurrentStatus : MonoBehaviour
{
    //現在の状態制御
    //切り替えはそれぞれのスクリプトから行う
    //StartUpとisRunningはどちらか一方しか成り立たない
    //isStoped,isgoaled,isErroredとStartUp,isRunningは互いに成り立つタイミングがある

    public bool isStartUp = false;
    //完全に止まったらfalseにする
    public bool isRunning = false;
    //ポーズ画面や走行時エラー時の停止する際のフラグ
    //完全に止まる前の途中で止まることが決まった段階でもtrueにする
    //つまりisStpedのみのときは完全に止まっているポーズやエラー時
    public bool isStoped = false;
    public bool isGoaled = false;
    public bool isErrored = false;

    [Header("開発モード")]
    public bool isDevelop = false;

    [Header("CanvasManagement.csがアタッチされたオブジェクト")]
    [SerializeField] private CanvasManagement canvasManagement;

    //前回の状態を保持する変数
    private int previousStatus = -1;

    private void Awake()
    {
        //最初はスタートアップ状態固定なので変える
        isStartUp = true;
    }

    private void Update()
    {
        int nowStatus = currentStatus();
        if (previousStatus != nowStatus)
        {
            canvasManagement.switchCanvas(nowStatus);
            previousStatus = nowStatus;
        }
    }

    //呼び出されると今の状態を返す
    public int currentStatus()
    {
        if (isStartUp) return 0;
        if (isRunning) return 1;
        if (isStoped) return 2;
        if (isGoaled) return 3;
        if (isErrored) return 4;
        return -1;

    }

    //呼び出されると開発モードかどうかを返す
    public int developStatus()
    {
        if (isDevelop) return 100;
        else return -100;
    }
}
