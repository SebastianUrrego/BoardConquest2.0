using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MenuManager - Menu principal con selector de jugadores y boton Jugar.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("=== BOTONES DE JUGADORES ===")]
    public Button btn2Players;
    public Button btn3Players;
    public Button btn4Players;

    [Header("=== BOTON JUGAR ===")]
    public Button btnPlay;

    [Header("=== TEXTO DE SELECCION ===")]
    public TextMeshProUGUI txtSelected;

    [Header("=== ESCENA DE JUEGO ===")]
    public string gameSceneName = "SampleScene";

    [Header("=== COLORES ===")]
    public Color colorSelected = new Color(0.2f, 0.85f, 0.3f);
    public Color colorNormal   = new Color(0.25f, 0.25f, 0.35f);

    private int _selectedPlayers = 0;

    private void Start()
    {
        btn2Players.onClick.AddListener(() => SelectPlayers(2));
        btn3Players.onClick.AddListener(() => SelectPlayers(3));
        btn4Players.onClick.AddListener(() => SelectPlayers(4));
        btnPlay.onClick.AddListener(StartGame);

        btnPlay.interactable = false;
        SelectPlayers(2); // seleccionar 2 por defecto
    }

    private void SelectPlayers(int count)
    {
        _selectedPlayers     = count;
        btnPlay.interactable = true;
        UpdateUI();
    }

    private void StartGame()
    {
        if (_selectedPlayers == 0) return;
        PlayerPrefs.SetInt("PlayerCount", _selectedPlayers);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    private void UpdateUI()
    {
        SetButtonColor(btn2Players, _selectedPlayers == 2);
        SetButtonColor(btn3Players, _selectedPlayers == 3);
        SetButtonColor(btn4Players, _selectedPlayers == 4);

        if (txtSelected != null)
            txtSelected.text = $"{_selectedPlayers} Jugadores";
    }

    private void SetButtonColor(Button btn, bool isSelected)
    {
        var colors             = btn.colors;
        colors.normalColor     = isSelected ? colorSelected : colorNormal;
        colors.highlightedColor = isSelected ? colorSelected * 1.1f : new Color(0.35f,0.35f,0.45f);
        colors.selectedColor   = isSelected ? colorSelected : colorNormal;
        btn.colors             = colors;
    }
}
