import { useState, useRef, useEffect } from 'react';
import type { Product, CartItem } from './types';
import { Header } from './components/Header';
import { HeroBanner } from './components/HeroBanner';
import { ProductList } from './components/ProductList';
import { CartPanel } from './components/CartPanel';
import { useProducts } from './hooks/useProducts';
import { addToCart, fetchCart, updateCartItem, removeFromCart, clearCart } from './api';
import './App.css';

export function App() {
  const { products, loading, error } = useProducts();
  const [cartMessage, setCartMessage] = useState<string | null>(null);
  const [cartItems, setCartItems] = useState<CartItem[]>([]);
  const [isCartOpen, setIsCartOpen] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const cartItemCount = cartItems.reduce((sum, i) => sum + i.quantity, 0);

  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, []);

  async function loadCart() {
    const items = await fetchCart();
    setCartItems(items);
  }

  function showMessage(message: string) {
    setCartMessage(message);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => setCartMessage(null), 3000);
  }

  async function handleAddToCart(product: Product) {
    try {
      await addToCart({ productId: product.id, quantity: 1 });
      await loadCart();
      showMessage(`"${product.name}" added to cart!`);
    } catch {
      showMessage('Failed to add item to cart.');
    }
  }

  async function handleOpenCart() {
    setIsCartOpen(true);
    await loadCart();
  }

  async function handleUpdateQuantity(productId: number, quantity: number) {
    try {
      await updateCartItem(productId, quantity);
      await loadCart();
    } catch {
      showMessage('Failed to update cart item.');
      await loadCart();
    }
  }

  async function handleRemoveItem(productId: number) {
    try {
      await removeFromCart(productId);
      await loadCart();
    } catch {
      showMessage('Failed to remove item from cart.');
      await loadCart();
    }
  }

  async function handleClearCart() {
    try {
      await clearCart();
      await loadCart();
    } catch {
      showMessage('Failed to clear cart.');
      await loadCart();
    }
  }

  return (
    <div className="app">
      <Header cartItemCount={cartItemCount} onCartClick={handleOpenCart} />
      <HeroBanner />

      <main className="app__main">
        <h1 className="app__section-heading">Our products</h1>

        {cartMessage && (
          <div className="app__notification" role="status">
            {cartMessage}
          </div>
        )}

        {loading && <p className="app__loading">Loading products…</p>}
        {error && <p className="app__error">Error: {error}</p>}
        {!loading && !error && (
          <ProductList products={products} onAddToCart={handleAddToCart} />
        )}
      </main>

      <CartPanel
        isOpen={isCartOpen}
        onClose={() => setIsCartOpen(false)}
        cartItems={cartItems}
        onUpdateQuantity={handleUpdateQuantity}
        onRemoveItem={handleRemoveItem}
        onClearCart={handleClearCart}
      />
    </div>
  );
}
