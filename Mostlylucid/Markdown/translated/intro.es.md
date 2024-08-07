# Limpiando el humo

## Sistemas libres de nube para startups.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30</datetime>

Primero NO estoy diciendo que la nube es de alguna manera mala o innecesaria sólo que para muchas startups puede ser innecesario / gastos para
o bien sus sistemas principales o dev / test.

### ¿Por qué utilizar servicios basados en la nube?

1. Administrador... esta es mi razón número uno por qué los servicios en la nube podrían ser una buena idea para las startups*solo quieres poner tu sistema en marcha, tienes poca experiencia en devops y no toleras el tiempo de inactividad.
2. Escalado - esto está sobreutilizado especialmente para las estadísticas.*Sea realista acerca de su escala / crecimiento*.
3. Cumplimiento - es más fácil y rápido alcanzar el cumplimiento completo de ISO 9001:2015 cuando se ejecuta en la nube (muchos como[Azure ya hace este tipo de informes / pruebas](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### ¿Por qué no utilizar servicios basados en la nube?

1. Costo - una vez que su sistema alcanza ni tipo de complejidad sus costos pueden comenzar a dispararse. Incluso para los servicios simples lo que usted paga verus lo que obtiene en términos de rendimiento es extremadamente caro en la nube, por ejemplo
   si desea ejecutar un sistema ASP.NET en la nube con 4 núcleos, 7GB de RAM y 10GB(!) de almacenamiento (ver más adelante, para este precio puede comprar un servidor FULL Hetzner durante 5 meses!)

![img.png](img.png?width=500&format=webp)

2. Portabilidad - una vez que se construye un sistema complejo (por ejemplo, utilizando tablas de Azure, colas de almacenamiento, SQL Server, etc) se puede esencialmente quedarse atascado utilizando estos sistemas y pagar lo que Microsoft dicta.

3. Skillset - incluso si usted ha evitado tener que tener un papel DevOps en su equipo para administrar su propio sistema que todavía necesitará Azure las habilidades de gestión para diseñar, construir y mantener un sistema Azure. Esto a menudo se pasa por alto al tomar la decisión.

Este 'blog' (me siento tan viejo) detallará lo que necesita saber como desarrollador.NET para ponerse en marcha con sistemas incluso bastante complejos en su propio hardware (utilidad).

Cubrirá muchos aspectos de este tipo de desarrollo 'bootstrap' de Docker & Docker Compose, seleccionando servicios, configurando sistemas usando Caddy, OpenSearch, Postgres, ASP.NET, HTMX y Alpine.js.