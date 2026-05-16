using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    AudioSource SE;

    [SerializeField] private string nextScene;


    void Start()
    {
        SE = GetComponent<AudioSource>();

    }

    void Update()
    {

    }

    public void NextScene()
    {
        StartCoroutine(SEtoChange());
    }

    IEnumerator SEtoChange()
    {
        SE.Play();
        yield return new WaitForSeconds(SE.clip.length);
        SceneManager.LoadScene(nextScene);
    }

}