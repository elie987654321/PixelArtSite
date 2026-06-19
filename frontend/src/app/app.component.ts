import { Component, OnInit } from '@angular/core';
import { DrawingService } from './service/drawing.service';
import { Drawing } from './model/drawing.model';
import { DrawingViewComponent } from './drawing-view/drawing-view.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [DrawingViewComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  drawings: Drawing[] = [];
  loading = true;
  error?: string;

  constructor(private readonly drawingService: DrawingService) {}

  ngOnInit(): void {
    this.drawingService.getAll().subscribe({
      next: (drawings) => {
        this.drawings = drawings;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = "Couldn't reach the API. Is the backend running on :5126?";
        this.loading = false;
      },
    });
  }
}
