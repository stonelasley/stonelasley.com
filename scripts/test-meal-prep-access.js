#!/usr/bin/env node

/**
 * Test script to verify Meal Prep page access
 */

const { Client } = require('@notionhq/client');
const path = require('path');

// Load environment variables
require('dotenv').config({ path: path.resolve(process.cwd(), '.env.local') });

const NOTION_API_KEY = process.env.NOTION_API_KEY;
const MEALPREP_PAGE_ID = process.env.MEALPREP_PAGE_ID;

// Format page ID with hyphens (Notion requires UUID format)
function formatPageId(id) {
  if (!id) return null;
  // Remove any existing hyphens
  const cleanId = id.replace(/-/g, '');
  // Add hyphens in UUID format: 8-4-4-4-12
  return `${cleanId.slice(0, 8)}-${cleanId.slice(8, 12)}-${cleanId.slice(12, 16)}-${cleanId.slice(16, 20)}-${cleanId.slice(20, 32)}`;
}

async function testMealPrepAccess() {
  console.log('========================================');
  console.log('Testing Meal Prep Page Access');
  console.log('========================================\n');

  // Validate environment
  if (!NOTION_API_KEY) {
    console.error('✗ NOTION_API_KEY not set in .env.local');
    process.exit(1);
  }

  if (!MEALPREP_PAGE_ID) {
    console.error('✗ MEALPREP_PAGE_ID not set in .env.local');
    console.log('\n💡 Add this to your .env.local:');
    console.log('MEALPREP_PAGE_ID=2914836644028060840fcadb65505388');
    process.exit(1);
  }

  const notion = new Client({ auth: NOTION_API_KEY });
  const formattedPageId = formatPageId(MEALPREP_PAGE_ID);

  console.log(`Raw Page ID: ${MEALPREP_PAGE_ID}`);
  console.log(`Formatted Page ID: ${formattedPageId}`);
  console.log(`Page URL: https://www.notion.so/Meal-Prep-${MEALPREP_PAGE_ID}\n`);

  // Test 1: Check if page exists and is accessible
  console.log('Test 1: Attempting to retrieve page...');
  try {
    const page = await notion.pages.retrieve({ page_id: formattedPageId });
    console.log('✓ SUCCESS! Page is accessible\n');

    // Extract page title
    let title = 'Untitled';
    for (const [key, property] of Object.entries(page.properties)) {
      if (property?.type === 'title' && Array.isArray(property.title)) {
        const titleText = property.title.map(t => t.plain_text).join('');
        if (titleText) {
          title = titleText;
          break;
        }
      }
    }

    console.log('Page Details:');
    console.log(`  Title: ${title}`);
    console.log(`  ID: ${page.id}`);
    console.log(`  Created: ${page.created_time}`);
    console.log(`  Last Edited: ${page.last_edited_time}`);
    console.log('');

    // Test 2: Check if we can read blocks
    console.log('Test 2: Attempting to read page content...');
    try {
      const blocks = await notion.blocks.children.list({
        block_id: formattedPageId,
        page_size: 10
      });
      console.log(`✓ SUCCESS! Found ${blocks.results.length} blocks\n`);

      if (blocks.results.length === 0) {
        console.log('⚠️  Warning: Page has no content blocks');
        console.log('   Add some content to the page and run npm run fetch-content\n');
      }

      console.log('========================================');
      console.log('✓ All tests passed!');
      console.log('========================================');
      console.log('\nYou can now run: npm run fetch-content');

    } catch (blockError) {
      console.error('✗ FAILED to read blocks');
      console.error(`Error: ${blockError.message}\n`);
      console.log('This might mean the integration has page access but not content access.');
      process.exit(1);
    }

  } catch (error) {
    console.error('✗ FAILED to retrieve page\n');
    console.error('Error Details:');
    console.error(`  Code: ${error.code}`);
    console.error(`  Message: ${error.message}\n`);

    console.log('========================================');
    console.log('Troubleshooting Steps:');
    console.log('========================================\n');

    console.log('1. Open the Meal Prep page in Notion:');
    console.log(`   https://www.notion.so/Meal-Prep-${MEALPREP_PAGE_ID}\n`);

    console.log('2. Click the "..." menu in the top right corner\n');

    console.log('3. Look for "Add connections" or "Connections"\n');

    console.log('4. Select your Notion integration from the list\n');

    console.log('5. Click "Confirm" to grant access\n');

    console.log('6. Run this test again: node scripts/test-meal-prep-access.js\n');

    console.log('========================================\n');

    // Check if it's a permissions issue
    if (error.code === 'object_not_found') {
      console.log('⚠️  This error usually means:');
      console.log('   • The page exists but is NOT shared with your integration');
      console.log('   • OR the page ID is incorrect\n');
    } else if (error.code === 'unauthorized') {
      console.log('⚠️  This error means:');
      console.log('   • Your NOTION_API_KEY is invalid or expired');
      console.log('   • Create a new integration at: https://www.notion.so/my-integrations\n');
    }

    process.exit(1);
  }
}

testMealPrepAccess();
