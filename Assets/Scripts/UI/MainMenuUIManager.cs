using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{

    public void SmallGrid()
    {
        GridSceneParameters.gridName = "smallGrid";
        SceneManager.LoadScene("Grid Scene");
    }

    public void MediumGrid()
    {
        GridSceneParameters.gridName = "mediumGrid";
        SceneManager.LoadScene("Grid Scene");
    }

    public void BigGrid()
    {
        GridSceneParameters.gridName = "bigGrid";
        SceneManager.LoadScene("Grid Scene");
    }


    public void Exit()
    {
        // Exit the application
        Application.Quit();

        // (This will not work in the Unity editor, but will work in a build)
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
