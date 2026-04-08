using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadModeSelectScene()
    {
        SceneManager.LoadScene("ModeSelectScene");
    }

    public void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}