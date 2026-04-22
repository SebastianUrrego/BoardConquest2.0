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

    private void Awake()
    {
        // Lee la cantidad guardada desde el menú
        ActivePlayers = PlayerPrefs.GetInt("PlayerCount", defaultPlayerCount);
        Debug.Log($"[GameInitializer] Jugadores activos: {ActivePlayers}");

        SetupPieces();
    }

    private void SetupPieces()
    {
        // Siempre activos: Amarillo y Verde (jugadores 1 y 2)
        SetTeamActive(yellowPieces, true);
        SetTeamActive(greenPieces,  true);

        // Rojo activo solo si hay 3 o 4 jugadores
        SetTeamActive(redPieces,  ActivePlayers >= 3);

        // Azul activo solo si hay 4 jugadores
        SetTeamActive(bluePieces, ActivePlayers >= 4);
    }

    private void SetTeamActive(GameObject[] pieces, bool active)
    {
        foreach (var piece in pieces)
            if (piece != null)
                piece.SetActive(active);
    }
}
