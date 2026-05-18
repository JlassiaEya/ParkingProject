$WarningPreference = 'SilentlyContinue'

Write-Host "--- Test de resilience (service en panne) ---"
Write-Host "1. Vérication et nettoyage..."
Stop-Process -Name "ApiGateway" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "ServicePlaces" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue

Write-Host "2. Démarrage de l'API Gateway en arrière-plan..."
$gatewayProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "ApiGateway" -PassThru -WindowStyle Hidden

Write-Host "Attente du démarrage du serveur (health check)..."
$started = $false
for ($i=0; $i -lt 15; $i++) {
    Start-Sleep -Seconds 2
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get -ErrorAction Stop
        if ($health.status -eq "Gateway OK") {
            $started = $true
            break
        }
    } catch {
        Write-Host "." -NoNewline
    }
}
Write-Host ""
if (-not $started) {
    Write-Host "Erreur : l'API Gateway n'a pas pu démarrer à temps."
    Stop-Process -Id $gatewayProc.Id -Force
    exit
}

Write-Host "3. Authentification pour recuperer le Token JWT..."
try {
    $loginResp = Invoke-RestMethod -Uri "http://localhost:5000/auth/login" -Method Post -ContentType "application/json" -Body '{"Username":"admin","Password":"admin123"}'
    $token = $loginResp.token
    Write-Host "Token obtenu avec succes."
} catch {
    Write-Host "Impossible de se connecter: $_"
    Stop-Process -Id $gatewayProc.Id -Force
    exit
}


Write-Host "4. Exécution de la requête de test vers /api/places (Service Places volontairement arrêté)..."
$curlOut = curl.exe -s -i -H "Authorization: Bearer $token" http://localhost:5000/api/places
Write-Host "Resultat attendu : HTTP 502 Bad Gateway"
Write-Host "-------------------- REPONSE --------------------"
Write-Host $curlOut
Write-Host "-------------------------------------------------"

Write-Host "5. Arrêt de la Gateway..."
Stop-Process -Id $gatewayProc.Id -Force -ErrorAction SilentlyContinue
Write-Host "Test terminé."
