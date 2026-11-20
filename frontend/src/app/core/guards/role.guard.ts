import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class RoleGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const expectedRole = route.data.role;
    return this.authService.getUser() &&
      this.authService.getUser().role === expectedRole
      ? true
      : this.router.createUrlTree(['/chat']);
  }
}
