using ServiceNotifications.Models;

namespace ServiceNotifications.Services;

public class NotificationRepository : INotificationRepository
{
    private readonly List<AlerteMessage> _alertes = new();
    private readonly object _lock = new();

    public void AjouterNotification(AlerteMessage alerte)
    {
        lock (_lock)
        {
            _alertes.Add(alerte);
            if (_alertes.Count > 100) _alertes.RemoveAt(0);
        }
    }

    public IEnumerable<AlerteMessage> GetDernieresAlertes()
    {
        lock (_lock) return _alertes.OrderByDescending(a => a.Timestamp).ToList();
    }
}
