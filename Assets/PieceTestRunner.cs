using UnityEngine;

/// <summary>
/// PieceTestRunner — Solo para probar el movimiento en Play Mode.
/// Adjunta este script a cualquier GameObject temporal en la escena.
/// ELIMÍNALO antes de hacer build final.
/// </summary>
public class PieceTestRunner : MonoBehaviour
{
    [Header("Ficha a probar")]
    public PieceController piece;

    [Header("Pasos a mover con la tecla Space")]
    [Range(1, 12)]
    public int stepsOnSpace = 6;

    [Header("Sacar de casa con tecla H")]
    public KeyCode leaveHomeKey = KeyCode.H;
    public KeyCode moveKey      = KeyCode.Space;

    private void Update()
    {
        if (piece == null) return;

        // H → Saca la ficha de casa
        if (Input.GetKeyDown(leaveHomeKey))
        {
            Debug.Log("[Test] Sacando ficha de casa...");
            piece.LeaveHome();
        }

        // Space → Mueve N casillas
        if (Input.GetKeyDown(moveKey) && !piece.IsMoving())
        {
            Debug.Log($"[Test] Moviendo {stepsOnSpace} casillas...");
            piece.Move(stepsOnSpace);
        }
    }

    private void OnGUI()
    {
        if (piece == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 16 };
        GUI.Box(new Rect(10, 10, 260, 90),
            $"Equipo:  {piece.teamColor}\n" +
            $"Vueltas: {piece.Laps}\n" +
            $"Puntos:  {piece.Points}\n" +
            $"[H] Salir casa  [Space] Mover {stepsOnSpace}",
            style);
    }
}
