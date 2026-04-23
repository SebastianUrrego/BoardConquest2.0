using UnityEngine;

/// <summary>
/// PieceSelector - Adjunta este script a cada ficha.
/// Detecta clics del raton y notifica al TurnManager que ficha fue seleccionada.
/// REQUISITO: La ficha debe tener un Collider para que OnMouseDown funcione.
/// </summary>
public class PieceSelector : MonoBehaviour
{
    private PieceController _piece;

    private void Awake()
    {
        _piece = GetComponent<PieceController>();
        if (_piece == null) _piece = GetComponentInParent<PieceController>();
    }

    private void OnMouseDown()
    {
        if (TurnManager.Instance == null || _piece == null) return;
        if (TurnManager.Instance.Phase == TurnManager.TurnPhase.WaitingForPieceSelection)
            TurnManager.Instance.SelectPiece(_piece);
    }
}
