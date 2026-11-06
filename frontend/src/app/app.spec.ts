import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('shopping bag', () => {
  it('adds, updates, persists, and removes a product', async () => {
    localStorage.removeItem('ewear_cart');
    await TestBed.configureTestingModule({ imports: [App], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const app = TestBed.createComponent(App).componentInstance;
    const product = { id: 1, name: 'Test Shirt', category: 'Shirts' as const, description: '', price: 68, imageUrl: '/assets/shirt.png', color: 'Ivory', stock: 2, featured: false, active: true };
    app.products.set([product]);
    app.addToCart(product);
    expect(app.cartCount()).toBe(1);
    expect(app.cartOpen()).toBe(false);
    expect(app.noticeBagLink()).toBe(true);
    expect(JSON.parse(localStorage.getItem('ewear_cart') ?? '[]')).toEqual([{ productId: 1, quantity: 1 }]);
    app.changeQuantity(1, 1);
    expect(app.cartCount()).toBe(2);
    app.changeQuantity(1, 1);
    expect(app.cartCount()).toBe(2);
    app.remove(1);
    expect(app.cartCount()).toBe(0);
  });
});
