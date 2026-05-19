# Copilot Instructions

## Project Overview

Mock e-commerce site used for coding exercises. Full-stack monorepo:
- **Frontend**: React 19 + TypeScript + Vite (in `src/frontend/`)
- **Backend**: ASP.NET Core Web API on .NET 10 (in `src/backend/`)
- **Tests**: Vitest (frontend, at `test/frontend/`) + xUnit (backend, at `test/backend/`)

---

## Repository Structure

```
/
├── package.json               # Root: workspace config, runs vitest
├── vitest.config.ts           # Frontend test config (jsdom, test/frontend/**)
├── tsconfig.json              # Root TS config (references frontend + tests)
├── src/
│   ├── frontend/              # React app (Vite workspace package)
│   │   ├── src/
│   │   │   ├── api/index.ts   # HTTP client functions (fetchProducts, addToCart, etc.)
│   │   │   ├── types/index.ts # Shared TypeScript types
│   │   │   ├── hooks/         # Custom React hooks (useProducts)
│   │   │   ├── components/    # Header, HeroBanner, ProductCard, ProductList
│   │   │   └── App.tsx        # Root component, cart state management
│   │   └── vite.config.ts     # Dev proxy: /api → http://localhost:5063
│   └── backend/
│       └── MockEcommerce.Api/
│           ├── Program.cs         # DI registration, CORS, middleware
│           ├── Endpoints/         # ProductEndpoints.cs, CartEndpoints.cs
│           ├── Models/            # Product.cs, CartItem.cs
│           └── Services/          # IProductService, MockProductService, ICartService, InMemoryCartService
└── test/
    ├── frontend/              # Vitest + React Testing Library tests
    └── backend/
        └── MockEcommerce.Api.Tests/  # xUnit integration + unit tests
```

---

## Backend (.NET 10 / ASP.NET Core)

### Models
- **`Product`**: `Id`, `Name`, `Description`, `Price` (decimal), `Category`, `Stock` (int), `ImageUrl`
- **`CartItem`**: `ProductId`, `ProductName`, `UnitPrice` (decimal), `Quantity` (int), computed `TotalPrice`

### Product Catalog (MockProductService — 5 hardcoded products)

| Id | Name | Price | Category | Stock |
|----|------|-------|----------|-------|
| 1 | Wireless Headphones | $79.99 | Electronics | 25 |
| 2 | Running Shoes | $59.99 | Footwear | 40 |
| 3 | Water Bottle | $24.99 | Accessories | 100 |
| 4 | Mechanical Keyboard | $109.99 | Electronics | 15 |
| 5 | Yoga Mat | $34.99 | Fitness | 60 |

### Services
- **`IProductService`** / **`MockProductService`**: Returns the static list above; registered as Singleton
- **`ICartService`** / **`InMemoryCartService`**: Thread-safe in-memory cart using `Lock`; registered as Singleton; **all methods currently throw `NotImplementedException`** — this is intentional, meant to be implemented as an exercise

### Endpoints
All endpoints are minimal API style using `MapGroup()`:

| Method | Route | Handler | Status |
|--------|-------|---------|--------|
| GET | `/api/products` | `GetAll` | ✅ Implemented |
| GET | `/api/products/{id}` | `GetById` | ✅ Implemented |
| GET | `/api/cart` | `GetCart` | ❌ NotImplemented |
| POST | `/api/cart` | `AddToCart` | ❌ NotImplemented |
| DELETE | `/api/cart/{productId}` | `RemoveFromCart` | ❌ NotImplemented |
| DELETE | `/api/cart` | `ClearCart` | ❌ NotImplemented |

### AddToCartRequest
```csharp
record AddToCartRequest(int ProductId, int Quantity);
```

### Run backend
```bash
cd src/backend/MockEcommerce.Api
dotnet run
# Runs on http://localhost:5063
```

### Run backend tests
```bash
cd test/backend/MockEcommerce.Api.Tests
dotnet test
```

---

## Frontend (React 19 + TypeScript + Vite)

### TypeScript Types (`src/frontend/src/types/index.ts`)
```typescript
interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  category: string;
  stock: number;
  imageUrl: string;
}

interface AddToCartRequest {
  productId: number;
  quantity: number;
}
```

### API Layer (`src/frontend/src/api/index.ts`)
- Base URL: `/api` (proxied to `http://localhost:5063` in dev)
- `fetchProducts()` → GET `/api/products`
- `fetchProductById(id: number)` → GET `/api/products/{id}`
- `addToCart(request: AddToCartRequest)` → POST `/api/cart`

### Custom Hooks
- **`useProducts()`**: Fetches products on mount, returns `{ products, loading, error }`

### Components
- **`Header`**: Shop name, nav links, cart button with item count
- **`HeroBanner`**: Promotional banner
- **`ProductList`**: Maps products array → `ProductCard` components
- **`ProductCard`**: Displays product details; "Add to Cart" button disabled when `stock === 0`

### App State (`App.tsx`)
- `cartItemCount`: Total items added to cart
- `cartMessage`: Success/error notification string
- Notification auto-dismisses after 3 seconds (managed with `useRef` + `useEffect`)

### Run frontend
```bash
cd src/frontend
npm run dev
# Runs on http://localhost:5173
```

---

## Testing

### Frontend tests (Vitest + React Testing Library)
- Config: `vitest.config.ts` at root
- Test files: `test/frontend/**/*.{test,spec}.{ts,tsx}`
- Environment: jsdom
- Setup file: `src/frontend/src/test-setup.ts` (imports `@testing-library/jest-dom`)
- Run: `npm test` or `npm run test:frontend` from repo root

### Backend tests (xUnit + WebApplicationFactory)
- Integration tests use `WebApplicationFactory<Program>` for full HTTP pipeline
- Unit tests test services directly
- Run: `dotnet test` from `test/backend/MockEcommerce.Api.Tests/`

---

## Key Conventions

- Minimal API endpoints in .NET (no controllers), grouped with `MapGroup()`
- Service interfaces registered in DI; concrete types resolved at runtime
- Frontend never directly calls backend URLs — always via `/api` proxy
- CORS allows `http://localhost:5173` (frontend dev origin)
- Nullable enabled on backend; strict mode enabled on frontend TypeScript
- All cart service methods intentionally unimplemented — primary exercise target
- Images use `placehold.co` as placeholder URLs
- `InMemoryCartService` is a Singleton and shares cart state across all sessions (noted as temporary)
