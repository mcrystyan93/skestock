import {
  ICON_CATALOG,
  buildIconAssetPath,
  toHumanReadableIconName,
  toIconFileName
} from './icon-catalog';

describe('icon catalog', () => {
  it('turns a kebab-case filename into a readable name', () => {
    expect(toHumanReadableIconName('arrow-up-right.svg')).toBe('Arrow Up Right');
    expect(toHumanReadableIconName('360-degrees.svg')).toBe('360 Degrees');
  });

  it('builds the public path used by the Angular assets configuration', () => {
    expect(buildIconAssetPath('folder-open.svg')).toBe('/assets/icons/folder-open.svg');
  });

  it('removes the extension while preserving the actual asset filename', () => {
    expect(toIconFileName('folder-open.svg')).toBe('folder-open');
  });

  it('contains the generated asset catalog with readable names and paths', () => {
    expect(ICON_CATALOG.length).toBeGreaterThan(3000);
    expect(ICON_CATALOG).toContainEqual({
      name: 'Arrow Up',
      fileName: 'arrow-up',
      path: '/assets/icons/arrow-up.svg'
    });
  });
});
