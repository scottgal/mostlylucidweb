# Hinzufügen von mermaid.js mit htmx

<!--category-- HTMX, Markdown -->
<datetime class="hidden">2024-08-02T20:00</datetime>

## Einleitung

Mermaid ist ein einfaches Diagrammformat, das textbasierte Eingaben nimmt und Diagramme im SVG-Format generiert. Es ist ein großartiges Werkzeug für die Erstellung von Flussdiagrammen, Sequenzdiagrammen, Gantt-Diagrammen und mehr. In diesem Tutorial werden wir untersuchen, wie man Mermaid mit htmx verwendet, um interaktive Diagramme zu erstellen, die dynamisch ohne Seitenneuladen aktualisieren.
Die Mermaid Website ist [Hierher](https://mermaid.js.org/) und hat viel mehr Informationen, als ich hier bereitstellen kann.

[TOC]

## Markdown und Meerjungfrau

Mermaid-Diagramme können in Ihre Markdown-Dateien aufgenommen werden, indem Sie die folgende Syntax verwenden:

<pre>
# My Markdown Title
```mermaid
graph LR
    A[Start] --> B[Look for movie]
    B --> C{Found?}
    C -->|Yes| D[Watch it]
    C -->|No| E[Look for another movie]
    D --> E
```
</pre>
Dies ermöglicht es Ihnen, Mermaid-Diagramme direkt in Ihre Markdown-Dateien aufzunehmen, die als SVG-Bilder wiedergegeben werden, wenn die Datei in HTML konvertiert wird.

```mermaid
graph LR
    A[Start] --> B[Look for movie]
    B --> C{Found?}
    C -->|Yes| D[Watch it]
    C -->|No| E[Look for another movie]
    D --> E
```

Sie können auch Meerjungfrau-Diagramme zu normalen HTML-Dateien hinzufügen, indem Sie die folgende Syntax verwenden:

```html
<pre class="mermaid">
    graph TD
    A[Start] --> B[Look for movie]
    B --> C{Found?}
    C -->|Yes| D[Watch it]
    C -->|No| E[Look for another movie]
    D --> E
</pre>
```

### Beispiele für Meerjungfrauen-Diagramme

Mermaid ist ein leistungsstarkes Tool, mit dem Sie eine breite Palette von Diagrammen mit einfachen textbasierten Syntax erstellen können.
Hier sind einige Beispiele für die Arten von Diagrammen, die Sie mit Mermaid erstellen können:

- Pie Charts:

```mermaid
pie title NETFLIX
"Time spent looking for movie" : 90
"Time spent watching it" : 10
```

- Flugpläne:
Flussdiagramme können die Richtung angeben, z.B. LR (von links nach rechts), RL (von rechts nach links), TB (von oben nach unten), BT (von unten nach oben).

```mermaid
flowchart LR
    A[Start] --> B{Is it?}
    B -->|Yes| C[OK]
    C --> D[Rethink]
    D --> B
    B ---->|No| E[End]
```

- Sequenzdiagramme:

```mermaid
sequenceDiagram 
    participant A
    participant B
    A->>B: Hi B, how are you?
    B-->>A: Fine, thanks!
```

- Gantt-Diagramme:

```mermaid
gantt
    title A Gantt Diagram
    dateFormat  YYYY-MM-DD
    section Section
    A task           :a1, 2024-08-01, 30d
    Another task     :after a1  , 20d
```

-Entität Beziehungsdiagramme:

```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE-ITEM : contains
    CUSTOMER }|..|{ DELIVERY-ADDRESS : uses
```

-Benutzer-Reisediagramme:

```mermaid
journey
    title My working day
    section Go to work
        Make tea: 5: Me
        Go upstairs: 15: Me
        Do work: 60: Me
    section Go home
        Go downstairs: 15: Me
        Sit down: 5: Me
```

etc...Siehe diese Seite für mehr der MYRIAD von Diagrammen, die Sie mit Mermaid erstellen können [Hierher](https://mermaid.js.org/syntax/examples.html)

## Erste Schritte mit Mermaid und htmx

Zuerst müssen Sie die Mermaid-Bibliothek in Ihre HTML-Datei aufnehmen. Sie können dies tun, indem Sie das folgende Script-Tag zu Ihrem Dokument hinzufügen:

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.min.js
"></script>
```

Der Nächste in Ihrem _Layout.cshtml-Datei müssen Sie das folgende Script-Tag hinzufügen, um Mermaid zu initialisieren (in der Regel tun Sie dies am unteren Rand der Datei)

```html
<script>
    document.addEventListener('DOMContentLoaded', function () {
        mermaid.initialize({ startOnLoad: true });
    });
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        mermaid.run();
        
    });

</script>
```

Das macht zwei Dinge:

1. Es initialisiert Mermaid, wenn die Seite geladen wird; wenn Sie also direkt zu einer Seite mit einem Mermaid-Diagramm navigieren (z.B. [/blog/mermaidandhtmx](/blog/mermaidandhtmx) ) wird es korrekt rendern.
2. Wenn Sie htmx wie in unserem [vorheriges Tutorial](/blog/htmxwithaspnetcore) es wird das Mermaid-Diagramm nach der Aktualisierung der Seite erneut ausgeben (das htmx:afterswap-Ereignis).