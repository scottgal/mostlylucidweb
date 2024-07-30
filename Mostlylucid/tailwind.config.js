// tailwind.config.js
module.exports = {
    content: [
        './Pages/**/*.{html,cshtml}',
        './Components/**/*.{html,cshtml}',
        './Views/**/*.{html,cshtml}',
    ],
    theme: {
        extend: {},
    },
    plugins: [
        require('daisyui'),
    require("@tailwindcss/forms"),
    require("@tailwindcss/typography"),
    require('@tailwindcss/aspect-ratio'),
    ],
};