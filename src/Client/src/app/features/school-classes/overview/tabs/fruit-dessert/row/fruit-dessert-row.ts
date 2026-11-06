import { Component } from '@angular/core';
import { FormField } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CongregationDropdown } from '@ske/shared/congregations';
import { FruitDessertEntry } from '../fruit-dessert-entry';

@Component({
  selector: 'tr[ske-fruit-dessert-row]',
  imports: [
    FormField,
    CongregationDropdown,
    NzCheckboxComponent,
    NzButtonComponent,
    NzIconDirective,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent
  ],
  templateUrl: './fruit-dessert-row.html'
})
export class FruitDessertRow extends FruitDessertEntry {}
