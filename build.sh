#!/usr/bin/env bash
dotnet publish -f netcoreapp3.1 -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true