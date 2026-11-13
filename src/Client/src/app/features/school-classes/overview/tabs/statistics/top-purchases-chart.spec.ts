import { TestBed } from '@angular/core/testing';
import { ThemeService } from '@ske/theme';
import { TopPurchasesChart } from './top-purchases-chart';

describe('TopPurchasesChart', () => {
  function setup(noun?: string) {
    TestBed.configureTestingModule({
      providers: [{ provide: ThemeService, useValue: { currentTheme: () => 'light' } }]
    });
    const fixture = TestBed.createComponent(TopPurchasesChart);
    fixture.componentRef.setInput('metric', 'frequency');
    fixture.componentRef.setInput('ariaLabel', 'grafic');
    if (noun) fixture.componentRef.setInput('noun', noun);
    return fixture;
  }

  it('says there are no purchases by default', async () => {
    const fixture = setup();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Nu există achiziții pentru filtrele selectate.');
  });

  it('uses the given noun in the empty state and the frequency series name', async () => {
    const fixture = setup('donații');
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Nu există donații pentru filtrele selectate.');
    expect(fixture.componentInstance.series()[0].name).toBe('Donații');
  });
});
