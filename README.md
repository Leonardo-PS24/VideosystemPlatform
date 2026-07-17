# Videosystem Internal Platform

> Piattaforma modular aziendale per applicazioni interne - Videosystem S.r.l.

## 📋 Descrizione

La **Videosystem Internal Platform** è un'infrastruttura moderna e scalabile progettata per ospitare e centralizzare tutte le applicazioni aziendali interne di Videosystem S.r.l. 

L'architettura adottata è **Portal (Single Page Application in React) + Web API C# (.NET 10)**, che offre:
- ✅ Autenticazione centralizzata unica per tutte le applicazioni (con supporto login esterno Google e Microsoft)
- ✅ Dashboard centrale con accesso rapido basato sui permessi
- ✅ Gestione utenti e permessi granulari centralizzati (solo Admin)
- ✅ Struttura modulare con deploy e integrazione fluida
- ✅ Separazione netta tra Frontend (React) e Backend (C# REST API)

---

## 🏗️ Architettura & Struttura Cartelle

```
VideosystemPlatform/
├── src/
│   ├── Platform.Client          # Frontend SPA (React + TypeScript + Vite)
│   ├── Platform.Portal          # Backend API (.NET 10 Web API) & Server Statico
│   ├── Platform.Shared          # Libreria C# condivisa (Costanti, Entità, Utility)
│   └── Apps/
│       └── ConfigurationKiosk   # Modulo Kiosk Checklist (Checklist, Hub SignalR)
└── docs/                        # Documentazione dettagliata di progetto
```

### Componenti Principali

#### 1. Platform.Client (Porta: 5173 / Proxy backend)
- Sviluppata in **React 19 + TypeScript + Vite**.
- Fornisce la Single Page Application (SPA) con il design system Videosystem (Google Fonts Outfit/Roboto + Bootstrap).
- Gestisce Login, Dashboard con orologio digitale, Gestione Utenti, Gestione Permessi, e compilazione/cronologia Kiosk Checklist.

#### 2. Platform.Portal (Porta: 5001 / HTTPS)
- Sviluppata in **ASP.NET Core 10.0 Web API**.
- Serve i file compilati di React (dalla cartella `wwwroot/`) e gestisce le richieste `/api/*` e i controller di account/OAuth.
- Autenticazione ibrida: Cookie HttpOnly per la sicurezza della sessione SPA + integrazione Google e Microsoft OAuth.

#### 3. Platform.Shared
Libreria condivisa con:
- Costanti aziendali (colori sociali, ruoli).
- Helper comuni (crittografia AES-256, validazione file).
- Modelli base ed entità database (`AuditableEntity`).

#### 4. Apps/ConfigurationKiosk
Modulo integrato nella piattaforma per la gestione delle checklist di calibrazione e configurazione delle macchine aziendali:
- Gestisce checklist interattive con salvataggio bozza ed autosalvataggio automatico debounced.
- Integra aggiornamenti in tempo reale sul progresso delle compilazioni tramite **SignalR WebSocket**.

---

## 🎨 Design System & Colori Aziendali

La piattaforma segue l'identità visiva ufficiale di **Videosystem S.r.l.**:
- **Verde Primario**: `#00945E` (usato per l'header aziendale, card dei moduli, bottoni principali).
- **Colore Secondario**: `#FFFFFF` / Grigio Chiaro (`#F8F9FA`).
- **Iconografia**: Google Material Icons e Bootstrap Icons.
- **Tipografia**: Outfit / Inter / Roboto.

---

## 🚀 Quick Start

### Prerequisiti
- ✅ **.NET 10 SDK** o superiore
- ✅ **Node.js** (versione 18 o superiore)
- ✅ Istanza **PostgreSQL** (Supabase localmente su `192.168.1.111:54322`)

### Configurazione Iniziale

1. **Clonare la repository**:
   ```bash
   git clone [repository-url]
   cd VideosystemPlatform
   ```

2. **Configurazione Backend**:
   Modificare `src/Platform.Portal/appsettings.json` impostando la stringa di connessione PostgreSQL:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=192.168.1.111;Port=54322;Database=postgres;Username=postgres;Password=[tua-password];SSL Mode=Disable;Trust Server Certificate=True"
   }
   ```

3. **Configurazione OAuth (Google/Microsoft)**:
   Configurare le credenziali in `appsettings.json` o tramite User Secrets:
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "[GOOGLE_CLIENT_ID]",
       "ClientSecret": "[GOOGLE_CLIENT_SECRET]"
     },
     "Microsoft": {
       "ClientId": "[MICROSOFT_CLIENT_ID]",
       "ClientSecret": "[MICROSOFT_CLIENT_SECRET]"
     }
   }
   ```

4. **Migrazioni del Database**:
   ```bash
   cd src/Platform.Portal
   dotnet ef database update
   ```

---

## 💻 Avvio in Ambiente di Sviluppo

Per avviare la piattaforma in locale con caricamento a caldo (Hot Reload), eseguire in due terminali paralleli:

### Terminale 1 (Backend API)
```bash
cd src/Platform.Portal
dotnet run
```
*Le API risponderanno su `https://localhost:5001`.*

### Terminale 2 (Frontend React)
```bash
cd src/Platform.Client
npm install
npm run dev
```
*Il frontend sarà accessibile su `http://localhost:5173` (con proxy automatico configurato verso il backend C#).*

---

## 📊 Stack Tecnologico

| Componente | Tecnologia Adottata |
|------------|---------------------|
| **Frontend** | React 19, TypeScript, Vite, Bootstrap 5 |
| **Backend** | ASP.NET Core 10 Web API, SignalR |
| **Database** | PostgreSQL / Supabase, Entity Framework Core 10 |
| **Autenticazione** | ASP.NET Core Identity (HttpOnly Cookie) + OAuth (Google, Microsoft) |
| **Realtime** | WebSocket (ASP.NET Core SignalR) |

---

## 🏢 Videosystem S.r.l.
Via Lago di Albano, 45 | 36015 Schio - Italia  
Web: [www.videosystem.it](https://www.videosystem.it)

© 2026 Videosystem S.r.l. - Uso interno aziendale
