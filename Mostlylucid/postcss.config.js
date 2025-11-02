module.exports = {
    plugins: {
        "@tailwindcss/postcss": {},  // <-- Tailwind is loaded here
        autoprefixer: {},
        cssnano: { preset: 'default' }
    }
}