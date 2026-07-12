using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_RunningWayPoint : MonoBehaviour
{
    //消えた時のSE格納用変数
    private AudioSource SE;

    private void Awake()
    {
        SE = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit!!!");
        StartCoroutine(SEtoSetActiveFalse());
    }

    IEnumerator SEtoSetActiveFalse()
    {
        SE.Play();
        yield return new WaitForSeconds(SE.clip.length);
        gameObject.SetActive(false);

        //Start地点のウェイポイントは音が鳴らないようにするために
        //「Script_RunningStartLoad.cs」内で一番最初のウェイポイントは
        //作成した後に、当たり判定をなくしてSetActiveをfalseにするようにしている

    }
}
