import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
})
export class LoginComponent {
  readonly form = this.formBuilder.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });
  submitting = false;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar
  ) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.authService.login(this.form.value).subscribe(
      () => this.router.navigate(['/chat']),
      () => {
        this.submitting = false;
        this.snackBar.open('Đăng nhập thất bại', 'Đóng', { duration: 4000 });
      }
    );
  }
}
