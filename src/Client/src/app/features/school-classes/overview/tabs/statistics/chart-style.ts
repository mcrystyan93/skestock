import type { ApexOptions } from 'apexcharts';

// Shared look for the statistics charts: faint dashed grid, no axis chrome, inherited font.
export const CHART_FONT = 'inherit';

export const CHART_GRID: ApexOptions['grid'] = {
  borderColor: 'rgba(128, 128, 128, 0.25)',
  strokeDashArray: 4,
  xaxis: { lines: { show: false } },
  padding: { left: 8, right: 16 }
};

export const CHART_AXIS_CHROME = {
  axisBorder: { show: false },
  axisTicks: { show: false }
};

export const barGradient = (horizontal: boolean): ApexOptions['fill'] => ({
  type: 'gradient',
  gradient: {
    type: horizontal ? 'horizontal' : 'vertical',
    shade: 'light',
    shadeIntensity: 0.25,
    opacityFrom: 1,
    opacityTo: 0.85,
    stops: [0, 100]
  }
});
