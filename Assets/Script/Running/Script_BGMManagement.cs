using System.Collections;
using UnityEngine;

public class Script_BGMManagement : MonoBehaviour
{
    private AudioSource[] BGMs;
    private int currentIndex = 0;

    private void Start()
    {
        // AudioSourceを取得
        BGMs = GetComponents<AudioSource>();

        // 念のため全停止
        foreach (AudioSource bgm in BGMs)
        {
            bgm.Stop();
            bgm.playOnAwake = false;
        }

        // 起動時に1回だけランダムな曲順を作る
        ShuffleBGMs();

        // 再生開始
        StartCoroutine(PlayBGMs());
    }

    private IEnumerator PlayBGMs()
    {
        while (true)
        {
            // 現在の曲を再生
            BGMs[currentIndex].Play();

            // 曲が終わるまで待機
            yield return new WaitWhile(() => BGMs[currentIndex].isPlaying);

            // 次の曲へ
            currentIndex++;

            // 最後まで行ったら先頭へ戻る
            if (currentIndex >= BGMs.Length)
            {
                currentIndex = 0;
            }
        }
    }

    private void ShuffleBGMs()
    {
        for (int i = BGMs.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);

            AudioSource temp = BGMs[i];
            BGMs[i] = BGMs[rand];
            BGMs[rand] = temp;
        }
    }
}