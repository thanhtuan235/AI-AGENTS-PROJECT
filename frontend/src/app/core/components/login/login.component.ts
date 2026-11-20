import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  readonly form = this.formBuilder.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });
  submitting = false;
  hidePassword = true;
  errorMessage = '';

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
    this.errorMessage = '';
    this.authService.login(this.form.value).subscribe(
      () => this.router.navigate(['/chat']),
      (error: HttpErrorResponse) => {
        this.submitting = false;
        this.errorMessage =
          error.error?.message || 'Tên đăng nhập hoặc mật khẩu không đúng.';
        this.snackBar.open(this.errorMessage, 'Đóng', { duration: 4000 });
      }
    );
  }
}
