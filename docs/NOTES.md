# Note Tecniche - Videosystem Platform

## Architettura Implementata

### Pattern: Portal + Microservices (SPA + Web API C#)

La piattaforma adotta una struttura modulare che separa nettamente la presentazione dal backend di business logic:

1. **Platform.Client** - Interfaccia Utente (SPA):
   - Sviluppata in React 19 + TypeScript + Vite.
   - Fornisce un'esperienza fluida e reattiva priva di ricaricamenti di pagina completi.
   - Gestione centralizzata del layout, menu laterale, orologio digitale dinamico, stato di login ed autorizzazione dei menu.

2. **Platform.Portal** - Shell Backend di Gateway:
   - Web API C# .NET 10.
   - Fornisce i file statici compilati di React.
   - Espone le API REST in formato JSON per la sessione utente, autenticazione esterna OAuth (Google, Microsoft) e gestione degli accessi.
   - Gestisce la sicurezza tramite cookie crittografati HTTP-Only.

3. **Platform.Shared** - Libreria Condivisa:
   - Contiene entità di base per il DB (`AuditableEntity`).
   - Costanti di branding e stili predefiniti (colori sociali Videosystem).
   - Servizi trasversali come la validazione dei file caricati e la crittografia.

4. **Apps/** - Moduli Verticali Indipendenti:
   - Attualmente ospita il modulo **ConfigurationKiosk**.
   - Ogni app ha le proprie tabelle DB, i propri servizi di business logic, i propri controller API ed eventuali hub in tempo reale (SignalR).

---

## Dettagli Tecnici Chiave

### Autenticazione Ibrida e OAuth
Il portale centrale utilizza una strategia di autenticazione basata su sessione cookie ASP.NET Core Identity.
- **Login Esterno (Google / Microsoft)**: I reindirizzamenti OAuth sfidano l'utente sui server esterni. Una volta autenticato, il server .NET genera la sessione e restituisce il cookie al browser.
- **Proxying in Sviluppo**: Il client React (porta `5173`) inoltra le chiamate a `/api/*` e `/Account/*` a `https://localhost:5001`. Questo preserva la trasmissione sicura dei cookie di sessione sullo stesso dominio virtuale.

### Realtime con SignalR Websocket
Nel modulo **ConfigurationKiosk**:
- Il client stabilisce una connessione WebSocket persistente con `/kioskhub`.
- Per consentire questo handshake, il proxy di sviluppo in `vite.config.ts` ha abilitato l'opzione `ws: true`.
- Ogni volta che un utente compila una checklist, le modifiche debounced vengono inviate via API ed innescano l'evento SignalR `DataUpdated` che aggiorna in tempo reale il progresso visualizzato dagli altri operatori.

### Database Strategy (PostgreSQL + Supabase local)
Il database del portale e delle app è ospitato su PostgreSQL (Supabase locale).
- Il Pooler di Supabase risponde sulla porta locale `54322`.
- La stringa di connessione ha impostato `SSL Mode=Disable` e `Trust Server Certificate=True` per bypassare controlli di certificato SSL non validati in ambiente di sviluppo locale.

---

## Best Practices & Sicurezza

- ✅ **Sicurezza Cookie**: Sessioni memorizzate su cookie crittografati con flag `HttpOnly` e `Secure`, bloccando letture javascript dannose.
- ✅ **Validazione dei Template**: I file JSON dei template checklist caricati dagli amministratori sono validati dal parser client di React prima dell'invio.
- ✅ **Gestione Permessi**: Un pannello amministratore consente di concedere privilegi (`View`, `Create`, `Edit`, `Delete`) per singole applicazioni. La modale applica dipendenze automatiche (es. per concedere modifiche si assegna in automatico la visualizzazione).
- ✅ **Gestione Tipi**: I campi specificati nel JSON Kiosk (`toggle`, `checkbox`, `date`, `signature`, `file`, etc.) sono mappati uno a uno e renderizzati con i corrispondenti controlli responsive Bootstrap nel modulo di compilazione.

---

**Autore**: Platform Team Videosystem  
**Versione**: 1.1.0  
**Ultimo Aggiornamento**: Luglio 2026
