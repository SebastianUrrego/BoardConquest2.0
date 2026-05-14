using UnityEngine;

/// <summary>
/// GameInitializer - Lee cuántos jugadores eligió el menú y activa solo esas fichas.
/// Adjunta este script a un GameObject vacío en la escena de juego llamado "GameInitializer".
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("=== FICHAS POR EQUIPO ===")]
    [Tooltip("Arrastra las 4 fichas de cada equipo en orden")]
    public GameObject[] yellowPieces = new GameObject[4];
    public GameObject[] greenPieces  = new GameObject[4];
    public GameObject[] redPieces    = new GameObject[4];
    public GameObject[] bluePieces   = new GameObject[4];

    [Header("=== VALOR POR DEFECTO (si no viene del menú) ===")]
    public int defaultPlayerCount = 2;

    // Cuántos jugadores están activos esta partida
    public static int ActivePlayers { get; private set; }

    public bool autoStart = true;

    private void Start()
    {
        if (autoStart)
        {
            InitGame();
        }
    }

    public void InitGame()
    {
        // Lee la cantidad guardada desde el menú
        ActivePlayers = PlayerPrefs.GetInt("PlayerCount", defaultPlayerCount);
        Debug.Log($"[GameInitializer] Jugadores activos: {ActivePlayers}");

        SetupPieces();
    }

    private void SetupPieces()
    {
        // Desactivar todos por defecto
        SetTeamActive(yellowPieces, false);
        SetTeamActive(greenPieces, false);
        SetTeamActive(redPieces, false);
        SetTeamActive(bluePieces, false);

        // Activar solo los que hayan sido seleccionados
        for (int i = 0; i < ActivePlayers; i++)
        {
            int colorInt = PlayerPrefs.GetInt("PlayerColor_" + i, i); // 0=Yellow, 1=Green, 2=Red, 3=Blue
            
            switch (colorInt)
            {
                case 0: SetTeamActive(yellowPieces, true); break;
                case 1: SetTeamActive(greenPieces, true); break;
                case 2: SetTeamActive(redPieces, true); break;
                case 3: SetTeamActive(bluePieces, true); break;
            }
        }
    }

    private void SetTeamActive(GameObject[] pieces, bool active)
    {
        foreach (var piece in pieces)
            if (piece != null)
                piece.SetActive(active);
    }
}
