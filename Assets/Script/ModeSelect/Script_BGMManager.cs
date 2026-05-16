using UnityEngine;
using UnityEngine.SceneManagement;

//このスクリプトは、ほかのどのシーンに遷移しても、ゲームが起動している間は常に稼働し続けるよ！！！

public class Script_BGMManager : MonoBehaviour
{
    //どこからでもこのマネージャーにアクセスできるようにする変数（シングルトン）
    //ほかのスクリプトから「BGMMnager.Instance」の形でこのマネージャーを呼び出すための窓口
    //この変数には、このクラスの本物がまるごとはいっているということ
    //このスクリプトがアタッチされたオブジェクトを、ゲームの世界に単体で存在させている（上記のコメントの面倒な言い方）
    //Instanceは変数名、{ get; private set; }はルールを表す
    //getは外部から自由に中身を覗いたり、関数を実行したりできる
    //private setはInstance=〇〇などで中身を書き換える処理は、このスクリプトの内部からしかできないようにしている
    ///シングルトンは、クラスから作られるインスタンスを常に1つだけに制限する。
    ///通常、同じクラスから複数のインスタンスを生成できる。
    ///しかし、シングルトンを使うことで「世界に一つだけのインスタンス」を保証し、
    ///どこからでもそのオブジェクトにアクセスできるようになる
    public static Script_BGMManager BGMManagerInstance { get; private set; }

    //実際にゲーム内で流すBGMを格納する用変数
    private AudioSource BGM;

    //インスペクター側でBGMを格納する用配列変数
    [Header("「モード選択」と「ルート選択」で流すBGM")]
    [SerializeField] private AudioClip[] BGMList;

    //直前にいたシーンの名前を保存する用変数
    private string currentSceneName;

    private void Awake()
    {
        //もしまだこのスクリプトが存在していない場合
        if (BGMManagerInstance == null)
        {
            //自分自身を唯一の存在として登録する
            BGMManagerInstance = this;

            //シーンが切り替わっても、このBGMが自動消去されないようにする
            DontDestroyOnLoad(gameObject);

            //Unityのシステムに対し「新しいシーンの読み込みが終わったらOnSceneLoadedを実行して」と登録する
            SceneManager.sceneLoaded += OnSceneLoaded;

            //シーン起動時にこのスクリプトが配置されているシーンの名前を取得して格納する
            currentSceneName = SceneManager.GetActiveScene().name;

            //このスクリプトがアタッチされているオブジェクトのAudioSourceコンポーネントを取得する
            BGM = GetComponent<AudioSource>();

            //１曲目開始
            PlayRandomBGM();
        }
        //すでに別のシーンなどで、これが存在している場合
        else
        {
            Destroy(gameObject);
        }
    }

    //このスクリプト（オブジェクト）が削除されるときにUnityが自動で呼び出す関数
    private void OnDestroy()
    {
        //もし自分が登録されていたら
        if (BGMManagerInstance == this)
        {
            //登録したままだとエラーになるから、シーン読み込み時のイベント連動を解除する
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    //新しいシーンが読み込まれた瞬間に、Unityのシステムが自動的に呼び出す用関数
    //Unityがシーン切り替わり時に読み込まれるように設定を上記でしているので引数は自動で入れる
    ///第一引数は新しく読み込まれたシーンのデータ
    ///第二引数はそのシーンをどういう方法で開いたかの方法。今回は使っていないがUnityのがちがち規約的に必要
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        //新しく読み込まれた遷移先のシーン名を格納する
        string nextSceneName = scene.name;

        //遷移先のシーンが「ModeSelect」か「CreateRoute」だったら
        if(nextSceneName== "ModeSelect"||nextSceneName== "CreateRoute")
        {
            //直前にいたシーンが「ModeSelect」で、かつ遷移先が「CreateRoute」であるか
            if (currentSceneName == "ModeSelect" && nextSceneName == "CreateRoute")
            {
                //なにもしないぜ！寿司もつくらないぜ！BGMも特に変えないぜ！
            }
            //「Title」や、ランニング画面からのリタイアなどでシーンを戻ったりした場合
            else
            {
                PlayRandomBGM();
            }
        }
        //管理対象の「ModeSelect」か「CreateRoute」以外のシーンに戦死した場合
        else
        {
            BGM.Stop();
        }

        //次にシーンが切り替わるときに備えて、今の遷移先シーン名を「直前にいたシーン名」にする
        currentSceneName = nextSceneName;
    }

    //登録されたBGMのリストからランダムに１曲を選んで再生する
    public void PlayRandomBGM()
    {
        //インスペクターで曲のリストが空だった場合に、エラーで止まらないようにする
        if (BGMList == null || BGMList.Length == 0) return;

        //リストの曲数に合わせて、ランダムな数を生成する
        int randomNum = Random.Range(0, BGMList.Length);

        //選ばれた番号の音楽をセットする
        BGM.clip = BGMList[randomNum];

        //セットしたBGMを流す
        BGM.Play();
    }

}
