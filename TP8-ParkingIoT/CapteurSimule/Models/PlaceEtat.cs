namespace CapteurSimule.Models;

/// <summary>
/// Représente l'état d'une place de parking
/// publié par un capteur IoT sur le broker MQTT.
/// </summary>
public record PlaceEtat
{
    /// <summary>Identifiant unique de la place (ex: A1, B5, C12)</summary>
    public string PlaceId { get; init; } = string.Empty;

    /// <summary>État de la place : "libre" ou "occupee"</summary>
    public string Etat { get; init; } = string.Empty;

    /// <summary>Horodatage ISO 8601 de la mesure</summary>
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>Identifiant du capteur physique</summary>
    public string CapteurId { get; init; } = string.Empty;

    /// <summary>Niveau de batterie du capteur en % (0-100)</summary>
    public int NiveauBatterie { get; init; }

    /// <summary>Signal RSSI du capteur en dBm (force du signal WiFi)</summary>
    public int Rssi { get; init; }
}
