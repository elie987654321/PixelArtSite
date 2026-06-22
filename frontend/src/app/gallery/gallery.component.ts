import { Component, OnInit } from '@angular/core';
import { DrawingService } from '../service/drawing.service';
import { Drawing } from '../model/drawing.model';
import { DrawingViewComponent } from '../drawing-view/drawing-view.component';

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [DrawingViewComponent],
  templateUrl: './gallery.component.html',
  styleUrl: './gallery.component.css',
})
export class GalleryComponent implements OnInit {
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
