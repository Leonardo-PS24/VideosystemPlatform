# 🚀 Getting Started - Videosystem Platform

## Avvio Rapido in Sviluppo (5 Minuti)

Questa guida illustra come avviare rapidamente la piattaforma sul computer locale dopo la riorganizzazione dell'architettura in **React Frontend** e **C# Web API**.

### 1. Prerequisiti
- ✅ **.NET 10 SDK** installato
- ✅ **Node.js** (v18+) installato
- ✅ Istanza **PostgreSQL/Supabase** attiva (localmente su porta `54322` o cloud)

---

### 2. Esegui il Database
Se non l'hai ancora fatto, applica le migrazioni EF Core sul tuo database PostgreSQL locale:
```bash
cd src/Platform.Portal
dotnet ef database update
```

---

### 3. Avvia il Backend API (.NET)
In un terminale dedicato, esegui il server backend:
```bash
cd src/Platform.Portal
dotnet run
```
*Il server risponderà all'indirizzo `https://localhost:5001`.*

---

### 4. Avvia il Frontend (React)
In un secondo terminale dedicato, avvia l'ambiente di sviluppo React:
```bash
cd src/Platform.Client
npm install
npm run dev
```
*Il frontend si avvierà su [http://localhost:5173](http://localhost:5173).*

---

### 5. Login
Apri il browser su [http://localhost:5173](http://localhost:5173). 

**Credenziali di Amministratore predefinite:**
- **Username**: `admin`
- **Password**: `Admin123!`

---

## 🛠️ Risoluzione Problemi Comuni

### Errore CORS o Cookie?
La piattaforma utilizza lo sviluppo tramite il dev server di Vite che esegue un proxy inverso verso il backend su `https://localhost:5001`. Accedi sempre tramite l'URL di Vite **`http://localhost:5173`** per fare in modo che i cookie HttpOnly della sessione vengano trasmessi automaticamente ed in modo sicuro.

### Errore 404 sulle chiamate API?
Assicurati che il backend C# sia in esecuzione sulla porta `5001`. Se la porta differisce, aggiorna la configurazione del proxy nel file `src/Platform.Client/vite.config.ts`.
