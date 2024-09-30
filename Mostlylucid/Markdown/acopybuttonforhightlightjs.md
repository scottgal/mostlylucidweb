# A Copy Button For Highlight.js
<!--category-- DaisyUI, Tailwind, Highlight.js, Javascript -->
<datetime class="hidden">2024-09-28T14:15</datetime>

# Introduction
In this site I use Hightlight.js to render code snippets client side. I like this as it keeps my server side code clean and simple. However, I wanted to add a copy button to each code snippet so that users could easily copy the code to their clipboard. This is a simple task but I thought I would document it here for anyone else who might want to do the same.

Oh and this is all waiting to me adding the newsletter functionality to actually show up on the site. As soon as I get the energy to do that I'll add this in.

The endpoint is that we have a copy button like this on the site:
![Copy Button](copybutton.png)

**NOTE: All credit for this article goes to [Faraz Patankar](https://dev.to/farazpatankar/highlightjs-copy-button-plugin-3dld) who's article I used to create this one. I just wanted to document the changes I made to it here for my own reference and to share with others.**

[TOC]

# The Options
There's a couple of ways to do this; for example there's a [copy button plugin](https://github.com/arronhunt/highlightjs-copy) for Higlight.js but I decided I wanted more control over the button and the styling. So I came across [this article](https://dev.to/farazpatankar/highlightjs-copy-button-plugin-3dld) for adding a copy button. 

## The Problems
While that article is a great approach it had a couple of issues that stopped it being perfect for me:

1. It uses a font that I don't use on my site (but then manually has the SVG too, not sure why).
```javascript
  // Lucide copy icon
    copyButton.innerHTML = `<svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-copy"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`;
```
While this works, I already use BoxIcons on this site which has a [copy icon in there already](https://icon-sets.iconify.design/bx/copy/). 

2. It uses a toast library which I don't have on this site.
```javascript
  // Notify user that the content has been copied
      toast.success("Copied to clipboard", {
        description: "The code block content has been copied to the clipboard.",
      });
```
3. It knocks out the y-overflow on the code block and puts the icon at the bottom which I didn't want. So mine is top right.

# My Adaptations

## The Main Function
This plugin hooks into the `after:highlightElement` event and adds a button to the code block.

So first I copied Faraz's code and then made the following changes:

1. Instead of appending it to the end of the code block I prepend it to the start.
2. Instead of the SVG I used the BoxIcons version by just adding those classes to the inserted button & setting the textsize to `text-xl`.
3. I removed the toast notification and replaced it with a simple `showToast` function that I have in my site (see later)
4. I added an `aria-label` and `title` to the button for accessibility (and to give a nice hover effect).

```javascript
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
```

## `showToast` Function
This relies on a razor partial I added to my project. 
This partial uses the [DaisyUI Toast component](https://daisyui.com/components/toast/) to show a message to the user. 
I like this approach as it keeps the Javascript clean and simple and allows me to style the toast message in the same way as the rest of the site.


```html
<div id="toast" class="toast toast-bottom fixed z-50 hidden overflow-y-hidden">
    <div id="toast-message" class="alert">
        <div>
            <span id="toast-text">Notification message</span>
        </div>
    </div>
    <p class="hidden right-1 bx-copy  cursor-pointer alert-success alert-warning alert-error alert-info"></p>
</div>
```
You'll note this also has an odd hidden `p` tag at the bottom, this is just for Tailwind to parse these classes when it builds the site's CSS.

The Javascript function is simple, it just shows the toast message for a set time and then hides it again. 

```javascript
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
```

We can then call this using the `showToast` function in the `copyButton.onclick` event.

```javascript
showToast("The code block content has been copied to the clipboard.", 3000, "success");
```
I added this partial right at the top of my `_Layout.cshtml` file so it's available on every page.

```html
<partial name="_Toast"  />``
```

Now when we show blog posts the code blocks have:
![Copy Button](copybutton.png)

# In Conclusion
So that's it, a simple change to Faraz's code to make it work for me. I hope this helps someone else out there.