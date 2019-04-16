#Building Spark engine
In order to build the Data Accelerator Spark engine, you will need the following

#Requirements
 - Maven
 - Java SDK
 - Ensure both JAVA_HOME, M2_HOME and MAVEN_HOME are properly defined in your environment

#How to build
From a prompt:

> mvn package -f project-name

examples:
```
> mvn package -f datax-core
> mvn package -f datax-keyvault
> mvn package -f datax-utility
> mvn package -f datax-host
> mvn package -f datax-udf-samples
```


## Publish to Maven Repo
<TODO replace with external repo>

> mvn deploy -f project-name

examples:
```
> mvn deploy -f datax-core
> mvn deploy -f datax-utility
> mvn deploy -f datax-host
> mvn deploy -f datax-udf-samples
```

## Publish to Storage Account to cluster
Note: you will have to do `az login` first in order to use the login mode when uploading blob to remote storage account, also your account should have permission to the storage account associated with the cluster.

> deploy module-name staging

examples:
```
> deploy core staging
> deploy utility staging
> deploy host staging
> deploy udf-samples staging
```


#How to create a PR
 - Ensure all tests are passing by doing the following
 - Create a pull request aginst the master branch

