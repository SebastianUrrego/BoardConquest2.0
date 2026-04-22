using UnityEngine;

/// <summary>
/// BoardManager - Ruta circular simple.
/// Las fichas recorren el mismo círculo indefinidamente sumando puntos por vuelta completa.
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // CASILLAS DEL TABLERO
    // ─────────────────────────────────────────────
    [Header("=== CASILLAS DEL CÍRCULO ===")]
    [Tooltip("Arrastra TODAS las casillas blancas en orden (sentido del recorrido)")]
    public Transform[] track;

    [Header("=== ÍNDICE DE ENTRADA AL CÍRCULO POR EQUIPO ===")]
    [Tooltip("En qué casilla del track entra cada equipo cuando sale de casa")]
    public int yellowStartIndex = 0;
    public int greenStartIndex  = 13;
    public int redStartIndex    = 26;
    public int blueStartIndex   = 39;

    [Header("=== CASILLAS DE ESPERA (fuera del círculo) ===")]
    public Transform[] yellowHome = new Transform[4];
    public Transform[] greenHome  = new Transform[4];
    public Transform[] redHome    = new Transform[4];
    public Transform[] blueHome   = new Transform[4];

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Devuelve el Transform de una casilla dado un índice absoluto (puede superar track.Length).
    /// El módulo garantiza que siempre dé la vuelta correctamente.
    /// </summary>
    public Transform GetSquare(int absoluteIndex)
    {
        if (track == null || track.Length == 0) return null;
        int wrapped = ((absoluteIndex % track.Length) + track.Length) % track.Length;
        return track[wrapped];
    }

    /// <summary>Total de casillas en el círculo.</summary>
    public int TrackLength => track != null ? track.Length : 0;

    /// <summary>Índice de inicio en el track para cada equipo.</summary>
    public int GetStartIndex(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Yellow: return yellowStartIndex;
            case PlayerColor.Green:  return greenStartIndex;
            case PlayerColor.Red:    return redStartIndex;
            case PlayerColor.Blue:   return blueStartIndex;
            default: return 0;
        }
    }

    /// <summary>Casillas de espera (casa) del equipo.</summary>
    public Transform[] GetHomeSquares(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Yellow: return yellowHome;
            case PlayerColor.Green:  return greenHome;
            case PlayerColor.Red:    return redHome;
            case PlayerColor.Blue:   return blueHome;
            default: return yellowHome;
        }
    }

    // ─────────────────────────────────────────────
    // GIZMOS — Visualiza el recorrido en el Editor
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (track == null || track.Length < 2) return;

        Gizmos.color = Color.white;
        for (int i = 0; i < track.Length; i++)
        {
            if (track[i] == null) continue;
            Transform next = track[(i + 1) % track.Length];
            if (next != null)
                Gizmos.DrawLine(track[i].position, next.position);
            Gizmos.DrawSphere(track[i].position, 0.1f);
        }

        // Puntos de entrada por equipo
        DrawStart(yellowStartIndex, Color.yellow);
        DrawStart(greenStartIndex,  Color.green);
        DrawStart(redStartIndex,    Color.red);
        DrawStart(blueStartIndex,   Color.cyan);
    }

    private void DrawStart(int idx, Color c)
    {
        if (track == null || idx >= track.Length || track[idx] == null) return;
        Gizmos.color = c;
        Gizmos.DrawSphere(track[idx].position + Vector3.up * 0.35f, 0.22f);
    }
#endif
}

public enum PlayerColor { Yellow, Green, Red, Blue }
