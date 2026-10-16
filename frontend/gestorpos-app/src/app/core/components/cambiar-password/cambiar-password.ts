import { Component, input, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroupDirective, NgForm, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extraerMensajeError } from '../../utils/error.util';

export const LARGO_MINIMO_PASSWORD = 8;

function repetirIgual(control: AbstractControl): ValidationErrors | null {
  const nueva = control.get('nueva')?.value;
  const repetir = control.get('repetir')?.value;
  return repetir && nueva !== repetir ? { noCoincide: true } : null;
}

// El "no coinciden" es un error del formulario, no del campo: se lo marca en "Repetir" una vez que se tocó.
class NoCoincideMatcher implements ErrorStateMatcher {
  isErrorState(control: AbstractControl | null, form: FormGroupDirective | NgForm | null): boolean {
    return !!control && control.touched && !!form?.form?.hasError('noCoincide');
  }
}

/** Formulario de "contraseña actual / nueva / repetir". Quien lo usa le pasa cómo guardar (negocio o panel interno). */
@Component({
  selector: 'app-cambiar-password',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './cambiar-password.html',
  styleUrl: './cambiar-password.scss',
})
export class CambiarPassword {
  private readonly fb = inject(FormBuilder);

  readonly guardar = input.required<(actual: string, nueva: string) => Observable<unknown>>();
  /** Viene de una contraseña temporal: el primer campo pasa a llamarse "Contraseña temporal". */
  readonly temporal = input(false);
  readonly textoBoton = input('Guardar contraseña');
  readonly cambiada = output<void>();

  readonly largoMinimo = LARGO_MINIMO_PASSWORD;
  readonly matcherRepetir = new NoCoincideMatcher();
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly ocultar = signal(true);

  readonly form = this.fb.nonNullable.group(
    {
      actual: ['', [Validators.required]],
      nueva: ['', [Validators.required, Validators.minLength(LARGO_MINIMO_PASSWORD)]],
      repetir: ['', [Validators.required]],
    },
    { validators: repetirIgual },
  );

  enviar(): void {
    if (this.form.invalid || this.guardando()) return;
    this.guardando.set(true);
    this.error.set(null);
    const { actual, nueva } = this.form.getRawValue();
    this.guardar()(actual, nueva).subscribe({
      next: () => {
        this.guardando.set(false);
        this.form.reset();
        this.cambiada.emit();
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
