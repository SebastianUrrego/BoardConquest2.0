using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MenuController — Controla el panel de menu/pausa dentro de SampleScene.
///
/// Adjunta este script a un GameObject (ej. el propio Canvas o un objeto vacio "MenuController").
/// Asigna en el Inspector:
///   - menuPanel   → el GameObject Panel del Canvas (el fondo/imagen de menu)
///   - playButton  → el Button de jugar
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
    [Tooltip("El GameObject Panel dentro del Canvas")]
    public GameObject menuPanel;

    [Tooltip("El boton de Jugar")]
    public Button playButton;

    [Header("=== CONFIGURACION ===")]
    [Tooltip("Nombre de la escena de juego a cargar (si este script esta en el menu principal)")]
    public string gameSceneName = "SampleScene";

    [Tooltip("Si true, solo oculta el panel y deja jugar en esta misma escena")]
    public bool hideOnPlay = true;

    private void Start()
    {
        // Conectar el boton automaticamente si no se hizo en el Inspector
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        // Asegurarse de que el panel arranque visible
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    // Llamado por el boton Jugar (tambien puede asignarse en el Inspector manualmente)
    public void OnPlayClicked()
    {
        Debug.Log("[MenuController] Boton Jugar presionado.");

        if (hideOnPlay)
        {
            // Modo: estamos en SampleScene, solo ocultamos el panel y el juego corre
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Tambien ocultar todo el Canvas si no hay mas elementos UI necesarios
            // (opcional: descomenta si quieres ocultar el Canvas entero)
            // gameObject.SetActive(false);
        }
        else
        {
            // Modo: estamos en MainMenu, cargar la escena de juego
            LoadGameScene();
        }
    }

    // Carga la escena de juego (util si este script vive en la MainMenu)
    public void LoadGameScene()
    {
        Debug.Log($"[MenuController] Cargando escena: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    // Permite pasar el numero de jugadores antes de jugar
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
