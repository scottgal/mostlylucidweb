import {build} from 'esbuild';
// These are development-only dependencies. The published enhancement bundle stays renderer-neutral.
await build({entryPoints:['examples/demo.js'],outdir:'dist/demo',bundle:true,splitting:true,format:'esm',platform:'browser',target:'es2022',minify:true,plugins:[{name:'public-enhancements',setup(builder){builder.onResolve({filter:/index\.bundle\.js$/},()=>({path:'../index.bundle.js',external:true}));}}],loader:{'.woff2':'dataurl'}});
