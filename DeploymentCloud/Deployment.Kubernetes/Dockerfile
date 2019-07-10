FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app/aspnetcore
EXPOSE 5000
ARG servicetarname
ARG servicedllname
ENV servicedllname=$servicedllname
ADD $servicetarname .
ENV ASPNETCORE_URLS="http://*:5000" PATH="$PATH:/app/aspnetcore:/app/aspnetcore/"
COPY "finalrun.sh" "./" 
# Adding the execute permission. 
RUN chmod +x ./finalrun.sh
ENTRYPOINT ["finalrun.sh"]
