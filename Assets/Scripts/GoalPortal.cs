using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalPortal : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName = "GameScene_Ch2";

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}