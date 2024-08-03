# Lo viejo es nuevo de nuevo.

## Modelos Dev para aplicaciones web

<datetime class="hidden">2024-07-30T13:30</datetime>

En mi LONG (30 años) historia de la construcción de aplicaciones web ha habido muchas maneras de construir una aplicación web.

1. Pure HTML 1990-> - el primer mecanismo (si se ignora BBS / sistemas basados en texto) para la construcción de aplicaciones web fue el HTML antiguo plano. Construir una página web, enumerar un montón de productos y proporcionar un correo electrónico en dirección, número de teléfono o incluso correo electrónico para enviar pedidos.
   Esto tenía algunas ventajas y (muchas) desventajas.

- En primer lugar, era simple; usted acaba de dar una lista de un montón de productos, el usuario seleccionó lo que quería, luego envió un cheque a la dirección y esperó para obtener sus productos
- Se renderizó rápidamente (importante en esos días como la mayoría de la gente accedía a la web a través de módems, estás hablando *kilobytes* por segundo).
- Lo fue. *equitativamente* fácil de actualizar. Sólo actualizaría el archivo HTML y lo subiría a cualquier servidor que estuviera usando (usando FTP más comúnmente)
- Sin embargo fue SLOW... el servicio de correo no es rápido, los cheques son lentos en efectivo, etc...

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - podría decirse que la primera tecnología 'activa' utilizada para la web. Usarías C (el primer idioma que usé) o algo así como Perl para generar contenido HTML

- Finalmente tienes que usar los comienzos de la web'moderna', estos usarían una variedad de formatos 'datos' para mantener contenido y bases de datos más recientes para permitir el nivel de interacción comparable a las aplicaciones'modernas'.

- Eran complejos de codificar y actualizar. Estos eran CÓDIGO, mientras que en los últimos tiempos había lenguajes plantillados utilizados para producir HTML el todavía no era simple.

- No *real* Depuración.

- En los primeros días, mientras que usted podía aceptar tarjetas de crédito estas transacciones fueron *relativamente* Inseguras y las puertas de entrada de pago anticipadas eran todavía un poco de un salvaje-oeste.

3. Las lenguas "templadas" (~1995->). Los gustos de PHP, ColdFusion y sí ASP (no.net!) fueron el comienzo de permitir el 'desarrollo rápido' para aplicaciones web.

- Fueron relativamente rápidos de actualizar (todavía utilizando sobre todo FTP)
- Para este momento SSL se había convertido en ampliamente adoptado para los sitios de comercio electrónico por lo que finalmente fueron capaces de ser razonablemente seguros entrar en los detalles de pago en línea.
- Las bases de datos habían comenzado a madurar, por lo que ahora era posible disponer de un sistema de bases de datos "adecuado" para manejar los datos de los productos, los datos de los clientes, etc.
- Alimentó el primer 'boom dotcom' - MUCHOS nuevos sitios web y tiendas aparecieron, muchos fallaron (MÁS REALMENTE a principios de los años 2000) fue un poco como un salvaje oeste.

4. La era moderna (2001->). Tras esta primera oleada de emoción del comercio electrónico comenzaron a aparecer marcos de programación web más'maduros'. Esto permitió el uso de patrones y enfoques más establecidos.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - el patrón Model-View-Controller. Esta era realmente una forma de organizar el código que permitía la separación de responsabilidades en segmentos convincentes de diseño de aplicaciones. Mi primera experiencia de esto fue en los días de J2EE & JSP.
- [RAD](https://en.wikipedia.org/wiki/Rapid_application_development) - Desarrollo de aplicaciones rápidas. Como el nombre sugiere que esto se centró en 'hacer que las cosas funcionen' rápidamente. Este fue el enfoque seguido en ASP.NET (forma 1999->) con el marco WebForms.