const autoprefixer = require('autoprefixer');
module.exports = [{
    entry: ['./src/app.scss', './src/app.js'],
    output: {
      // This is necessary for webpack to compile
      // But we never use style-bundle.js
      filename: './static/app.js',
    },
    module: {
      rules: [
        {
          test: /\.scss$/,
          use: [
            {
              loader: 'file-loader',
              options: {
                name: 'static/app.css',
              },
            },
            {loader: 'extract-loader'},
            {loader: 'css-loader'},
            {
              loader: 'postcss-loader',
              options: {
                plugins: () => [autoprefixer()]
              }
            },
            {
              loader: 'sass-loader',
              options: {
                // Prefer Dart Sass
                implementation: require('sass'),

                // See https://github.com/webpack-contrib/sass-loader/issues/804
                webpackImporter: false,
                sassOptions: {
                  includePaths: ['./node_modules'],
                },
              },
            }
          ],
        },
        {
          test: /\.js$/,
          loader: 'babel-loader',
          //query: {
           // presets: ['@babel/preset-env'],
          //},
        }
      ],
    },
  }];
