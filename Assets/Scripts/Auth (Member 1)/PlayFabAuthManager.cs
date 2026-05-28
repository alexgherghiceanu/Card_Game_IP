using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayFabAuthManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject logoutButton; // Noul tau buton de Logout

    [Header("Input Fields (TextMeshPro)")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField usernameInput;

    [Header("Feedback Text")]
    public TextMeshProUGUI messageText;

    private bool isLoggedIn = false;

    private void Start()
    {
        // Ne asiguram ca butonul de logout e ascuns cand porneste jocul
        if (logoutButton != null)
        {
            logoutButton.SetActive(false);
        }
    }

    // --- REGISTRARE ---
    public void RegisterButton()
    {
        if (passwordInput.text.Length < 6)
        {
            UpdateMessage("Password must be at least 6 characters!", Color.red);
            return;
        }

        var request = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
            Username = usernameInput.text,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        UpdateMessage("Registered and logged in successfully!", Color.green);
        HandleLoginSuccess();
    }

    // --- LOGARE ---
    public void LoginButton()
    {
        if (isLoggedIn)
        {
            Debug.LogWarning("Un utilizator este deja logat pe acest PC!");
            return;
        }

        var request = new LoginWithEmailAddressRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Logare cu succes!");

        // 1. Optional: Daca aveai un text de feedback pe ecran, il poti pune pe verde
        if (messageText != null)
        {
            messageText.text = "Logged in! Loading game...";
            messageText.color = Color.green;
        }

        // 2. Ii spunem Managerului de Inventar sa inceapa sa descarce cartile in fundal
        if (PlayFabInventoryManager.Instance != null)
        {
            PlayFabInventoryManager.Instance.GetCatalogAndInventory();
        }

        // 3. INCARCAM SCENA URMATOARE (Asigura-te ca numele corespunde exact!)
        SceneManager.LoadScene("MainMenuScene");
    }

    // --- LOGICA DE SUCCES COMBINATA ---
    private void HandleLoginSuccess()
    {
        isLoggedIn = true;

        if (PlayFabInventoryManager.Instance != null)
        {
            PlayFabInventoryManager.Instance.GetCatalogAndInventory();
        }

        // Load the next scene 
        SceneManager.LoadScene(1);
    }

    // --- DECONECTARE (LOGOUT) ---
    public void LogoutButton()
    {
        // Aceasta functie sterge sesiunea locala din memoria Unity
        PlayFabClientAPI.ForgetAllCredentials();

        isLoggedIn = false;

        // Resetam interfata
        if (logoutButton != null)
        {
            logoutButton.SetActive(false);
        }

        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }

        // Golim campurile de input (optional, dar recomandat)
        emailInput.text = "";
        passwordInput.text = "";
        usernameInput.text = "";

        UpdateMessage("Logged out successfully.", Color.yellow);
    }

    // --- GESTIONAREA ERORILOR ---
    private void OnError(PlayFabError error)
    {
        UpdateMessage(error.ErrorMessage, Color.red);
        Debug.LogError("Eroare PlayFab: " + error.GenerateErrorReport());
    }

    private void UpdateMessage(string msg, Color color)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.color = color;
        }
    }
}