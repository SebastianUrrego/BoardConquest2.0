using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// MenuController — Controla el panel de menu/pausa dentro de SampleScene.
///
/// Adjunta este script a un GameObject (ej. el propio Canvas o un objeto vacio "MenuController").
/// Asigna en el Inspector:
///   - menuPanel   → el GameObject Panel del Canvas (el fondo/imagen de menu)
///   - playButton  → el Button de jugar
///   - playerCountDropdown → El dropdown de cuantos jugadores (2,3,4)
///   - playerColorDropdowns → Arreglo de Dropdowns para color de cada jugador
///
/// Al pulsar Jugar:
///   - El Canvas/Panel se desactiva
///   - El juego arranca (TurnManager comienza)
///
/// Si esta escena ES la escena de juego (SampleScene), simplemente ocultamos el panel.
/// Si quisieras ir a SampleScene desde MainMenu usa LoadGameScene().
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("=== REFERENCIAS UI ===")]
    [Tooltip("El GameObject Panel dentro del Canvas (Menu Principal)")]
    public GameObject menuPanel;

    [Tooltip("El Panel de configuracion de la partida")]
    public GameObject settingsPanel;

    [Tooltip("El boton de Jugar (Abre configuracion)")]
    public Button playButton;

    [Tooltip("El boton de Comenzar (Inicia la partida)")]
    public Button startGameButton;

    [Tooltip("El boton de Salir")]
    public Button quitButton;

    [Header("=== CONFIGURACION DE PARTIDA ===")]
    [Tooltip("Dropdown para cantidad de jugadores (Opciones: 2, 3, 4)")]
    public Dropdown playerCountDropdown;
    
    [Tooltip("Dropdowns para el color de J1, J2, J3, J4 (Opciones: Amarillo=0, Verde=1, Rojo=2, Azul=3)")]
    public Dropdown[] playerColorDropdowns;

    [Header("=== CONFIGURACION ===")]
    [Tooltip("Nombre de la escena de juego a cargar (si este script esta en el menu principal)")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Si true, solo oculta el panel y deja jugar en esta misma escena")]
    public bool hideOnPlay = true;

    private void Start()
    {
        // Conectar los botones
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);
            OnPlayerCountChanged(playerCountDropdown.value);
        }

        // Estado inicial
        if (menuPanel != null) menuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnPlayerCountChanged(int index)
    {
        int count = index + 2; // Index 0 = 2, 1 = 3, 2 = 4

        // Ocultar o mostrar los dropdowns de color extra
        if (playerColorDropdowns != null)
        {
            for (int i = 0; i < playerColorDropdowns.Length; i++)
            {
                if (playerColorDropdowns[i] != null)
                {
                    playerColorDropdowns[i].gameObject.SetActive(i < count);
                }
            }
        }
    }

    // Llamado por el boton Jugar del Menu Principal
    public void OnPlayClicked()
    {
        Debug.Log("[MenuController] Boton Jugar presionado. Abriendo configuracion.");
        
        // Ocultar menu principal, mostrar configuracion
        // Si no hay panel de configuracion, pasamos directo al juego
        if (settingsPanel != null)
        {
            // Opcional: ocultar botones principales en vez de todo el panel si comparten fondo
            // Pero lo ideal es que settingsPanel sea independiente o un hijo que se activa
            settingsPanel.SetActive(true);
            
            if (playButton != null) playButton.gameObject.SetActive(false);
            if (quitButton != null) quitButton.gameObject.SetActive(false);
        }
        else
        {
            OnStartGameClicked();
        }
    }

    // Llamado por el boton Comenzar en el panel de Configuracion
    public void OnStartGameClicked()
    {
        Debug.Log("[MenuController] Boton Comenzar presionado. Iniciando partida.");

        SaveMatchSettings();

        if (hideOnPlay)
        {
            // Modo: estamos en SampleScene, ocultamos el menu entero y el juego corre
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Iniciar el juego
            var initializer = FindObjectOfType<GameInitializer>();
            if (initializer != null) initializer.InitGame();

            var turnManager = FindObjectOfType<TurnManager>();
            if (turnManager != null) turnManager.InitGame();
        }
        else
        {
            // Modo: estamos en MainMenu, cargar la escena de juego
            LoadGameScene();
        }
    }

    private void SaveMatchSettings()
    {
        int count = 2;
        if (playerCountDropdown != null)
        {
            count = playerCountDropdown.value + 2;
        }
        PlayerPrefs.SetInt("PlayerCount", Mathf.Clamp(count, 2, 4));

        for (int i = 0; i < 4; i++)
        {
            int defaultColor = i; // 0=Yellow, 1=Green, 2=Red, 3=Blue
            if (playerColorDropdowns != null && i < playerColorDropdowns.Length && playerColorDropdowns[i] != null)
            {
                defaultColor = playerColorDropdowns[i].value;
            }
            PlayerPrefs.SetInt("PlayerColor_" + i, defaultColor);
        }

        PlayerPrefs.Save();
        Debug.Log($"[MenuController] Partida guardada: {count} jugadores.");
    }

    // Carga la escena de juego (util si este script vive en la MainMenu)
    public void LoadGameScene()
    {
        Debug.Log($"[MenuController] Cargando escena: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    // Permite pasar el numero de jugadores antes de jugar (obsoleto si usas Dropdown)
    public void SetPlayerCount(int count)
    {
        PlayerPrefs.SetInt("PlayerCount", Mathf.Clamp(count, 2, 4));
        PlayerPrefs.Save();
        Debug.Log($"[MenuController] Jugadores seleccionados: {count}");
    }

    // Boton de salir del juego
    public void QuitGame()
    {
        Debug.Log("[MenuController] Saliendo del juego.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
