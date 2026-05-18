using Grpc.Core;
using ParkingGrpc.Protos;
using ParkingConsole.Services;
using ParkingConsole.Models;
using GrpcPlaceService = ParkingGrpc.Protos.PlaceService;
namespace ParkingGrpc.Services;

/// <summary>
/// Implémentation du service gRPC pour les places
/// </summary>
public class PlaceGrpcService : GrpcPlaceService.PlaceServiceBase
{
    private readonly IPlaceService _placeService;
    private readonly ILogger<PlaceGrpcService> _logger;
    private readonly PlaceEventService _eventService;

    public PlaceGrpcService(
        IPlaceService placeService,
        ILogger<PlaceGrpcService> logger,
        PlaceEventService eventService)
    {
        _placeService = placeService;
        _logger = logger;
        _eventService = eventService;
    }

    /// <summary>
    /// Récupère toutes les places
    /// </summary>
    public override Task<PlaceList> GetAllPlaces(Empty request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetAllPlaces - Début");

        var places = _placeService.Obtenir();
        var response = new PlaceList();

        foreach (var place in places)
        {
            response.Places.Add(MapToProto(place));
        }

        _logger.LogInformation("gRPC GetAllPlaces - {Count} places récupérées", places.Count);

        return Task.FromResult(response);
    }

    /// <summary>
    /// Récupère une place par ID
    /// </summary>
    public override Task<PlaceResponse> GetPlace(PlaceRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetPlace - ID: {Id}", request.Id);

        var place = _placeService.Obtenir(request.Id);

        if (place == null)
        {
            _logger.LogWarning("gRPC GetPlace - Place ID {Id} non trouvée", request.Id);

            return Task.FromResult(new PlaceResponse
            {
                Success = false,
                Message = $"La place avec l'ID {request.Id} n'existe pas."
            });
        }

        _logger.LogInformation("gRPC GetPlace - Place n°{Numero} trouvée", place.Numero);

        return Task.FromResult(new PlaceResponse
        {
            Place = MapToProto(place),
            Success = true,
            Message = "Place récupérée avec succès"
        });
    }

    /// <summary>
    /// Met à jour une place ET publie un événement
    /// </summary>
    public override async Task<UpdatePlaceResponse> UpdatePlace(
        UpdatePlaceRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC UpdatePlace - ID: {Id}, EstOccupee: {EstOccupee}",
            request.Id,
            request.EstOccupee
        );

        var place = _placeService.Obtenir(request.Id);
        if (place == null)
        {
            return new UpdatePlaceResponse
            {
                Success = false,
                Message = $"La place avec l'ID {request.Id} n'existe pas."
            };
        }

        bool success;
        string eventType;

        if (request.EstOccupee)
        {
            success = _placeService.OccuperPlace(request.Id);
            eventType = "OCCUPIED";
        }
        else
        {
            success = _placeService.LibererPlace(request.Id);
            eventType = "FREED";
        }

        if (!success)
        {
            return new UpdatePlaceResponse
            {
                Success = false,
                Message = "Impossible de mettre à jour la place."
            };
        }

        place = _placeService.Obtenir(request.Id);

        // Publier l'événement pour les subscribers
        await _eventService.PublishAsync(new PlaceUpdate
        {
            Place = MapToProto(place!),
            EventType = eventType,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        });

        return new UpdatePlaceResponse
        {
            Place = MapToProto(place!),
            Success = true,
            Message = "Place mise à jour avec succès"
        };
    }

    /// <summary>
    /// Streaming serveur : envoie les mises à jour en temps réel
    /// </summary>
    public override async Task WatchPlaces(
        Empty request,
        IServerStreamWriter<PlaceUpdate> responseStream,
        ServerCallContext context)
    {
        string subscriberId = Guid.NewGuid().ToString();
        _logger.LogInformation("Client connecté pour WatchPlaces : {SubscriberId}", subscriberId);

        var channel = _eventService.Subscribe(subscriberId);

        try
        {
            // Lire les événements du channel et les envoyer au client
            await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                _logger.LogDebug(
                    "Envoi événement au client {SubscriberId} : {EventType} pour place {PlaceId}",
                    subscriberId,
                    update.EventType,
                    update.Place.Id
                );

                await responseStream.WriteAsync(update);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client déconnecté : {SubscriberId}", subscriberId);
        }
        finally
        {
            _eventService.Unsubscribe(subscriberId);
        }
    }

    private PlaceMessage MapToProto(Place place)
    {
        return new PlaceMessage
        {
            Id = place.Id,
            Numero = place.Numero,
            Etage = place.Etage,
            EstOccupee = place.EstOccupee,
            DateDerniereMiseAJour = place.DateDerniereMiseAJour.ToString("yyyy-MM-ddTHH:mm:ss"),
            Description = place.Description
        };
    }
    /// <summary>
    /// Streaming client : reçoit un flux de données de capteurs
    /// </summary>
    public override async Task<UploadSummary> UploadSensorData(
        IAsyncStreamReader<SensorData> requestStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Début réception flux de données capteurs");

        var startTime = DateTime.UtcNow;
        int totalMessages = 0;
        var placesUpdated = new HashSet<int>();

        try
        {
            // Lire toutes les données du stream
            await foreach (var data in requestStream.ReadAllAsync(context.CancellationToken))
            {
                totalMessages++;

                _logger.LogDebug(
                    "Donnée capteur reçue - Sensor: {SensorId}, Place: {PlaceId}, Value: {Value}",
                    data.SensorId,
                    data.PlaceId,
                    data.Value
                );

                // Mettre à jour la place selon la valeur du capteur
                bool shouldBeOccupied = data.Value >= 0.5; // Seuil de détection

                var place = _placeService.Obtenir(data.PlaceId);
                if (place != null && place.EstOccupee != shouldBeOccupied)
                {
                    bool success;
                    if (shouldBeOccupied)
                    {
                        success = _placeService.OccuperPlace(data.PlaceId);
                    }
                    else
                    {
                        success = _placeService.LibererPlace(data.PlaceId);
                    }

                    if (success)
                    {
                        placesUpdated.Add(data.PlaceId);

                        // Publier l'événement
                        place = _placeService.Obtenir(data.PlaceId);
                        await _eventService.PublishAsync(new PlaceUpdate
                        {
                            Place = MapToProto(place!),
                            EventType = shouldBeOccupied ? "OCCUPIED" : "FREED",
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        });
                    }
                }
            }

            var endTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Fin réception flux - {TotalMessages} messages reçus, {PlacesUpdated} places mises à jour",
                totalMessages,
                placesUpdated.Count
            );

            return new UploadSummary
            {
                TotalMessages = totalMessages,
                PlacesUpdated = placesUpdated.Count,
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Success = true,
                Message = $"{totalMessages} messages traités avec succès"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement du flux de capteurs");

            return new UploadSummary
            {
                TotalMessages = totalMessages,
                PlacesUpdated = placesUpdated.Count,
                Success = false,
                Message = $"Erreur : {ex.Message}"
            };
        }
    }
    /// <summary>
    /// Streaming bidirectionnel : contrôle temps réel des places
    /// </summary>
    public override async Task ControlPlaces(
        IAsyncStreamReader<ControlMessage> requestStream,
        IServerStreamWriter<ControlResponse> responseStream,
        ServerCallContext context)
    {
        string clientId = Guid.NewGuid().ToString();
        _logger.LogInformation("Client connecté pour ControlPlaces : {ClientId}", clientId);

        try
        {
            await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
            {
                _logger.LogDebug(
                    "Commande reçue de {ClientId} : {Command} pour place {PlaceId}",
                    clientId,
                    message.Command,
                    message.PlaceId
                );

                ControlResponse response = message.Command switch
                {
                    "GET_STATUS" => await HandleGetStatus(message.PlaceId),
                    "OCCUPY" => await HandleOccupy(message.PlaceId),
                    "FREE" => await HandleFree(message.PlaceId),
                    "PING" => new ControlResponse
                    {
                        Status = "OK",
                        Message = "PONG",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    _ => new ControlResponse
                    {
                        Status = "ERROR",
                        Message = $"Commande inconnue : {message.Command}",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                };

                await responseStream.WriteAsync(response);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client déconnecté : {ClientId}", clientId);
        }
    }

    private async Task<ControlResponse> HandleGetStatus(int placeId)
    {
        var place = _placeService.Obtenir(placeId);

        if (place == null)
        {
            return new ControlResponse
            {
                Status = "ERROR",
                Message = $"Place {placeId} non trouvée",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        return new ControlResponse
        {
            Status = "OK",
            Message = $"Place {place.Numero} est {(place.EstOccupee ? "occupée" : "libre")}",
            Place = MapToProto(place),
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private async Task<ControlResponse> HandleOccupy(int placeId)
    {
        var place = _placeService.Obtenir(placeId);
        if (place == null)
        {
            return new ControlResponse
            {
                Status = "ERROR",
                Message = $"Place {placeId} non trouvée",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        bool success = _placeService.OccuperPlace(placeId);

        if (success)
        {
            place = _placeService.Obtenir(placeId);
            await _eventService.PublishAsync(new PlaceUpdate
            {
                Place = MapToProto(place!),
                EventType = "OCCUPIED",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        return new ControlResponse
        {
            Status = success ? "OK" : "ERROR",
            Message = success ? "Place occupée avec succès" : "Impossible d'occuper la place",
            Place = success ? MapToProto(place!) : null,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private async Task<ControlResponse> HandleFree(int placeId)
    {
        var place = _placeService.Obtenir(placeId);
        if (place == null)
        {
            return new ControlResponse
            {
                Status = "ERROR",
                Message = $"Place {placeId} non trouvée",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        bool success = _placeService.LibererPlace(placeId);

        if (success)
        {
            place = _placeService.Obtenir(placeId);
            await _eventService.PublishAsync(new PlaceUpdate
            {
                Place = MapToProto(place!),
                EventType = "FREED",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        return new ControlResponse
        {
            Status = success ? "OK" : "ERROR",
            Message = success ? "Place libérée avec succès" : "Impossible de libérer la place",
            Place = success ? MapToProto(place!) : null,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

}
