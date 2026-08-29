import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NzLayoutComponent } from 'ng-zorro-antd/layout';

@Component({
  selector: 'ske-simple',
  imports: [RouterOutlet, NzLayoutComponent],
  templateUrl: './simple.html',
})
export class Simple {}
