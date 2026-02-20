using UnityEngine;

public class Player : MonoBehaviour
{
    Sprite sprite;
    int currentX = 0;
    int currentY = 0;

    Player(sprite sp, int x, int y)
    {
        sprite = sp;
        currentX = x;
        currentY = y;
    }
    // gamemanager 에서 플레이어 생성

    // Update is called once per frame
    void Update()
    {
        
    }
}
