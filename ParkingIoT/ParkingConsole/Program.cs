using ParkingConsole.Models;
using ParkingConsole.Repositories;
using ParkingConsole.Services;

// =============================================
// INJECTION DE DÉPENDANCES (manuelle)
// =============================================
// On crée les Repository d'abord
IPlaceRepository placeRepository = new PlaceRepository();
ICapteurRepository capteurRepository = new CapteurRepository();
IEvenementRepository evenementRepository = new EvenementRepository();

// On passe les Repository au Service via le constructeur
// Le Service ne sait pas que c'est une liste en mémoire
IPlaceService placeService = new PlaceService(placeRepository, evenementRepository);

// =============================================
// DÉMONSTRATION
// =============================================
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║   PARKING INTELLIGENT - Démonstration    ║");
Console.WriteLine("╚══════════════════════════════════════════╝\n");

// --- 1. Afficher toutes les places ---
Console.WriteLine("━━━ 1. Toutes les places du parking ━━━\n");
foreach (Place place in placeService.Obtenir())
{
    Console.WriteLine($"  {place.Afficher()}");
}

// --- 2. Afficher le résumé initial ---
Console.WriteLine("\n━━━ 2. Résumé initial ━━━\n");
AfficherResume(placeService.ObteniRresume());

// --- 3. Occuper quelques places ---
Console.WriteLine("\n━━━ 3. Occuper des places ━━━\n");
Console.WriteLine("  → Occuper la place ID 1 :");
placeService.OccuperPlace(1);

Console.WriteLine("  → Occuper la place ID 3 :");
placeService.OccuperPlace(3);

Console.WriteLine("  → Occuper la place ID 5 :");
placeService.OccuperPlace(5);

// --- 4. Essayer d'occuper une place déjà occupée ---
Console.WriteLine("\n━━━ 4. Tenter d'occuper une place déjà occupée ━━━\n");
Console.WriteLine("  → Occuper la place ID 1 (déjà occupée) :");
placeService.OccuperPlace(1);

// --- 5. Essayer d'occuper une place inexistante ---
Console.WriteLine("\n━━━ 5. Tenter d'occuper une place inexistante ━━━\n");
Console.WriteLine("  → Occuper la place ID 99 :");
placeService.OccuperPlace(99);

// --- 6. Libérer une place ---
Console.WriteLine("\n━━━ 6. Libérer une place ━━━\n");
Console.WriteLine("  → Libérer la place ID 3 :");
placeService.LibererPlace(3);

// --- 7. Afficher les places libres ---
Console.WriteLine("\n━━━ 7. Places libres ━━━\n");
foreach (Place place in placeService.ObteniePlacesLibres())
{
    Console.WriteLine($"  {place.Afficher()}");
}

// --- 8. Afficher par étage ---
Console.WriteLine("\n━━━ 8. Places par étage ━━━\n");
for (int etage = 1; etage <= 3; etage++)
{
    Console.WriteLine($"  ┌─ Étage {etage} ─────────────────────────────────────");
    foreach (Place place in placeService.ObtenirParEtage(etage))
    {
        Console.WriteLine($"  │  {place.Afficher()}");
    }
    Console.WriteLine($"  └────────────────────────────────────────────\n");
}

// --- 9. Afficher le résumé final ---
Console.WriteLine("━━━ 9. Résumé final ━━━\n");
AfficherResume(placeService.ObteniRresume());

// --- 10. Afficher l'historique des événements ---
Console.WriteLine("\n━━━ 10. Historique des événements ━━━\n");
foreach (Evenement evenement in evenementRepository.Obtenir())
{
    Console.WriteLine($"  {evenement.Afficher()}");
}

// --- 11. Afficher les capteurs ---
Console.WriteLine("\n━━━ 11. Liste des capteurs ━━━\n");
foreach (Capteur capteur in capteurRepository.Obtenir())
{
    Console.WriteLine($"  {capteur.Afficher()}");
}

// --- 12. Occuper toutes les places restantes (test alerte parking plein) ---
Console.WriteLine("\n━━━ 12. Occuper toutes les places restantes ━━━\n");
foreach (Place place in placeService.ObteniePlacesLibres().ToList())
{
    Console.WriteLine($"  → Occuper la place ID {place.Id} :");
    placeService.OccuperPlace(place.Id);
}

// --- 13. Résumé final après parking plein ---
Console.WriteLine("\n━━━ 13. Résumé après parking plein ━━━\n");
AfficherResume(placeService.ObteniRresume());

//Afficher seulement les événements PlaceOccupee
Console.WriteLine("\n━━━ Historique : places occupées uniquement ━━━\n");

foreach (Evenement evenement in evenementRepository.Obtenir())
{
    if (evenement.Type == TypeEvenement.PlaceOccupee)
    {
        Console.WriteLine($"  {evenement.Afficher()}");
    }
}
//Nombre de capteurs par place
Console.WriteLine("\n━━━ Nombre de capteurs par place ━━━\n");

foreach (Place place in placeService.Obtenir())
{
    var capteurs = capteurRepository.ObtenirParPlace(place.Id);
    Console.WriteLine($"Place n°{place.Numero} → {capteurs.Count} capteur(s)");
}
// =============================================
// MÉTHODE UTILITAIRE
// =============================================
static void AfficherResume(ResumeParkingDto resume)
{
    Console.WriteLine($"  Total places      : {resume.TotalPlaces}");
    Console.WriteLine($"  Places libres     : {resume.PlacesLibres}");
    Console.WriteLine($"  Places occupées   : {resume.PlacesOccupees}");
    Console.WriteLine($"  Taux occupation   : {resume.TauxOccupation:F1}%");
    Console.WriteLine($"  Parking plein     : {(resume.EstPlein ? "OUI 🚨" : "Non")}");
    Console.WriteLine($"  Parking vide      : {(resume.EstVide ? "OUI" : "Non")}");
}
