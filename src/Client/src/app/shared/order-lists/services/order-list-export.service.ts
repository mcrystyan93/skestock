import { computed, inject, Service, signal } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs';
// noinspection ES6PreferShortImport
import { OrderListExportFormat, OrderListsHttp } from './order-lists.http';

const FALLBACK_FILE_NAME = 'comanda';

const FORMAT_LABELS: Record<OrderListExportFormat, string> = { xlsx: 'fișierului Excel', png: 'imaginii' };

/**
 * Downloads the Excel or PNG export of a submitted order list and saves it in the browser,
 * reading the file name from the response's Content-Disposition header.
 */
@Service()
export class OrderListExportService {
  private readonly _orderListsHttp = inject(OrderListsHttp);
  private readonly _nzMessageService = inject(NzMessageService);

  private readonly _downloadingIds = signal<ReadonlySet<string>>(new Set());

  public readonly downloadingIds = computed(() => this._downloadingIds());

  public isDownloading(id: string): boolean {
    return this._downloadingIds().has(id);
  }

  public download(id: string, format: OrderListExportFormat = 'xlsx'): void {
    if (this.isDownloading(id))
      return;

    this.setDownloading(id, true);

    this._orderListsHttp.export(id, format)
      .pipe(finalize(() => this.setDownloading(id, false)))
      .subscribe({
        next: (response) => this.saveResponse(response, format),
        error: () => this._nzMessageService.error(`Descărcarea ${FORMAT_LABELS[format]} a eșuat.`)
      });
  }

  private saveResponse(response: HttpResponse<Blob>, format: OrderListExportFormat): void {
    const blob = response.body;

    if (!blob) {
      this._nzMessageService.error(`Descărcarea ${FORMAT_LABELS[format]} a eșuat.`);
      return;
    }

    const fileName = parseContentDispositionFileName(response.headers.get('Content-Disposition'), format);
    triggerBrowserDownload(blob, fileName);
  }

  private setDownloading(id: string, downloading: boolean): void {
    const next = new Set(this._downloadingIds());

    if (downloading)
      next.add(id);
    else
      next.delete(id);

    this._downloadingIds.set(next);
  }
}

function parseContentDispositionFileName(header: string | null, format: OrderListExportFormat): string {
  const fallback = `${FALLBACK_FILE_NAME}.${format}`;

  if (!header)
    return fallback;

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);

  if (utf8Match?.[1])
    return decodeURIComponent(utf8Match[1].trim());

  const asciiMatch = /filename="?([^";]+)"?/i.exec(header);

  if (asciiMatch?.[1])
    return asciiMatch[1].trim();

  return fallback;
}

function triggerBrowserDownload(blob: Blob, fileName: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(objectUrl);
}
