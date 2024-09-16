export  function typeahead() {
    return {
        query: '',
        results: [],
        highlightedIndex: -1, // Tracks the currently highlighted index

        search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }
            let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
                .then(response => {
                    if(response.ok){
                        return  response.json();
                    }
                    return Promise.reject(response);
                })
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                    this.$nextTick(() => {
                        htmx.logAll();
                        htmx.process(document.getElementById('searchresults'));
                    });
                })
                .catch((response) => {
                    console.log(response.status, response.statusText);
                    if(response.status === 400)
                    {
                        console.log('Bad request, reloading page to try to fix it.');
                        window.location.reload();
                    }
                    response.json().then((json) => {
                        console.log(json);
                    })
                    console.log("Error fetching search results");
                });
        },

        moveDown() {
            if (this.highlightedIndex < this.results.length - 1) {
                this.highlightedIndex++;
            }
        },

        moveUp() {
            if (this.highlightedIndex > 0) {
                this.highlightedIndex--;
            }
        },

        selectHighlighted() {
            if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
                this.selectResult(this.highlightedIndex);
                
            }
        },

        selectResult(selectedIndex) {
       let links = document.querySelectorAll('#searchresults a');
           
       links[selectedIndex].click();
       
            this.$nextTick(() => {
                this.results = []; // Clear the results
                this.highlightedIndex = -1; // Reset the highlighted index
                this.query = ''; // Clear the query
            });
        }
    }
}