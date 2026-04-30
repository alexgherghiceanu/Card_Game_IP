using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    // Funcțiile care vor fi apelate de butoane
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
        // Presupunând că veți avea o scenă separată pentru magazin
        SceneManager.LoadScene("StoreScene");
    }
}