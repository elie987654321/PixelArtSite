import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GalleryComponent } from './main/gallery/gallery.component';
import { DrawingOptionsComponent } from './main/editor/drawing-options/drawing-options.component';
import { LoginComponent } from './auth/main/login/login.component';
import { RegisterComponent } from './auth/main/register/register.component';
import { ExistingDrawingEditorWrapper } from './main/editor/drawing-editor/drawing-editor.component';
import { authGuard } from './auth/guard/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: '', component: HomeComponent, canActivate: [authGuard] },
  { path: 'drawings', component: GalleryComponent, canActivate: [authGuard] },
  { path: 'drawings/:id', component: ExistingDrawingEditorWrapper, canActivate: [authGuard] },
  { path: 'create', component: DrawingOptionsComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
