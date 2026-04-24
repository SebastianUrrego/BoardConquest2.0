using UnityEngine;

/// <summary>
/// CameraController — cámara dinámica que sigue la ficha seleccionada.
///
/// Comportamiento:
///   • Modo TABLERO  : vista cenital del tablero completo (posición por defecto).
///   • Modo SEGUIR   : cuando hay una ficha moviéndose, la cámara se acerca y la sigue.
/// Transiciones siempre con SmoothDamp para suavidad.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── Referencias ─────────────────────────────
    [Header("=== POSICIÓN DEL TABLERO (vista general) ===")]
    [Tooltip("Posición y rotación que tiene la cámara cuando nadie se mueve")]
    public Transform boardViewPoint;   // GameObject vacío con la pos/rot deseada

    [Header("=== PARÁMETROS DE SEGUIMIENTO ===")]
    public float followHeight   = 6f;   // altura relativa sobre la ficha
    public float followDistance = 5f;   // distancia detrás de la ficha
    public float followFOV      = 50f;  // FOV al seguir
    public float boardFOV       = 60f;  // FOV en vista de tablero

    [Header("=== SUAVIZADO ===")]
    public float posSmooth  = 0.35f;   // tiempo SmoothDamp posición
    public float rotSmooth  = 0.25f;   // tiempo SmoothDamp rotación
    public float fovSmooth  = 0.4f;    // tiempo SmoothDamp FOV

    // ── Estado interno ──────────────────────────
    private Camera       _cam;
    private Transform    _target;        // ficha que se está siguiendo (null = tablero)
    private Vector3      _velPos;
    private float        _velFov;

    // Rotación suavizada
    private Quaternion _currentRot;
    private float      _rotVelocity;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _currentRot = transform.rotation;
    }

    void Start()
    {
        // Suscribirse al evento de ficha resaltada del TurnManager
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPieceHighlighted += OnPieceHighlighted;
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPieceHighlighted -= OnPieceHighlighted;
    }

    void OnPieceHighlighted(PieceController piece)
    {
        _target = (piece != null) ? piece.transform : null;
    }

    void LateUpdate()
    {
        if (_target != null && _target.gameObject.activeSelf)
            FollowPiece();
        else
            ReturnToBoard();
    }

    // ── Seguir la ficha ──────────────────────────
    void FollowPiece()
    {
        // Posición objetivo: detrás y arriba de la ficha
        Vector3 desiredPos = _target.position
                           - _target.forward * followDistance
                           + Vector3.up      * followHeight;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _velPos, posSmooth);

        // Mirar a la ficha
        Quaternion targetRot = Quaternion.LookRotation(
            (_target.position + Vector3.up * 0.5f) - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, Time.deltaTime / rotSmooth);

        // FOV
        if (_cam != null)
            _cam.fieldOfView = Mathf.SmoothDamp(
                _cam.fieldOfView, followFOV, ref _velFov, fovSmooth);
    }

    // ── Volver a vista de tablero ────────────────
    void ReturnToBoard()
    {
        Vector3    targetPos = boardViewPoint != null
            ? boardViewPoint.position : new Vector3(0, 18, 0);
        Quaternion targetRot = boardViewPoint != null
            ? boardViewPoint.rotation : Quaternion.Euler(70, 0, 0);

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref _velPos, posSmooth * 1.5f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, Time.deltaTime / (rotSmooth * 1.5f));

        if (_cam != null)
            _cam.fieldOfView = Mathf.SmoothDamp(
                _cam.fieldOfView, boardFOV, ref _velFov, fovSmooth);
    }
}
