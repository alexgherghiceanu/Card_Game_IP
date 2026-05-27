using UnityEngine;
using TMPro; // Pentru elementele de text
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

public class Shop_script : MonoBehaviour
{
    [Header("Conexiuni UI")]
    public TextMeshProUGUI balanceText; // Aici afisam banii
    public TextMeshProUGUI resultText;  // Aici afisam ce carte a picat

    void Start()
    {
        resultText.text = "Bine ai venit la magazin!";
        UpdateBalance();
    }

    // 1. Cerem serverului sa ne spuna cati bani avem
    public void UpdateBalance()
    {
        var request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request, OnInventorySuccess, OnError);
    }

    private void OnInventorySuccess(GetUserInventoryResult result)
    {
        // Verificam daca jucatorul are moneda noastra "CO" (Coins)
        if (result.VirtualCurrency != null && result.VirtualCurrency.ContainsKey("CO"))
        {
            balanceText.text = "Bani (CO): " + result.VirtualCurrency["CO"].ToString();
        }
        else
        {
            balanceText.text = "Bani (CO): 0";
        }
    }

    // 2. Functia care va fi pusa pe butonul de BUY
    public void BuyPack()
    {
        resultText.text = "Deschidem pachetul...";

        var request = new PurchaseItemRequest
        {
            CatalogVersion = "Cards",    // Numele catalogului tau
            ItemId = "booster_pack",     // ID-ul pachetului din PlayFab
            VirtualCurrency = "CO",      // Moneda cu care platim
            Price = 100                  // Pretul pachetului
        };

        PlayFabClientAPI.PurchaseItem(request, OnPackPurchased, OnBuyError);
    }

    private void OnPackPurchased(PurchaseItemResult result)
    {
        // 1. Scadem banii de pe ecran
        UpdateBalance();

        string idCarteNoua = "";
        string numeCarte = "";
        // 2. Cautam in obiectele primite
        if (result.Items != null && result.Items.Count > 0)
        {
            foreach (var item in result.Items)
            {
                // Ignoram pachetul in sine, vrem doar ce a picat din el!
                if (item.ItemId != "booster_pack")
                {
                    idCarteNoua = item.ItemId;
                    numeCarte = item.DisplayName;
                    break; // Am gasit cartea, ne oprim.
                }
            }
        }

        // 3. Afisam rezultatul
        if (!string.IsNullOrEmpty(idCarteNoua))
        {
            resultText.text = "<color=green>Felicitari! Ai primit cartea: " + numeCarte + "</color>";
        }
        else
        {
            resultText.text = "<color=yellow>Pachetul pare sa fie gol! (Vezi Pasul 2)</color>";
        }

        // 4. Actualizam inventarul de lupta
        if (PlayFabInventoryManager.Instance != null)
        {
            PlayFabInventoryManager.Instance.GetPlayerInventory();
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private void OnBuyError(PlayFabError error)
    {
        resultText.text = "<color=red>Eroare! Fonduri insuficiente?</color>";
        Debug.LogError(error.GenerateErrorReport());
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}