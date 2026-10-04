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
    }

    public void ShowAchievements()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(true);
        FillAchievements();
    }

    // Llena la lista con los 10 logros; los que faltan se ven apagados
    void FillAchievements()
    {
        var am = AchievementManager.Instance;
        if (am == null || achievementsList == null) return;

        foreach (Transform child in achievementsList) Destroy(child.gameObject);

        foreach (var logro in am.Logros)
        {
            bool got = am.IsUnlocked(logro.tipo);

            var row = new GameObject(logro.nombre, typeof(RectTransform));
            row.transform.SetParent(achievementsList, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 64);

            var iconGo = new GameObject("Icono", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = logro.icono;
            icon.preserveAspect = true;
            icon.color = got ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.8f);

            var textGo = new GameObject("Texto", typeof(RectTransform));
            textGo.transform.SetParent(row.transform, false);
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 60);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = got ? new Color(1f, 0.9f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
            text.text = $"<b>{logro.nombre}</b>  {(got ? "(obtenido)" : "")}\n{logro.condicion}";
        }
    }
}
