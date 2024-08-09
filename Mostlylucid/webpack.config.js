﻿const TerserPlugin = require('terser-webpack-plugin');
const path = require('path');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        mode: isProduction ? 'production' : 'development', // Set mode based on environment
        entry: './src/js/main.js', // Your entry file
        output: {
            filename: 'main.js',
            path: path.resolve(__dirname, 'wwwroot/js/dist'), // Corrected output directory path
        },
        resolve: {
            extensions: ['.js'], // Optional but fine to include
        },
        optimization: {
            minimize: isProduction, // Only minimize in production mode
            minimizer: isProduction ? [
                new TerserPlugin({
                    terserOptions: {
                        mangle: {
                            // You can set `true` to enable variable and function name mangling
                            properties: false, // Do not mangle properties (you might not want this if you rely on specific property names)
                        },
                        format: {
                            comments: false, // Remove comments
                            beautify: false, // Disable beautification (set to true if you want readable output)
                        },
                        compress: {
                            drop_console: true, // Drop console statements
                            keep_fnames: true, // Keep function names
                            keep_classnames: true, // Keep class names
                        },
                    },
                    extractComments: false,
                }),
            ] : [],
        },
    };
};