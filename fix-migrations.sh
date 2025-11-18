#!/bin/bash

# This script fixes the migration history by manually inserting records
# Run this script: bash fix-migrations.sh

echo "Fixing migration history..."

# Get the connection string from appsettings.Development.json
CONNECTION_STRING=$(grep -A 1 "DefaultConnection" BeC.OpenId.Connect/appsettings.Development.json | tail -1 | sed 's/.*": "//;s/",*//')

if [ -z "$CONNECTION_STRING" ]; then
    echo "Error: Could not find connection string in appsettings.Development.json"
    echo "Please edit this script and set the CONNECTION_STRING variable manually"
    exit 1
fi

echo "Connection string found"
echo ""
echo "Please run the SQL from FixMigrationHistory.sql in your database management tool"
echo "Connection string: $CONNECTION_STRING"
echo ""
echo "After running the SQL, execute: dotnet ef database update"

