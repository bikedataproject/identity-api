FROM mcr.microsoft.com/dotnet/sdk:5.0

# copy api assemblies and files
RUN mkdir /var/app
RUN mkdir /var/app/data
RUN mkdir /var/app/logs
COPY ./bin/release/net5.0/publish /var/app

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