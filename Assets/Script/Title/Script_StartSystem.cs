using UnityEngine;
using UnityEngine.SceneManagement;

public class Script_StartSystem : MonoBehaviour
{
    //セット画像を格納するオブジェクト
    [Header("セット画像")]
    [SerializeField] private GameObject setRawImage;

    //ランニング画像を格納するオブジェクト
    [Header("ランニング画像")]
    [SerializeField]private GameObject runningRawImage;

    //遷移するシーン名を格納するオブジェクト
    [Header("遷移するシーン名")]
    [SerializeField] private string nextSceneName;

    //エンターキーを押したときのSE
    [Header("スタート時SE")]
    [SerializeField] private AudioSource startSE;

    //アニメーターを格納しておくオブジェクト
    private Animator animator;

    //二重入力を防止するフラグ
    private bool isTriggerd = false;

    void Start()
    {
        try
        {
            //もしセット画像がセットされていなかったら例外処理をする
            if (setRawImage == null)
            {
                throw new System.NullReferenceException("準備ができていない...まるでわいの人生や...");
            }
            //あったら有効化する
            setRawImage.SetActive(true);

            //もしランニング画像がセットされていなかったら例外処理をする
            if (runningRawImage == null)
            {
                throw new System.NullReferenceException("ランニング画像が無いぜ！セットしな！");
            }
            //あったら無効化する
            runningRawImage.SetActive(false);

            //ランニング画像のアニメーションを格納する
            animator = runningRawImage.GetComponent<Animator>();
            //ランニング画像のアニメーションがなかったら例外
            if (animator == null)
            {
                throw new System.Exception("うごけ...ない...あに.めー...しょん...つけ....");
            }

            //もしシーン名が入力されていなかったら例外処理をする
            if (string.IsNullOrEmpty(nextSceneName))
            {
                throw new System.NullReferenceException("次はどこに行けばええんや！シーン名入れろ!");
            }

            //もしスタートSEがなかったら例外
            if (startSE == null)
            {
                throw new System.NullReferenceException("SEが無いと気合が入らないよね～");
            }
        }
        //例外をキャッチしたら強制終了する
        catch(System.Exception ex)
        {
            ForceQuit.HandleCriticalError();
        }



    }

    void Update()
    {
        //もしまだエンターがー押されていないかつ、エンターキーが押されたら
        if (!isTriggerd && Input.GetKeyDown(KeyCode.Return))
        {
            //二重入力を防止するトリガーを切り替える
            isTriggerd = true;

            //スタート時のSEを鳴らす
            startSE.Play();

            //アニメーションが終わってから画面遷移をする
            Title_SceneChange();
        }
    }

    //画像を表示し、アニメーションが終わるタイミングでシーン遷移をする関数
    private void Title_SceneChange()
    {
        try
        {
            //セット画像を無効化
            setRawImage.SetActive(false);

            //ランニング画像を有効化
            runningRawImage.SetActive(true);

            //もしランニング画像のアニメーションがあれば
            if (animator != null)
            {
                //再生が始まったアニメーションの情報を取得する
                AnimatorStateInfo runningAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);

                //アニメーションの長さが経過した瞬間にシーンを遷移する
                ///Invokeは第一引数に""を用いて、実行したい関数名を直接入れる
                ///エラーが出ないように「nameof」を使用し、
                ///変数・クラス・関数などの「名前」を、そのまま安全に文字列(string)として取得する
                Invoke(nameof(LoadNextScene), runningAnimatorStateInfo.length);
            }
            //ランニング画像のアニメーションがなかったら
            else
            {
                //すぐに遷移する
                LoadNextScene();
                
            }
        }
        catch(System.Exception ex)
        {
            ForceQuit.HandleCriticalError();
        }
    }

    //シーン遷するときに使う関数
    private void LoadNextScene()
    {
        //指定したシーンへ切り替える
        SceneManager.LoadScene(nextSceneName);
    }
}
