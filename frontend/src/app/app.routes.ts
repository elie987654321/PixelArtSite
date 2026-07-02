import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GalleryComponent } from './gallery/gallery.component';
import { DrawingOptionsComponent } from './drawing-options/drawing-options.component';
import { LoginComponent } from './login/login.component';
import { authGuard } from './guard/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', component: HomeComponent, canActivate: [authGuard] },
  { path: 'drawings', component: GalleryComponent, canActivate: [authGuard] },
  { path: 'create', component: DrawingOptionsComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
