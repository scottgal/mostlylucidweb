import {typeahead} from "./typeahead";

import  {submitTranslation, viewTranslation} from   "./translations";
import {simplemde} from  "./simplemde";

import {setup, setValues} from "./comments";
import {globalSetup} from "./global";
window.globalSetup = globalSetup;
import "./mdeswitch";
window.mostlylucid = {};
window.mostlylucid.typeahead = typeahead;
window.mostlylucid.translations = {};
window.mostlylucid.translations.submitTranslation = submitTranslation;
window.mostlylucid.translations.viewTranslation = viewTranslation;
window.mostlylucid.simplemde =simplemde();

window.mostlylucid.comments={}; 
window.mostlylucid.comments.setup=setup;
window.mostlylucid.comments.setValues = setValues;


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
window.onload =function(ev) {
    // Google Sign-In Initialization
    if (!googleSignInInitialized) {
       initGoogleSignIn();
        googleSignInInitialized = true;  // Set the flag to true after initialization
    }
    hljs.highlightAll();
    try {
        window.initMermaid();
    }
    catch (e) {
        console.error('Failed to initialize Mermaid:', e);
    }
    
    // Highlight.js Initialization
    updateMetaUrls();

}
document.body.addEventListener('htmx:afterSwap', function(evt) {
    console.log('HTMX afterSwap triggered', evt);

    if (evt.detail.target.id !== 'contentcontainer') return
    hljs.highlightAll();
    try {
        window.initMermaid();
    }
    catch (e) {
        console.error('Failed to initialize Mermaid:', e);
    }
    updateMetaUrls();
    //initGoogleSignIn();
    setLogoutLink();
});


function updateMetaUrls()
{
    var currentUrl = window.location.href;

    // Set the current URL in the og:url and twitter:url meta tags
    document.getElementById('metaOgUrl').setAttribute('content', currentUrl);
    document.getElementById('metaTwitterUrl').setAttribute('content', currentUrl);
}




function renderButton(element) {
    // Check if the button has already been initialized
    if (!element.getAttribute('data-google-rendered')) {
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
        // Mark the element as initialized
        element.setAttribute('data-google-rendered', 'true');
    }
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