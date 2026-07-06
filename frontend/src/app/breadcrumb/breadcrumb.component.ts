import { Component } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';

interface Crumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.css',
})
export class BreadcrumbComponent {
  crumbs: Crumb[] = [];

  constructor(
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => (this.crumbs = this.build()));
    this.crumbs = this.build();
  }

  private build(): Crumb[] {
    const crumbs: Crumb[] = [];
    let route: ActivatedRoute | null = this.route.root;
    let url = '';

    while (route) {
      const segment = route.snapshot.url.map((part) => part.path).join('/');
      if (segment) url += `/${segment}`;

      const label = route.snapshot.routeConfig?.data?.['breadcrumb'] as
         string | undefined;
      if (label) crumbs.push({ label, url: url || '/' });

      route = route.firstChild;
    }

    return crumbs.length ? [{ label: 'Home', url: '/' }, ...crumbs] : [];
  }
}
