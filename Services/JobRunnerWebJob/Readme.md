How to setup your own Job: 

1. Create a Job class inheriting from `IJob` under the JobRunner project.
2. You need to schedule the job under `RegisterScheduledJobs` in the JobRunner project. 

Debug locally
1. Update the ActiveQueue in the appsettings to ensure that the correct queue is accessed in the debugging. 

