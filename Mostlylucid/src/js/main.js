// Initialize the mostlylucid namespace if not already defined
import hljsRazor from "highlightjs-cshtml-razor";
window.mostlylucid = window.mostlylucid || {};
import mermaid from "mermaid";
import Alpine from 'alpinejs';
import htmx from "htmx.org";
import hljs from "highlight.js";
import EasyMDE from "easymde";
import 'easymde/dist/easymde.min.css';
import '../css/easymde-overrides.css';
import flatpickr from "flatpickr";
import 'flatpickr/dist/flatpickr.min.css';
import '../css/flatpickr-overrides.css';
import "./blog-index";
import "./explainer-embeds";
import { renderMath } from './katex-render';


window.EasyMDE = EasyMDE;
window.flatpickr = flatpickr;

window.Alpine = Alpine;
window.hljs=hljs;
window.htmx = htmx;

// Configure HTMX history behavior
htmx.config.refreshOnHistoryMiss = true;  // Full page reload instead of restoring stale partial

window.mermaid=mermaid;
window.mermaid.initialize({startOnLoad : false});
import { init } from '@mostlylucid/mermaid-enhancements/min';
import '@mostlylucid/mermaid-enhancements/styles.css';


// Importing modules
import { typeahead } from "./typeahead";
import { submitTranslation, viewTranslation } from "./translations";
import { codeeditor } from "./simplemde_editor";
import { globalSetup } from "./global";
import  {comments} from  "./comments";
import { queryParamClearer, queryParamToggler } from "./query-params";
import { initBrokenLinks } from "./broken-links";
import { showToast, showHTMXToast } from "./toast";
import "./highlight-copy";
import "./htmx-events";
// removed bare import of package to avoid TS source resolution

window.mostlylucid.comments = comments();

// Attach imported modules to the mostlylucid namespace
window.mostlylucid.typeahead = typeahead;

// Also register with Alpine.data() for better compatibility
if (typeof Alpine !== 'undefined' && Alpine.data) {
    Alpine.data('typeahead', typeahead);
}
window.mostlylucid.translations = {
    submitTranslation: submitTranslation,
    viewTranslation: viewTranslation
};
window.mostlylucid.simplemde = codeeditor(); // Assuming simplemde() returns the instance
window.globalSetup = globalSetup;

// Expose query param utilities on window for Alpine.js
window.queryParamClearer = queryParamClearer;
window.queryParamToggler = queryParamToggler;

// Also expose on mostlylucid namespace for backwards compatibility
window.mostlylucid.queryParamClearer = queryParamClearer;
window.mostlylucid.queryParamToggler = queryParamToggler;
window.mostlylucid.initBrokenLinks = initBrokenLinks;

// Track Alpine initialization to prevent duplicate starts
let alpineStarted = false;

function startAlpine() {
    if (alpineStarted) {
        return false;
    }

    if (window.Alpine && typeof window.Alpine.start === 'function') {
        try {
            window.Alpine.start();
            alpineStarted = true;
            console.log('Alpine.js started');
            return true;
        } catch (err) {
            console.log('Alpine start failed:', err.message);
            return false;
        }
    }
    return false;
}

// Start Alpine as soon as it's available (theme switcher needs it early)
if (!startAlpine()) {
    // If not ready yet, wait a bit and try again
    setTimeout(() => {
        startAlpine();
    }, 100);
}

function setLogoutLink(container = document) {
    // Get the logout link - search within container for HTMX swaps
    var logoutLink = container.querySelector('a[data-logout-link]');

    if (logoutLink) {
        try {
            // Get the current URL
            var currentUrl = window.location.href;

            // Check if link already has the return URL set to avoid duplicate updates
            if (logoutLink.href.includes('returnUrl=' + encodeURIComponent(currentUrl))) {
                return;
            }

            // Update the href attribute to include the return URL
            var baseUrl = logoutLink.href.split('?')[0]; // Get the base URL without query parameters
            logoutLink.href = baseUrl + '?returnUrl=' + encodeURIComponent(currentUrl);

            console.log('Logout link updated:', logoutLink.href);
        } catch (error) {
            console.error('Error setting logout link:', error);
        }
    }
}

// Make setLogoutLink available globally for debugging and manual calls if needed
window.setLogoutLink = setLogoutLink;

window.mermaidinit =async  function() {

    try {
      await  init().then(r => console.log('Mermaid initialized'));
    } catch (e) {
        console.error('Failed to initialize Mermaid:', e);
    }

}


function highlightCodeBlocks(container = document) {
    // Only highlight code blocks that haven't been highlighted yet
    const codeBlocks = container.querySelectorAll('pre code:not(.hljs)');
    codeBlocks.forEach((block) => {
        try {
            hljs.highlightElement(block);
        } catch (err) {
            console.error('Failed to highlight code block:', err);
            // Ensure block has at least basic styling even if highlighting fails
            block.classList.add('hljs');
        }
    });

    // Also check for any code blocks that might have lost their highlighting
    // (e.g., after DOM manipulation) and re-highlight them
    const unhighlightedBlocks = container.querySelectorAll('pre code.hljs:not([data-highlighted])');
    unhighlightedBlocks.forEach((block) => {
        try {
            // Remove hljs class first to force re-highlight
            block.classList.remove('hljs');
            hljs.highlightElement(block);
        } catch (err) {
            console.error('Failed to re-highlight code block:', err);
            // Re-add hljs class for styling
            block.classList.add('hljs');
        }
    });
}

// Cloudflare-safe HTMX event listener registration
function registerHTMXListener() {
    // Defensive check: Only register if htmx is available
    if (typeof window.htmx === 'undefined') {
        console.warn('HTMX not loaded yet, retrying in 100ms...');
        setTimeout(registerHTMXListener, 100);
        return;
    }

    // Only trigger updates after HTMX swaps content in #contentcontainer or #commentlist
    document.body.addEventListener('htmx:afterSettle', async function(evt) {
        // History restores can omit detail.target; use the restored content container.
        const target = evt.detail?.target ?? document.getElementById('contentcontainer');
        const targetId = target?.id;
        if (targetId !== 'contentcontainer' && targetId !== 'commentlist' && targetId!=="blogpost") {
            console.log("Ignoring swap event for target:", targetId);
            return;
        }

        console.log('HTMX afterSettle triggered for:', targetId);

        try {
            // Re-initialize Google Sign-In button if present in swapped content
            initGoogleSignIn();
        } catch (err) {
            console.error('Failed to re-initialize Google Sign-In:', err);
        }

        try {
            // Highlight code blocks in swapped content BEFORE Mermaid (important!)
            highlightCodeBlocks(target);
            console.log('Highlight.js applied after HTMX swap');
        } catch (err) {
            console.error('Failed to highlight after HTMX swap:', err);
        }

        try {
            // Re-initialize Mermaid diagrams in the new content
            await mermaidinit();
            console.log('Mermaid applied after HTMX swap');
        } catch (err) {
            console.error('Failed to initialize Mermaid after HTMX swap:', err);
        }

        try {
            // Double-check highlighting after Mermaid renders
            await new Promise(resolve => requestAnimationFrame(resolve));
            highlightCodeBlocks(target);
        } catch (err) {
            console.error('Failed to re-highlight after Mermaid:', err);
        }

        try {
            // Update logout link in the swapped content (search entire document in case it's in nav)
            setLogoutLink(document);
        } catch (err) {
            console.error('Failed to set logout link:', err);
        }

        // Rewrite any broken links in the swapped content
        try {
            initBrokenLinks(target);
        } catch (err) {
            console.error('Failed to rewrite broken links:', err);
        }

        // Render math expressions in swapped content
        try {
            renderMath(target);
            console.log('KaTeX applied after HTMX swap');
        } catch (err) {
            console.error('Failed to render math after HTMX swap:', err);
        }

        console.log('HTMX afterSettle complete for:', targetId);
    }, { once: false }); // Don't use once - we need this for all HTMX swaps

    console.log('HTMX event listener registered successfully');
}

async function initializePage() {
    try {
        initGoogleSignIn();
    } catch (err) {
        console.error('Failed to initialize Google Sign-In:', err);
    }

    // Wait for Alpine to be ready before initializing Mermaid
    // This ensures themeInit() has run and the theme event has been fired
    await new Promise(resolve => {
        if (window.Alpine && window.Alpine.version) {
            resolve();
        } else {
            document.addEventListener('alpine:init', resolve, { once: true });
        }
    });

    try {
        // Register highlight.js copy button plugin
        if (typeof window.addCopyPlugin === 'function') {
            window.addCopyPlugin();
        }
    } catch (err) {
        console.error('Failed to register hljs copy plugin:', err);
    }

    try {
        // Register Razor syntax highlighting
        const hljsRazor = require('highlightjs-cshtml-razor');
        hljs.registerLanguage("cshtml-razor", hljsRazor);
    } catch (err) {
        console.error('Failed to register Razor language:', err);
    }

    // Wait for a short delay to ensure all DOM elements are fully rendered
    // This is especially important for server-side rendered content
    await new Promise(resolve => requestAnimationFrame(resolve));

    // Initialize highlight.js first so code is styled correctly
    try {
        highlightCodeBlocks();
        console.log('Highlight.js initialized on page load');
    } catch (err) {
        console.error('Failed to highlight code blocks:', err);
    }

    // Initialize Mermaid after theme is set and code is highlighted
    try {
        await mermaidinit();
        console.log('Mermaid initialized on page load');
    } catch (err) {
        console.error('Failed to initialize Mermaid:', err);
    }

    // Double-check: retry highlighting after Mermaid initializes
    // Sometimes code blocks in dynamic content need a second pass
    try {
        await new Promise(resolve => requestAnimationFrame(resolve));
        highlightCodeBlocks();
    } catch (err) {
        console.error('Failed to re-highlight code blocks:', err);
    }

    try {
        setLogoutLink();
        updateMetaUrls();
    } catch (err) {
        console.error('Failed to set logout link or update meta URLs:', err);
    }

    // Initialize broken link rewriter (runs in background, non-blocking)
    try {
        initBrokenLinks();
    } catch (err) {
        console.error('Failed to initialize broken links:', err);
    }

    // Render math expressions on page load
    try {
        renderMath();
        console.log('KaTeX initialized on page load');
    } catch (err) {
        console.error('Failed to render math:', err);
    }

    console.log('Document is ready - all initializations complete');

    // Register HTMX listener with Cloudflare-safe retry mechanism
    registerHTMXListener();
}

// Cloudflare Rocket Loader compatibility: ensure all dependencies are loaded
function waitForDependencies(maxAttempts = 50) {
    return new Promise((resolve, reject) => {
        let attempts = 0;

        const checkDependencies = () => {
            attempts++;

            // Check if all critical dependencies are loaded
            const depsReady =
                typeof window.hljs !== 'undefined' &&
                typeof window.mermaid !== 'undefined' &&
                typeof window.Alpine !== 'undefined' &&
                typeof window.htmx !== 'undefined';

            if (depsReady) {
                console.log('All dependencies loaded after', attempts, 'attempts');

                // Try to start Alpine if not already started
                startAlpine();

                resolve();
            } else if (attempts >= maxAttempts) {
                console.warn('Timeout waiting for dependencies. Missing:', {
                    hljs: typeof window.hljs === 'undefined',
                    mermaid: typeof window.mermaid === 'undefined',
                    alpine: typeof window.Alpine === 'undefined',
                    htmx: typeof window.htmx === 'undefined'
                });
                // Continue anyway - individual initializations have their own error handling
                resolve();
            } else {
                // Retry with exponential backoff
                const delay = Math.min(50 * Math.pow(1.2, attempts), 500);
                setTimeout(checkDependencies, delay);
            }
        };

        checkDependencies();
    });
}

// Robust initialization that handles Cloudflare Rocket Loader interference
async function safeInitialize() {
    try {
        // Wait for dependencies to load (handles Rocket Loader delays)
        await waitForDependencies();

        // Wait for DOM to be fully ready
        if (document.readyState === 'loading') {
            await new Promise(resolve => {
                document.addEventListener('DOMContentLoaded', resolve, { once: true });
            });
        }

        // Now run initialization
        await initializePage();
    } catch (err) {
        console.error('Failed to initialize page:', err);
        // Retry once after a delay
        setTimeout(() => {
            initializePage().catch(e => console.error('Retry failed:', e));
        }, 1000);
    }
}

// Start initialization
safeInitialize();






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
                type: "icon",
                size: "medium",
                theme: "filled_black",
                shape: "circle"
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



// Expose toast functions globally for use in templates and other scripts
window.showToast = showToast;
window.showHTMXToast = showHTMXToast;

// Also expose on mostlylucid namespace
window.mostlylucid.showToast = showToast;
window.mostlylucid.showHTMXToast = showHTMXToast;
