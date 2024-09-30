// Initialize the mostlylucid namespace if not already defined
import hljsRazor from "highlightjs-cshtml-razor";
window.mostlylucid = window.mostlylucid || {};
import mermaid from "mermaid";
import htmx from "htmx.org";
import hljs from "highlight.js";

window.hljs=hljs;
window.htmx = htmx;
window.mermaid=mermaid;
mermaid.initialize({startOnLoad:false});
// Importing modules
import { typeahead } from "./typeahead";
import { submitTranslation, viewTranslation } from "./translations";
import { codeeditor } from "./simplemde";
import { globalSetup } from "./global";
import  {comments} from  "./comments"; 
import "./mdeswitch";
window.mostlylucid.comments = comments();

// Attach imported modules to the mostlylucid namespace
window.mostlylucid.typeahead = typeahead;
window.mostlylucid.translations = {
    submitTranslation: submitTranslation,
    viewTranslation: viewTranslation
};
window.mostlylucid.simplemde = codeeditor(); // Assuming simplemde() returns the instance
window.globalSetup = globalSetup;

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

window.mermaidinit = function() {
    mermaid.initialize({ startOnLoad: false });
        try {
            window.initMermaid().then(r => console.log('Mermaid initialized'));
        } catch (e) {
            console.error('Failed to initialize Mermaid:', e);
        }
    
}

window.onload = function(ev) {
    if(document.readyState === 'complete') {
        initGoogleSignIn();
        mermaidinit();
        const hljsRazor = require('highlightjs-cshtml-razor');
        hljs.registerLanguage("cshtml-razor", hljsRazor);
        hljs.highlightAll();
        setLogoutLink();
        updateMetaUrls();
        console.log('Document is ready');

        // Only trigger updates after HTMX swaps content in #contentcontainer or #commentlist
        document.body.addEventListener('htmx:afterSettle', function(evt) {
            const targetId = evt.detail.target.id;
            if (targetId !== 'contentcontainer' && targetId !== 'commentlist' && targetId!=="blogpost") {
             console.log("Ignoring swap event for target:", targetId);
                return;
            }
            initGoogleSignIn();
            console.log('HTMX afterSettle triggered', evt);
            mermaidinit();
            hljs.highlightAll();
             setLogoutLink();
        });
    }
};






function updateMetaUrls() {
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

window.changePageSize = function changePageSize(linkUrl = null) {
    const pageSize = document.getElementById('pageSize').value;
    let url = new URL(linkUrl || window.location.href);
    if (url.pathname.endsWith('/')) {
        url.pathname = url.pathname.slice(0, -1);
    }
    url.searchParams.delete('page');
    url.searchParams.delete('pageSize');
    url.searchParams.set('page', '1');
    url.searchParams.set('pageSize', pageSize);
    
    const newUrl = url.toString();
    htmx.ajax('get', newUrl, {
        target: '#content',
        swap: 'innerHTML',
        headers: { "pagerequest": "true" }
    }).then(() => {
        history.pushState({}, null, newUrl);
    });
}

hljs.addPlugin({
    "after:highlightElement": ({ el, text }) => {
        const wrapper = el.parentElement;
        if (wrapper == null) {
            return;
        }

        /**
         * Make the parent relative so we can absolutely
         * position the copy button
         */
        wrapper.classList.add("relative");
        const copyButton = document.createElement("button");
        copyButton.classList.add(
            "absolute",
            "top-2",
            "right-1",
            "p-2",
            "text-gray-500",
            "hover:text-gray-700",
            "bx",
            "bx-copy",
            "text-xl",
            "cursor-pointer"
        );
        copyButton.setAttribute("aria-label", "Copy code to clipboard");
        copyButton.setAttribute("title", "Copy code to clipboard");

        copyButton.onclick = () => {
            navigator.clipboard.writeText(text);

            // Notify user that the content has been copied
            showToast("The code block content has been copied to the clipboard.", 3000, "success");
        
        };
        // Append the copy button to the wrapper
        wrapper.prepend(copyButton);
    },
});

window.showToast = function(message, duration = 3000, type = 'success') {
    const toast = document.getElementById('toast');
    const toastText = document.getElementById('toast-text');
    const toastMessage = document.getElementById('toast-message');

    // Set message and type
    toastText.innerText = message;
    toastMessage.className = `alert alert-${type}`; // Change alert type (success, warning, error)

    // Show the toast
    toast.classList.remove('hidden');

    // Hide the toast after specified duration
    setTimeout(() => {
        toast.classList.add('hidden');
    }, duration);
}