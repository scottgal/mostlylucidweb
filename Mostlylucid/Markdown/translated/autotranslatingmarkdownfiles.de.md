# Automatisches Übersetzen von Markdown-Dateien mit EasyNMT

## Einleitung

EasyNMT ist ein lokal installierbarer Service, der eine einfache Schnittstelle zu einer Reihe von maschinellen Übersetzungsdiensten bietet. In diesem Tutorial werden wir EasyNMT verwenden, um eine Markdown-Datei automatisch von Englisch in mehrere Sprachen zu übersetzen.

## Voraussetzungen

Um diesem Tutorial zu folgen, ist eine Installation von EasyNMT erforderlich. Normalerweise leite ich es als Docker-Service. Die Installationsanleitung finden Sie [Hierher](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) die wie man es als Docker-Service laufen lässt.

```shell
docker run -p 24080:80 easynmt/api:2.0-cpu
```

ODER wenn Sie eine GPU zur Verfügung haben:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cpu
```

HINWEIS: EasyNMT ist nicht der SMOOTHEST Service, aber es ist das Beste, was ich für diesen Zweck gefunden habe. Es ist ein bisschen persnickety über die Eingabe Zeichenkette, die es übergeben wird, so dass Sie möglicherweise einige Vorverarbeitung Ihres Eingabetextes tun müssen, bevor Sie es an EasyNMT übergeben.