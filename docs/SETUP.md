# Setup Dettagliato - Videosystem Internal Platform

## 📋 Indice

1. [Prerequisiti](#prerequisiti)
2. [Installazione](#installazione)
3. [Configurazione Database](#configurazione-database)
4. [Configurazione Servizi Esterni (OAuth & Email)](#configurazione-servizi-esterni-oauth--email)
5. [Avvio dello Sviluppo](#avvio-dello-sviluppo)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisiti

### Software Richiesto

| Software | Versione Minima | Download |
|----------|----------------|----------|
| **.NET SDK** | 10.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| **Node.js** | 18.0 (LTS raccomandata) | [nodejs.org](https://nodejs.org/) |
| **PostgreSQL** | 14+ (es. Supabase locale) | [supabase.com](https://supabase.com) o [postgresql.org](https://www.postgresql.org/) |
| **Visual Studio / Rider** | 2022+ / Rider 2024+ | [visualstudio.microsoft.com](https://visualstudio.microsoft.com) |

---

## Installazione

### Passo 1: Clone del Progetto
```bash
git clone [repository-url]
cd VideosystemPlatform
```

### Passo 2: Ripristino Dipendenze NuGet (.NET)
```bash
dotnet restore VideosystemPlatform.sln
```

### Passo 3: Ripristino Dipendenze NPM (React)
```bash
cd src/Platform.Client
npm install
```

---

## Configurazione Database

La piattaforma utilizza **PostgreSQL** (connesso localmente all'infrastruttura Supabase locale del server dell'utente su `192.168.1.111`).

### Configurazione Stringa di Connessione
Modifica il file `src/Platform.Portal/appsettings.json` o `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=192.168.1.111;Port=54322;Database=postgres;Username=postgres;Password=YOUR_SECRET_PASSWORD;SSL Mode=Disable;Trust Server Certificate=True"
}
```

*Nota: Nei parametri sopra è configurato il pooler local di Supabase (porta `54322`) con crittografia SSL disabilitata per i test locali.*

### Applicazione Migrazioni EF Core

Dal terminale esegui i comandi EF Core per popolare lo schema PostgreSQL:
```bash
cd src/Platform.Portal
dotnet ef database update
```

All'avvio, la piattaforma eseguirà automaticamente il seeding degli utenti predefiniti (`admin` e `user`) e delle aziende configurate.

---

## Configurazione Servizi Esterni (OAuth & Email)

I provider esterni Google e Microsoft gestiscono la sola autenticazione di login (OAuth 2.0).

### Google & Microsoft Login
Modifica `src/Platform.Portal/appsettings.json` inserendo i parametri configurati sui portali Google Cloud Console e Microsoft Azure Portal:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Microsoft": {
    "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
    "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
  }
}
```

---

## Avvio dello Sviluppo

Per consentire l'Hot Reload del frontend in sviluppo:

1. **Avviare il server C# Web API**:
   ```bash
   cd src/Platform.Portal
   dotnet run
   ```
   *(Risponderà su `https://localhost:5001`)*

2. **Avviare il client React**:
   ```bash
   cd src/Platform.Client
   npm run dev
   ```
   *(Risponderà su `http://localhost:5173`)*

Apri il browser all'indirizzo **`http://localhost:5173`** (Vite fa da proxy automatico per le chiamate API verso la porta `5001`).

---

## Troubleshooting

### Le chiamate API falliscono con errore 404/500?
Verifica che il server C# su `https://localhost:5001` sia attivo e che la stringa del proxy in `src/Platform.Client/vite.config.ts` corrisponda al tuo indirizzo backend locale.

### Errore di File Lock durante la Build .NET?
Se ricevi l'errore `MSB3027` o `MSB3021` che notifica che `Platform.Portal.exe` o `ConfigurationKiosk.dll` sono bloccati, significa che il server `dotnet run` è ancora attivo in background. Terminalo usando `CTRL+C` o arrestando il processo prima di rilanciare la build.
