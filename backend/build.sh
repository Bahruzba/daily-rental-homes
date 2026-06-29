#!/usr/bin/env bash
set -e

cd "$(dirname "$0")"

dotnet restore DailyRentalHomes.slnx

dotnet build DailyRentalHomes.slnx --configuration Release --no-restore
