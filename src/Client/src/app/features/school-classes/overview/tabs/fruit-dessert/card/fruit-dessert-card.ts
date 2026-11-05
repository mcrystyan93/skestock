import { Component } from '@angular/core';
import { FormField } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CongregationDropdown } from '@ske/shared/congregations';
import { FruitDessertEntry } from '../fruit-dessert-entry';

@Component({
  selector: 'ske-fruit-dessert-card',
  imports: [
    FormField,
    CongregationDropdown,
    NzButtonComponent,
    NzCardComponent,
    NzCheckboxComponent,
    NzIconDirective,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent
  ],
  templateUrl: './fruit-dessert-card.html',
  host: { class: 'block min-w-0' }
})
export class FruitDessertCard extends FruitDessertEntry {}
