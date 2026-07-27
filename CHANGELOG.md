# Changelog

Tutte le modifiche notevoli a questo progetto saranno documentate in questo file.

Il formato è basato su [Keep a Changelog](https://keepachangelog.com/it/1.0.0/),
e questo progetto aderisce al [Semantic Versioning](https://semver.org/lang/it/).

## [1.1.0] - 2026-07-17

### Aggiunto
- ✨ Migrazione dell'interfaccia utente da C# MVC Razor ad una moderna Single Page Application (SPA) in **React 19 + TypeScript + Vite**.
- ✨ Riorganizzazione della struttura di progetto: spostato il modulo frontend in una cartella isolata `src/Platform.Client`, separandolo dal backend per una migliore manutenibilità.
- ✨ Migrazione del database da SQL Server a **PostgreSQL (Supabase)**.
- ✨ Supporto all'autenticazione tramite provider esterni: integrati **Google** e **Microsoft Account** OAuth.
- ✨ Riscritto il modulo **Gestione Utenti** e creata la nuova **Gestione Permessi Applicativi** (tramite modale con dipendenze logiche di lettura/scrittura) in React.
- ✨ Gestione dei template delle checklist KioskConfig in React con supporto per il caricamento e la validazione client-side di file `.json`.

### Corretto
- 🐛 Risolto l'errore 400 Bad Request durante la creazione di template checklist dovuto al tipo di dato di `version` (stringa float convertita ad intero prima dell'invio).

## [1.0.0] - 2024-11-25

### Aggiunto
- 🎉 Versione iniziale della Videosystem Internal Platform
- ✨ Platform.Portal: Shell centrale con autenticazione e dashboard
- ✨ Platform.Shared: Libreria condivisa con utilities comuni
- ✨ Autenticazione tramite ASP.NET Core Identity
- ✨ JWT service per comunicazione sicura tra servizi
- ✨ Gestione utenti completa (CRUD) con ruoli Admin/User
- ✨ Dashboard responsive con card delle applicazioni
- ✨ UI personalizzata con colori aziendali Videosystem (#00945E)
- ✨ Bootstrap 5 + Material Icons per UI moderna
- ✨ Logging centralizzato con Serilog
- ✨ Encryption helper per dati sensibili (AES-256)
- ✨ File validation helper per upload sicuri
- ✨ Seed data con utenti di default
- 📚 Documentazione completa (README, SETUP, NOTES)
- 🔧 File di configurazione per Development e Production

### Sicurezza
- 🔒 Password hashing con Identity (PBKDF2)
- 🔒 HTTPS obbligatorio
- 🔒 CSRF protection su tutti i form
- 🔒 Input validation client e server-side
- 🔒 Role-based authorization

### Infrastruttura
- 🏗️ Architettura Portal + Microservices
- 🏗️ Clean Architecture con separazione livelli
- 🏗️ Entity Framework Core 8 con SQL Server
- 🏗️ Dependency Injection
- 🏗️ Configuration per ambiente

### Documentazione
- 📖 README principale con panoramica completa
- 📖 SETUP.md con istruzioni dettagliate
- 📖 GETTING_STARTED.md per quick start
- 📖 NOTES.md con note tecniche e best practices
- 📖 CHANGELOG.md per tracking modifiche

---

## Template per Release Future

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Aggiunto
- Nuove funzionalità

### Modificato
- Modifiche a funzionalità esistenti

### Deprecato
- Funzionalità che saranno rimosse

### Rimosso
- Funzionalità rimosse

### Corretto
- Bug fix

### Sicurezza
- Correzioni vulnerabilità
```

---

**Formato versioni**: MAJOR.MINOR.PATCH

- **MAJOR**: Modifiche incompatibili con API precedenti
- **MINOR**: Nuove funzionalità retrocompatibili
- **PATCH**: Bug fix retrocompatibili
