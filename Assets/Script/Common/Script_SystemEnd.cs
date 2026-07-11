using UnityEngine;

public class Script_SystemEnd : MonoBehaviour
{

    // ボタンを押したときに呼び出す
    public void QuitGame()
    {
        Debug.Log("ゲームを終了します");

        Application.Quit();

#if UNITY_EDITOR
        // Unityエディタ上でテストするとき用
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
