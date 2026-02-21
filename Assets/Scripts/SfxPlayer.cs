using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    [SerializeField] AudioManager.Sfx targetSfx;
    void Start()
    {
        AudioManager.instance?.PlaySfx(targetSfx);
    }

}
