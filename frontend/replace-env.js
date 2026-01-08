#!/usr/bin/env node

/**
 * Script to replace environment placeholders in production build
 * This runs after Angular build to inject runtime environment variables
 */

const fs = require('fs');
const path = require('path');

// Get environment variables
const RAILWAY_API_URL = process.env.RAILWAY_API_URL || process.env.API_BASE_URL || 'https://api.yourdomain.com';
const VERCEL_URL = process.env.VERCEL_URL ? `https://${process.env.VERCEL_URL}` : 'https://admin.yourdomain.com';

console.log('🔧 Replacing environment variables in build...');
console.log(`  API URL: ${RAILWAY_API_URL}`);
console.log(`  Frontend URL: ${VERCEL_URL}`);

// Find main JavaScript file in dist folder
const distPath = path.join(__dirname, 'dist/bec-admin-dashboard/browser');

if (!fs.existsSync(distPath)) {
  console.error('❌ Build directory not found. Run "npm run build" first.');
  process.exit(1);
}

// Get all JS files
const files = fs.readdirSync(distPath).filter(file => file.endsWith('.js'));

files.forEach(file => {
  const filePath = path.join(distPath, file);
  let content = fs.readFileSync(filePath, 'utf8');

  // Replace placeholders
  const originalContent = content;
  content = content.replace(/RAILWAY_API_URL_PLACEHOLDER/g, RAILWAY_API_URL);
  content = content.replace(/VERCEL_URL_PLACEHOLDER/g, VERCEL_URL);

  // Only write if changes were made
  if (content !== originalContent) {
    fs.writeFileSync(filePath, content, 'utf8');
    console.log(`  ✅ Updated ${file}`);
  }
});

console.log('✨ Environment variables replaced successfully!');
