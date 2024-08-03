# Traduire automatiquement les fichiers de balisage avec EasyNMT

## Présentation

EasyNMT est un service installable localement qui fournit une interface simple à un certain nombre de services de traduction automatique. Dans ce tutoriel, nous utiliserons EasyNMT pour traduire automatiquement un fichier Markdown de l'anglais vers plusieurs langues.

## Préalables

Une installation de EasyNMT est nécessaire pour suivre ce tutoriel. D'habitude, c'est un service Docker. Vous pouvez trouver les instructions d'installation [Ici.](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) qui couvre la façon de le faire fonctionner en tant que service de docker.

```shell
docker run -p 24080:80 easynmt/api:2.0-cpu
```

OU si vous avez un GPU disponible:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cpu
```

NOTE: EasyNMT n'est pas le service SMOOTHEST à exécuter, mais c'est le meilleur que j'ai trouvé à cet effet. Il est un peu persnickety sur la chaîne d'entrée qu'il est passé, de sorte que vous pouvez avoir besoin de faire un certain pré-traitement de votre texte d'entrée avant de le passer à EasyNMT.