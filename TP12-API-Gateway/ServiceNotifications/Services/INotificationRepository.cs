using ServiceNotifications.Models;

namespace ServiceNotifications.Services;

public interface INotificationRepository
{
    void AjouterNotification(AlerteMessage alerte);
    IEnumerable<AlerteMessage> GetDernieresAlertes();
}
