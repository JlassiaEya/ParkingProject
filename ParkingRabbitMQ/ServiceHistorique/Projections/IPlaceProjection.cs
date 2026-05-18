namespace ServiceHistorique.Projections;

public record EtatPlace(
    int PlaceId,
    bool EstOccupee,
    DateTime DerniereModification,
    int NombreOccupations,   // Combien de fois occupée
    int NombreLiberations    // Combien de fois libérée
);
public interface IPlaceProjection
{
    // Reconstruire l'état actuel de toutes les places
    IReadOnlyList<EtatPlace> ProjecterTout();

    // Reconstruire l'état d'une place à un instant T (replay partiel)
    EtatPlace? ProjecterPlace(int placeId, DateTime? jusqu_au = null);
}
