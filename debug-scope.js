const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  try {
    // Navigate to the app
    await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });
    
    // Login (assuming default credentials)
    await page.fill('input[type="email"]', 'admin@example.com');
    await page.fill('input[type="password"]', 'admin123');
    await page.click('button:has-text("تسجيل الدخول")');
    
    // Wait for navigation
    await page.waitForURL('http://localhost:4200/dashboard');
    
    // Navigate to catalog types (expenses)
    await page.goto('http://localhost:4200/types/expenses', { waitUntil: 'networkidle' });
    
    // Click "Add New" button
    await page.click('button:has-text("إضافة نوع")');
    
    // Wait for dialog
    await page.waitForSelector('p-dialog');
    
    // Check if scope field exists
    const scopeLabel = await page.$('label[for="catalog-scope"]');
    console.log('Scope label found:', !!scopeLabel);
    
    if (scopeLabel) {
      const scopeSelect = await page.$('p-select[inputId="catalog-scope"]');
      console.log('Scope select found:', !!scopeSelect);
      
      if (scopeSelect) {
        const isVisible = await scopeSelect.isVisible();
        console.log('Scope select visible:', isVisible);
        
        // Get computed style
        const boundingBox = await scopeSelect.boundingBox();
        console.log('Scope select bounding box:', boundingBox);
        
        // Check if it's in the DOM but hidden
        const display = await scopeSelect.evaluate(el => window.getComputedStyle(el).display);
        console.log('Scope select display:', display);
        
        const visibility = await scopeSelect.evaluate(el => window.getComputedStyle(el).visibility);
        console.log('Scope select visibility:', visibility);
      }
    } else {
      console.log('Scope label NOT found');
      
      // Check if the if condition is even working
      const fieldDivs = await page.$$('div.field');
      console.log('Total field divs:', fieldDivs.length);
      
      // Get the HTML of the dialog content
      const dialogContent = await page.$('p-dialog .p-dialog-content');
      if (dialogContent) {
        const html = await dialogContent.evaluate(el => el.innerHTML);
        console.log('Dialog content HTML:', html.substring(0, 500));
      }
    }
    
  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
})();
