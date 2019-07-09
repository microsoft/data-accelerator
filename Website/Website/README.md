# Data Accelerator Website

### Environment Setup
1. Install NodeJs from http://nodejs.org, version 10.6.0 or later

2. Install VS Code extension to get consistent code formatting on Save from https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode

3. Set the following environment variable. The ```key vault name``` should point to the Azure key vault service which back your DataX instance.
```
set DATAX_KEYVAULT_NAME=<key vault name>
```

4. Set the following environment variable. The ```secret name prefix``` should point to the secret name prefix in the Azure key vault service which back your DataX instance.
```
set DATAX_KEYVAULT_SECRET_PREFIX=<secret name prefix>
```

### Build and run a local Website instance

1. Install all dependency packages of this website.
```
npm install
```

2. Build non-optimized version website using ```npm run dev```. While the website tend to be larger in size and slow down your web experience, you benefit
this by getting a better development experience when debugging the sources on the browser of your choice. 
```
npm run dev
```

3. Start website
```
npm start
```

4. You should see the following message. Follow what it said and login to allow the local web app use your credential to access key vault. NOTE: the AAD account you login should have access to the azure keyvault service.
```
To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code <a temporary code> to authenticate.
```

5. View website on http://localhost:2020

### Building website
When you have changes to the website or one of its dependency packages changes. You will have to rebuild the website. You have 2 options

#### Manually rebuilding 
Run this command everytime you want to rebuild website.
```
npm run dev
```

#### Automatically rebuilding
Run this command which will automatically rebuild website if it detects that any of its files changes or files under node_modules (dependency packages) changes
```
npm run devwatch
```

### Building production version of website
When you are done developing the website, you will want to build the optimized version of the website for production (obfuscated, minified and other compiler optimizations) leading to smaller output sizes and 
faster performance for production consumption.
```
npm run build
```

### Deploy production version of website with zip-push option on Azure Web App
* Before deploying
download and install AzureCLI, login first to get access for deployment:
```
az login
```

* Deploy
```
node .\deploy\zipsite.js
.\deploy\deploy.cmd <webapp name> <subscription name>
```

### (Optional) How to target a locally hosted service
If you are locally hosting a datax service on your machine, you can configure the website to target your locally hosted
service by defining the following enivornment variables. You only need to define the ones you are locally hosting.
The local URL should be in the format 'http://localhost:port#'.
```
set DATAXDEV_GENERATOR_LOCAL_SERVICE=<local URL of Generator service>
set DATAXDEV_INTERACTIVE_QUERY_LOCAL_SERVICE=<local URL of Interactive Query service>
set DATAXDEV_SCHEMA_INFERENCE_LOCAL_SERVICE=<local URL of Schema Inference service>
set DATAXDEV_LIVE_DATA_LOCAL_SERVICE=<local URL of Live Data service>
```

### (Optional) How to target services hosted on AKS cluster
If you are hosting DataX services on an AKS cluster, you can configure the website to target the services hosted on an AKS cluster by defining the following secret in the kvServices keyvault. 
Secret Name: <DATAX_KEYVAULT_SECRET_PREFIX>+"kubernetesServices" where you need to use the prefix: <DATAX_KEYVAULT_SECRET_PREFIX>) from the step 4. 
Secret Value: In the secret value: You only need to define the ones you are hosting on AKS cluster.
The <External IP> should be added for each of the services where they can be listened to on the AKS cluster as specified in the example below:
```
{"Flow.InteractiveQueryService":"http://<External IP for Interactive Query Service>:5000","Flow.SchemaInferenceService":"http://<External IP for Schema Inference Service>:5000","Flow.ManagementService":"http://<External IP for Flow Management Service>:5000","Flow.LiveDataService":"http://<External IP for live Data Service>:5000"}
```
