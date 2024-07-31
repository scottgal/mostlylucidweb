// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
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
                this.isDarkMode = true;
            } else {
                localStorage.theme = "light";
                document.documentElement.classList.remove("dark");
                this.isDarkMode = false;
            }
        },
        themeSwitch() {
            if (localStorage.theme === "dark") {
                localStorage.theme = "light";
                document.documentElement.classList.remove("dark");
                this.isDarkMode = false;
            } else {
                localStorage.theme = "dark";
                document.documentElement.classList.add("dark");
                this.isDarkMode = true;
            }
        },
    };
}