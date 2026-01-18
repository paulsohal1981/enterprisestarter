import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { HeaderComponent } from './components/header/header.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, MatSidenavModule, HeaderComponent, SidebarComponent],
  template: `
    <div class="layout-container">
      <app-header (menuToggle)="toggleSidenav()"></app-header>
      <mat-sidenav-container class="sidenav-container">
        <mat-sidenav
          #sidenav
          mode="side"
          [opened]="sidenavOpened()"
          class="sidenav">
          <app-sidebar></app-sidebar>
        </mat-sidenav>
        <mat-sidenav-content class="content">
          <router-outlet></router-outlet>
        </mat-sidenav-content>
      </mat-sidenav-container>
    </div>
  `,
  styles: [`
    .layout-container {
      display: flex;
      flex-direction: column;
      height: 100vh;
    }
    .sidenav-container {
      flex: 1;
      margin-top: 64px;
    }
    .sidenav {
      width: 250px;
    }
    .content {
      padding: 24px;
    }
    @media (max-width: 768px) {
      .sidenav-container {
        margin-top: 56px;
      }
    }
  `]
})
export class MainLayoutComponent {
  sidenavOpened = signal(true);

  toggleSidenav(): void {
    this.sidenavOpened.update(v => !v);
  }
}
