import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './access-denied.component.html',
  styleUrl: './access-denied.component.css'
})
export class AccessDeniedComponent {
  readonly auth = inject(AuthService);
}
