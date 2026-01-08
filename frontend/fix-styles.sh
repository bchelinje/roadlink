#!/bin/bash
echo "🧹 Step 1: Stopping any running dev servers..."
pkill -f "ng serve" 2>/dev/null || true
pkill -f "angular" 2>/dev/null || true
sleep 2

echo "🗑️  Step 2: Removing all cache directories..."
rm -rf .angular
rm -rf node_modules/.cache
rm -rf dist

echo "✅ Step 3: Cache cleared!"
echo ""
echo "📝 Now run these commands:"
echo "   npm start"
echo ""
echo "Then in your browser:"
echo "   1. Open DevTools (F12)"
echo "   2. Go to Network tab"
echo "   3. Check 'Disable cache'"
echo "   4. Hard refresh (Ctrl+Shift+R or Cmd+Shift+R)"
