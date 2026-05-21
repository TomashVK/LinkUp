using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const int TargetFrameRate = 60;

    private void Awake()
    {
        Application.targetFrameRate = TargetFrameRate;
    }
}
