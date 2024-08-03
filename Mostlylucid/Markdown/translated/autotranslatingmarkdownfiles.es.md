# Traduciendo automáticamente archivos de marcado con EasyNMT

## Introducción

EasyNMT es un servicio instalable localmente que proporciona una interfaz sencilla a una serie de servicios de traducción automática. En este tutorial, usaremos EasyNMT para traducir automáticamente un archivo Markdown del inglés a varios idiomas.

## Requisitos previos

Se requiere una instalación de EasyNMT para seguir este tutorial. Usualmente lo ejecuto como un servicio Docker. Puede encontrar las instrucciones de instalación [aquí](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) que cubre cómo ejecutarlo como un servicio de docker.

```shell
docker run -p 24080:80 easynmt/api:2.0-cpu
```

O si tiene una GPU disponible:

shell
docker run -p 24080:80 easynmt/api:2.0-cuda

NOTE: EasyNMT isn't the SMOOTHEST service to run, but it's the best I've found for this purpose. It is a bit persnickety about the input string it's passed, so you may need to do some pre-processing of your input text before passing it to EasyNMT.
