FROM mcr.microsoft.com/dotnet/aspnet:2.1

# copy api assemblies and files
RUN mkdir /var/app
RUN mkdir /var/app/data
RUN mkdir /var/app/logs
COPY ./bin/release/netcoreapp2.1/publish /var/app
COPY appsettings.json /var/app/appsettings.json

# couple data folder data volume.
VOLUME ["/var/app/data"]
VOLUME ["/var/app/logs"]

# SET base url
ENV ASPNETCORE_URLS http://+:5000

# Expose ports
EXPOSE 5000

# Set the default command to execute when creating a new container
WORKDIR /var/app/
CMD ["dotnet", "BikeDataProject.Identity.API.dll"]