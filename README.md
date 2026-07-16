# Product Management API — Backend (C# / ASP.NET Core 8)

A REST API for product management and authentication built with **C# / .NET 8 / ASP.NET Core**.
It implements all features required by the recruitment test specification:

- 🔐 JWT-based authentication (register, login, token refresh)
- 📦 Full product CRUD with search, category filter and pagination
- ✅ Request validation with consistent JSON error envelopes
- 🛡️ Authorization for write endpoints (POST / PUT / DELETE)
- ⏱️ Rate limiting (1 request / 5 s for writes, 3 requests / 60 s for auth)
- ⚡ In-memory response caching for the GET endpoints
- 🌐 CORS enabled for any domain
- 📖 Swagger / OpenAPI documentation
- 🐳 Docker & docker-compose support

---

## 🛠️ Tech Stack

| Concern              | Choice                                    |
|----------------------|-------------------------------------------|
| Language             | C# 12                                     |
| Framework            | ASP.NET Core 8 Web API                    |
| ORM                  | Entity Framework Core 8 (InMemory provider)|
| Auth                 | JWT Bearer tokens (symmetric HMAC-SHA256) |
| Password hashing     | BCrypt.Net-Next                           |
| Rate limiting        | `Microsoft.AspNetCore.RateLimiting`       |
| Caching              | `Microsoft.Extensions.Caching.Memory`     |
| API docs             | Swashbuckle / Swagger UI                  |
| Containerization     | Docker, docker-compose                    |

> The InMemory database provider is used so the API runs out of the box with
> zero external dependencies. To use a real database, swap the provider in
> `Program.cs` (e.g. `UseSqlServer`, `UseNpgsql`, `UseMySql`) and run EF
> migrations.

---

## 📁 Project Structure

```
ProductApi/
├── Controllers/
│   ├── AuthController.cs          # /api/auth (register, login, refresh)
│   └── ProductsController.cs      # /api/products (CRUD)
├── Data/
│   ├── AppDbContext.cs            # EF Core DbContext
│   └── DbSeeder.cs                # Seeds sample products
├── DTOs/
│   ├── AuthDtos.cs                # Auth request/response DTOs
│   ├── ProductDtos.cs             # Product request/response DTOs
│   └── ApiResponse.cs             # Standard response envelopes
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs # Global exception handler
│   └── CustomValidationFilter.cs    # Consistent validation errors
├── Models/
│   ├── Product.cs                 # Product entity (audit fields)
│   ├── User.cs                    # User entity
│   └── RefreshToken.cs            # Refresh token entity
├── Services/
│   ├── AuthService.cs             # Register / login logic
│   ├── TokenService.cs            # JWT + refresh token generation
│   ├── ProductsService.cs         # Product CRUD + cache logic
│   └── CurrentUserResolver.cs     # Extract user from JWT claims
├── Properties/launchSettings.json
├── appsettings.json               # JWT & rate limit config
├── Dockerfile
├── docker-compose.yml
├── Program.cs                     # App composition root
└── ProductApi.csproj
```

---

## 🚀 Running the Application

### Option A — Run with the .NET SDK

```bash
# Restore & build
dotnet restore
dotnet build

# Run (development)
dotnet run
```

The API will be available at `http://localhost:5262` (development) or the port
specified in `launchSettings.json`. Swagger UI is at `/swagger`.

### Option B — Run with Docker (recommended for delivery)

```bash
# Build and start the container
docker compose up --build

# Or, build and run manually
docker build -t product-api .
docker run -p 8080:8080 product-api
```

The API will be available at `http://localhost:8080`. Swagger UI is at
`http://localhost:8080/swagger`.

---

## 📖 API Documentation

### Base URL

```
http://localhost:8080
```

Interactive Swagger UI: `http://localhost:8080/swagger`

### Standard Response Envelopes

**Success (single object)**
```json
{
  "success": true,
  "message": "Product created successfully",
  "data": { ... }
}
```

**Success (paginated list)**
```json
{
  "data": [ ... ],
  "page": 1,
  "limit": 10,
  "total": 8,
  "total_pages": 1,
  "has_next": false,
  "has_prev": false
}
```

**Error**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "Title": ["title is required"],
    "Images": ["images must contain at least 1 image url"]
  }
}
```

---

### 🔐 Authentication

#### POST `/api/auth/register`
Register a new user. Rate limited: **3 requests / 60 seconds**.

**Request body**
```json
{
  "username": "john_doe",
  "password": "supersecret",
  "password_confirmation": "supersecret"
}
```

**Response 200**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "authentication_token": "eyJhbGciOiJIUzI1NiIs...",
    "refresh_token": "g5MGnuPksQjNO9yv4Wvh93mnZMIP...",
    "token_type": "Bearer",
    "expires_in": 900
  }
}
```

#### POST `/api/auth/login`
Authenticate and receive tokens. Rate limited: **3 requests / 60 seconds**.

**Request body**
```json
{
  "username": "john_doe",
  "password": "supersecret"
}
```

**Response 200** — same envelope as register.

#### POST `/api/auth/refresh`
Exchange a refresh token for a new access token + rotated refresh token.

**Request body**
```json
{
  "refresh_token": "g5MGnuPksQjNO9yv4Wvh93mnZMIP..."
}
```

---

### 📦 Products

All write endpoints require the header:
```
Authorization: Bearer <authentication_token>
```

#### GET `/api/products`
Returns all products with optional filtering and pagination.

**Query parameters**
| Parameter | Type   | Description                              |
|-----------|--------|------------------------------------------|
| search    | string | Searches product titles (case-insensitive) |
| category  | string | Filters by exact category match          |
| page      | int    | Page number (default 1)                  |
| limit     | int    | Items per page (default 10, max 100)     |

**Example**
```
GET /api/products?search=watch&category=Electronics&limit=10&page=1
```

#### GET `/api/products/{id}`
Returns a single product. **404** if not found.

#### POST `/api/products`
Creates a new product. Rate limited: **1 request / 5 seconds**.

**Request body** (required: `title`, `price`, `category`, `images` with ≥1 entry)
```json
{
  "title": "Awesome T-Shirt",
  "price": 99.99,
  "description": "High-quality cotton t-shirt",
  "category": "Clothes",
  "images": ["https://placeimg.com/640/480/any"]
}
```

**Response 201** — the created product including audit fields
(`created_at`, `created_by`, `created_by_id`, `updated_*`).

#### PUT `/api/products/{id}`
Updates a product. All fields are optional (partial update). Rate limited:
**1 request / 5 seconds**. **404** if the product does not exist.

#### DELETE `/api/products/{id}`
Deletes a product. Rate limited: **1 request / 5 seconds**. **404** if not found.

---

## 🛡️ Security & Audit Features

- **Passwords** are hashed with BCrypt (work factor 12) and never returned.
- **JWT access tokens** are signed with HMAC-SHA256 and expire after 15 minutes.
- **Refresh tokens** are stored in the database, rotated on each use, and
  can be revoked.
- **Audit fields** (`created_by`, `created_by_id`, `updated_by`,
  `updated_by_id`) are populated automatically from the authenticated user's
  JWT claims on every write operation.
- **CORS** allows requests from any origin, enabling web apps on different
  domains to interact with the API.

---

## ⚙️ Configuration (`appsettings.json`)

```json
{
  "Jwt": {
    "Key": "SuperSecretProductApiKey_2025_WithAtLeast32Chars!!",
    "Issuer": "ProductApi",
    "Audience": "ProductApi",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

> ⚠️ In production, override `Jwt:Key` with a strong secret via an
> environment variable or a secrets manager. Never commit real secrets.

---

## 🧪 Quick Test (cURL)

```bash
BASE=http://localhost:8080

# 1. Register
curl -X POST $BASE/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"john_doe","password":"supersecret","password_confirmation":"supersecret"}'

# 2. Login
TOKEN=$(curl -s -X POST $BASE/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"john_doe","password":"supersecret"}' \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['authentication_token'])")

# 3. List products
curl $BASE/api/products?limit=5

# 4. Create a product (auth required)
curl -X POST $BASE/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Mug","price":12.5,"category":"Home","images":["https://placeimg.com/640/480/any"]}'

# 5. Update a product (auth required)
curl -X PUT $BASE/api/products/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"price":89.99}'

# 6. Delete a product (auth required)
curl -X DELETE $BASE/api/products/1 -H "Authorization: Bearer $TOKEN"
```

---

## ✅ Test Coverage Summary

All endpoints have been verified end-to-end:

| Endpoint                  | Auth | Rate Limit   | Tested Scenarios                            |
|---------------------------|------|--------------|---------------------------------------------|
| GET /api/products         | ❌   | —            | filter, search, pagination, cache           |
| GET /api/products/{id}    | ❌   | —            | found, 404 not found, cache                 |
| POST /api/products        | ✅   | 1/5s         | valid, validation 400, unauthorized 401, 429|
| PUT /api/products/{id}    | ✅   | 1/5s         | update, 404, audit fields, 429              |
| DELETE /api/products/{id} | ✅   | 1/5s         | delete, 404, 429                            |
| POST /api/auth/register   | ❌   | 3/60s        | valid, mismatch 400, duplicate 400, 429     |
| POST /api/auth/login      | ❌   | 3/60s        | valid, invalid creds 401, 429               |
| POST /api/auth/refresh    | ❌   | —            | valid rotation, invalid 401                 |

---

## 📜 License

Provided as part of a recruitment coding test. Free to use for evaluation purposes.
