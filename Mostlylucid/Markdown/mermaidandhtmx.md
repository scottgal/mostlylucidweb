# Adding mermaid.js with htmx

<!--category-- HTMX, Markdown -->

<datetime class="hidden">2024-08-02T20:00</datetime>


## Introduction
Mermaid is a simple diagramming format that takes text-based input and generates diagrams in SVG format. It is a great tool for creating flowcharts, sequence diagrams, Gantt charts, and more. In this tutorial, we will explore how to use Mermaid with htmx to create interactive diagrams that update dynamically without page reloads.
The Mermaid site is [here](https://mermaid.js.org/) and has far more information than I can provide here.



### Examples of Mermaid diagrams
Mermaid is a powerful tool that lets you build a wide range of diagrams using simple text-based syntax.
Here are some examples of the types of diagrams you can create with Mermaid:

-Pie charts:
```mermaid
pie title NETFLIX
"Time spent looking for movie" : 90
"Time spent watching it" : 10
```

-Flowcharts:
```mermaid
flowchart TD
    A[Start] --> B{Is it?}
    B -->|Yes| C[OK]
    C --> D[Rethink]
    D --> B
    B ---->|No| E[End]
```

-Sequence diagrams:
```mermaid
sequenceDiagram
    participant A
    participant B
    A->>B: Hi B, how are you?
    B-->>A: Fine, thanks!
```

## Getting started with Mermaid and htmx
First you need to include the Mermaid library in your HTML file. You can do this by adding the following script tag to your document:

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.min.js
"></script>
```

Next in your _Layout.cshtml file you need to add the following script tag to initialize Mermaid (you normally do this at the bottom of the file)

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

This does two things; 
1. It initializes Mermaid when the page loads; so if you directly navigate to a page with a Mermaid diagram (e.g. [/blog/mermaidandhtmx](/blog/mermaidandhtmx) ) it will render correctly.
2. If you use htmx as in our [previous tutorial](/blog/htmxwithaspnetcore) it will re-render the Mermaid diagram after the page has been updated (the htmx:afterswap event).