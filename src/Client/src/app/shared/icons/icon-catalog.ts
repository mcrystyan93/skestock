import { ICON_ASSET_NAMES } from './icon-catalog.generated';

export type IconPickerValue = {
  name: string;
  fileName: string;
  path: string;
};

export function toIconFileName(fileName: string): string {
  return fileName.replace(/\.svg$/i, '');
}

export function toHumanReadableIconName(fileName: string): string {
  const name = toIconFileName(fileName)
    .replace(/[-_]+/g, ' ')
    .trim();

  return name
    .split(/\s+/)
    .map((word) => word.length === 0 ? word : `${word[0].toUpperCase()}${word.slice(1)}`)
    .join(' ');
}

export function buildIconAssetPath(fileName: string): string {
  return `/assets/icons/${fileName}`;
}

export const ICON_CATALOG: readonly IconPickerValue[] = ICON_ASSET_NAMES.map((fileName) => ({
  name: toHumanReadableIconName(fileName),
  fileName: toIconFileName(fileName),
  path: buildIconAssetPath(fileName)
}));
