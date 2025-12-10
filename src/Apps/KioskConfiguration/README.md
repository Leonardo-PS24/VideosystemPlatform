# KioskConfiguration

Applicazione per la gestione delle checklist di configurazione kiosk con sistema dinamico basato su template JSON.

## 📋 Features

✅ Template JSON dinamici - Nessuna modifica al codice per nuovi modelli
✅ 13 tipi di campo supportati (text, checkbox, dropdown, file upload, etc.)
✅ Validazione client e server-side
✅ Campi condizionali (mostra/nascondi basato su altri campi)
✅ Upload file multipli con preview
✅ Campi password criptati nel database
✅ Export PDF delle checklist completate
✅ Audit trail completo (chi, cosa, quando)
✅ Sistema di approvazione checklist

## 🚀 Installazione

### STEP 1: Crea struttura directory

```powershell
cd C:\Users\leona\RiderProjects\VideosystemPlatform\VideosystemPlatform\src\Apps

# Esegui lo script PowerShell
# (scaricalo dai file che ti ho fornito: setup_kiosk_configuration.ps1)
.\setup_kiosk_configuration.ps1
```

### STEP 2: Copia i file nel progetto

```powershell
$basePath = "C:\Users\leona\RiderProjects\VideosystemPlatform\VideosystemPlatform\src\Apps\KioskConfiguration"

# Copia Models
Copy-Item ConfigurationTemplate.cs "$basePath\Models\"
Copy-Item KioskConfiguration.cs "$basePath\Models\"

# Copia Services
Copy-Item ITemplateService.cs "$basePath\Services\"
Copy-Item TemplateService.cs "$basePath\Services\"

# Copia Data
Copy-Item ApplicationDbContext.cs "$basePath\Data\"

# Copia file progetto
Copy-Item Platform.Apps.KioskConfiguration.csproj "$basePath\"
Copy-Item Program.cs "$basePath\"
Copy-Item appsettings.json "$basePath\"

# Copia template JSON di esempio
Copy-Item kiosk-premium-outdoor-v1.json "$basePath\ConfigurationTemplates\"

# Copia documentazione
Copy-Item KioskConfiguration_Template_JSON_Guide.txt "$basePath\"
```

### STEP 3: Aggiungi progetto alla Solution

```powershell
cd C:\Users\leona\RiderProjects\VideosystemPlatform\VideosystemPlatform

dotnet sln add src\Apps\KioskConfiguration\Platform.Apps.KioskConfiguration.csproj
```

### STEP 4: Restore packages

```powershell
cd src\Apps\KioskConfiguration
dotnet restore
```

### STEP 5: Crea database migration

```powershell
dotnet ef migrations add InitialCreate --context ApplicationDbContext

dotnet ef database update --context ApplicationDbContext
```

### STEP 6: Build e Run

```powershell
dotnet build
dotnet run
```

L'applicazione sarà disponibile su: https://localhost:5001

## 📁 Struttura Progetto

```
KioskConfiguration/
├── ConfigurationTemplates/          ← File JSON qui!
│   └── kiosk-premium-outdoor-v1.json
├── Controllers/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Models/
│   ├── ConfigurationTemplate.cs
│   ├── KioskConfiguration.cs
│   └── ViewModels/
├── Services/
│   ├── ITemplateService.cs
│   └── TemplateService.cs
├── Views/
│   ├── Configuration/
│   ├── Template/
│   └── Shared/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── uploads/
│       └── configurations/
├── Program.cs
├── appsettings.json
└── Platform.Apps.KioskConfiguration.csproj
```

## 📄 Come Aggiungere Nuovi Template

### Metodo 1: Creare file JSON manualmente

1. Crea nuovo file in `ConfigurationTemplates/`
2. Segui la guida: `KioskConfiguration_Template_JSON_Guide.txt`
3. Restart applicazione (auto-load al boot)

### Metodo 2: Upload tramite UI (TODO)

1. Vai su `/Template/Upload`
2. Carica file JSON
3. Sistema valida e salva nel database

## 🔧 Configurazione

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KioskConfiguration;..."
  },
  "FileUpload": {
    "MaxFileSize": 10485760,         // 10 MB
    "AllowedExtensions": [".jpg", ".png", ".pdf"],
    "UploadPath": "wwwroot/uploads/configurations"
  }
}
```

## 📊 Database Schema

### ConfigurationTemplates
Template JSON salvati nel database

### KioskConfigurations
Configurazioni compilate (header)

### ConfigurationFieldValues
Valori dei campi dinamici

### ConfigurationAttachments
File allegati (foto, PDF)

## 🎯 Prossimi Sviluppi

- [ ] Controller e Views CRUD complete
- [ ] Dynamic Form Renderer Component
- [ ] Export PDF checklist compilata
- [ ] Sistema di approvazione (Approve/Reject)
- [ ] Dashboard statistiche
- [ ] Integrazione sistema permessi Platform.Portal
- [ ] API REST per integrazione esterna
- [ ] Mobile app per compilazione checklist on-site

## 📚 Documentazione

- **Template JSON Guide**: `KioskConfiguration_Template_JSON_Guide.txt`
- **API Reference**: Coming soon
- **User Manual**: Coming soon

## 🐛 Troubleshooting

**Problema: Template non vengono caricati**
- Verifica che i file JSON siano in `ConfigurationTemplates/`
- Controlla che il JSON sia valido (usa jsonlint.com)
- Guarda i log in `logs/kiosk-configuration-*.txt`

**Problema: Database non si crea**
- Verifica connection string in appsettings.json
- Esegui: `dotnet ef database update --context ApplicationDbContext`

**Problema: File upload fallisce**
- Verifica permessi scrittura su `wwwroot/uploads/configurations/`
- Controlla MaxFileSize in appsettings.json

## 📞 Contatti

**Videosystem S.r.l.**
Via Lago di Albano, 45 | 36015 Schio - Italia
+39 0445 500 500
www.videosystem.it

---

**Versione**: 1.0
**Data**: 2025-11-27
**Autore**: Platform Development Team
