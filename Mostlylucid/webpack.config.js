const path = require('path');

var buildConfig = function (env) {
    const isProd = !!env.production;
    console.log("isProd: " + isProd);

    var entry = {};
    var outputPath = path.resolve(__dirname, 'wwwroot/js/dist');
    entry.main = path.resolve(__dirname, "src/js", "main.ts");

    return {
        context: __dirname,
        devtool: isProd ? false : "eval-source-map",
        entry: entry,
        module: {
            rules: [
                {test: /\.tsx?$/, loader: 'ts-loader'}
            ],
        },
        resolve: {
            extensions: ['.ts', '.js']
        },
        output: {
            filename: '[name].js',
            path: outputPath,
            library: 'Mostlylucid'
        },
        mode: isProd ? 'production' : 'development',
        watch: false,
        plugins: []
    };
};

module.exports = buildConfig;