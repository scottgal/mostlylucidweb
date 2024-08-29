export  function codeeditor() {
    return {
        initialize: initialize,
        saveContentToDisk: saveContentToDisk,
        setupCodeEditor: setupCodeEditor,
        updateContent: updateContent,
        populateCategories: populateCategories,
        getinstance: getinstance
    }
}

  function setupCodeEditor(elementId){
    console.log('Page loaded without refresh');

    const simplemde = initialize(elementId);
    // Trigger on change event of SimpleMDE editor
    simplemde.codemirror.on("keydown", function(instance, event) {
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
            updateContent(simplemde);
        }
    });

}


function populateCategories(categories) {
    var categoriesDiv = document.getElementById('categories');
    categoriesDiv.innerHTML = ''; // Clear the div

    categories.forEach(function(category) {
        // Create the span element
        let span = document.createElement('span');
        span.className = 'inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white mr-2'; // Apply the style class
        span.textContent = category;

        // Append the span to the categories div
        categoriesDiv.appendChild(span);
    });
}


function updateContent(simplemde) {

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
            window.mostlylucid.simplemde.populateCategories(data.categories);


            mermaid.run();
            hljs.highlightAll();
        })
        .catch(error => console.error('Error:', error));
}

function initialize(elementId ,  reducedToolbar = false){ 

    // Ensure there's a global map for SimpleMDE instances
    if (!window.simplemdeInstances) {
        window.simplemdeInstances = {};
    }

    const element = document.getElementById(elementId);

    if (!element) return;

    // Destroy previous instance if it exists
    if (window.simplemdeInstances[elementId]) {
        window.simplemdeInstances[elementId].toTextArea();
        window.simplemdeInstances[elementId] = null;
    }
let simplemdeInstance = {};
    
    if(reducedToolbar)
        {
            simplemdeInstance = new SimpleMDE({
                element: element,
                toolbar: [
                    "bold", "italic", "heading", "|", "quote", "unordered-list", "ordered-list", "|"]});
        }
        else
        {


            // Initialize a new SimpleMDE instance and store it in the map
            simplemdeInstance = new SimpleMDE({
                forceSync: true,
                element: element,
                toolbar: [
                    "bold", "italic", "heading", "|", "quote", "unordered-list", "ordered-list", "|",
                    {
                        name: "save",
                        action: function (editor) {
                            var params = new URLSearchParams(window.location.search);
                            var slug = params.get("slug");
                            var language = params.get("language");

                            if (language === null) language = "en";

                            saveContentToDisk(editor.value(), slug, language);
                        },
                        className: "bx bx-save", // FontAwesome floppy disk icon
                        title: "Save"
                    },
                    "|",
                    {
                        name: "insert-category",
                        action: function (editor) {
                            var category = prompt("Enter categories separated by commas", "EasyNMT, ASP.NET, C#");

                            if (category) {
                                var currentContent = editor.value();
                                var categoryTag = `<!--category-- ${category} -->\n\n`;
                                editor.value(currentContent + categoryTag);
                            }
                        },
                        className: "bx bx-tag", // FontAwesome tag icon
                        title: "Insert Categories"
                    },
                    "|",
                    {
                        name: "update",
                        action: function () {
                            updateContent();
                        },
                        className: "bx bx-refresh",
                        title: "Update"
                    },
                    "|",
                    {
                        name: "insert-datetime",
                        action: function (editor) {
                            // Get current datetime in the format YYYY-MM-DDTHH:MM
                            var now = new Date();
                            var formattedDateTime = now.toISOString().slice(0, 16);

                            // Create the datetime tag
                            var datetimeTag = `<datetime class="hidden">${formattedDateTime}</datetime>\n\n`;

                            // Insert the datetime tag into the editor
                            var currentContent = editor.value();
                            editor.value(currentContent + datetimeTag);
                        },
                        className: "bx bx-calendar", // FontAwesome or Boxicons calendar icon
                        title: "Insert Datetime"
                    },
                    "|", "preview", "side-by-side", "fullscreen"
                ]
            });
        }

    // Store the SimpleMDE instance using the elementId as the key
    window.simplemdeInstances[elementId] = simplemdeInstance;
        return simplemdeInstance;
}

 function saveContentToDisk(content, slug, language) {
    console.log("Saving content to disk...");

    // Determine the filename based on the slug and language
    var filename;
    if(slug === undefined || slug === null || slug === "") slug = "untitled";
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


// Example of accessing the SimpleMDE instance later by elementId
function getinstance(elementId) {
    return window.simplemdeInstances ? window.simplemdeInstances[elementId] : null;
}