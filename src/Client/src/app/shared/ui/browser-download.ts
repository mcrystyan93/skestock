/** Saves a blob through a temporary anchor, so the browser downloads it under `fileName`. */
export function triggerBrowserDownload(blob: Blob, fileName: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(objectUrl);
}

/** Reads the file name from a Content-Disposition header (RFC 5987 `filename*` first), else returns `fallback`. */
export function parseContentDispositionFileName(header: string | null, fallback: string): string {
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
