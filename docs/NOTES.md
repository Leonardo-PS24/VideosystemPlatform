# Note Tecniche - Videosystem Platform

## Architettura Implementata

### Pattern: Portal + Microservices

La piattaforma è stata progettata con un'architettura modulare che separa:

1. **Platform.Portal** - Shell centrale che fornisce:
   - Autenticazione unificata
   - Dashboard con accesso alle applicazioni
   - Gestione utenti centralizzata
   - Emissione JWT token per le applicazioni

2. **Platform.Shared** - Libreria condivisa con:
   - Modelli e costanti comuni
   - Servizi riutilizzabili (JWT, encryption)
   - Helper utilities

3. **Apps/** - Applicazioni indipendenti che possono:
   - Essere deployate separatamente
   - Avere il proprio database
   - Scalare indipendentemente
   - Autenticarsi tramite JWT del Portal

### Vantaggi dell'Architettura

✅ **Scalabilità**: Ogni app può scalare indipendentemente  
✅ **Manutenibilità**: Modifiche isolate non impattano altre app  
✅ **Deploy Indipendente**: Release separate per ogni componente  
✅ **Team Autonomi**: Team diversi possono lavorare su app diverse  
✅ **Tecnologie Miste**: Possibilità di usare tech diverse per app diverse  

## Dettagli Tecnici

### Autenticazione

**Portal → Apps:**
```
User → Login Portal → JWT Token → App valida token → Accesso concesso
```

Il Portal emette un JWT token che contiene:
- UserId
- Username
- Email
- Ruoli (Admin, User)

Ogni app valida il token usando la stessa chiave segreta condivisa.

### Database Strategy

**Approccio Database-per-App:**
- `VideosystemPortal` - Database del Portal (utenti, ruoli)
- `VideosystemKiosk` - Database dell'app Kiosk
- Future app avranno il proprio database

Questo permette:
- Isolamento dei dati
- Schema indipendente per ogni app
- Backup/restore separati
- Scalabilità del database

### Comunicazione Inter-Servizi

Attualmente: **Nessuna comunicazione diretta** tra app.

Se necessario in futuro, implementare:
- REST API tra servizi
- Message queue (RabbitMQ, Azure Service Bus)
- gRPC per performance elevate

### Logging Centralizzato

Tutti i servizi loggano usando **Serilog** con:
- Console output (sviluppo)
- File output (produzione)
- Formato standardizzato

In futuro, considerare:
- Seq (https://datalust.co/seq)
- ELK Stack (Elasticsearch, Logstash, Kibana)
- Azure Application Insights

## Best Practices Implementate

### Sicurezza

✅ Password hashing con Identity (PBKDF2)  
✅ JWT con scadenza (60 minuti default)  
✅ HTTPS obbligatorio in produzione  
✅ CSRF protection su tutti i form  
✅ Input validation client + server  
✅ Password encryption per dati sensibili (AES-256)  

### Performance

✅ Async/await per operazioni I/O  
✅ DbContext con connection pooling  
✅ Static files caching  
✅ Bootstrap CDN per performance  

### Codice

✅ Clean Architecture  
✅ Dependency Injection  
✅ Repository pattern (tramite EF Core)  
✅ DTO per separazione concerns  
✅ Commenti XML su metodi pubblici  

## Roadmap Futura

### Funzionalità da Aggiungere

1. **Email Service**
   - Notifiche via email
   - Password reset
   - Conferma registrazione

2. **Audit Log**
   - Tracciamento azioni utenti
   - Storia modifiche dati
   - Compliance GDPR

3. **Dashboard Analytics**
   - Statistiche utilizzo app
   - Grafici attività utenti
   - Report automatici

4. **API Gateway** (se molte app)
   - Routing unificato
   - Rate limiting
   - Caching

5. **Health Checks**
   - Monitoraggio stato app
   - Alert su problemi
   - Dashboard stato servizi

### Tecnologie da Considerare

- **Blazor Server/WebAssembly**: Per app più interattive
- **SignalR**: Per real-time features
- **Redis**: Per caching distribuito
- **Docker**: Per containerizzazione
- **Kubernetes**: Per orchestrazione (se molte app)

## Migrazione da Identity Locale ad Active Directory

Quando si vorrà integrare Active Directory:

1. **Installare pacchetto**:
```bash
dotnet add package Microsoft.AspNetCore.Authentication.Negotiate
```

2. **Configurare in Program.cs**:
```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
```

3. **Mapping utenti AD → Database**:
   - Sincronizzazione automatica al primo login
   - Mantenere tabella utenti per dati aggiuntivi
   - Ruoli possono essere gestiti internamente o da AD Groups

## Performance Tips

### Database

- Usare indici su colonne frequently queried
- Abilitare query caching
- Considerare read replicas per carichi pesanti
- Monitorare slow queries

### Frontend

- Lazy loading per JavaScript pesante
- Image optimization
- CDN per static assets
- HTTP/2 per multiplexing

### Backend

- Response caching per dati statici
- Distributed caching (Redis) per sessioni
- Background jobs per task lunghi (Hangfire)
- Connection pooling già abilitato

## Monitoraggio Produzione

### Metriche da Tracciare

- Response time (p50, p95, p99)
- Error rate
- CPU/Memory usage
- Database query performance
- Failed login attempts
- Active users

### Tools Consigliati

- **Application Insights** (Azure)
- **New Relic**
- **Datadog**
- **Prometheus + Grafana** (self-hosted)

## Note sulla Sicurezza

### Checklist Pre-Produzione

- [ ] JWT SecretKey robusta (min 32 char casuali)
- [ ] Connection strings in environment variables
- [ ] HTTPS con certificato valido
- [ ] Firewall configurato
- [ ] Database user con privilegi minimi
- [ ] Backup automatici configurati
- [ ] Rate limiting su login endpoint
- [ ] Security headers configurati
- [ ] XSS protection abilitata
- [ ] SQL injection prevention (parametrizzazione query)

### Security Headers da Aggiungere

In `Program.cs`:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

## Contatti Team

Per domande tecniche:
- Team IT Videosystem
- Email: it@videosystem.it

---

**Documento**: INTC_202511251127  
**Autore**: Platform Team  
**Versione**: 1.0.0
