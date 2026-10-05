using UnityEngine;
using UnityEngine.UI;

// Menú principal (sección 2.6): Nueva Partida, Continuar, Logros, Créditos y Salir.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private Transform achievementsList;
    [SerializeField] private Font font;
    [SerializeField] private GameObject firstButton;
    [SerializeField] private GameObject achievementsBack;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject controlsBack;

    void Start()
    {
        Time.timeScale = 1f;
        if (continueButton != null)
            continueButton.interactable = GameManager.Instance != null && GameManager.Instance.HasSavedGame;
        ShowMain();
    }

    public void NuevaPartida()
    {
        if (GameManager.Instance != null) GameManager.Instance.NewGame();
    }

    public void Continuar()
    {
        if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
    }

    public void Creditos()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.CreditsScene);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (achievementsPanel != null) achievementsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (firstButton != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void ShowControls()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (controlsBack != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(controlsBack);
    }

    public void ShowAchievements()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(true);
        FillAchievements();
        if (achievementsBack != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(achievementsBack);
    }

    void FillAchievements()
    {
        AchievementsList.Fill(achievementsList, font);
    }
}
