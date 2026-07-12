using UnityEngine;

public class OptisonManagement : MonoBehaviour
{
    [Header("センターマーカー")]
    [SerializeField] private GameObject centerMarkar;
    [Header("フレーム")]
    [SerializeField] private GameObject frame;

    private void Start()
    {
        if (Script_SettingManagement.UseCenterMarker)
        {
            centerMarkar.SetActive(true);
        }
        else
        {
            centerMarkar.SetActive(false);
        }
        if (Script_SettingManagement.UseFlame)
        {
            frame.SetActive(true);
        }
        else
        {
            frame.SetActive(false);
        }
    }
}
