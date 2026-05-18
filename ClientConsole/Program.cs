using System.Net.Http.Json;
using System.Text.Json;

var gateway = "http://localhost:5000";
var http    = new HttpClient { BaseAddress = new Uri(gateway) };

Console.WriteLine("=====================================================");
Console.WriteLine(" Client Parking Intelligent - Simulation Application ");
Console.WriteLine("=====================================================");
Console.WriteLine();

// ── ETAPE 1 : Authentification ─────────────────────────────────────────
Console.WriteLine("[1] Authentification aupres de la Gateway...");

var loginPayload = new { username = "operateur", password = "oper456" };
var loginResponse = await http.PostAsJsonAsync("/auth/login", loginPayload);

if (!loginResponse.IsSuccessStatusCode)
{
    Console.WriteLine($"  ECHEC : {loginResponse.StatusCode}");
    return;
}

var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
var token     = loginData.GetProperty("token").GetString()!;
var role      = loginData.GetProperty("role").GetString()!;

Console.WriteLine($"  OK - Connecte en tant que : {loginData.GetProperty("user")} (role: {role})");
Console.WriteLine($"  Token expire dans : {loginData.GetProperty("expiresIn")} secondes");
Console.WriteLine();

http.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

// ── ETAPE 2 : Etat des places ──────────────────────────────────────────
Console.WriteLine("[2] Consultation des places disponibles...");

var placesRaw  = await http.GetAsync("/api/places");
var placesBody = await placesRaw.Content.ReadAsStringAsync();

if (!placesRaw.IsSuccessStatusCode || string.IsNullOrWhiteSpace(placesBody))
{
    Console.WriteLine($"  ECHEC : {placesRaw.StatusCode}");
    Console.WriteLine($"  Reponse brute : {placesBody}");
    Console.WriteLine();
}
else
{
    var places = JsonSerializer.Deserialize<JsonElement>(placesBody).EnumerateArray().ToList();
    var libres = places.Count(p => !p.GetProperty("estOccupee").GetBoolean());
    var total  = places.Count;

    Console.WriteLine($"  Total : {total} places | Libres : {libres} | Occupees : {total - libres}");

    foreach (var place in places.Take(5))
    {
        var etat = place.GetProperty("estOccupee").GetBoolean() ? "OCCUPEE" : "LIBRE  ";
        Console.WriteLine($"  [{etat}] Place {place.GetProperty("numero")} - Etage {place.GetProperty("etage")}");
    }
    if (places.Count > 5) Console.WriteLine($"  ... et {places.Count - 5} autres");
    Console.WriteLine();
}

// ── ETAPE 3 : Statistiques ─────────────────────────────────────────────
Console.WriteLine("[3] Statistiques d'occupation...");

try
{
    var statsRaw  = await http.GetAsync("/api/places/stats");
    var statsBody = await statsRaw.Content.ReadAsStringAsync();

    if (statsRaw.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(statsBody))
    {
        var stats = JsonSerializer.Deserialize<JsonElement>(statsBody);
        Console.WriteLine($"  Taux d'occupation : {stats.GetProperty("tauxOccupation")}");  // ✅ pas de % supplémentaire
        Console.WriteLine($"  Places libres     : {stats.GetProperty("placesLibres")}");     // ✅ placesLibres
    }
    else
    {
        Console.WriteLine($"  ECHEC : {statsRaw.StatusCode} - {statsBody}");
    }
}
catch (Exception ex) { Console.WriteLine($"  Erreur stats : {ex.Message}"); }
Console.WriteLine();

// ── ETAPE 4 : Qualite de l'air ─────────────────────────────────────────
Console.WriteLine("[4] Qualite de l'air...");

try
{
    var qualiteRaw  = await http.GetAsync("/api/qualite");
    var qualiteBody = await qualiteRaw.Content.ReadAsStringAsync();

    if (qualiteRaw.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(qualiteBody))
    {
        var qualite = JsonSerializer.Deserialize<JsonElement>(qualiteBody);
        Console.WriteLine($"  CO2 : {qualite.GetProperty("co2Ppm")} ppm");
        Console.WriteLine($"  Niveau : {qualite.GetProperty("niveau")}");
        Console.WriteLine($"  Temperature : {qualite.GetProperty("temperature")} C");
    }
    else
    {
        Console.WriteLine($"  Service Qualite non disponible ({qualiteRaw.StatusCode})");
    }
}
catch (Exception ex) { Console.WriteLine($"  Service Qualite non disponible : {ex.Message}"); }
Console.WriteLine();

// ── ETAPE 5 : Alertes ──────────────────────────────────────────────────
Console.WriteLine("[5] Alertes recentes...");

try
{
    var alertesRaw  = await http.GetAsync("/api/notif");
    var alertesBody = await alertesRaw.Content.ReadAsStringAsync();

    if (alertesRaw.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(alertesBody))
    {
        var liste = JsonSerializer.Deserialize<JsonElement>(alertesBody).EnumerateArray().ToList();
        if (liste.Count == 0)
            Console.WriteLine("  Aucune alerte");
        else
            foreach (var a in liste.Take(3))
                Console.WriteLine($"  ALERTE : {a.GetProperty("type")} - {a.GetProperty("tauxOccupation")}% - {a.GetProperty("placesLibres")} places libres - {a.GetProperty("timestamp")}");  // ✅ bons noms
    }
    else
    {
        Console.WriteLine($"  Service Notifications non disponible ({alertesRaw.StatusCode})");
    }
}
catch (Exception ex) { Console.WriteLine($"  Service Notifications non disponible : {ex.Message}"); }

Console.WriteLine();
Console.WriteLine("=====================================================");
Console.WriteLine(" Consultation terminee avec succes ! ");
Console.WriteLine("=====================================================");