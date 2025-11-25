# 🚀 Getting Started - Videosystem Platform

## Avvio Rapido in 5 Minuti

### 1. Prerequisiti
- ✅ .NET 8 SDK installato
- ✅ Visual Studio 2022 o Rider

### 2. Apri il Progetto
Doppio click su `VideosystemPlatform.sln`

### 3. Avvia l'Applicazione
- Imposta `Platform.Portal` come progetto di avvio
- Premi `F5`

### 4. Login
Vai su https://localhost:5001

**Credenziali Admin:**
- Username: `admin`
- Password: `Admin123!`

## ✨ Cosa Puoi Fare

### Come Admin
- ✅ Accedere alla dashboard
- ✅ Gestire utenti (creare, modificare, disattivare)
- ✅ Accedere a tutte le applicazioni

### Come User
- ✅ Accedere alla dashboard
- ✅ Accedere alle applicazioni assegnate

## 📚 Documentazione Completa

- [Setup Dettagliato](docs/SETUP.md) - Configurazione avanzata
- [README](README.md) - Panoramica completa

## 🆘 Problemi Comuni

### Database non trovato?
```bash
cd src/Platform.Portal
dotnet ef database update
```

### Porta già in uso?
Modifica `Properties/launchSettings.json` e cambia le porte.

## 📞 Supporto

Contatta il team IT interno Videosystem per assistenza.

---

**Buon Sviluppo! 🎉**

*Videosystem S.r.l. - Internal Platform Team*
