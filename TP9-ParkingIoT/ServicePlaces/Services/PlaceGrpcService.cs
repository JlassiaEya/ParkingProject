using Grpc.Core;
using ServicePlaces.Services;

namespace ServicePlaces.GrpcServices;

public class PlaceGrpcService : PlaceService.PlaceServiceBase
{
    private readonly IPlaceRepository _repo;
    private readonly PlaceChangeNotifier _notifier;

    public PlaceGrpcService(IPlaceRepository repo, PlaceChangeNotifier notifier)
    {
        _repo = repo;
        _notifier = notifier;
    }

    public override async Task WatchPlaces(
        Empty request,
        IServerStreamWriter<PlaceUpdate> responseStream,
        ServerCallContext context)
    {
        // Envoyer l'état initial de toutes les places
        foreach (var place in _repo.GetAll())
        {
            await responseStream.WriteAsync(new PlaceUpdate
            {
                PlaceId = place.Id,
                Numero = place.Numero,
                EstOccupee = place.EstOccupee,
                Timestamp = place.DerniereMiseAJour.ToString("O")
            });
        }

        // S'abonner aux changements en temps réel
        var channel = System.Threading.Channels.Channel.CreateUnbounded<PlaceUpdate>();
        _notifier.Subscribe(channel.Writer);

        try
        {
            await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(update);
            }
        }
        finally
        {
            _notifier.Unsubscribe(channel.Writer);
        }
    }
}