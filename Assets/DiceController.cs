using System;
using System.Collections;
using UnityEngine;
 
/// <summary>
/// DiceController - Dado 3D con animación de caída y rebote.
/// Desactiva el Rigidbody durante la animación para evitar conflictos de física.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DiceController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("=== POSICIÓN ===")]
    [Tooltip("Altura desde donde cae el dado")]
    public float dropHeight = 4f;
 
    [Header("=== ANIMACIÓN ===")]
    public float fallDuration    = 0.35f;
    public int   bounceCount     = 3;
    public float bounceHeight    = 0.5f;
    public float spinSpeed       = 720f;
 
    [Header("=== ROTACIONES POR CARA (ajusta según tu modelo) ===")]
    public Vector3 face1Rotation = new Vector3(0,   0,   0);
    public Vector3 face2Rotation = new Vector3(180, 0,   0);
    public Vector3 face3Rotation = new Vector3(90,  0,   0);
    public Vector3 face4Rotation = new Vector3(270, 0,   0);
    public Vector3 face5Rotation = new Vector3(0,   0,   90);
    public Vector3 face6Rotation = new Vector3(0,   0,   270);
 
    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    [HideInInspector] public int result = 0;
    private bool       _isRolling  = false;
    private Rigidbody  _rb;
    private Vector3    _restPosition;   // posición de reposo guardada al inicio
 
    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────
    public event Action<int> OnRollComplete;
 
    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _rb           = GetComponent<Rigidbody>();
        _restPosition = transform.position;  // guarda la posición inicial como posición de reposo
        FreezeRigidbody(false);              // comienza con física normal en reposo
    }
 
    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────
    public void Roll()
    {
        if (_isRolling) return;
        StartCoroutine(RollRoutine());
    }
 
    public bool IsRolling() => _isRolling;
 
    // ─────────────────────────────────────────────
    // ANIMACIÓN PRINCIPAL
    // ─────────────────────────────────────────────
    private IEnumerator RollRoutine()
    {
        _isRolling = true;
        result     = 0;
 
        // ── PASO 1: Congelar el Rigidbody ──────────
        // Esto evita que la física interfiera con la animación
        FreezeRigidbody(true);
 
        // Número aleatorio 1-6
        int rolled = UnityEngine.Random.Range(1, 7);
 
        // Posición de inicio (arriba)
        Vector3 startPos = _restPosition + Vector3.up * dropHeight;
        transform.position = startPos;
 
        // Rotación aleatoria de inicio
        transform.rotation = Quaternion.Euler(
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f)
        );
 
        // Eje de rotación aleatorio durante la caída
        Vector3 spinAxis = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;
 
        // ── PASO 2: CAÍDA ──────────────────────────
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
 
            // Gravedad simulada (curva cuadrática)
            transform.position = Vector3.Lerp(startPos, _restPosition, t * t);
 
            // Rotación que se frena al aterrizar
            float spinFactor = 1f - t;
            transform.Rotate(spinAxis * spinSpeed * spinFactor * Time.deltaTime);
 
            yield return null;
        }
 
        // Asegura posición exacta al aterrizar
        transform.position = _restPosition;
 
        // ── PASO 3: REBOTES ────────────────────────
        float currentBounceHeight = bounceHeight;
        for (int i = 0; i < bounceCount; i++)
        {
            float bounceDur = 0.12f + (i * 0.02f);
 
            elapsed = 0f;
            while (elapsed < bounceDur)
            {
                elapsed += Time.deltaTime;
                float t      = elapsed / bounceDur;
                float height = Mathf.Sin(t * Mathf.PI) * currentBounceHeight;
                transform.position = _restPosition + Vector3.up * height;
                yield return null;
            }
 
            transform.position     = _restPosition;
            currentBounceHeight   *= 0.38f; // rebotes progresivamente más pequeños
        }
 
        // ── PASO 4: GIRO SUAVE A LA CARA CORRECTA ──
        yield return StartCoroutine(SnapToFace(rolled, 0.18f));
 
        // ── PASO 5: Descongelar Rigidbody ──────────
        // Lo dejamos congelado en reposo para que no ruede ni caiga
        // Si quieres que tenga física después cambia esto a FreezeRigidbody(false)
        FreezeRigidbody(false);
        _rb.linearVelocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
 
        result     = rolled;
        _isRolling = false;
        OnRollComplete?.Invoke(result);
 
        Debug.Log($"[Dado] Resultado: {result}");
    }
 
    // ─────────────────────────────────────────────
    // GIRO SUAVE A LA CARA CORRECTA
    // ─────────────────────────────────────────────
    private IEnumerator SnapToFace(int face, float duration)
    {
        Quaternion startRot  = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(GetFaceRotation(face));
 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
 
        transform.rotation = targetRot;
    }
 
    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Congela o descongela el Rigidbody completamente.
    /// isKinematic=true → el script controla el movimiento (sin física).
    /// isKinematic=false → la física normal toma el control.
    /// </summary>
    private void FreezeRigidbody(bool freeze)
    {
        _rb.isKinematic = freeze;
        if (freeze)
        {
            _rb.linearVelocity        = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
 
    private Vector3 GetFaceRotation(int face)
    {
        switch (face)
        {
            case 1: return face1Rotation;
            case 2: return face2Rotation;
            case 3: return face3Rotation;
            case 4: return face4Rotation;
            case 5: return face5Rotation;
            case 6: return face6Rotation;
            default: return Vector3.zero;
        }
    }
}
 