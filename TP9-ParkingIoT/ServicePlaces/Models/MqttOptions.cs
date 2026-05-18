namespace ServicePlaces.Models;

public class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Topic { get; set; } = "parking/places/#";
    public string ClientId { get; set; } = "service-places-001";
}