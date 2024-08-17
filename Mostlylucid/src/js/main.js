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
document.addEventListener('DOMContentLoaded', function () {
    initGoogleSignIn();
    hljs.highlightAll();
    mermaid.initialize({ startOnLoad: true });
    initializeSimpleMDE();
    setLogoutLink();
});
document.body.addEventListener('htmx:afterSwap', function(evt) {
    hljs.highlightAll();
   initializeSimpleMDE();
    const url = evt.detail.pathInfo.requestPath;

    // Track the page view in Umami
    if (typeof umami !== 'undefined' && url) {
        umami.track(props => ({ ...props, url:url }));

    }
    else
    {
        console.log('umami is not defined');
    }
    initGoogleSignIn();
    mermaid.run();
    setLogoutLink();
});

function initializeSimpleMDE() {
    const element = document.getElementById("comment");
    if (!element) return;
    if (window.simplemde) {
        window.simplemde.toTextArea();
        window.simplemde = null;
    }

    // Initialize a new SimpleMDE instance
    window.simplemde = new SimpleMDE({ element: element });

}

window.simplemde = null;
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
    if (response.credential) {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/login', true);
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.onload = function () {
            if (xhr.status === 200) {
                window.location.reload();
            } else {
                console.error('Failed to log in.');
            }
        };
        xhr.send(JSON.stringify({ idToken: response.credential }));
    } else {
        console.error('No credential in response.');
    }
}

function updatePageSize(size) {
    const url = new URL(window.location.href);
    document.querySelectorAll('.pagination .page-link').forEach(link => {
        const linkUrl = new URL(link.getAttribute('href'), window.location.origin);
        linkUrl.searchParams.set('pageSize', size);
        link.setAttribute('hx-get', linkUrl.toString());
    });

}