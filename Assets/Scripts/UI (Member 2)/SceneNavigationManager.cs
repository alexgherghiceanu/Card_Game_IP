using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void LoadBattle()
    {
        SceneManager.LoadScene("BattleScene");
    }

    public void LoadCollection()
    {
        SceneManager.LoadScene("CollectionScene");
    }

    public void LoadStore()
    {
        SceneManager.LoadScene("StoreScene");
    }
}