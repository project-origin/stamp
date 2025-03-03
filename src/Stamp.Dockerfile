ARG PROJECT=ProjectOrigin.Stamp.Server
ARG USER=dotnetuser

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0.200 AS build
ARG PROJECT

WORKDIR /src

COPY ./Protos ./Protos
COPY ./${PROJECT} ./${PROJECT}

RUN dotnet restore ${PROJECT}
RUN dotnet build ${PROJECT} -c Release --no-restore -o /app/build
RUN dotnet publish ${PROJECT} -c Release -o /app/publish

# ------- production image -------
FROM mcr.microsoft.com/dotnet/aspnet:9.0.2-noble AS production
ARG PROJECT
ARG USER

ENV USER=dotnetuser
ENV APPLICATION=${PROJECT}
RUN groupadd -r "$USER" && useradd -r -g "$USER" "$USER"

WORKDIR /app
COPY --chown=root:root --from=build /app/publish .
RUN chmod -R 655 .

USER $USER
EXPOSE 5000
ENTRYPOINT ["/bin/sh", "-c", "dotnet ${APPLICATION}.dll \"${@}\"", "--" ]
