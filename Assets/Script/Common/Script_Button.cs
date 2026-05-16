using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


public class Script_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    //ビデオを埋め込んでいないのとテキストのみのボタンは
    //拡大の挙動が違い、フォントサイズが変わらなかった
    //それに対応している

    //今のボタンのサイズと元の大きさを扱う用変数
    RectTransform Button_Size;
    Vector2 originalButton_Size;

    //今のフォントサイズと元の大きさを扱う用変数
    Button Button;
    TMP_Text Button_Text;
    float originalFont_Size;

    //インスペクター側で倍率を設定する用変数
    public float BigButton;

    //OnEnabledの１回目判定用変数
    bool isFirstOnEnabled = true;


    // Start is called before the first frame update
    void Awake()
    {
        Button_Size = GetComponent<RectTransform>();
        originalButton_Size = Button_Size.sizeDelta;

        //アタッチされているボタンを取得する
        Button = GetComponent<Button>();

        //上記のボタンの子オブジェクトのTMP_Textを取得する
        Button_Text = Button.GetComponentInChildren<TMP_Text>();

        originalFont_Size = Button_Text.fontSize;


    }

    // Update is called once per frame
    void Update()
    {

    }

    //これはオブジェクトがアクティブになったときに呼び出される
    //
    void OnEnable()
    {
        if (isFirstOnEnabled)
        {
            isFirstOnEnabled = false;
        }
        else
        {
            Button_Size.sizeDelta = originalButton_Size;

            //ここでフォントサイズのみを別で変えている
            Button_Text.fontSize = originalFont_Size;

        }
    }

    //マウスがのったとき
    public void OnPointerEnter(PointerEventData eventdata)
    {
        Button_Size.sizeDelta = originalButton_Size * BigButton;

        //ここでフォントサイズのみを別で変えている
        Button_Text.fontSize *= BigButton;

    }

    //マウスが外れた時
    public void OnPointerExit(PointerEventData eventdata)
    {
        Button_Size.sizeDelta = originalButton_Size;

        //ここでフォントサイズのみを別で変えている
        Button_Text.fontSize /= BigButton;
    }



}
