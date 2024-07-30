# What's old is new again

## Dev models for Web Applications

In my LONG (30 year) history of building web applications there have been many ways to build a web app.
1. Pure HTML 1990-> - the very first (if you ignore BBS / text based systems) mechanism for building web apps was Plain Old HTML. Building a web page, list a bunch of products and provide a mail in address, phone number or even email to send orders to.
This had a few advantages and (many) disadvantages. 
- Firstly it was simple; you just gave a list of a bunch of products, the user selected whatever they wanted then sent a cheque to the address and waited to get your goods
- It rendered quickly (important in those days as most people accessed the web over modems, you're talking *kilobytes* per second). 
- It was *fairly* straightforward to update. You'd just update the HTML file and upload it to whatever server you were using (using FTP most commonly)
- However it was SLOW...the mail service ain't quick, cheques are slow to cash etc...

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - arguably the first 'active' technology used for the web. You would use either C (the first language I used) or something like Perl to generate HTML content
- You finally got to use the beginnings of the 'modern' web, these would use a variety of 'data' formats to hold content and latterly early databases to allow the level of interaction comparable to  'modern' applications.

- They were complex to code and update. These were CODE, while latterly there were templated languages used to output HTML the still weren't simple. 
- No *real* debugging.
- In the early days while you could accept credit cards these transactions were *relatively* insecure and the early payment gateways were still a bit of a wild-west.

3. The 'template' languages (~1995->). The likes of PHP, ColdFusion and yes ASP (no .net!) were the start of allowing 'Rapid Development' for web applications.
- They were relatively quick to update (still mostly using FTP)
- By this time SSL had become widely adopted for e-commerce sites so you finally were able to be reasonably safe entering payment details online. 
- Databases had started to mature so it was now possible to have a 'proper' database system to handle product data, customer data etc. 
- It fueled the first 'dotcom boom' - MANY new websites and stores popped up, many failed (MOST really by the early 2000s) it was a bit of a wild west.

4. The modern era (2001->). Following this first rush  of ecommerce  excitement more 'mature' web programming frameworks started to appear. These allowed the use of more established patterns and approaches.
- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - the Model-View-Controller pattern. This was really a way of organising code allowing the separation of responsibilities into cogent segments of application design. My first experience of this was back in the days of J2EE & JSP.  
- [RAD](https://en.wikipedia.org/wiki/Rapid_application_development) - Rapid Application Development. As the name suggests this was focused on 'getting stuff working' quickly. This was the approach followed in ASP.NET (form 1999->) with the WebForms framework.
