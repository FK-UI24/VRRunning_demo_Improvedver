using System.Collections;
using TMPro;
using UnityEngine;

public class TypeWriterText : MonoBehaviour
{
    [Header("タイプライター効果を付けたいテキストオブジェクト")]
    [SerializeField] private GameObject textObject;

    [Header("文字が表示される間隔(s)")]
    [SerializeField] private float interval = 0.05f;

    [Header("テキストをループさせるか")]
    [SerializeField] private bool loop = true;

    private TMP_Text tmpText;
    private string fullText;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        tmpText = textObject.GetComponentInChildren<TMP_Text>();
        fullText = tmpText.text;
        // 最初は空にしておく
        tmpText.text = ""; 
    }

    private void OnEnable()
    {
        StartTyping();
    }

    // タイプライターを開始する（何度呼んでも最初から再生）
    public void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeWriterLoop());
    }

    private IEnumerator TypeWriterLoop()
    {
        do
        {
            // 最初は完全に空にして少し待つ
            tmpText.text = "";
            yield return new WaitForSeconds(interval); // 最初の空状態をちょっと見せる

            // 1文字ずつ追加
            foreach (char c in fullText)
            {
                tmpText.text += c;
                yield return new WaitForSeconds(interval);
            }

            if (!loop)
                break;

            // 全部表示後、少し待って次のループで再び空に戻す
            yield return new WaitForSeconds(0.5f);

        } while (loop);
    }
}
