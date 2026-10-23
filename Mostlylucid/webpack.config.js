const TerserPlugin = require('terser-webpack-plugin');
const path = require('path');
const fs = require('fs');
const {createHash} = require('crypto');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        mode: isProduction ? 'production' : 'development',
        entry: {
            main: './src/js/main.js',
            explainers: './src/js/explainers/index.js',
        },
        output: {
            filename: '[name].js',
            chunkFilename: '[name].[contenthash].js',
            path: path.resolve(__dirname, 'wwwroot/js/dist'),
            publicPath: '/js/dist/',
            clean: true,
        },
        plugins: [{
            apply(compiler) {
                // The standalone HTML has no Razor tag helper to append asset versions.
                compiler.hooks.afterEmit.tap('VersionExplainerAssets', () => {
                    const htmlPath = path.resolve(__dirname, 'wwwroot/interactive/explainers/index.html');
                    const version = createHash('sha256')
                        .update(fs.readFileSync(path.resolve(__dirname, 'wwwroot/js/dist/explainers.js')))
                        .update(fs.readFileSync(path.resolve(__dirname, 'wwwroot/interactive/explainers/style.css')))
                        .digest('hex').slice(0, 12);
                    const html = fs.readFileSync(htmlPath, 'utf8');
                    const updated = html
                        .replace(/(href="style\.css)(?:\?v=[^"]*)?"/, `$1?v=${version}"`)
                        .replace(/(src="\/js\/dist\/explainers\.js)(?:\?v=[^"]*)?"/, `$1?v=${version}"`);
                    if (updated !== html) fs.writeFileSync(htmlPath, updated);
                });
            }
        }],
        module: {
            rules: [
                {
                    test: /\.css$/i,
                    use: ['style-loader', 'css-loader'],
                },
                {
                    test: /\.[jt]s$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader',
                        options: {
                            presets: [
                                '@babel/preset-typescript',
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
            extensions: ['.js', '.mjs', '.ts'],
            extensionAlias: { '.js': ['.js', '.ts'] },
            modules: [path.resolve(__dirname, 'node_modules'), 'node_modules'],
            alias: {
                '@mostlylucid/mermaid-enhancements$': path.resolve(__dirname, '../mostlylucid-mermaid/src/index.ts'),
                '@mostlylucid/mermaid-enhancements/min$': path.resolve(__dirname, '../mostlylucid-mermaid/src/index.ts'),
                '@mostlylucid/mermaid-enhancements/styles.css$': path.resolve(__dirname, '../mostlylucid-mermaid/src/styles.css')
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
