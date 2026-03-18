import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-candidate-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="candidate-layout">
      <header class="candidate-header">
        <div class="logo">AI Competency Assessment</div>
        <div class="user-profile">
          <span>Test Taker</span>
        </div>
      </header>
      <main class="candidate-main">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .candidate-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background-color: #f5f7fa;
      font-family: 'Inter', sans-serif;
    }
    .candidate-header {
      background-color: #ffffff;
      height: 64px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      z-index: 10;
    }
    .logo {
      font-weight: 700;
      font-size: 1.25rem;
      color: #1a202c;
    }
    .user-profile {
      font-size: 0.9rem;
      color: #4a5568;
    }
    .candidate-main {
      flex: 1;
      padding: 32px 24px;
      max-width: 1200px;
      margin: 0 auto;
      width: 100%;
    }
  `]
})
export class CandidateLayoutComponent {}
