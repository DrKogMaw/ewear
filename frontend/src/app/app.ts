import { Component, computed, effect, HostListener, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

type Category = 'All' | 'Shirts' | 'Pants' | 'Boots';
type Product = { id: number; name: string; category: Exclude<Category, 'All'>; description: string; price: number; imageUrl: string; color: string; stock: number; featured: boolean; active: boolean };
type CartItem = { productId: number; quantity: number };
type User = { id: string; phone: string; role: 'customer' | 'staff' };
type ProductInput = Omit<Product, 'id' | 'active'>;

@Component({ selector: 'app-root', imports: [CurrencyPipe, FormsModule], templateUrl: './app.html', styleUrl: './app.scss' })
export class App implements OnInit {
  private http = inject(HttpClient);
  private returnFocus: HTMLElement | null = null;
  private dialogWasOpen = false;
  private noticeTimer?: ReturnType<typeof setTimeout>;
  private addedTimer?: ReturnType<typeof setTimeout>;
  readonly categories: Category[] = ['All', 'Shirts', 'Pants', 'Boots'];
  readonly products = signal<Product[]>([]);
  readonly category = signal<Category>('All');
  readonly cart = signal<CartItem[]>(this.readCart());
  readonly user = signal<User | null>(null);
  readonly cartOpen = signal(false);
  readonly authOpen = signal(false);
  readonly staffOpen = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly uploading = signal(false);
  readonly notice = signal('');
  readonly noticeKind = signal<'success' | 'info'>('success');
  readonly noticeBagLink = signal(false);
  readonly recentlyAddedId = signal<number | null>(null);
  readonly theme = signal<'light' | 'dark'>(this.readTheme());
  readonly error = signal('');
  readonly authStep = signal<'phone' | 'code'>('phone');
  readonly developmentCode = signal('');
  readonly filtered = computed(() => this.category() === 'All' ? this.products() : this.products().filter(p => p.category === this.category()));
  readonly cartCount = computed(() => this.cart().reduce((sum, i) => sum + i.quantity, 0));
  readonly cartDetails = computed(() => this.cart().map(i => ({ ...i, product: this.products().find(p => p.id === i.productId) })).filter(i => !!i.product));
  readonly subtotal = computed(() => this.cartDetails().reduce((sum, i) => sum + i.quantity * i.product!.price, 0));
  phone = '';
  code = '';
  editingId: number | null = null;
  productForm: ProductInput = this.blankProduct();

  constructor() {
    effect(() => {
      if (typeof document === 'undefined') return;
      document.documentElement.dataset['theme'] = this.theme();
      localStorage.setItem('ewear_theme', this.theme());
    });
    effect(() => {
      const open = this.cartOpen() || this.authOpen() || this.staffOpen();
      if (typeof document === 'undefined') return;
      if (open && !this.dialogWasOpen) {
        this.returnFocus = document.activeElement as HTMLElement;
        document.body.style.overflow = 'hidden';
        setTimeout(() => this.activeDialog()?.focus(), 0);
      } else if (!open && this.dialogWasOpen) {
        document.body.style.overflow = '';
        setTimeout(() => this.returnFocus?.focus(), 0);
      }
      this.dialogWasOpen = open;
    });
  }

  private activeDialog(): HTMLElement | null {
    return document.querySelector<HTMLElement>('.staff-drawer[role="dialog"], .auth-dialog[role="dialog"], .drawer[role="dialog"]');
  }

  @HostListener('document:keydown', ['$event'])
  onDialogKeydown(event: KeyboardEvent): void {
    const dialog = this.activeDialog();
    if (!dialog) return;
    if (event.key === 'Escape') {
      event.preventDefault();
      if (this.staffOpen()) this.staffOpen.set(false);
      else if (this.authOpen()) this.authOpen.set(false);
      else this.cartOpen.set(false);
      return;
    }
    if (event.key !== 'Tab') return;
    const focusable = Array.from(dialog.querySelectorAll<HTMLElement>('button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled])')).filter(el => el.getClientRects().length > 0);
    if (!focusable.length) { event.preventDefault(); dialog.focus(); return; }
    const first = focusable[0], last = focusable[focusable.length - 1];
    if (event.shiftKey && (document.activeElement === dialog || document.activeElement === first || !dialog.contains(document.activeElement))) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && (document.activeElement === last || !dialog.contains(document.activeElement))) { event.preventDefault(); first.focus(); }
  }

  ngOnInit(): void {
    this.loadProducts();
    const token = sessionStorage.getItem('ewear_token');
    if (token) this.http.get<User>('/api/auth/me', { headers: { Authorization: `Bearer ${token}` } }).subscribe({ next: user => this.user.set(user), error: () => sessionStorage.removeItem('ewear_token') });
  }
  loadProducts(): void {
    this.http.get<Product[]>('/api/products').subscribe({ next: products => { this.products.set(products); this.cart.update(items => items.filter(i => products.some(p => p.id === i.productId && p.stock > 0))); this.saveCart(); this.loading.set(false); }, error: () => { this.error.set('The collection could not load. Check that the API is running and refresh.'); this.loading.set(false); } });
  }
  setCategory(category: Category): void { this.category.set(category); this.mobileMenuOpen.set(false); document.getElementById('collection')?.scrollIntoView({ behavior: 'smooth' }); }
  toggleTheme(): void { this.theme.update(theme => theme === 'light' ? 'dark' : 'light'); }
  addToCart(product: Product): void {
    if (product.stock <= 0) return;
    const currentQuantity = this.cart().find(item => item.productId === product.id)?.quantity ?? 0;
    if (currentQuantity >= product.stock) { this.showNotice(`All available ${product.name} pieces are in your bag.`, false, 'info'); return; }
    this.cart.update(items => { const existing = items.find(i => i.productId === product.id); return existing ? items.map(i => i.productId === product.id ? { ...i, quantity: Math.min(i.quantity + 1, product.stock) } : i) : [...items, { productId: product.id, quantity: 1 }]; });
    this.saveCart();
    this.recentlyAddedId.set(product.id);
    if (this.addedTimer) clearTimeout(this.addedTimer);
    this.addedTimer = setTimeout(() => this.recentlyAddedId.set(null), 1800);
    this.showNotice(`${product.name} added to your bag.`, true);
  }
  changeQuantity(productId: number, change: number): void {
    const product = this.products().find(p => p.id === productId);
    this.cart.update(items => items.map(i => i.productId === productId ? { ...i, quantity: Math.max(0, Math.min(i.quantity + change, product?.stock ?? 0)) } : i).filter(i => i.quantity > 0)); this.saveCart();
  }
  remove(productId: number): void { this.cart.update(items => items.filter(i => i.productId !== productId)); this.saveCart(); }
  private saveCart(): void { localStorage.setItem('ewear_cart', JSON.stringify(this.cart())); }
  private readCart(): CartItem[] { try { const data = JSON.parse(localStorage.getItem('ewear_cart') ?? '[]'); return Array.isArray(data) ? data.filter(i => Number.isInteger(i.productId) && Number.isInteger(i.quantity) && i.quantity > 0) : []; } catch { return []; } }
  private readTheme(): 'light' | 'dark' { return typeof localStorage !== 'undefined' && localStorage.getItem('ewear_theme') === 'dark' ? 'dark' : 'light'; }
  private showNotice(message: string, showBagLink = false, kind: 'success' | 'info' = 'success'): void {
    if (this.noticeTimer) clearTimeout(this.noticeTimer);
    this.notice.set(message);
    this.noticeKind.set(kind);
    this.noticeBagLink.set(showBagLink);
    this.noticeTimer = setTimeout(() => { this.notice.set(''); this.noticeBagLink.set(false); }, 3800);
  }

  openAuth(): void { this.error.set(''); this.authStep.set('phone'); this.authOpen.set(true); }
  requestCode(): void {
    this.busy.set(true); this.error.set('');
    this.http.post<{ developmentCode: string | null }>('/api/auth/request-code', { phone: this.phone.trim() }).subscribe({ next: result => { this.developmentCode.set(result.developmentCode ?? ''); this.authStep.set('code'); this.busy.set(false); }, error: e => this.fail(e) });
  }
  verifyCode(): void {
    this.busy.set(true); this.error.set('');
    this.http.post<{ token: string; user: User; isNew: boolean }>('/api/auth/verify', { phone: this.phone.trim(), code: this.code.trim() }).subscribe({
      next: result => { sessionStorage.setItem('ewear_token', result.token); this.user.set(result.user); this.authOpen.set(false); this.busy.set(false); this.code = ''; this.showNotice(result.isNew ? 'Welcome to Ewear. Your account is ready.' : 'Welcome back to Ewear.'); }, error: e => this.fail(e),
    });
  }
  signOut(): void { sessionStorage.removeItem('ewear_token'); this.user.set(null); this.staffOpen.set(false); this.showNotice('You have signed out.', false, 'info'); }
  openStaff(): void { if (this.user()?.role === 'staff') { this.error.set(''); this.staffOpen.set(true); } }
  edit(product: Product): void { this.editingId = product.id; this.productForm = { name: product.name, category: product.category, description: product.description, price: product.price, imageUrl: product.imageUrl, color: product.color, stock: product.stock, featured: product.featured }; }
  newProduct(): void { this.editingId = null; this.productForm = this.blankProduct(); }
  uploadImage(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    const token = sessionStorage.getItem('ewear_token');
    if (!file || !token) return;
    if (file.size > 5_000_000) { this.error.set('Choose an image under 5 MB.'); return; }
    const form = new FormData(); form.append('file', file);
    this.uploading.set(true); this.error.set('');
    this.http.post<{ imageUrl: string }>('/api/staff/uploads', form, { headers: { Authorization: `Bearer ${token}` } }).subscribe({
      next: result => { this.productForm.imageUrl = result.imageUrl; this.uploading.set(false); },
      error: e => { this.uploading.set(false); this.fail(e); },
    });
  }
  saveProduct(): void {
    const token = sessionStorage.getItem('ewear_token'); if (!token) return;
    this.busy.set(true); this.error.set(''); const url = this.editingId ? `/api/staff/products/${this.editingId}` : '/api/staff/products';
    const options = { headers: { Authorization: `Bearer ${token}` } };
    const call = this.editingId ? this.http.put<Product>(url, this.productForm, options) : this.http.post<Product>(url, this.productForm, options);
    call.subscribe({ next: () => { this.busy.set(false); this.newProduct(); this.loadProducts(); this.showNotice('Product saved.'); }, error: e => this.fail(e) });
  }
  deleteProduct(product: Product): void {
    if (!confirm(`Remove ${product.name} from the store?`)) return;
    const token = sessionStorage.getItem('ewear_token'); if (!token) return;
    this.http.delete(`/api/staff/products/${product.id}`, { headers: { Authorization: `Bearer ${token}` } }).subscribe({ next: () => { this.loadProducts(); this.remove(product.id); this.showNotice(`${product.name} removed from the collection.`); }, error: e => this.fail(e) });
  }
  private blankProduct(): ProductInput { return { name: '', category: 'Shirts', description: '', price: 0, imageUrl: '/assets/shirt.png', color: '', stock: 0, featured: false }; }
  private fail(response: HttpErrorResponse): void { this.busy.set(false); this.error.set(response.error?.error ?? 'Something went wrong. Please try again.'); }
}
