import { Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

export interface SeoConfig {
  title: string;
  description: string;
  url?: string;
  image?: string;
  noIndex?: boolean;
}

const BASE_URL = 'https://www.prt-fy.com';
const DEFAULT_IMAGE = `${BASE_URL}/og-image.png`;

@Injectable({ providedIn: 'root' })
export class SeoService {
  constructor(private title: Title, private meta: Meta) {}

  update(config: SeoConfig): void {
    const fullTitle = config.title.includes('Portify')
      ? config.title
      : `${config.title} | Portify`;
    const url = config.url ? `${BASE_URL}${config.url}` : BASE_URL;
    const image = config.image ?? DEFAULT_IMAGE;

    // Page title
    this.title.setTitle(fullTitle);

    // Primary
    this.meta.updateTag({ name: 'description', content: config.description });
    this.meta.updateTag({
      name: 'robots',
      content: config.noIndex ? 'noindex, nofollow' : 'index, follow',
    });

    // Canonical
    this.upsertLink('canonical', url);

    // Open Graph
    this.meta.updateTag({ property: 'og:title', content: fullTitle });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:image', content: image });

    // Twitter
    this.meta.updateTag({ name: 'twitter:title', content: fullTitle });
    this.meta.updateTag({ name: 'twitter:description', content: config.description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
  }

  private upsertLink(rel: string, href: string): void {
    if (typeof document === 'undefined') return;
    let el = document.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`);
    if (!el) {
      el = document.createElement('link');
      el.setAttribute('rel', rel);
      document.head.appendChild(el);
    }
    el.setAttribute('href', href);
  }
}

