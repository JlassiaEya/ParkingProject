using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingConsole.ListeChainee;

    public class Noeud<T>
    {
        public T Donnee { get; set; }          // La donnée stockée (ex : une Place)
        public Noeud<T>? Suivant { get; set; } // Le lien vers le nœud suivant

        public Noeud(T donnee)
        {
            Donnee = donnee;
            Suivant = null;  // Par défaut, pas de suivant
        }
    }
