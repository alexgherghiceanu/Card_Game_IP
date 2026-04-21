using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class PlayFabAuthManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI messageText;

    public void RegisterUser()
    {
        if (usernameInput.text.Length < 3)
        {
            Debug.LogWarning("Username too short.");
            if (messageText != null)
            {
                messageText.text = "Username must be at least 3 characters!";
                messageText.color = Color.red;
            }
            return;
        }

        if (passwordInput.text.Length < 6)
        {
            Debug.LogWarning("Password too short.");
            if (messageText != null)
            {
                messageText.text = "Password must be at least 6 characters!";
                messageText.color = Color.red;
            }
            return;
        }

        if (messageText != null)
        {
            messageText.text = "Creating account...";
            messageText.color = Color.yellow;
        }

        var request = new RegisterPlayFabUserRequest
        {
            Username = usernameInput.text,
            Email = emailInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    public void LoginUser()
    {
        if (messageText != null)
        {
            messageText.text = "Logging in...";
            messageText.color = Color.yellow;
        }

        var request = new LoginWithEmailAddressRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        if (messageText != null)
        {
            messageText.text = "Account created successfully! Please login.";
            messageText.color = Color.green;
        }
        Debug.Log("Account registered successfully!");
    }

    private void OnLoginSuccess(LoginResult result)
    {
        if (messageText != null)
        {
            messageText.text = "Logged in successfully!";
            messageText.color = Color.green;
        }
        Debug.Log("Successful login! Player ID: " + result.PlayFabId);

        PlayFabInventoryManager.Instance.GetCatalogAndInventory();
    }

    private void OnError(PlayFabError error)
    {
        if (messageText != null)
        {
            messageText.text = "Error: " + error.ErrorMessage;
            messageText.color = Color.red;
        }
        Debug.LogWarning("PlayFab Error: " + error.GenerateErrorReport());
    }
}