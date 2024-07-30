# Clearing the smoke
## Cloud free systems for startups.

<!--category-- Clearing the smoke, introduction -->

First I am NOT saying that the cloud is somehow evil or unnecessary just that for many startups it can be unnecessary / expensice for 
either your main or dev / test systems.

### Why use cloud based services?

1. Admin...this is my number one reason why cloud services might be a good idea for startups *you just want to get your system up and running, you have little devops experience and no tolerance for downtime.
2. Scaling - this is overused especially for statups. *Be realistic about your scale / growth*. 
3. Compliance - it's easier and quicker to reach full ISO 9001:2015 compliance when running in the cloud (many like [Azure already do this sort of reporting / testing](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Why not use cloud based services?

1. Cost - once your system reaches ny sort of complexity your costs can start to skyrocket. Even for simple services what you pay verus what you get in terms of performance is wildly overpriced in the cloud for example 
if you want to run an ASP.NET system in the cloud with 4 cores, 7GB RAM & 10GB(!) storage (see later, for this price you can purchase a FULL Hetzner server for 5 months!)

![img.png](img.png)

2. Portability - once you build a complex system (say, using Azure Tables, Storage Queues, SQL Server etc) you can essentially be stuck using these systems & paying whatever Microsoft dictates.

3. Skillset - even if you have avoided having to have a DevOps role in your team for administering your own system you'll still need Azure managing skills to design, build and maintain an Azure system. This is often overlooked when making the choice.

This 'blog' (I feel so old) will detail what you need to know as a .NET Developer to get up and running with even fairly complex systems on your own (utility) hardware. 

It will cover many aspects of this sort of 'bootstrap' development from Docker & Docker Compose, selecting services, configuring systems using Caddy, OpenSearch, Postgres, ASP.NET, HTMX and Alpine.js