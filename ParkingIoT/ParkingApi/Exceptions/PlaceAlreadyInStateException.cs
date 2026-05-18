namespace ParkingApi.Exceptions;

public class PlaceAlreadyInStateException : Exception
{
    public int PlaceId { get; }
    public bool EstOccupee { get; }

    public PlaceAlreadyInStateException(int placeId, bool estOccupee)
        : base($"La place {placeId} est déjà {(estOccupee ? "occupée" : "libre")}.")
    {
        PlaceId = placeId;
        EstOccupee = estOccupee;
    }
}
