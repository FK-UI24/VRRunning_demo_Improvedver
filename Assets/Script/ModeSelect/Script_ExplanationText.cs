using UnityEngine;
using TMPro;

///ボタン側でマウス感知センサーをつける
///ボタンのインスペクター側から「Event Trigger」コンポーネントを追加する
///「Add New Event Type」ボタンを押し、「PointerEnetr」を選択する
///+ボタンを押し、生成された空欄にこのスクリプトをアタッチしているオブジェクトを格納する
///「Script_ExplanationText」→「ShowExplanation(int)」を選び、数字の入力欄に説明文に対応する番号を入力する
///続けて「Add New Event Type」ボタンを押し、「PointerExit」を選択する
///同じようにオブジェクトを登録し、「ShowRandomMessage()」を選択する

public class Script_ExplanationText : MonoBehaviour
{
    //解説を表示するTMPTextコンポーネント
    [Header("解説を表示するTMPテキスト")]
    [SerializeField] private TMP_Text explanationText;

    [Header("解説するボタンの配列")]
    [SerializeField] private GameObject[] buttons;

    [Header("ボタンの順番にそろえて、それぞれのボタンの解説")]
    [SerializeField] private string[] explanation;

    //どこも選択されていないときに表示されるメッセージのリスト
    private readonly string[] randomMessages = new string[]
    {
        "ここに選択しているボタンの詳細が表示されるよ！！！",
        "ボタンにカーソルを乗せている間は解説が出てるよ～",
        "ここで流れているBGMの共通点はわかる？",
        "これデモ版でもあり改良版でもあるから、こっちのほうが使いやすいと思う...",
        "トレッドミルって知ってた？意外と知ってる人が少ないんだね～",
        "１,２回やったらもういいかなってなるよね...",
        "外を走るほうが気持ちいいよ！",
        "このシステムは実際の企業・団体・組織などとは一切関係がないです。",
        "有酸素運動をやった日の夜って、結構気持ちいよね",
        "梅雨前はだいたい体調崩す",
        "コーヒーの飲む理由は、カフェイン摂取のため",
        "安全のために必ずトレッドミルの取っ手に触れてね！",
        "開発者がいないときに使用するのは自由だけど、けがとかの責任は一切取んないよ！",
        "これを作っているのは2026年5月半ばくらいです...就活が始まってしまいました...",
        "これを入力しているのは2026年5月17日です！",
        "カフェイン摂取すると眠くならん？",
        "ここに表示されるコメントは特に考えずに思いついたことを入れてます。",
        "プロジェクトのバックアップは取っておきな。簡単に全部消えるよ。",
        "これはいわゆるリエンジニアリングなのかな...ちがうな...",
        "プログラムにはめっちゃコメント付けとくとあとで見直すとき助かるよ！",
        "AIに全てのプログラムを書いてもらうことに少しだけう～んと思ってしまう",
        "個人開発だから、やりたいようにやれるぜ！",
        "私も入れてよ。",
        "いかいのとびらが、ひらかれた",
        "イェア！"
    };


    void Start()
    {
        //シーンが開いたときはランダムなメッセージを表示しておく
        showRandomMessage();
    }

    public void showExplanation(int n)
    {
        //登録された配列の範囲内か確認して、違かったらreturnする
        if (explanationText == null || explanation == null || n < 0 || n >= explanation.Length) return;

        //引数に対応する解説を表示する
        explanationText.text = explanation[n];
    }

    //ボタン側から呼び出し、ランダムメッセージを表示する
    public void showRandomMessage()
    {
        //もし解説がないか、ランダムメッセージがないならreturnする
        if (explanationText == null || randomMessages.Length == 0) return;

        //ランダムメッセージの数の範囲で乱数を生成する
        int randomNum = Random.Range(0, randomMessages.Length);
        explanationText.text = randomMessages[randomNum];
    }
}
