/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './**/*.{razor,html,cshtml,cs}'
    ],
    // Adiciona uma 'safelist' para garantir que estas classes sejam sempre incluídas
    safelist: [
        'bg-red-500',
        'hover:bg-red-600',
        'text-red-600',
        'disabled:bg-red-300'
    ],
    theme: {
        extend: {},
    },
    plugins: [],
}

