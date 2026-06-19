using UnityEngine;

public class Script_AKIHABARAManagement : MonoBehaviour
{
    void Start()
    {
        // 自分の子を全取得
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (var mf in meshFilters)
        {
            // すでにColliderがある場合はスキップ
            if (mf.GetComponent<Collider>() == null)
            {
                MeshCollider col = mf.gameObject.AddComponent<MeshCollider>();

                // Mesh設定（重要）
                col.sharedMesh = mf.sharedMesh;
            }
        }

        Debug.Log("MeshCollider一括追加完了");
    }

}
