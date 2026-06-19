import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-create-drawing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './create-drawing.component.html',
  styleUrl: './create-drawing.component.css',
})
export class CreateDrawingComponent {}
