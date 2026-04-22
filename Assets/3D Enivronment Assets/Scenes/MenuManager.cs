using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MenuManager - Menú de inicio con selección de 2, 3 o 4 jugadores.
/// Guarda la cantidad elegida en PlayerPrefs para que la escena de juego la lea.
/// </summary>
public class MenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI (asignar en Inspector)
    // ─────────────────────────────────────────────
    [Header("=== BOTONES DE JUGADORES ===")]
    public Button btn2Players;
    public Button btn3Players;
    public Button btn4Players;

    [Header("=== BOTÓN JUGAR ===")]
    public Button btnPlay;

    [Header("=== TEXTO DE SELECCIÓN ===")]
    [Tooltip("Texto que muestra cuántos jugadores están seleccionados")]
    public TextMeshProUGUI txtSelected;

    [Header("=== NOMBRE DE LA ESCENA DE JUEGO ===")]
    [Tooltip("Nombre exacto de tu escena del juego (SampleScene, GameScene, etc.)")]
    public string gameSceneName = "SampleScene";

    [Header("=== COLORES DE LOS BOTONES ===")]
    public Color colorSelected   = new Color(0.2f, 0.8f, 0.2f);  // verde al seleccionar
    public Color colorNormal     = new Color(1f,   1f,   1f);     // blanco normal

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private int _selectedPlayers = 0;  // 0 = nada seleccionado aún

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    private void Start()
    {
        // Conectar botones
        btn2Players.onClick.AddListener(() => SelectPlayers(2));
        btn3Players.onClick.AddListener(() => SelectPlayers(3));
        btn4Players.onClick.AddListener(() => SelectPlayers(4));
        btnPlay.onClick.AddListener(StartGame);

        // El botón Jugar empieza desactivado hasta que se elija un número
        btnPlay.interactable = false;

        UpdateUI();
    }

    // ─────────────────────────────────────────────
    // LÓGICA
    // ─────────────────────────────────────────────
    private void SelectPlayers(int count)
    {
        _selectedPlayers     = count;
        btnPlay.interactable = true;
        UpdateUI();
    }

    private void StartGame()
    {
        if (_selectedPlayers == 0) return;

        // Guarda la cantidad de jugadores para leerla desde la escena de juego
        PlayerPrefs.SetInt("PlayerCount", _selectedPlayers);
        PlayerPrefs.Save();

        Debug.Log($"[Menu] Iniciando juego con {_selectedPlayers} jugadores...");
        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────
    // ACTUALIZAR VISUAL
    // ─────────────────────────────────────────────
    private void UpdateUI()
    {
        // Resalta el botón seleccionado
        SetButtonColor(btn2Players, _selectedPlayers == 2);
        SetButtonColor(btn3Players, _selectedPlayers == 3);
        SetButtonColor(btn4Players, _selectedPlayers == 4);

        // Texto de estado
        if (txtSelected != null)
        {
            txtSelected.text = _selectedPlayers == 0
                ? "Elige cuántos jugadores"
                : $"{_selectedPlayers} Jugadores seleccionados";
        }
    }

    private void SetButtonColor(Button btn, bool isSelected)
    {
        var colors      = btn.colors;
        colors.normalColor    = isSelected ? colorSelected : colorNormal;
        colors.selectedColor  = isSelected ? colorSelected : colorNormal;
        btn.colors      = colors;
    }
}
