import { expect, test } from '@playwright/test';
import { InvoiceApprovalsPage } from '../pages/InvoiceApprovalsPage';

test('a manager sees why a 75,000 invoice cannot be approved and it stays pending', async ({ page }) => {
  const invoiceApprovalsPage = new InvoiceApprovalsPage(page);
  const customerName = `Playwright manager limit ${Date.now()}`;

  await invoiceApprovalsPage.signIn('demo.manager', 'ChangeMe123!');
  await invoiceApprovalsPage.submitInvoice(customerName, 75_000);
  await invoiceApprovalsPage.attemptApproval(customerName);

  const decisionAlert = invoiceApprovalsPage.getDecisionAlert();
  await expect(decisionAlert).toBeVisible();
  await expect(decisionAlert).toHaveText(
    'Manager approval limit is 50000; invoice amount is 75000.',
  );

  await invoiceApprovalsPage.showInvoices();
  await expect(invoiceApprovalsPage.getInvoiceStatus(customerName)).toHaveText('Pending Approval');
});
