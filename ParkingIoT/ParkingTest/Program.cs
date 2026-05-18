using ParkingConsole.Models;
using ParkingConsole.ListeChainee;

// On peut utiliser les classes du projet ParkingConsole
// grâce à la référence qu'on a ajoutée

Place place = new Place(1, 101, 1, "Test depuis un autre projet");
Console.WriteLine(place.Afficher());

ListeChainee<Place> liste = new ListeChainee<Place>();
liste.Ajouter(place);
liste.AfficherStructure();


