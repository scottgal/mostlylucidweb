(function(window) {
   
    window.blogswitcher =() =>  {
        const blogLinks = document.querySelectorAll('div.prose  a[href^="/"]');

        // Iterate through all the selected blog links
        blogLinks.forEach(link => {
            link.addEventListener('click', function(event) {
               event.preventDefault();
                let link = event.currentTarget;
                let url = link.href;
                htmx.ajax('get', url, {
                    target: '#contentcontainer',  // The container to update
                    swap: 'innerHTML',            // Replace the content inside the target


                }).then(function() {
                    history.pushState(null, '', url);
                  
                        window.scrollTo({
                            top: 0,
                            behavior: 'smooth' // For a smooth scrolling effect
                        });
                    
                });
            });
        });
        
    };
    
    
})(window);