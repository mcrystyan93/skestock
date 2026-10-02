# Visual verification: class configuration

Captured the running Angular app at 900 px high for these viewport widths:

`320, 375, 479, 480, 481, 575, 576, 577, 767, 768, 769, 991, 992, 993, 1199, 1200, 1201, 1599, 1600, 1601, 1919, 1920, 1921`

- `config-<width>.png` shows the global configuration page in its empty state.
- `class-<width>.png` shows the departments tab for an existing class whose one-time configuration has not been
  initialized.
- All images use CSS pixel scale and full-page capture. Representative narrow, tablet, desktop, and wide captures were
  inspected visually. The layouts stack on narrow screens; class tabs scroll horizontally at phone widths.
- No configuration was saved and the existing class was not initialized during screenshot capture.
- Playwright test files were neither created nor run; browser automation was used only to capture UI screenshots and
  inspect the rendered page.
