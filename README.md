# Videosystem Internal Platform

> Piattaforma modulare per applicazioni aziendali interne - Videosystem S.r.l.

## 📋 Descrizione

La **Videosystem Internal Platform** è un'infrastruttura moderna e scalabile progettata per ospitare tutte le applicazioni aziendali interne di Videosystem S.r.l. 

L'architettura adottata è **Portal + Microservices**, che permette:
- ✅ Autenticazione centralizzata unica per tutte le applicazioni
- ✅ Dashboard centrale con accesso rapido alle applicazioni
- ✅ Gestione utenti unificata
- ✅ Deploy indipendente di ogni applicazione
- ✅ Scalabilità e manutenibilità ottimali

## 🏗️ Architettura

```
VideosystemPlatform/
├── Platform.Portal          # Shell centrale + Autenticazione
├── Platform.Shared          # Libreria condivisa
└── Apps/
    ├── KioskRegistration    # Prima applicazione (gestione kiosk)
    └── [Future Apps]        # Applicazioni future
```

### Componenti Principali

#### 1. Platform.Portal (https://localhost:5001)
- **Dashboard centrale** con lista applicazioni disponibili
- **Autenticazione** tramite ASP.NET Core Identity
- **Gestione utenti** completa (solo Admin)
- **JWT Token issuer** per comunicazione sicura tra servizi

#### 2. Platform.Shared
Libreria condivisa con:
- Costanti comuni (colori aziendali, configurazioni)
- Servizi condivisi (JWT, logging)
- Helper utilities (crittografia, validazione file)
- Modelli base (entità auditable, API response)

#### 3. Apps/KioskRegistration (https://localhost:5002)
Prima applicazione della piattaforma per la gestione dei kiosk aziendali.
*(Documentazione dettagliata in `/Apps/KioskRegistration/README.md`)*

## 🎨 Design System

### Colori Aziendali Videosystem
- **Verde principale**: `#00945E`
- **Bianco**: `#FFFFFF`
- **Grigio scuro**: `#333333`
- **Grigio chiaro**: `#F8F9FA`

### UI Framework
- **Bootstrap 5** per layout responsive
- **Material Icons** per iconografia
- **Font Roboto** per tipografia

## 🔐 Sicurezza

- ✅ **Autenticazione** via ASP.NET Core Identity
- ✅ **Password hashing** con Identity default (PBKDF2)
- ✅ **JWT Token** per comunicazione tra servizi
- ✅ **HTTPS obbligatorio** in produzione
- ✅ **CSRF Protection** su tutti i form
- ✅ **Input Validation** client e server-side
- ✅ **Role-based authorization** (Admin, User)

## 🚀 Quick Start

### Prerequisiti
- .NET 8 SDK
- SQL Server (LocalDB o Express)
- Visual Studio 2022 / Rider / VS Code

### Setup Iniziale

1. **Clone del repository**
```bash
git clone [repository-url]
cd VideosystemPlatform
```

2. **Configurazione Database**
   
Modifica `src/Platform.Portal/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VideosystemPortal;Trusted_Connection=True;"
}
```

3. **Creazione Database**
```bash
cd src/Platform.Portal
dotnet ef database update
```

4. **Avvio del Portal**
```bash
dotnet run
```

Apri: https://localhost:5001

### Credenziali di Default

**Administrator:**
- Username: `admin`
- Password: `Admin123!`

**User Standard:**
- Username: `user`
- Password: `User123!`

## 📊 Stack Tecnologico

| Componente | Tecnologia |
|------------|-----------|
| **Backend** | ASP.NET Core 8 MVC |
| **Frontend** | Razor Pages, Bootstrap 5 |
| **Database** | SQL Server + Entity Framework Core |
| **Autenticazione** | ASP.NET Core Identity + JWT |
| **Logging** | Serilog |
| **ORM** | Entity Framework Core 8 |

## 📝 Standard di Codifica

Il progetto segue rigorosamente le **Linee Guida Videosystem**:

- ✅ Clean Architecture (Domain, Application, Infrastructure, Presentation)
- ✅ Convenzioni di naming .NET standard
- ✅ Commenti XML per metodi pubblici
- ✅ Separazione logica nei servizi (no logica nei controller)
- ✅ DTO per input/output
- ✅ Logging centralizzato con Serilog
- ✅ Configurazioni per ambiente (Development, Staging, Production)

## 🔧 Configurazione Applicazioni

Per aggiungere una nuova applicazione alla dashboard, modifica `appsettings.json` del Portal:

```json
"Applications": [
  {
    "Name": "Nome Applicazione",
    "Description": "Descrizione breve",
    "Url": "https://localhost:5003",
    "Icon": "apps",
    "RequiredRole": "User"
  }
]
```

## 📖 Documentazione Aggiuntiva

- [Setup Dettagliato](docs/SETUP.md)
- [Guida Sviluppo](docs/DEVELOPMENT.md)
- [Architettura Clean](docs/ARCHITECTURE.md)
- [Linee Guida Videosystem](docs/VIDEOSYSTEM_GUIDELINES.md)

## 🏢 Informazioni Aziendali

**Videosystem S.r.l.**  
Via Lago di Albano, 45 | 36015 Schio - Italia  
Tel: +39 0445 500 500  
Web: www.videosystem.it

## 📄 Licenza

© 2024 Videosystem S.r.l. - Uso interno aziendale

## 🤝 Supporto

Per supporto tecnico o domande, contattare il team IT interno.

---

**Versione**: 1.0.0  
**Data**: Novembre 2024  
**Documento**: INTC_202511251125
