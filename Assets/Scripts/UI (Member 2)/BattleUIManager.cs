using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleUIManager : MonoBehaviour
{
    [Header("Zone Cărți (Pentru Membrul 4)")]
    public Transform playerHandArea;
    public Transform playerBoardArea;
    public Transform enemyBoardArea;

    [Header("Feedback Vizual (Hover)")]
    public Outline playerBoardOutline;

    [Header("HUD Stats (Pentru Membrul 3)")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;
    public Button endTurnButton;

    [Header("End Game (Pentru Membrul 3)")]
    public GameObject endGamePanel;
    public TextMeshProUGUI resultText;
    public Button returnToMenuButton;

    [Header("Audio (SFX)")]
    public AudioSource sfxSource;
    public AudioClip clickSound;
    public AudioClip cardPlaceSound;
    public AudioClip attackSound;

    [Header("Game Feel (VFX)")]
    public Transform uiCanvas; // Ce obiect tremură
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 15f; // Puterea tremuratului în pixeli
    private Vector3 originalPos;
    public GameObject cardDeathParticlePrefab;

    public void SetBoardHighlight(bool isHighlighted) { if (playerBoardOutline != null) playerBoardOutline.enabled = isHighlighted; }
    public void UpdatePlayerHP(int newHP) { if (playerHPText != null) playerHPText.text = "HP: " + newHP; }
    public void UpdatePlayerMana(int currentMana, int maxMana) { if (playerManaText != null) playerManaText.text = "Mana: " + currentMana + "/" + maxMana; }
    public void GoToMainMenu() { SceneManager.LoadScene("MainMenuScene"); }

    public void ShowEndGameScreen(bool isVictory)
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            if (resultText != null)
            {
                resultText.text = isVictory ? "VICTORY" : "DEFEAT";
                resultText.color = isVictory ? Color.green : Color.red;
            }
        }
    }

    // --- FUNCȚII AUDIO & VFX ---
    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null) sfxSource.PlayOneShot(clickSound);
    }

    public void PlayCardPlaceSound()
    {
        if (sfxSource != null && cardPlaceSound != null) sfxSource.PlayOneShot(cardPlaceSound);
    }

    public void PlayAttackSound()
    {
        if (sfxSource != null && attackSound != null) sfxSource.PlayOneShot(attackSound);

        // Când se cheamă sunetul de atac, declanșăm automat și cutremurul!
        if (uiCanvas != null) StartCoroutine(ShakeCoroutine());
    }


    // Logica matematică a cutremurului
    private IEnumerator ShakeCoroutine()
    {
        originalPos = uiCanvas.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            uiCanvas.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        uiCanvas.localPosition = originalPos; // Întoarce ecranul la normal
    }

    public void SpawnDeathParticles(Vector3 cardPosition)
    {
        if (cardDeathParticlePrefab != null)
        {
            // Creăm explozia exact pe coordonatele cărții care a murit
            GameObject particles = Instantiate(cardDeathParticlePrefab, cardPosition, Quaternion.identity);

            // O ștergem din memorie după 1.5 secunde
            Destroy(particles, 1.5f);
        }
    }
}