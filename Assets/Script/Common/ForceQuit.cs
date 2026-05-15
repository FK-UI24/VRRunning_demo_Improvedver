using UnityEngine;


//システムを強制終了させるスクリプト
public static　class ForceQuit
{
    public static void HandleCriticalError()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;    //エディタ停止
#else
        UnityEngine.Application.Quit();                     //アプリ終了
#endif
    }
}