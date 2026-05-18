namespace ParkingApi.Exceptions;

public class PlaceNotFoundException : Exception
{
    public int PlaceId { get; }

    public PlaceNotFoundException(int placeId)
        : base($"La place avec l'ID {placeId} n'existe pas.")
    {
        PlaceId = placeId;
    }
}
