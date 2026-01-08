#!/bin/bash

# Test Stripe API keys
echo "Testing Stripe API Keys..."
echo ""

# Test Secret Key
echo "1. Testing Secret Key (backend)..."
curl https://api.stripe.com/v1/customers \
  -u sk_test_51SUXWv4G8afz9y4g4stYeHLBf2nnwOkrf9jv80doolEEE2aNQRbwvWu2DE0F6qCWFfqc5b1uTSh7hPBdzliVGdwb00N2BaDDAW: \
  -d "description=Test Customer" 2>&1 | head -5

echo ""
echo "2. Testing Publishable Key (frontend)..."
echo "Your publishable key: pk_test_51SUXWv4G8afz9y4glyBEpHpl98o0WHVnNgTjyT39PFMAOVACjiFclRbo74ATAZwOnQPmkhtgHUbISgsvgXnGvlM600pnkIfnVC"
echo ""
echo "If secret key test shows an error, your keys might be revoked or from a different account."
echo "Check your Stripe Dashboard: https://dashboard.stripe.com/test/apikeys"
