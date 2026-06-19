using System.Collections.Specialized;
using UnityEngine;

public class Script_ButtonPopUp : MonoBehaviour
{
    [Header("ポップアップさせたいオブジェクト")]
    [SerializeField] private GameObject popupObject;

    [Header("最初は表示させるか")]
    [SerializeField] private bool firstDisplay;

    private AudioSource SE;

    private void Start()
    {
        //インスペクター側で設定したbool値に応じて最初の表示を変える
        if (firstDisplay)
        {
            popupObject.SetActive(true);
        }
        else
        {
            popupObject.SetActive(false);
        }


        SE = GetComponent<AudioSource>();
    }

    //呼び出されるとポップアップオブジェクトがアクティブか確認し、アクティブでなかったら表示する
    public void popupOpen()
    {
        if (!popupObject.activeSelf)
        {
            popupObject.SetActive(true);
            SE.Play();
        }
    }

    //呼び出されるとポップアップオブジェクトがアクティブか確認し、アクティブであったら表示をやめる
    public void popupClose()
    {
        if (popupObject.activeSelf)
        {
            popupObject.SetActive(false);
            SE.Play();
        }
    }

}
