# Specifica Formato JSON per Configuration Kiosk

Questo documento descrive il formato JSON richiesto per creare nuovi template di checklist nel modulo Configuration Kiosk.

## Struttura Generale

Il file JSON deve contenere un oggetto radice con una proprietà `sections`, che è un array di oggetti sezione.

```json
{
  "sections": [
    {
      "title": "Nome della Sezione",
      "fields": [
        // ... lista dei campi ...
      ]
    }
  ]
}
```

## Definizione dei Campi (Fields)

Ogni oggetto nell'array `fields` rappresenta una domanda o un controllo nella checklist.

### Proprietà Comuni

| Proprietà | Tipo   | Obbligatorio | Descrizione                                      |
|-----------|--------|--------------|--------------------------------------------------|
| `id`      | string | Sì           | Identificativo univoco del campo (senza spazi).  |
| `type`    | string | Sì           | Tipo di input: `"text"`, `"checkbox"`, `"select"`. |
| `label`   | string | Sì           | Testo della domanda o etichetta visibile.        |

### Tipi di Campo

#### 1. Testo (`text`)
Campo di input testuale semplice.

```json
{
  "id": "serial_number",
  "type": "text",
  "label": "Numero di Serie Componente"
}
```

#### 2. Checkbox (`checkbox`)
Casella di controllo Sì/No.

```json
{
  "id": "is_clean",
  "type": "checkbox",
  "label": "La superficie è pulita?"
}
```

#### 3. Selezione (`select`)
Menu a tendina con opzioni predefinite. Richiede la proprietà `options`.

```json
{
  "id": "color",
  "type": "select",
  "label": "Colore Finitura",
  "options": ["Nero", "Bianco", "Grigio", "Custom"]
}
```

## Esempio Completo

```json
{
  "sections": [
    {
      "title": "Ispezione Preliminare",
      "fields": [
        {
          "id": "check_visual",
          "type": "checkbox",
          "label": "Ispezione visiva superata"
        },
        {
          "id": "operator_notes",
          "type": "text",
          "label": "Note operatore"
        }
      ]
    },
    {
      "title": "Configurazione Hardware",
      "fields": [
        {
          "id": "voltage_setting",
          "type": "select",
          "label": "Impostazione Voltaggio",
          "options": ["220V", "110V"]
        }
      ]
    }
  ]
}
```
