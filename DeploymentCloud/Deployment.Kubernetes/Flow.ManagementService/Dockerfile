FROM microsoft/dotnet:2.2-aspnetcore-runtime


WORKDIR /app/aspnetcore
EXPOSE 5000
ADD "FlowManagement.tar" .
ENV ASPNETCORE_URLS="http://*:5000" PATH="$PATH:/app/aspnetcore:/app/aspnetcore/" AppId="" \
	TenantId="" ClientSecret=""
#ENV AzureServicesAuthConnectionString="RunAs=App;AppId=${AppId};TenantId=${TenantId};AppKey=${ClientSecret}"
COPY "finalrun.sh" "./" 
# Adding the execute permission. 
RUN chmod +x ./finalrun.sh
ENTRYPOINT ["finalrun.sh"]