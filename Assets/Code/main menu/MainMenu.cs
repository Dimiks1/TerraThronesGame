using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("Start Game!");
        SceneManager.LoadScene("GameScene");
    }

    public void OpenDeckEditor()
    {
        Debug.Log("Open Deck Editor!");
        SceneManager.LoadScene("DeckEditorScene");
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game!");
        Application.Quit();
    }
}
