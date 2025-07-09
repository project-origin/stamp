ARG PROJECT=ProjectOrigin.Stamp.Server

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0.301 AS build
ARG PROJECT

WORKDIR /src

COPY ./Protos ./Protos
COPY ./${PROJECT} ./${PROJECT}

RUN dotnet restore ${PROJECT}
RUN dotnet build ${PROJECT} -c Release --no-restore -o /app/build
RUN dotnet publish ${PROJECT} -c Release -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:9.0.7-noble-chiseled-extra AS production
ARG PROJECT

ENV APPLICATION=${PROJECT}

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000
EXPOSE 5001

USER $APP_UID

ENTRYPOINT ["dotnet", "ProjectOrigin.Stamp.Server.dll"]
