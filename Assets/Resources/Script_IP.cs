using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName ="IPConfig",menuName ="Config/IP Address")]
public class Script_IP : ScriptableObject
{
    [Header("IPアドレス")]
    public string ipaddress;
    [Header("ポート番号")]
    public string port;
}
