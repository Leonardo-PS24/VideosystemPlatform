# Setup Dettagliato - Videosystem Internal Platform

## 📋 Indice

1. [Prerequisiti](#prerequisiti)
2. [Installazione](#installazione)
3. [Configurazione Database](#configurazione-database)
4. [Primo Avvio](#primo-avvio)
5. [Configurazione Avanzata](#configurazione-avanzata)
6. [Troubleshooting](#troubleshooting)

## Prerequisiti

### Software Richiesto

| Software | Versione Minima | Download |
|----------|----------------|----------|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| SQL Server | 2019+ / LocalDB | https://www.microsoft.com/sql-server |
| Visual Studio / Rider | 2022+ | https://visualstudio.microsoft.com |

### Verifica Installazione

Apri un terminale e verifica le versioni installate:

```bash
dotnet --version
# Output atteso: 8.0.x o superiore
```

## Installazione

### Passo 1: Clone del Progetto

Se usi Git:
```bash
git clone [repository-url]
cd VideosystemPlatform
```

Oppure estrai l'archivio ZIP nella cartella desiderata.

### Passo 2: Ripristino Dipendenze

```bash
dotnet restore VideosystemPlatform.sln
```

Questo comando scaricherà tutti i pacchetti NuGet necessari.

### Passo 3: Build del Progetto

```bash
dotnet build VideosystemPlatform.sln --configuration Debug
```

## Configurazione Database

### Opzione 1: SQL Server LocalDB (Consigliato per sviluppo)

LocalDB è incluso con Visual Studio. La stringa di connessione di default è:

```json
"Server=(localdb)\\mssqllocaldb;Database=VideosystemPortal;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Non richiede configurazione aggiuntiva.

### Opzione 2: SQL Server Express

Se usi SQL Server Express, modifica `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VideosystemPortal;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### Opzione 3: SQL Server con Autenticazione

Per autenticazione SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=VideosystemPortal;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true"
}
```

⚠️ **Importante**: Non committare mai le credenziali nel repository!

### Creazione Database

#### Usando Entity Framework CLI

```bash
cd src/Platform.Portal

# Crea/aggiorna il database
dotnet ef database update

# Se necessario, crea una nuova migrazione
dotnet ef migrations add InitialCreate
```

#### Usando Visual Studio

1. Apri **Package Manager Console**
2. Seleziona **Platform.Portal** come progetto di default
3. Esegui:
```powershell
Update-Database
```

### Verifica Database

Il database dovrebbe contenere le seguenti tabelle:
- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `AspNetRoleClaims`

## Primo Avvio

### Avvio da Visual Studio

1. Apri `VideosystemPlatform.sln`
2. Imposta **Platform.Portal** come progetto di avvio
3. Premi `F5` per avviare in debug mode

### Avvio da CLI

```bash
cd src/Platform.Portal
dotnet run
```

L'applicazione sarà disponibile su:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

### Prima Configurazione

1. **Accedi con l'admin di default:**
   - Username: `admin`
   - Password: `Admin123!`

2. **Crea un nuovo utente di test** (opzionale):
   - Vai su **Admin** → **Utenti**
   - Click su **Nuovo Utente**
   - Compila il form e salva

3. **Verifica la dashboard:**
   - Dovresti vedere la card "Kiosk Registration" (se l'app è configurata)

## Configurazione Avanzata

### Modifica Porta di Default

File: `Properties/launchSettings.json`

```json
{
  "profiles": {
    "Platform.Portal": {
      "applicationUrl": "https://localhost:CUSTOM_PORT;http://localhost:CUSTOM_PORT_HTTP"
    }
  }
}
```

### Configurazione JWT

File: `appsettings.json`

```json
"Jwt": {
  "SecretKey": "LA_TUA_CHIAVE_SEGRETA_MINIMO_32_CARATTERI",
  "Issuer": "VideosystemPlatform",
  "Audience": "VideosystemApps",
  "ExpirationMinutes": 60
}
```

⚠️ **Importante**: In produzione, usa una chiave robusta e memorizzala in modo sicuro!

### Configurazione Logging

File: `appsettings.json`

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "WriteTo": [
    {
      "Name": "Console"
    },
    {
      "Name": "File",
      "Args": {
        "path": "logs/platform-portal-.txt",
        "rollingInterval": "Day",
        "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
      }
    }
  ]
}
```

I log saranno salvati in `logs/platform-portal-YYYY-MM-DD.txt`.

### Aggiungere Applicazioni alla Dashboard

File: `appsettings.json`

```json
"Applications": [
  {
    "Name": "Kiosk Registration",
    "Description": "Gestione e registrazione dei kiosk aziendali",
    "Url": "https://localhost:5002",
    "Icon": "devices",
    "RequiredRole": "User"
  },
  {
    "Name": "Nuova Applicazione",
    "Description": "Descrizione della nuova app",
    "Url": "https://localhost:5003",
    "Icon": "apps",
    "RequiredRole": "Admin"
  }
]
```

**Icone Material Icons disponibili**: `apps`, `devices`, `inventory`, `people`, `settings`, `dashboard`, ecc.
Vedi: https://fonts.google.com/icons

## Troubleshooting

### Errore: "Cannot connect to SQL Server"

**Causa**: SQL Server non è in esecuzione o la stringa di connessione è errata.

**Soluzione**:
1. Verifica che SQL Server sia in esecuzione
2. Controlla la stringa di connessione in `appsettings.json`
3. Prova con SQL Server Management Studio

### Errore: "The type initializer for 'Serilog.Log' threw an exception"

**Causa**: Configurazione Serilog non valida.

**Soluzione**:
Verifica la sintassi JSON in `appsettings.json`, sezione `Serilog`.

### Errore: "A database operation failed while processing the request"

**Causa**: Database non creato o migrazioni non applicate.

**Soluzione**:
```bash
cd src/Platform.Portal
dotnet ef database update
```

### Errore: "Unable to resolve service for type 'IJwtService'"

**Causa**: JwtService non registrato correttamente in `Program.cs`.

**Soluzione**:
Verifica che in `Program.cs` sia presente:
```csharp
builder.Services.AddSingleton<IJwtService>(new JwtService(jwtSecretKey));
```

### Porta già in uso

**Errore**: `Unable to bind to https://localhost:5001`

**Soluzione**:
1. Cambia porta in `Properties/launchSettings.json`
2. Oppure termina il processo che usa quella porta:
```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID [PID] /F

# Linux/Mac
lsof -i :5001
kill [PID]
```

### Certificato SSL non fidato

**Errore**: Browser mostra avviso certificato non sicuro in sviluppo.

**Soluzione**:
```bash
dotnet dev-certs https --trust
```

### Reset Completo Database

Se vuoi ripartire da zero:

```bash
cd src/Platform.Portal

# Elimina database
dotnet ef database drop --force

# Ricrea database
dotnet ef database update
```

## Deploy in Produzione

### Prerequisiti Produzione
- Windows Server 2019+ con IIS installato
- SQL Server 2019+
- ASP.NET Core Runtime 8.0

### Pubblicazione

```bash
dotnet publish src/Platform.Portal/Platform.Portal.csproj `
  -c Release `
  -o ./publish
```

### Configurazione IIS

1. Crea un nuovo **Application Pool**:
   - Name: `VideosystemPlatform`
   - .NET CLR Version: `No Managed Code`
   - Managed Pipeline Mode: `Integrated`

2. Crea un nuovo **Website**:
   - Site name: `VideosystemPlatform`
   - Physical path: percorso cartella `publish`
   - Binding: HTTPS su porta 443
   - Application Pool: `VideosystemPlatform`

3. **Installa ASP.NET Core Hosting Bundle** sul server:
   https://dotnet.microsoft.com/download/dotnet/8.0

4. **Configurazione appsettings.Production.json**:
   - Aggiorna connection string per SQL Server produzione
   - Cambia JwtSecretKey con chiave robusta
   - Configura logging appropriato

### Checklist Pre-Produzione

- [ ] Connection string SQL Server aggiornata
- [ ] JwtSecretKey robusta configurata
- [ ] HTTPS configurato con certificato valido
- [ ] Backup database schedulato
- [ ] Logging configurato
- [ ] Firewall configurato per permettere accesso DB

## Supporto

Per assistenza tecnica, contattare il team IT interno Videosystem.

---

**Documento**: INTC_202511251126  
**Versione**: 1.0.0  
**Data**: Novembre 2024
