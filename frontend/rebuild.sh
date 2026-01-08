#!/bin/bash
echo "🧹 Cleaning Angular cache..."
rm -rf .angular/cache

echo "🧹 Cleaning node_modules cache..."
rm -rf node_modules/.cache

echo "✅ Cache cleared. Please restart your dev server with: npm start"
