import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, DashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  title = 'ETF Investment Tracker';
}
