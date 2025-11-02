// Tailwind CSS v4 - Minimal config for content paths only
// All theme configuration has been moved to src/css/main.css using @theme
module.exports = {
    content: {
        files: ["./Views/**/*.cshtml", "./EmailSubscription/**/*.cshtml"],
        extract: {
            cshtml: (content) => content.match(/[^<>"'`\s]*/g) || [],
        },
    },
};