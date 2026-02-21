using UnityEngine;

public class BgmChanger : MonoBehaviour
{
    [SerializeField] AudioManager.BgmType targetBgm;
    void Start()
    {
        AudioManager.instance?.ChangeBgm(targetBgm);
    }
}
