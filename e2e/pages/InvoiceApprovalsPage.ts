import { type Locator, type Page } from '@playwright/test';

export class InvoiceApprovalsPage {
  constructor(private readonly page: Page) {}

  async signIn(username: string, password: string): Promise<void> {
    await this.page.goto('/');
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }

  async submitInvoice(customerName: string, amount: number): Promise<void> {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const invoiceRow = this.invoiceRow(customerName);
    await invoiceRow
      .getByRole('button', { name: /Submit invoice .+ for approval/ })
      .click();
  }

  async attemptApproval(customerName: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    await this.pendingInvoice(customerName)
      .getByRole('button', { name: /Approve invoice .+/ })
      .click();
  }

  getDecisionAlert(): Locator {
    return this.page.getByRole('alert');
  }

  async showInvoices(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  getInvoiceStatus(customerName: string): Locator {
    return this.invoiceRow(customerName).getByRole('cell').nth(3);
  }

  private invoiceRow(customerName: string): Locator {
    return this.page.getByRole('row').filter({ hasText: customerName });
  }

  private pendingInvoice(customerName: string): Locator {
    return this.page.getByRole('listitem').filter({ hasText: customerName });
  }
}
