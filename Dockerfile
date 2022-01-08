FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
EXPOSE 80

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
RUN apt update
# install libreoffice to fullfill requirements
RUN apt -y install wget libreoffice
RUN wget -nv https://ftp.halifax.rwth-aachen.de/tdf/libreoffice/stable/7.2.5/deb/x86_64/LibreOffice_7.2.5_Linux_x86-64_deb.tar.gz && tar xf LibreOffice_7.2.5_Linux_x86-64_deb.tar.gz
RUN dpkg -i LibreOffice_7.2.5.2_Linux_x86-64_deb/DEBS/*.deb
RUN apt -y remove libreoffice libreoffice-common
RUN rm -rf LibreOffice_7.2.5*
RUN ln -s /usr/local/bin/libreoffice7.2 /usr/local/bin/libreoffice
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Docx2Pdf.dll"]
