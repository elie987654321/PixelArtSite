import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GalleryComponent } from './gallery/gallery.component';
import { DrawingOptionsComponent } from './drawing-options/drawing-options.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'drawings', component: GalleryComponent },
  { path: 'create', component: DrawingOptionsComponent },
];
