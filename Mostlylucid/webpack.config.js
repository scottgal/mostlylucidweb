const TerserPlugin = require('terser-webpack-plugin');
const path = require('path');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        mode: isProduction ? 'production' : 'development',
        entry: {
            main: './src/js/main.js',
        },
        output: {
            filename: '[name].js',
            chunkFilename: '[name].[contenthash].js',
            path: path.resolve(__dirname, 'wwwroot/js/dist'),
            publicPath: '/js/dist/',
            clean: true,
        },
        module: {
            rules: [
                {
                    test: /\.css$/i,
                    use: ['style-loader', 'css-loader'],
                },
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader',
                        options: {
                            presets: [
                                ['@babel/preset-env', {
                                    targets: '> 0.25%, not dead',
                                    modules: false, // ✅ for tree shaking
                                }],
                            ],
                            // Babel 8 moved polyfill injection out of preset-env; this is the
                            // equivalent of the old useBuiltIns: 'usage', corejs: 3
                            plugins: [
                                ['polyfill-corejs3', {
                                    method: 'usage-global',
                                    version: require('core-js/package.json').version,
                                }],
                            ],
                        },
                    },
                },
            ],
        },
        resolve: {
            extensions: ['.js', '.mjs'],
            alias: {
                '@mostlylucid/mermaid-enhancements$': path.resolve(__dirname, 'node_modules/@mostlylucid/mermaid-enhancements/dist/index.min.js')
            }
        },
        optimization: {
            splitChunks: false,  // Disable code splitting to ensure synchronous loading
            runtimeChunk: false,  // Disable runtime chunk to ensure synchronous loading
            minimize: isProduction,
            minimizer: isProduction ? [
                new TerserPlugin({
                    terserOptions: {
                        ecma: 2020,
                        compress: {
                            drop_console: true,
                            passes: 3,
                            toplevel: true,
                            pure_funcs: ['console.info', 'console.debug'],
                        },
                        mangle: {
                            toplevel: true,
                        },
                        format: {
                            comments: false,
                        },
                    },
                    extractComments: true,
                }),
            ] : [],
        },
        devtool: isProduction ? false : 'eval-source-map',
        performance: {
            hints: isProduction ? 'warning' : false,
        }
    };
};
