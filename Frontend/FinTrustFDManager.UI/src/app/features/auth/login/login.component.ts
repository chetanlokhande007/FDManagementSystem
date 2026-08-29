import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService, LoginRequest } from '../../../services/auth.service';
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loginForm: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const req: LoginRequest = this.loginForm.value;

    this.authService.login(req).subscribe({
      next: (res: any) => {
        // Save session
        this.authService.setSession(res);

        // Redirect based on role (case-insensitive)
        const role = (res.role || '').toLowerCase();
        if (role === 'admin') {
          this.router.navigate(['/admin/dashboard']);
        } else if (role === 'ca') {
          this.router.navigate(['/ca/dashboard']);
        } else if (role === 'approver') {
          this.router.navigate(['/approver/dashboard']);
        } else {
          // Fallback for unknown roles
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err: any) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || 'Invalid email or password.';
      }
    });
  }
}
