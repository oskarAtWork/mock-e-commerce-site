# Cart Feature — Implementation Plan

## Phases

### Phase 0 — Backend models

**0.1 — Add `UpdateCartItemRequest` record**
- Add `record UpdateCartItemRequest(int Quantity)` to `src/backend/MockEcommerce.Api/Models/`.
- This must exist before the service interface or endpoint handler reference it.
- File: `src/backend/MockEcommerce.Api/Models/UpdateCartItemRequest.cs` (new file)

---

### Phase 1 — Backend service layer

**1.1 — Extend `ICartService` interface**
- Add `CartItem? Update(int productId, int quantity)` method signature with XML doc comment.
- File: `src/backend/MockEcommerce.Api/Services/ICartService.cs`

**1.2 — Implement `InMemoryCartService`**
- `GetAll()`: acquire lock, return `_cart.ToList()`.
- `GetByProductId(int)`: acquire lock, find and return item or `null`.
- `Add(CartItem)`: acquire lock; if item with matching `ProductId` exists increment its `Quantity`, otherwise add the new item; return the item.
- `Remove(int)`: acquire lock; find and remove item; return `true`/`false`.
- `Clear()`: acquire lock; call `_cart.Clear()`.
- `Update(int, int)`: acquire lock; find item; if found set `Quantity`, return item; if not found return `null`.
- File: `src/backend/MockEcommerce.Api/Services/InMemoryCartService.cs`

---

### Phase 2 — Backend endpoints


**2.1 — Implement existing cart endpoint handlers**
- `GetCart`: call `cartService.GetAll()`, return `TypedResults.Ok(items)`.
- `AddToCart`:
  - If `request.Quantity < 1` (covers zero and negatives) → `TypedResults.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Quantity must be at least 1."] })` → `400`.
  - If product not found → `TypedResults.NotFound("Product not found")`.
  - Compute `newTotal = (existingItem?.Quantity ?? 0) + request.Quantity`; if `newTotal > 5` (exactly 5 is allowed) → `TypedResults.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Cannot exceed 5 of any single item."] })` → `400`.
  - Call `cartService.Add(new CartItem { ProductId = ..., ProductName = product.Name, UnitPrice = product.Price, Quantity = request.Quantity })`.
  - Return `TypedResults.Created($"/api/cart/{productId}", item)` if new; `TypedResults.Ok(item)` if updated.
- `RemoveFromCart`: if `cartService.Remove(productId)` returns `false` → `TypedResults.NotFound()`; else `TypedResults.NoContent()`.
- `ClearCart`: call `cartService.Clear()`, return `TypedResults.NoContent()`.
- File: `src/backend/MockEcommerce.Api/Endpoints/CartEndpoints.cs`

**2.2 — Add `PUT /api/cart/{productId}` endpoint**
- Return type: `Results<Ok<CartItem>, NotFound, NotFound<string>, ValidationProblem>`.
- If `request.Quantity < 1` (zero or negative) → `TypedResults.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Quantity must be at least 1."] })` → `400`.
- If `request.Quantity > 5` → `TypedResults.ValidationProblem(new Dictionary<string,string[]> { ["quantity"] = ["Quantity cannot exceed 5."] })` → `400`. **Quantity == 5 must pass this check.**
- If product not found in catalogue → `TypedResults.NotFound("Product not found")`.
- Call `cartService.Update(productId, request.Quantity)`; if result is `null` (item not in cart) → `TypedResults.NotFound()`.
- Otherwise → `TypedResults.Ok(updatedItem)`.
- Register in `MapCartEndpoints`: `group.MapPut("/{productId:int}", UpdateCartItem).WithName("UpdateCartItem").WithSummary("Sets the absolute quantity of an item already in the cart.")`.
- File: `src/backend/MockEcommerce.Api/Endpoints/CartEndpoints.cs`

---

### Phase 3 — Backend tests


**3.1 — `InMemoryCartService` unit tests**
- Test `GetAll` returns empty list initially.
- Test `Add` inserts a new item.
- Test `Add` increments quantity on duplicate product.
- Test `GetByProductId` returns correct item or null.
- Test `Remove` returns `true` and removes item; returns `false` for unknown ID.
- Test `Clear` empties the cart.
- Test `Update` sets quantity and returns item; returns `null` for unknown ID.
- File: `test/backend/MockEcommerce.Api.Tests/Services/InMemoryCartServiceTests.cs` (new file)

**3.2 — Cart endpoint integration tests**
- `GET /api/cart` returns `200` with empty array initially.
- `POST /api/cart` with valid product returns `201` and correct `CartItem`.
- `POST /api/cart` with same product again returns `200` with incremented quantity.
- `POST /api/cart` with unknown product returns `404`.
- `POST /api/cart` with `quantity=0` returns `400`.
- `POST /api/cart` with `quantity=-1` returns `400`.
- `POST /api/cart` with `quantity=5` (first add) returns `201` — boundary must be accepted.
- `POST /api/cart` with quantity that would push total above 5 returns `400`.
- `POST /api/cart` with quantity that results in exactly 5 total returns `201` or `200` — boundary must be accepted.
- `PUT /api/cart/{productId}` with valid data returns `200` and updated item.
- `PUT /api/cart/{productId}` with `quantity=5` returns `200` — boundary must be accepted.
- `PUT /api/cart/{productId}` with `quantity=6` returns `400`.
- `PUT /api/cart/{productId}` with `quantity=0` returns `400`.
- `PUT /api/cart/{productId}` with `quantity=-1` returns `400`.
- `PUT /api/cart/{productId}` for item not in cart returns `404`.
- `PUT /api/cart/{productId}` for unknown product returns `404`.
- `DELETE /api/cart/{productId}` returns `204`; subsequent GET omits item.
- `DELETE /api/cart/{productId}` for unknown ID returns `404`.
- `DELETE /api/cart` returns `204`; subsequent GET returns empty array.
- File: `test/backend/MockEcommerce.Api.Tests/Endpoints/CartEndpointTests.cs` (new file)

---

### Phase 4 — Frontend types and API layer


**4.1 — Add `CartItem` and `UpdateCartItemRequest` types**
- Add `CartItem` interface (productId, productName, unitPrice, quantity, totalPrice) to `src/frontend/src/types/index.ts`.
- Add `UpdateCartItemRequest` interface (quantity) to the same file.
- Remove the local `CartItem` interface currently defined in `src/frontend/src/api/index.ts`.

**4.2 — Extend the API layer**
- Import new types.
- Add `fetchCart(): Promise<CartItem[]>` — `GET /api/cart`.
- Add `updateCartItem(productId: number, quantity: number): Promise<CartItem>` — `PUT /api/cart/{productId}`.
- Add `removeFromCart(productId: number): Promise<void>` — `DELETE /api/cart/{productId}`.
- Add `clearCart(): Promise<void>` — `DELETE /api/cart`.
- File: `src/frontend/src/api/index.ts`

---

### Phase 5 — Frontend cart panel component


**5.1 — Create `CartPanel` component**
- Props (all required):
  ```typescript
  isOpen: boolean
  onClose: () => void
  cartItems: CartItem[]
  onUpdateQuantity: (productId: number, quantity: number) => void
  onRemoveItem: (productId: number) => void
  onClearCart: () => void
  ```
- If `isOpen` is `false`, return `null` (unmount entirely).
- Render: semi-transparent backdrop `<div>` + `<aside role="dialog" aria-modal="true" aria-label="Shopping cart">`.
- Backdrop click → `onClose()`.
- `useEffect`: add `keydown` listener; if key is `"Escape"` call `onClose()`. Clean up on unmount.
- Empty state (`cartItems.length === 0`): render `<p>Your cart is empty.</p>`.
- Non-empty state:
  - `<ul>` of items; each `<li>` contains: product name, formatted unit price, `<input type="number" min="1" max="5">` wired to `onUpdateQuantity`, formatted line total, `<button aria-label="Remove {productName} from cart">` wired to `onRemoveItem`.
  - Subtotal line: `cartItems.reduce((s, i) => s + i.totalPrice, 0)` formatted as currency.
  - "Clear cart" `<button>` wired to `onClearCart()`.
- Root element has `aria-modal="true"`; close button present with visible label.
- Files:
  - `src/frontend/src/components/CartPanel/CartPanel.tsx`
  - `src/frontend/src/components/CartPanel/index.ts`

---

### Phase 6 — App state and Header integration


**6.1 — Update `App.tsx`**
- Add `isCartOpen` state (`false` by default).
- Add `cartItems` state (`CartItem[]`, empty array by default).
- Replace the `cartItemCount` increment approach: derive `cartItemCount` as `cartItems.reduce((sum, i) => sum + i.quantity, 0)`.
- Add `loadCart()` async helper that calls `fetchCart()` and sets `cartItems`.
- Update `handleAddToCart` to call `loadCart()` after a successful add (instead of manually incrementing the counter).
- Add `handleOpenCart` that sets `isCartOpen = true` and calls `loadCart()`.
- Add `handleUpdateQuantity(productId, quantity)` — calls `updateCartItem`, then `loadCart()`.
- Add `handleRemoveItem(productId)` — calls `removeFromCart`, then `loadCart()`.
- Add `handleClearCart()` — calls `clearCart()`, then `loadCart()`.
- Render `<CartPanel>` with appropriate props.
- Pass `onCartClick={handleOpenCart}` to `<Header>`.

**6.2 — Wire the existing cart icon button in `Header` to open the cart panel**
- The cart button already exists in `Header.tsx` with class `header__cart-button`. It currently has no `onClick` handler.
- Add `onCartClick: () => void` to `HeaderProps`.
- Add `onClick={onCartClick}` to the existing `<button className="header__cart-button">` element.
- In `App.tsx`, pass `onCartClick={handleOpenCart}` to `<Header>`.
- File: `src/frontend/src/components/Header/Header.tsx`

---

### Phase 7 — Frontend tests


**7.1 — `CartPanel` component tests**
- Renders nothing when `isOpen` is false.
- Shows empty state message when `cartItems` is empty.
- Renders item list with correct names, prices, and quantities.
- Calls `onRemoveItem` with correct product ID when remove button clicked.
- Calls `onClearCart` when clear button clicked.
- Calls `onUpdateQuantity` with new value when quantity input changed.
- Calls `onClose` on Escape key press.
- File: `test/frontend/components/CartPanel/CartPanel.test.tsx`

**7.2 — Update `Header` tests**
- Existing tests should pass with the addition of a no-op `onCartClick` prop.
- Add test: cart button click invokes `onCartClick`.
- File: `test/frontend/components/Header/Header.test.tsx`

**7.3 — Update `App` tests**
- After a successful `addToCart` POST, `GET /api/cart` is called and `cartItemCount` equals the sum of quantities returned.
- `cartItemCount` is `0` when `cartItems` is empty.
- File: `test/frontend/App.test.tsx`

---

## Dependency order

```
Phase 0 (models) → Phase 1 (service) → Phase 2 (endpoints) → Phase 3 (backend tests)
                                      ↘
Phase 4 (frontend types/API) → Phase 5 (CartPanel) → Phase 6 (App/Header) → Phase 7 (frontend tests)
```

- Phase 0 must complete before Phase 1 (service references the model) and Phase 4 (frontend mirrors the model shape).
- Phases 1–3 (backend) and Phases 4–7 (frontend) can proceed in parallel once Phase 0 is done.
- Phase 3 (backend tests) requires Phases 0–2 to be complete.
- Phase 7 (frontend tests) requires Phases 4–6 to be complete.
