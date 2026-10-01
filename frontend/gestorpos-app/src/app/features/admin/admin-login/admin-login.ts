import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { activarManifestAdmin, restaurarManifestNegocio } from '../../../core/utils/admin-manifest.util';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './admin-login.html',
  styleUrl: './admin-login.scss',
})
export class AdminLogin implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly adminAuth = inject(AdminAuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });
  readonly error = signal<string | null>(null);
  readonly iniciandoSesion = signal(false);

  ngOnInit(): void {
    activarManifestAdmin();
  }

  ngOnDestroy(): void {
    restaurarManifestNegocio();
  }

  ingresar(): void {
    if (this.form.invalid || this.iniciandoSesion()) return;
    this.error.set(null);
    this.iniciandoSesion.set(true);

    const { email, password } = this.form.getRawValue();
    this.adminAuth.login(email, password).subscribe({
      next: () => {
        this.iniciandoSesion.set(false);
        this.router.navigateByUrl('/admin/crear-negocio');
      },
      error: () => {
        this.iniciandoSesion.set(false);
        this.error.set('Email o contraseña incorrectos.');
      },
    });
  }
}
