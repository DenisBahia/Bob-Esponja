import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss'],
})
export class LandingComponent implements OnInit, OnDestroy {
  scrolled = false;
  activeSlide = 0;
  // UI toggle only: GitHub auth remains implemented in services/routes.
  readonly showGitHubAuth = false;

  readonly slides = [
    { label: 'Dashboard' },
    { label: 'Projections' },
    { label: '🧾 Tax Center' },
    { label: 'Buy History' },
    { label: 'Sell History' },
    { label: 'Portfolio Sharing' },
    { label: '📥 CSV Import' },
  ];

  private slideInterval: ReturnType<typeof setInterval> | null = null;
  private observer: IntersectionObserver | null = null;

  constructor(private router: Router, private auth: AuthService) {}

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
      return;
    }
    this.startSlideshow();
    this.setupScrollAnimations();
  }

  ngOnDestroy(): void {
    if (this.slideInterval) clearInterval(this.slideInterval);
    this.observer?.disconnect();
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled = window.scrollY > 60;
  }

  private startSlideshow(): void {
    this.slideInterval = setInterval(() => {
      this.activeSlide = (this.activeSlide + 1) % this.slides.length;
    }, 4000);
  }

  private setupScrollAnimations(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            this.observer?.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1, rootMargin: '0px 0px -50px 0px' }
    );
    setTimeout(() => {
      document.querySelectorAll('.reveal').forEach((el) => this.observer?.observe(el));
    }, 200);
  }

  scrollTo(id: string, event?: MouseEvent): void {
    event?.preventDefault();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  setSlide(index: number): void {
    this.activeSlide = index;
    if (this.slideInterval) {
      clearInterval(this.slideInterval);
      this.startSlideshow();
    }
  }
}
