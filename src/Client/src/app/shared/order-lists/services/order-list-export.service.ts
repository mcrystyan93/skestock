import { computed, inject, Service, signal } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs';
// noinspection ES6PreferShortImport
import { OrderListsHttp } from './order-lists.http';

const FALLBACK_FILE_NAME = 'comanda.xlsx';

/**
 * Downloads the Excel export of a submitted order list and saves it in the browser,
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

  public download(id: string): void {
    if (this.isDownloading(id))
      return;

    this.setDownloading(id, true);

    this._orderListsHttp.export(id)
      .pipe(finalize(() => this.setDownloading(id, false)))
      .subscribe({
        next: (response) => this.saveResponse(response),
        error: () => this._nzMessageService.error('Descărcarea fișierului Excel a eșuat.')
      });
  }

  private saveResponse(response: HttpResponse<Blob>): void {
    const blob = response.body;

    if (!blob) {
      this._nzMessageService.error('Descărcarea fișierului Excel a eșuat.');
      return;
    }

    const fileName = parseContentDispositionFileName(response.headers.get('Content-Disposition'));
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

function parseContentDispositionFileName(header: string | null): string {
  if (!header)
    return FALLBACK_FILE_NAME;

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);

  if (utf8Match?.[1])
    return decodeURIComponent(utf8Match[1].trim());

  const asciiMatch = /filename="?([^";]+)"?/i.exec(header);

  if (asciiMatch?.[1])
    return asciiMatch[1].trim();

  return FALLBACK_FILE_NAME;
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
