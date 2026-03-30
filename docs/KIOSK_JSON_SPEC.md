# Specifica Formato JSON per Configuration Kiosk

Questo documento descrive il formato JSON richiesto per creare template di checklist nel modulo Configuration Kiosk. Il sistema supporta un layout responsive a griglia e diversi tipi di input avanzati.

## Struttura Generale

Il file JSON deve contenere un oggetto radice con una proprietà `sections`, che è un array di oggetti sezione. Ogni sezione viene renderizzata come una "Card" distinta.

```json
{
  "checklist_id": "CODICE-UNIVOCO",
  "checklist_title": "Titolo della Checklist",
  "sections": [
    {
      "id": "section_id",
      "title": "Titolo della Sezione (es. HARDWARE)",
      "fields": [
        // ... lista dei campi ...
      ]
    }
  ]
}
```

## Definizione dei Campi (Fields)

Ogni oggetto nell'array `fields` rappresenta un controllo nella checklist.

### Proprietà Comuni

| Proprietà     | Tipo   | Obbligatorio | Descrizione                                      |
|---------------|--------|--------------|--------------------------------------------------|
| `id`          | string | Sì           | Identificativo univoco del campo (senza spazi).  |
| `type`        | string | Sì           | Tipo di input (vedi elenco sotto).               |
| `label`       | string | Sì           | Testo della domanda o etichetta visibile.        |
| `width`       | string | No           | Larghezza griglia: `"full"` (100%), `"half"` (50%), `"third"` (33%). Default: `"full"`. |
| `required`    | bool   | No           | Se `true`, il campo è obbligatorio per il completamento. |
| `description` | string | No           | Testo di aiuto mostrato in piccolo sotto l'etichetta. |
| `placeholder` | string | No           | Testo segnaposto (per text, textarea, number).   |

### Tipi di Campo Supportati

#### 1. Toggle Switch (`toggle`)
Interruttore binario (es. SI/NO, ON/OFF). Renderizzato come switch grafico.
*   **Opzioni**: Array di 2 stringhe. La prima è lo stato "spento" (sinistra), la seconda "acceso" (destra).

```json
{
  "id": "power_check",
  "type": "toggle",
  "label": "Alimentazione Attiva",
  "options": ["NO", "SI"],
  "width": "half"
}
```

#### 2. Checkbox (`checkbox`)
Casella di spunta classica, stilizzata. Ideale per conferme semplici.

```json
{
  "id": "visual_inspection",
  "type": "checkbox",
  "label": "Ispezione visiva superata",
  "width": "half"
}
```

#### 3. Testo (`text`)
Campo di input testuale a riga singola.

```json
{
  "id": "serial_number",
  "type": "text",
  "label": "Numero di Serie",
  "placeholder": "Inserisci SN...",
  "width": "third"
}
```

#### 4. Area di Testo (`textarea`)
Campo di testo multilinea per note o commenti lunghi.

```json
{
  "id": "notes",
  "type": "textarea",
  "label": "Note Tecniche",
  "width": "full",
  "placeholder": "Descrivi eventuali anomalie..."
}
```

#### 5. Numero (`number`)
Campo numerico con validazione opzionale.

```json
{
  "id": "voltage",
  "type": "number",
  "label": "Voltaggio (V)",
  "width": "third",
  "min": 0,
  "max": 240
}
```

#### 6. Selezione (`select`)
Menu a tendina classico.
*   **Opzioni**: Array di stringhe o oggetti `{value, label}`.

```json
{
  "id": "color",
  "type": "select",
  "label": "Colore Finitura",
  "options": ["Bianco", "Nero", "Grigio"],
  "width": "third"
}
```

#### 7. File Upload (`file`)
Area per caricare file o scattare foto. Mostra anteprima se è un'immagine.
*   **Accept**: Filtro tipo file (es. `image/*`, `.pdf`).

```json
{
  "id": "photo_evidence",
  "type": "file",
  "label": "Foto Evidenza",
  "accept": "image/*",
  "width": "half"
}
```

#### 8. Firma Digitale (`signature`)
Riquadro canvas per firma manuale (touch o mouse).

```json
{
  "id": "tech_signature",
  "type": "signature",
  "label": "Firma Tecnico",
  "width": "half"
}
```

#### 9. Data (`date`)
Selettore di data nativo.

```json
{
  "id": "test_date",
  "type": "date",
  "label": "Data Test",
  "width": "third"
}
```

#### 10. Link (`link`)
Pulsante che apre un URL esterno in una nuova scheda.

```json
{
  "id": "manual_link",
  "type": "link",
  "label": "Documentazione",
  "url": "https://docs.example.com",
  "button_text": "Apri Manuale",
  "width": "full"
}
```

## Layout e Griglia (Best Practices)

Il sistema usa una griglia a 12 colonne.
*   `width: "full"` -> 12 colonne (1 per riga)
*   `width: "half"` -> 6 colonne (2 per riga)
*   `width: "third"` -> 4 colonne (3 per riga)

**Consigli per il Design:**
1.  Raggruppa campi correlati (es. 3 misurazioni numeriche) usando `width: "third"`.
2.  Usa `width: "half"` per coppie domanda/risposta o toggle/foto.
3.  Usa `width: "full"` per `textarea`, `file` upload importanti o firme.
4.  Su dispositivi mobili, tutti i campi diventano automaticamente larghezza 100% (`full`) per usabilità.

## Esempio Completo

```json
{
  "checklist_title": "Collaudo Finale",
  "sections": [
    {
      "title": "Controlli Elettrici",
      "fields": [
        { "id": "power_on", "label": "Accensione", "type": "toggle", "options": ["NO", "SI"], "width": "half" },
        { "id": "led_check", "label": "LED Stato", "type": "checkbox", "width": "half" },
        { "id": "volt_1", "label": "V1", "type": "number", "width": "third" },
        { "id": "volt_2", "label": "V2", "type": "number", "width": "third" },
        { "id": "volt_3", "label": "V3", "type": "number", "width": "third" }
      ]
    },
    {
      "title": "Validazione",
      "fields": [
        { "id": "photo", "label": "Foto Etichetta", "type": "file", "accept": "image/*", "width": "half" },
        { "id": "sign", "label": "Firma", "type": "signature", "width": "half" }
      ]
    }
  ]
}
```
