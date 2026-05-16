using UnityEngine;
using System;
using TMPro;

public class Script_NowTime : MonoBehaviour
{
    //日付を表示するTMPTextコンポーネントを格納する用変数
    [Header("日付を表示するTMPテキスト")]
    [SerializeField] private TMP_Text dateText;

    //時間を表示するTMPTextコンポーネントを格納する用変数
    [Header("時間を表示するTMPテキスト")]
    [SerializeField] private TMP_Text timeText;

    private void Update()
    {
        //インスペクターでテキストが設定されていなかったら
        //デフォルトを表示（動かない）
        if (dateText == null) return;
        if (timeText == null) return;

        //パソコンやデバイスの「今の正確な日時」を丸ごと取得する
        DateTime now = DateTime.Now;

        //取得した日時を「yyyy/MM/dd」の形に変換する
        dateText.text = now.ToString("yyyy:MM:dd");

        //取得した日時を「HH:mm:ss」の形に変換する
        timeText.text = now.ToString("HH:mm:ss");
    }
}
