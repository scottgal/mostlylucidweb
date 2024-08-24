# Ce qui est vieux, c'est nouveau.

## Modèles Dev pour les applications Web

<datetime class="hidden">2024-07-30T13:30</datetime>

Dans mon histoire de LONG (30 ans) de la construction d'applications web, il y a eu de nombreuses façons de construire une application web.

1. Pure HTML 1990-> - le tout premier (si vous ignorez BBS / systèmes à base de texte) mécanisme pour la construction d'applications Web était Plain Old HTML. Construire une page Web, lister un tas de produits et fournir un courrier dans l'adresse, le numéro de téléphone ou même l'email pour envoyer des commandes à.
   Cela présentait quelques avantages et (beaucoup) inconvénients.

- Tout d'abord, c'était simple; vous venez de donner une liste d'un tas de produits, l'utilisateur a sélectionné ce qu'il voulait puis envoyé un chèque à l'adresse et a attendu pour obtenir vos marchandises
- Il rendu rapidement (important dans ces jours-là que la plupart des gens ont accédé au web sur les modems, vous parlez *kilooctets* par seconde).
- C'était *assez* simple à mettre à jour. Vous venez de mettre à jour le fichier HTML et de le télécharger sur n'importe quel serveur que vous utilisiez (en utilisant FTP le plus souvent)
- Cependant, c'était SLOW... le service de courrier n'est pas rapide, les chèques sont lents à l'argent, etc...

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - sans doute la première technologie « active » utilisée pour le web. Vous utiliseriez soit C (la première langue que j'ai utilisée) soit quelque chose comme Perl pour générer du contenu HTML

- Vous avez finalement dû utiliser les débuts du web «moderne», ceux-ci utiliseraient une variété de formats de «données» pour tenir le contenu et les bases de données plus tôt pour permettre le niveau d'interaction comparable aux applications «modernes».

- Ils étaient complexes à coder et à mettre à jour. Il s'agissait de CODE, alors qu'il y avait ensuite des langages modélisés utilisés pour afficher le HTML, ce qui n'était toujours pas simple.

- Numéro *réel* Le débogage.

- Dans les premiers jours, pendant que vous pouviez accepter les cartes de crédit, ces transactions étaient *relativement* l'insécurité et les passerelles de paiement précoce étaient encore un peu un wild-west.

3. Les langues 'templates' (~1995->). Les goûts de PHP, ColdFusion et oui ASP (non.net!) ont été le début d'autoriser le 'développement rapide' pour les applications web.

- Ils ont été relativement rapides à mettre à jour (toujours principalement en utilisant FTP)
- À ce moment SSL était devenu largement adopté pour les sites de commerce électronique, vous avez finalement pu être raisonnablement en sécurité en entrant les détails de paiement en ligne.
- Les bases de données avaient commencé à mûrir, de sorte qu'il était désormais possible d'avoir un système de base de données « propre » pour gérer les données sur les produits, les données sur les clients, etc.
- Il a alimenté le premier « boom de dotcom » - beaucoup de nouveaux sites Web et magasins ont surgi, beaucoup ont échoué (MOST vraiment au début des années 2000) il était un peu un wild west.

4. L'ère moderne (2001->). À la suite de cette première vague d'excitation du commerce électronique, des cadres de programmation web plus « matures » ont commencé à apparaître. Cela a permis d'utiliser des modèles et des approches plus établis.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - le modèle Model-View-Controller. C'était vraiment une façon d'organiser le code permettant la séparation des responsabilités en segments cohérents de la conception de l'application. Ma première expérience a été de retour à l'époque de J2EE & JSP.
- [NIVEAU](https://en.wikipedia.org/wiki/Rapid_application_development) - Développement d'applications rapides. Comme son nom l'indique, il s'agissait de « faire marcher les choses » rapidement. C'était l'approche suivie dans ASP.NET (formulaire 1999->) avec le cadre WebForms.