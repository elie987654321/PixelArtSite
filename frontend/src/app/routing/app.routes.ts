import { Routes } from '@angular/router';
import { HomeComponent } from '../components/home/home.component';
import { GalleryComponent } from '../components/gallery/gallery.component';
import { DrawingOptionsComponent } from '../components/editor/drawing-options/drawing-options.component';
import { LoginComponent } from '../components/login/login.component';
import { RegisterComponent } from '../components/register/register.component';
import { ExistingDrawingEditorWrapper } from '../components/editor/drawing-editor/drawing-editor.component';
import { authGuard } from './guard/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: '', component: HomeComponent, canActivate: [authGuard] },
  {
    path: 'drawings',
    canActivate: [authGuard],
    data: { breadcrumb: 'Gallery' },
    children: [
      { path: '', component: GalleryComponent },      
      {
        path: ':id',
        component: ExistingDrawingEditorWrapper,
        data: { breadcrumb: 'Drawing' },
      },
    ],
  },
  {
    path: 'create',
    component: DrawingOptionsComponent,
    data: { breadcrumb: 'New drawing' },
  },
  { path: '**', redirectTo: '' },
];
