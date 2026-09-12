import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'ske-class-analytics-kpis',
  imports: [CurrencyPipe, DecimalPipe],
  template: `
    <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-5" aria-label="Indicatori analiză">
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Cantitate în stoc</p>
        <p class="text-2xl font-semibold">{{ totalQuantity() | number:'1.0-2' }}</p>
      </article>
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Articole în stoc</p>
        <p class="text-2xl font-semibold">{{ totalItemCount() | number:'1.0-0' }}</p>
      </article>
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Categorii</p>
        <p class="text-2xl font-semibold">{{ totalCategoryCount() | number:'1.0-0' }}</p>
      </article>
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Cost total recepții</p>
        <p class="text-2xl font-semibold">{{ totalAmount() | currency:'RON' }}</p>
      </article>
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Număr recepții</p>
        <p class="text-2xl font-semibold">{{ receiptCount() | number:'1.0-0' }}</p>
      </article>
      <article class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800">
        <p class="text-sm text-gray-500">Cost mediu recepție</p>
        <p class="text-2xl font-semibold">{{ averageAmount() | currency:'RON' }}</p>
      </article>
    </section>
  `
})
export class ClassAnalyticsKpis {
  public readonly totalQuantity = input(0);
  public readonly totalItemCount = input(0);
  public readonly totalCategoryCount = input(0);
  public readonly totalAmount = input(0);
  public readonly receiptCount = input(0);
  public readonly averageAmount = input(0);
}
