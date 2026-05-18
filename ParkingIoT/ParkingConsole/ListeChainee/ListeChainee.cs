using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingConsole.ListeChainee;

public class ListeChainee<T>
{
    private Noeud<T>? _tete;  // Le premier nœud de la liste
    private int _taille;       // Le nombre d'éléments

    public ListeChainee()
    {
        _tete = null;
        _taille = 0;
    }

    // =============================================
    // PROPRIÉTÉ : Nombre d'éléments dans la liste
    // =============================================
    public int Taille => _taille;

    // =============================================
    // MÉTHODE : Ajouter un élément à la fin
    // =============================================
    public void Ajouter(T donnee)
    {
        Noeud<T> nouveauNoeud = new Noeud<T>(donnee);

        if (_tete == null)
        {
            // La liste est vide, le nouveau nœud devient la tête
            _tete = nouveauNoeud;
        }
        else
        {
            // On parcourt jusqu'à la fin de la liste
            Noeud<T> courant = _tete;
            while (courant.Suivant != null)
            {
                courant = courant.Suivant;
            }
            // On attache le nouveau nœud à la fin
            courant.Suivant = nouveauNoeud;
        }

        _taille++;
    }

    // =============================================
    // MÉTHODE : Ajouter un élément au début
    // =============================================
    public void AjouterAuDebut(T donnee)
    {
        Noeud<T> nouveauNoeud = new Noeud<T>(donnee);
        nouveauNoeud.Suivant = _tete;  // Le nouveau nœud pointe vers l'ancien tête
        _tete = nouveauNoeud;          // Le nouveau nœud devient la tête
        _taille++;
    }
    // =============================================
    // MÉTHODE : Récupérer un élément par index
    // =============================================
    public T? Obtenir(int index)
    {
        if (index < 0 || index >= _taille)
        {
            Console.WriteLine($"  ⚠️  Index {index} hors limites (taille : {_taille})");
            return default;  // Retourne null pour les types référence
        }

        Noeud<T> courant = _tete!;
        for (int i = 0; i < index; i++)
        {
            courant = courant.Suivant!;
        }

        return courant.Donnee;
    }

    // =============================================
    // MÉTHODE : Supprimer un élément par index
    // =============================================
    public bool Supprimer(int index)
    {
        if (index < 0 || index >= _taille || _tete == null)
            return false;

        if (index == 0)
        {
            // Supprimer la tête
            _tete = _tete.Suivant;
        }
        else
        {
            // On va jusqu'au nœud avant celui à supprimer
            Noeud<T> courant = _tete;
            for (int i = 0; i < index - 1; i++)
            {
                courant = courant.Suivant!;
            }
            // On "saute" le nœud à supprimer
            courant.Suivant = courant.Suivant?.Suivant;
        }

        _taille--;
        return true;
    }

    // =============================================
    // MÉTHODE : Vérifier si la liste est vide
    // =============================================
    public bool EstVide()
    {
        return _tete == null;
    }
    // =============================================
    // MÉTHODE : Vérifier de si l'index existe ou non
    // =============================================
    public bool Rechercher(int index)
    {
        return index >= 0 && index < _taille;
    }

    // =============================================
    // MÉTHODE : Afficher tous les éléments
    // =============================================
    public void AfficherTout()
    {
        if (_tete == null)
        {
            Console.WriteLine("  (Liste vide)");
            return;
        }

        Noeud<T> courant = _tete;
        int index = 0;
        while (courant != null)
        {
            Console.WriteLine($"  [{index}] {courant.Donnee}");
            courant = courant.Suivant!;
            index++;
        }
    }

    // =============================================
    // MÉTHODE : Inverser la liste chainée
    // =============================================
    // =============================================
    // MÉTHODE : Inverser la liste chainée + afficher
    // =============================================
    public void Inverser()
    {
        Console.WriteLine("Inversion de la liste :");

        Noeud<T>? precedent = null;
        Noeud<T>? courant = _tete;
        Noeud<T>? suivant = null;

        while (courant != null)
        {
            suivant = courant.Suivant;
            courant.Suivant = precedent;
            precedent = courant;
            courant = suivant;
        }

        _tete = precedent; 

        // Affichage 
        if (_tete == null)
        {
            Console.WriteLine("  (Liste vide)");
            return;
        }

        Noeud<T>? temp = _tete;
        while (temp != null)
        {
            if (temp.Suivant != null)
                Console.Write($"[{temp.Donnee}] ──► ");
            else
                Console.Write($"[{temp.Donnee}] ──► null");

            temp = temp.Suivant;
        }
        Console.WriteLine();
    }
    // =============================================
    // MÉTHODE : Afficher la structure chainée
    // =============================================
    public void AfficherStructure()
    {
        if (_tete == null)
        {
            Console.WriteLine("  (Liste vide)");
            return;
        }

        Noeud<T> courant = _tete;
        while (courant != null)
        {
            if (courant.Suivant != null)
                Console.Write($"[{courant.Donnee}] ──► ");
            else
                Console.Write($"[{courant.Donnee}] ──► null");

            courant = courant.Suivant!;
        }
        Console.WriteLine();
    }
}
