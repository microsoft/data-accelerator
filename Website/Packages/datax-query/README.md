# datax-query
Contains query features representing a streaming/batching data pipeline configuration management experience. 
This package is used to add additional features to the Data Accelerator website.

### Install Package to Accelerator Website or another package
1. At root folder of your consumer project, run ```npm install datax-query@1.1.1``` (or whatever version you want). 
This will install the package under the ```node_modules``` folder and automatically update the ```package.json``` file of your project. 
You can also manually input it into the ```package.json``` yourself and then run ```npm install```.

### Quick start to developing this package
1. Run ```npm install``` to install all dependency package of this NPM package.

2. Run ```npm run dev``` to build non-optimized bundles. While the packages tend to be larger in size and slow down your web experience, you benefit
this by getting a better development experience when debugging the sources on the browser of your choice.

3. When you are done developing your package, increment the version number of your NPM package in the ```package.json``` file.

4. Run ```npm run build``` to build optimized bundles (obfuscated, minified and other compiler optimizations) leading to smaller output sizes and 
faster performance for production consumption.

5. Run ```npm publish```

### Tips and Tricks
1. For your website, run ```npm run devwatch``` which will put your website into listening mode for file changes. Every time a file that the website
depends on under its folder and ```node_modules``` dependency folder changes, it will automatically re-compile.

2. Run ```npm run devpatch``` to build development bundles and this command will automatically execute a batch script to xcopy the built bundles
to our website's ```node_modules``` folder. This will cause your website to recompile itself to pick up the changes. 
This ```localdevpatch.bat``` script assumes that the website GIT repo and this packages GIT repo share the same parent folder.
If this is not the case, please change the paths of the script locally on your computer.
