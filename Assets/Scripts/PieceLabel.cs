using UnityEngine;
using TMPro;

/// <summary>
/// PieceLabel — etiqueta numérica flotante sobre la ficha.
/// Se resalta al ser seleccionada (emisión + color de texto).
/// Cuelga del transform PADRE (no del MeshHolder) para no verse afectado
/// por el squash/stretch de la animación.
/// </summary>
[RequireComponent(typeof(PieceController))]
public class PieceLabel : MonoBehaviour
{
    [Header("=== APARIENCIA ===")]
    public float  labelHeightOffset = 1.6f;
    public Color  normalTextColor   = Color.white;
    public Color  selectedTextColor = new Color(1f, 0.9f, 0.1f);
    public Color  selectedEmission  = new Color(0.6f, 0.45f, 0f);

    // ── Internos ──────────────────────────────────
    PieceController _piece;
    TextMeshPro     _tmp;
    Renderer[]      _renderers;   // renderers del MeshHolder
    bool            _selected;

    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _piece = GetComponent<PieceController>();
        CreateLabel();
    }

    void Start()
    {
        // Obtener renderers del MeshHolder (hijo con mesh)
        _renderers = GetComponentsInChildren<Renderer>();

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPieceHighlighted += OnHighlight;
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPieceHighlighted -= OnHighlight;
    }

    void OnHighlight(PieceController p)
    {
        bool now = (p == _piece);
        if (now == _selected) return;
        _selected = now;
        ApplyVisual();
    }

    // ── Crear etiqueta 3D ────────────────────────
    void CreateLabel()
    {
        // GameObject hijo del PADRE (no del MeshHolder)
        var go = new GameObject("PieceLabel_Canvas");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * labelHeightOffset;
        go.transform.localScale    = Vector3.one;

        // TextMeshPro directo (sin Canvas extra para evitar anidamiento raro)
        _tmp            = go.AddComponent<TextMeshPro>();
        _tmp.text       = (_piece.pieceIndex + 1).ToString();
        _tmp.fontSize   = 3f;
        _tmp.fontStyle  = FontStyles.Bold;
        _tmp.color      = normalTextColor;
        _tmp.alignment  = TextAlignmentOptions.Center;

        // Siempre mira a la cámara
        go.AddComponent<FaceCameraLabel>();
    }

    // ── Aplicar visual ───────────────────────────
    void ApplyVisual()
    {
        if (_tmp != null)
            _tmp.color = _selected ? selectedTextColor : normalTextColor;

        if (_renderers == null) return;
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                if (_selected)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionID, selectedEmission);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor(EmissionID, Color.black);
                }
            }
        }
    }
}

/// <summary>Gira el GameObject para mirar siempre a la cámara principal.</summary>
public class FaceCameraLabel : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }
}
