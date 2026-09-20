import { TestBed } from '@angular/core/testing';
import { type CategoryDto } from '@ske/models';
import { of, Subject, throwError } from 'rxjs';
import { CategoryDropdown } from './category-dropdown';
// noinspection ES6PreferShortImport
import { CategoriesHttp } from '../../services/categories.http';

describe('CategoryDropdown', () => {
  const category: CategoryDto = {
    id: 'category-1',
    name: 'Papetărie',
    itemCount: 2,
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
    icon: null,
  };

  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getById: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [CategoryDropdown],
      providers: [{ provide: CategoriesHttp, useValue: http }],
    });
  });

  it('shows a loading label before resolving an Id-only value', () => {
    const pendingCategory = new Subject<CategoryDto>();
    http.getById.mockReturnValue(pendingCategory);

    const fixture = TestBed.createComponent(CategoryDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: category.id });
    fixture.detectChanges();

    expect(dropdown.selectedCategoryLabel()).toBe('Se încarcă categoria…');

    pendingCategory.next(category);
    pendingCategory.complete();
    fixture.detectChanges();

    expect(dropdown.selectedCategoryLabel()).toBe(category.name);
    fixture.destroy();
  });

  it('shows an explicit fallback when the Id cannot be resolved', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(CategoryDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: category.id });
    fixture.detectChanges();

    expect(dropdown.selectedCategoryLabel()).toBe(`Categorie indisponibilă`);
    fixture.destroy();
  });

  it('uses a supplied name without making a detail request', () => {
    http.getById.mockReturnValue(of(category));

    const fixture = TestBed.createComponent(CategoryDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set(category);
    fixture.detectChanges();

    expect(dropdown.selectedCategoryLabel()).toBe(category.name);
    expect(http.getById).not.toHaveBeenCalled();
    fixture.destroy();
  });
});
