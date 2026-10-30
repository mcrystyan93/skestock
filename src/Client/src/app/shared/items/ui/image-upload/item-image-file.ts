import type { NzUploadFile } from 'ng-zorro-antd/upload';

export const IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp,image/gif';

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = IMAGE_ACCEPT.split(',');

/** The Romanian message for a file that cannot be uploaded as an item image, or null when it can. */
export function imageFileError(file: { type?: string; size?: number }): string | null {
  if (!ALLOWED_TYPES.includes(file.type ?? '')) return 'Format neacceptat';
  if ((file.size ?? 0) > MAX_IMAGE_BYTES) return 'Imaginea depășește 5 MB';

  return null;
}

/** The upload sends the File itself as the body; a plain object would be serialized as JSON. */
export function toUploadFile(file: File): NzUploadFile {
  return Object.assign(file, { uid: file.name, originFileObj: file }) as unknown as NzUploadFile;
}
