function global() {
    return {
        isMobileMenuOpen: false,
        isDarkMode: false,
        themeInit() {
            if (
                localStorage.theme === "dark" ||
                (!("theme" in localStorage) &&
                    window.matchMedia("(prefers-color-scheme: dark)").matches)
            ) {
                localStorage.theme = "dark";
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
            } else {
                localStorage.theme = "light";
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
            }
        },
        themeSwitch() {
            if (localStorage.theme === "dark") {
                localStorage.theme = "light";
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
            } else {
                localStorage.theme = "dark";
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
            }
        },
    };
}

window.global = global;
function setLogoutLink() {
    // Get the logout link
    var logoutLink = document.querySelector('a[data-logout-link]');

    if (logoutLink) {
        // Get the current URL
        var currentUrl = window.location.href;

        // Update the href attribute to include the return URL
        var baseUrl = logoutLink.href.split('?')[0]; // Get the base URL without query parameters
        logoutLink.href = baseUrl + '?returnUrl=' + encodeURIComponent(currentUrl);
    }
}
let googleSignInInitialized = false;
let simpleMDEInitialized = false;
let mermaidInitialized = false;
window.onload= function () {
    console.log('Window loaded');
    // Google Sign-In Initialization
    if (!googleSignInInitialized) {
       initGoogleSignIn();
        googleSignInInitialized = true;  // Set the flag to true after initialization
    }

    // Highlight.js Initialization
    hljs.highlightAll();

    // Mermaid.js Initialization
    if (!mermaidInitialized) {
        mermaid.initialize({ startOnLoad: true });
        mermaidInitialized = true;  // Set the flag to true after initialization
    }

    // SimpleMDE Initialization
    initializeSimpleMDE();
}
document.body.addEventListener('htmx:afterSwap', function(evt) {
    console.log('HTMX afterSwap triggered', evt);

    hljs.highlightAll();
    initializeSimpleMDE();

    const url = evt.detail.pathInfo.requestPath;

    if (typeof umami !== 'undefined' && url) {
        console.log('Tracking page view with Umami', url);
        umami.track(props => ({ ...props, url:url }));
    } else {
        console.log('umami is not defined');
    }

    initGoogleSignIn();
    mermaid.run();
    setLogoutLink();
});


function initializeSimpleMDE() {
    if(simpleMDEInitialized) return;
    const element = document.getElementsByClassName('markdowneditor')[0];
    
    if (!element) return;
    if (window.simplemde) {
        window.simplemde.toTextArea();
        window.simplemde = null;
    }

    // Initialize a new SimpleMDE instance
    window.simplemde = new SimpleMDE({ element: element,
        toolbar: [
            "bold", "italic", "heading", "|", "quote", "unordered-list", "ordered-list", "|",
            {
                name: "save",
                action: function(editor){
                    var params = new URLSearchParams(window.location.search);
                    var slug = params.get("slug");
                    var language = params.get("language");

                    // Check if the values exist
                    if (!slug || !language) {
                        console.error("Missing slug or language in the URL");
                        return;
                    }

                    saveContentToDisk(editor.value(), slug, language);
                   
                },
                className: "bx bx-save", // FontAwesome floppy disk icon
                title: "Save",
            },
            "|", "preview", "side-by-side", "fullscreen"
        ]});

}

window.mostlylucid = window.mostlylucid || {};

window.mostlylucid.typeahead = function () {
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

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
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
                this.selectResult(this.results[this.highlightedIndex]);
            }
        },

        selectResult(result) {
           window.location.href = result.url;
            this.results = []; // Clear the results
            this.highlightedIndex = -1; // Reset the highlighted index
        }
    }
}
window.simplemde = null;

function saveContentToDisk(content, slug, language) {
    console.log("Saving content to disk...");

    // Determine the filename based on the slug and language
    var filename;
    if (language === 'en') {
        filename = slug + ".md";
    } else {
        filename = slug + "." + language + ".md";
    }

    // Create a Blob with the content
    var blob = new Blob([content], { type: "text/markdown;charset=utf-8;" });

    // Create a temporary link element to trigger the download
    var link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = filename;

    // Append link to the body temporarily and click it to trigger download
    document.body.appendChild(link);
    link.click();

    // Clean up by removing the link element
    document.body.removeChild(link);

    console.log("Download triggered for " + filename);
}



function renderButton(element)
{
    google.accounts.id.renderButton(
        element,
        {
            type: "standard",
            size: "large",
            width: 200,
            theme: "filled_black",
            text: "sign_in_with",
            shape: "rectangular",
            logo_alignment: "left"
        }
    );
}
function initGoogleSignIn() {
    google.accounts.id.initialize({
        client_id: "839055275161-u7dqn2oco2729n6i5mk0fe7gap0bmg6g.apps.googleusercontent.com",
        callback: handleCredentialResponse
    });
    const element = document.getElementById('google_button');
    if (element) {
        renderButton(element);
    }
    const secondElement = document.getElementById('google_button2');
    if (secondElement) {
        renderButton(secondElement);
    }

}

function handleCredentialResponse(response) {
    console.log('Handling credential response:', response);

    if (response.credential) {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/login', true);
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.onload = function () {
            if (xhr.status === 200) {
                console.log('Login successful, reloading page...');
                window.location.reload();  // Ensure this is only triggered once
            } else {
                console.error('Failed to log in.');
            }
        };
        xhr.send(JSON.stringify({ idToken: response.credential }));
    } else {
        console.error('No credential in response.');
    }
}

window.codeEditorInit = function(){
    console.log('Page loaded without refresh');

    // Trigger on change event of SimpleMDE editor
    window.simplemde.codemirror.on("keydown", function(instance, event) {
        let triggerUpdate= false;
        // Check if the Enter key is pressed
        if ((event.ctrlKey || event.metaKey) && event.altKey && event.key.toLowerCase() === "r") {
            event.preventDefault(); // Prevent the default behavior (e.g., browser refresh)
            triggerUpdate = true;
        }
        if (event.key === "Enter")
        {
            triggerUpdate = true;
        }

        if (triggerUpdate) {

            var content = simplemde.value();

            // Send content to WebAPI endpoint
            fetch('/api/editor/getcontent', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ content: content })  // JSON object with 'content' key
            })
                .then(response => response.json())  // Parse the JSON response
                .then(data => {
                    // Render the returned HTML content into the div
                    document.getElementById('renderedcontent').innerHTML = data.htmlContent;
                    document.getElementById('title').innerHTML  = data.title;// Assuming the returned JSON has an 'htmlContent' property
                    const date = new Date(data.publishedDate);

                    const formattedDate = new Intl.DateTimeFormat('en-GB', {
                        weekday: 'long',  // Full weekday name
                        day: 'numeric',   // Day of the month
                        month: 'long',    // Full month name
                        year: 'numeric'   // Full year
                    }).format(date);

                    document.getElementById('publishedDate').innerHTML = formattedDate;
                    populateCategories(data.categories);


                    mermaid.run();
                    hljs.highlightAll();
                })
                .catch(error => console.error('Error:', error));
        }
    });

    function populateCategories(categories) {
        var categoriesDiv = document.getElementById('categories');
        categoriesDiv.innerHTML = ''; // Clear the div

        categories.forEach(function(category) {
            // Create the span element
            let span = document.createElement('span');
            span.className = 'inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white'; // Apply the style class
            span.textContent = category;

            // Append the span to the categories div
            categoriesDiv.appendChild(span);
        });
    }
}