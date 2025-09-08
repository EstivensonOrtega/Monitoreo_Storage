namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa un problema detectado durante el análisis inteligente.
/// </summary>
public class DetectedIssue
{
    /// <summary>
    /// Tipo de problema detectado.
    /// </summary>
    public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// Severidad del problema: Critical, High, Medium, Low.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Patrón específico detectado en los logs.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Número de ocurrencias del problema.
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Servicio o método afectado por el problema.
    /// </summary>
    public string AffectedService { get; set; } = string.Empty;

    /// <summary>
    /// Lista de acciones sugeridas para resolver el problema.
    /// </summary>
    public string[] SuggestedActions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indica si el problema requiere escalamiento inmediato.
    /// </summary>
    public bool EscalationRequired { get; set; }

    /// <summary>
    /// Justificación para la decisión de escalamiento.
    /// </summary>
    public string EscalationReason { get; set; } = string.Empty;
}
