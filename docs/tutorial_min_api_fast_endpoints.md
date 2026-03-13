# Complete Tutorial: Building a .NET 10 Minimal API with FastEndpoints, PostgreSQL, and WebVella.Database

This tutorial provides step-by-step instructions for creating a production-ready .NET 10 minimal API solution using FastEndpoints, PostgreSQL, and WebVella.Database for data access and migrations. It also covers setting up a unit test project with automatic Docker container management.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Part 1: Create the Solution Structure](#part-1-create-the-solution-structure)
3. [Part 2: Configure the API Project](#part-2-configure-the-api-project)
4. [Part 3: Create Entity Models](#part-3-create-entity-models)
5. [Part 4: Create Database Migrations](#part-4-create-database-migrations)
6. [Part 5: Create FastEndpoints](#part-5-create-fastendpoints)
7. [Part 6: Configure and Run the Application](#part-6-configure-and-run-the-application)
8. [Part 7: Create the Unit Test Project](#part-7-create-the-unit-test-project)
9. [Part 8: Configure Docker Integration for Tests](#part-8-configure-docker-integration-for-tests)
10. [Part 9: Write Integration Tests](#part-9-write-integration-tests)
11. [Part 10: Running and Verifying](#part-10-running-and-verifying)
12. [Complete Project Structure](#complete-project-structure)
13. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before starting, ensure you have the following installed:

- **.NET 10 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Docker Desktop** - Download from [docker.com](https://www.docker.com/products/docker-desktop)
- **Visual Studio 2022/2025** or **VS Code** with C# extension
- **PostgreSQL** (for local development) or use Docker

Verify installations:

```powershell
dotnet --version
# Should output: 10.x.x

docker --version
# Should output: Docker version 2x.x.x
```

---

## Part 1: Create the Solution Structure

### Step 1.1: Create the Solution Directory

Open PowerShell and navigate to your projects folder:

```powershell
cd E:\Projects
mkdir WebVella.App.Seed
cd WebVella.App.Seed
```

### Step 1.2: Create the Solution File

```powershell
dotnet new sln -n WebVella.App.Seed
```

### Step 1.3: Create the API Project

```powershell
dotnet new web -n WebVella.App.Seed.Api -f net10.0
dotnet sln add WebVella.App.Seed.Api/WebVella.App.Seed.Api.csproj
```

### Step 1.4: Create the Unit Test Project

```powershell
dotnet new xunit -n WebVella.App.Seed.Tests -f net10.0
dotnet sln add WebVella.App.Seed.Tests/WebVella.App.Seed.Tests.csproj
```

### Step 1.5: Add Project Reference

```powershell
dotnet add WebVella.App.Seed.Tests/WebVella.App.Seed.Tests.csproj reference WebVella.App.Seed.Api/WebVella.App.Seed.Api.csproj
```

---

## Part 2: Configure the API Project

### Step 2.1: Add Required NuGet Packages

Navigate to the API project and add the necessary packages:

```powershell
cd WebVella.App.Seed.Api

# FastEndpoints
dotnet add package FastEndpoints
dotnet add package FastEndpoints.Swagger

# WebVella.Database (PostgreSQL data access)
dotnet add package WebVella.Database

# Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
```

### Step 2.2: Update the Project File

Edit `WebVella.App.Seed.Api/WebVella.App.Seed.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FastEndpoints" Version="5.*" />
    <PackageReference Include="FastEndpoints.Swagger" Version="5.*" />
    <PackageReference Include="WebVella.Database" Version="1.*" />
  </ItemGroup>

  <!-- Embedded resources for migrations -->
  <ItemGroup>
    <EmbeddedResource Include="Migrations\**\*.sql" />
  </ItemGroup>

</Project>
```

### Step 2.3: Create the Folder Structure

Create the following folder structure in the API project:

```
WebVella.App.Seed.Api/
├── Endpoints/
│   └── Products/
├── Entities/
├── Migrations/
├── Services/
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

PowerShell commands:

```powershell
mkdir Endpoints
mkdir Endpoints\Products
mkdir Entities
mkdir Migrations
mkdir Services
```

### Step 2.4: Configure appsettings.json

Create `WebVella.App.Seed.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=webvella_seed;Username=postgres;Password=postgres"
  }
}
```

Create `WebVella.App.Seed.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=webvella_seed_dev;Username=postgres;Password=postgres"
  }
}
```

---

## Part 3: Create Entity Models

### Step 3.1: Create the Product Entity

Create `WebVella.App.Seed.Api/Entities/Product.cs`:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Entities;

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Discontinued = 2
}

public class ProductMetadata
{
    public string? Brand { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
}

[Table("products")]
[Cacheable(DurationSeconds = 300)]
public class Product
{
    [Key]
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    public Guid? CategoryId { get; set; }

    [JsonColumn]
    public ProductMetadata? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
```

### Step 3.2: Create the Category Entity

Create `WebVella.App.Seed.Api/Entities/Category.cs`:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Entities;

[Table("categories")]
[Cacheable(DurationSeconds = 600, SlidingExpiration = true)]
public class Category
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // External property - populated separately
    [External]
    public Category? Parent { get; set; }

    [External]
    public List<Product>? Products { get; set; }
}
```

### Step 3.3: Create the Order Entities

Create `WebVella.App.Seed.Api/Entities/Order.cs`:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Entities;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public class ShippingAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

[Table("orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    [JsonColumn]
    public ShippingAddress? ShippingAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Child collections for QueryMultipleList
    [External]
    [ResultSet(1, ForeignKey = "OrderId")]
    public List<OrderLine> Lines { get; set; } = [];
}

[Table("order_lines")]
public class OrderLine
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    // Computed property - not written to database
    [Write(false)]
    public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);
}
```

---

## Part 4: Create Database Migrations

### Step 4.1: Create the Initial Migration Class

Create `WebVella.App.Seed.Api/Migrations/M_1_0_0_0_InitialSchema.cs`:

```csharp
using WebVella.Database.Migrations;

namespace WebVella.App.Seed.Api.Migrations;

[DbMigration("1.0.0.0")]
public class M_1_0_0_0_InitialSchema : DbMigration
{
    // SQL is loaded from embedded resource: M_1_0_0_0_InitialSchema.Script.sql
}
```

### Step 4.2: Create the Initial Migration SQL Script

Create `WebVella.App.Seed.Api/Migrations/M_1_0_0_0_InitialSchema.Script.sql`:

```sql
-- =============================================
-- WebVella.App.Seed Initial Database Schema
-- Version: 1.0.0.0
-- =============================================

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- Categories Table
-- =============================================
CREATE TABLE IF NOT EXISTS categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_id);
CREATE INDEX IF NOT EXISTS idx_categories_active ON categories(is_active) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_categories_sort ON categories(sort_order);

-- =============================================
-- Products Table
-- =============================================
CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(18, 2) NOT NULL DEFAULT 0,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_products_sku ON products(sku);
CREATE INDEX IF NOT EXISTS idx_products_name ON products(name);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_products_status ON products(status);
CREATE INDEX IF NOT EXISTS idx_products_price ON products(price);

-- =============================================
-- Orders Table
-- =============================================
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(50) NOT NULL UNIQUE,
    customer_email VARCHAR(255) NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    total_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    shipping_address JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    completed_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_orders_number ON orders(order_number);
CREATE INDEX IF NOT EXISTS idx_orders_customer_email ON orders(customer_email);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_created ON orders(created_at DESC);

-- =============================================
-- Order Lines Table
-- =============================================
CREATE TABLE IF NOT EXISTS order_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    product_name VARCHAR(255) NOT NULL,
    product_sku VARCHAR(50) NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price DECIMAL(18, 2) NOT NULL,
    discount_percent DECIMAL(5, 2) NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_order_lines_order ON order_lines(order_id);
CREATE INDEX IF NOT EXISTS idx_order_lines_product ON order_lines(product_id);
```

### Step 4.3: Create Seed Data Migration

Create `WebVella.App.Seed.Api/Migrations/M_1_0_1_0_SeedData.cs`:

```csharp
using WebVella.Database;
using WebVella.Database.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace WebVella.App.Seed.Api.Migrations;

[DbMigration("1.0.1.0")]
public class M_1_0_1_0_SeedData : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        // No SQL schema changes in this migration
        return Task.FromResult(string.Empty);
    }

    public override async Task PostMigrateAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<IDbService>();

        // Check if seed data already exists
        var existingCategories = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM categories");

        if (existingCategories > 0)
        {
            return; // Data already seeded
        }

        await using var scope = await db.CreateTransactionScopeAsync();

        // Seed categories
        await db.ExecuteAsync("""
            INSERT INTO categories (id, name, description, sort_order, is_active, created_at)
            VALUES 
                ('11111111-1111-1111-1111-111111111111', 'Electronics', 'Electronic devices and accessories', 1, true, CURRENT_TIMESTAMP),
                ('22222222-2222-2222-2222-222222222222', 'Clothing', 'Apparel and fashion items', 2, true, CURRENT_TIMESTAMP),
                ('33333333-3333-3333-3333-333333333333', 'Books', 'Books and publications', 3, true, CURRENT_TIMESTAMP),
                ('44444444-4444-4444-4444-444444444444', 'Home & Garden', 'Home improvement and garden supplies', 4, true, CURRENT_TIMESTAMP)
            """);

        // Seed products
        await db.ExecuteAsync("""
            INSERT INTO products (id, sku, name, description, price, stock_quantity, status, category_id, metadata, created_at)
            VALUES 
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'ELEC-001', 'Wireless Mouse', 'Ergonomic wireless mouse with USB receiver', 29.99, 150, 1, '11111111-1111-1111-1111-111111111111', '{"brand": "TechCo", "tags": ["wireless", "ergonomic"]}', CURRENT_TIMESTAMP),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ELEC-002', 'Mechanical Keyboard', 'RGB mechanical keyboard with blue switches', 89.99, 75, 1, '11111111-1111-1111-1111-111111111111', '{"brand": "TechCo", "tags": ["mechanical", "rgb", "gaming"]}', CURRENT_TIMESTAMP),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'CLTH-001', 'Cotton T-Shirt', 'Premium cotton t-shirt, available in multiple colors', 19.99, 500, 1, '22222222-2222-2222-2222-222222222222', '{"brand": "FashionCo", "tags": ["cotton", "casual"]}', CURRENT_TIMESTAMP),
                ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'BOOK-001', 'Clean Code', 'A Handbook of Agile Software Craftsmanship by Robert C. Martin', 39.99, 200, 1, '33333333-3333-3333-3333-333333333333', '{"brand": "Pearson", "tags": ["programming", "software engineering"]}', CURRENT_TIMESTAMP),
                ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'HOME-001', 'Garden Hose 50ft', 'Heavy-duty expandable garden hose', 34.99, 100, 1, '44444444-4444-4444-4444-444444444444', '{"brand": "GardenPro", "tags": ["outdoor", "garden"]}', CURRENT_TIMESTAMP)
            """);

        await scope.CompleteAsync();
    }
}
```

---

## Part 5: Create FastEndpoints

### Step 5.1: Create Product Endpoints

#### Get All Products Endpoint

Create `WebVella.App.Seed.Api/Endpoints/Products/GetAllProductsEndpoint.cs`:

```csharp
using FastEndpoints;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Products;

public class GetAllProductsRequest
{
    public int? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetAllProductsResponse
{
    public List<ProductDto> Products { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public ProductMetadata? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GetAllProductsEndpoint : Endpoint<GetAllProductsRequest, GetAllProductsResponse>
{
    private readonly IDbService _db;

    public GetAllProductsEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/api/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all products with optional filtering and pagination";
            s.Description = "Returns a paginated list of products. Can filter by status and category.";
        });
    }

    public override async Task HandleAsync(GetAllProductsRequest req, CancellationToken ct)
    {
        var offset = (req.Page - 1) * req.PageSize;

        var whereClauses = new List<string>();
        var parameters = new Dictionary<string, object?>
        {
            ["Offset"] = offset,
            ["Limit"] = req.PageSize
        };

        if (req.Status.HasValue)
        {
            whereClauses.Add("status = @Status");
            parameters["Status"] = req.Status.Value;
        }

        if (req.CategoryId.HasValue)
        {
            whereClauses.Add("category_id = @CategoryId");
            parameters["CategoryId"] = req.CategoryId.Value;
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM products {whereClause}";
        var totalCount = await _db.ExecuteScalarAsync<int>(countSql, parameters);

        // Get products
        var sql = $"""
            SELECT 
                id AS "Id",
                sku AS "Sku",
                name AS "Name",
                description AS "Description",
                price AS "Price",
                stock_quantity AS "StockQuantity",
                status AS "Status",
                category_id AS "CategoryId",
                metadata AS "Metadata",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM products
            {whereClause}
            ORDER BY created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var products = await _db.QueryAsync<Product>(sql, parameters);

        var response = new GetAllProductsResponse
        {
            Products = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Status = p.Status.ToString(),
                CategoryId = p.CategoryId,
                Metadata = p.Metadata,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / req.PageSize)
        };

        await SendAsync(response, cancellation: ct);
    }
}
```

#### Get Product by ID Endpoint

Create `WebVella.App.Seed.Api/Endpoints/Products/GetProductEndpoint.cs`:

```csharp
using FastEndpoints;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Products;

public class GetProductRequest
{
    public Guid Id { get; set; }
}

public class GetProductEndpoint : Endpoint<GetProductRequest, ProductDto>
{
    private readonly IDbService _db;

    public GetProductEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a product by ID";
            s.Description = "Returns a single product with the specified ID.";
        });
    }

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        var product = await _db.GetAsync<Product>(req.Id);

        if (product is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var response = new ProductDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Status = product.Status.ToString(),
            CategoryId = product.CategoryId,
            Metadata = product.Metadata,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        await SendAsync(response, cancellation: ct);
    }
}
```

#### Create Product Endpoint

Create `WebVella.App.Seed.Api/Endpoints/Products/CreateProductEndpoint.cs`:

```csharp
using FastEndpoints;
using FluentValidation;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Products;

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public Guid? CategoryId { get; set; }
    public ProductMetadata? Metadata { get; set; }
}

public class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must be 50 characters or less");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must be 255 characters or less");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative");
    }
}

public class CreateProductResponse
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateProductEndpoint : Endpoint<CreateProductRequest, CreateProductResponse>
{
    private readonly IDbService _db;

    public CreateProductEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/api/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new product";
            s.Description = "Creates a new product and returns the created product details.";
        });
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        // Check if SKU already exists
        var existingSku = await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM products WHERE sku = @Sku)",
            new { req.Sku });

        if (existingSku)
        {
            AddError(r => r.Sku, "A product with this SKU already exists");
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, cancellation: ct);
            return;
        }

        // Validate category exists if provided
        if (req.CategoryId.HasValue)
        {
            var categoryExists = await _db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM categories WHERE id = @Id)",
                new { Id = req.CategoryId.Value });

            if (!categoryExists)
            {
                AddError(r => r.CategoryId, "Category not found");
                await HttpContext.Response.SendErrorsAsync(ValidationFailures, cancellation: ct);
                return;
            }
        }

        var product = new Product
        {
            Sku = req.Sku,
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            StockQuantity = req.StockQuantity,
            Status = req.Status,
            CategoryId = req.CategoryId,
            Metadata = req.Metadata,
            CreatedAt = DateTime.UtcNow
        };

        var inserted = await _db.InsertAsync(product);

        var response = new CreateProductResponse
        {
            Id = inserted.Id,
            Sku = inserted.Sku,
            Name = inserted.Name,
            CreatedAt = inserted.CreatedAt
        };

        await HttpContext.Response.SendCreatedAtAsync<GetProductEndpoint>(
            new { Id = inserted.Id },
            response,
            cancellation: ct);
    }
}
```

#### Update Product Endpoint

Create `WebVella.App.Seed.Api/Endpoints/Products/UpdateProductEndpoint.cs`:

```csharp
using FastEndpoints;
using FluentValidation;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Products;

public class UpdateProductRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public ProductStatus Status { get; set; }
    public Guid? CategoryId { get; set; }
    public ProductMetadata? Metadata { get; set; }
}

public class UpdateProductValidator : Validator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must be 255 characters or less");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative");
    }
}

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest, ProductDto>
{
    private readonly IDbService _db;

    public UpdateProductEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Put("/api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update an existing product";
            s.Description = "Updates the specified product and returns the updated product details.";
        });
    }

    public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
    {
        var product = await _db.GetAsync<Product>(req.Id);

        if (product is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        // Validate category exists if provided
        if (req.CategoryId.HasValue)
        {
            var categoryExists = await _db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM categories WHERE id = @Id)",
                new { Id = req.CategoryId.Value });

            if (!categoryExists)
            {
                AddError(r => r.CategoryId, "Category not found");
                await HttpContext.Response.SendErrorsAsync(ValidationFailures, cancellation: ct);
                return;
            }
        }

        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.StockQuantity = req.StockQuantity;
        product.Status = req.Status;
        product.CategoryId = req.CategoryId;
        product.Metadata = req.Metadata;
        product.UpdatedAt = DateTime.UtcNow;

        // Update only the changed properties
        var updated = await _db.UpdateAsync(product, [
            "Name", "Description", "Price", "StockQuantity",
            "Status", "CategoryId", "Metadata", "UpdatedAt"
        ]);

        if (!updated)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var response = new ProductDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Status = product.Status.ToString(),
            CategoryId = product.CategoryId,
            Metadata = product.Metadata,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        await SendAsync(response, cancellation: ct);
    }
}
```

#### Delete Product Endpoint

Create `WebVella.App.Seed.Api/Endpoints/Products/DeleteProductEndpoint.cs`:

```csharp
using FastEndpoints;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Products;

public class DeleteProductRequest
{
    public Guid Id { get; set; }
}

public class DeleteProductEndpoint : Endpoint<DeleteProductRequest>
{
    private readonly IDbService _db;

    public DeleteProductEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Delete("/api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a product";
            s.Description = "Deletes the product with the specified ID.";
        });
    }

    public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
    {
        // Check if product is referenced in any orders
        var hasOrders = await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM order_lines WHERE product_id = @Id)",
            new { req.Id });

        if (hasOrders)
        {
            AddError("Cannot delete product that is referenced in orders");
            await SendErrorsAsync(400, ct);
            return;
        }

        var deleted = await _db.DeleteAsync<Product>(req.Id);

        if (!deleted)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
```

### Step 5.2: Create Order Endpoints

Create `WebVella.App.Seed.Api/Endpoints/Orders/GetOrderEndpoint.cs`:

```csharp
using FastEndpoints;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Orders;

public class GetOrderRequest
{
    public Guid Id { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public ShippingAddress? ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OrderLineDto> Lines { get; set; } = [];
}

public class OrderLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
}

public class GetOrderEndpoint : Endpoint<GetOrderRequest, OrderDto>
{
    private readonly IDbService _db;

    public GetOrderEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/api/orders/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get an order by ID with all order lines";
            s.Description = "Returns a single order with all its order lines using QueryMultipleList.";
        });
    }

    public override async Task HandleAsync(GetOrderRequest req, CancellationToken ct)
    {
        // Using QueryMultipleList to fetch order with lines in a single query
        var sql = """
            -- Result Set 0: Parent entity (order)
            SELECT 
                id AS "Id",
                order_number AS "OrderNumber",
                customer_email AS "CustomerEmail",
                customer_name AS "CustomerName",
                status AS "Status",
                total_amount AS "TotalAmount",
                shipping_address AS "ShippingAddress",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt",
                completed_at AS "CompletedAt"
            FROM orders
            WHERE id = @Id;
            
            -- Result Set 1: Child entities (order lines) - mapped via OrderId
            SELECT 
                id AS "Id",
                order_id AS "OrderId",
                product_id AS "ProductId",
                product_name AS "ProductName",
                product_sku AS "ProductSku",
                quantity AS "Quantity",
                unit_price AS "UnitPrice",
                discount_percent AS "DiscountPercent"
            FROM order_lines
            WHERE order_id = @Id;
            """;

        var orders = await _db.QueryMultipleListAsync<Order>(sql, new { req.Id });
        var order = orders.FirstOrDefault();

        if (order is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var response = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            Lines = order.Lines.Select(l => new OrderLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductSku = l.ProductSku,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineTotal = l.LineTotal
            }).ToList()
        };

        await SendAsync(response, cancellation: ct);
    }
}
```

Create `WebVella.App.Seed.Api/Endpoints/Orders/CreateOrderEndpoint.cs`:

```csharp
using FastEndpoints;
using FluentValidation;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Orders;

public class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public ShippingAddress? ShippingAddress { get; set; }
    public List<CreateOrderLineRequest> Lines { get; set; } = [];
}

public class CreateOrderLineRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class CreateOrderValidator : Validator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(200).WithMessage("Customer name must be 200 characters or less");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one order line is required");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            line.RuleFor(l => l.DiscountPercent)
                .InclusiveBetween(0, 100).WithMessage("Discount must be between 0 and 100");
        });
    }
}

public class CreateOrderResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderEndpoint : Endpoint<CreateOrderRequest, CreateOrderResponse>
{
    private readonly IDbService _db;

    public CreateOrderEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/api/orders");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new order";
            s.Description = "Creates a new order with order lines. Uses transaction and advisory lock for inventory management.";
        });
    }

    public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
    {
        // Get all products for the order
        var productIds = req.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.GetListAsync<Product>(productIds);

        var productDict = products.ToDictionary(p => p.Id);

        // Validate all products exist and are active
        foreach (var line in req.Lines)
        {
            if (!productDict.TryGetValue(line.ProductId, out var product))
            {
                AddError($"Product {line.ProductId} not found");
                continue;
            }

            if (product.Status != ProductStatus.Active)
            {
                AddError($"Product {product.Sku} is not available for purchase");
            }

            if (product.StockQuantity < line.Quantity)
            {
                AddError($"Insufficient stock for product {product.Sku}. Available: {product.StockQuantity}");
            }
        }

        if (ValidationFailed)
        {
            await HttpContext.Response.SendErrorsAsync(ValidationFailures, cancellation: ct);
            return;
        }

        // Use advisory lock for inventory management
        await using var scope = await _db.CreateTransactionScopeAsync(
            lockKey: "order-creation");

        try
        {
            // Generate order number
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // Calculate total
            decimal totalAmount = 0;
            var orderLines = new List<OrderLine>();

            foreach (var lineReq in req.Lines)
            {
                var product = productDict[lineReq.ProductId];
                var unitPrice = product.Price;
                var lineTotal = lineReq.Quantity * unitPrice * (1 - lineReq.DiscountPercent / 100);
                totalAmount += lineTotal;

                orderLines.Add(new OrderLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSku = product.Sku,
                    Quantity = lineReq.Quantity,
                    UnitPrice = unitPrice,
                    DiscountPercent = lineReq.DiscountPercent
                });
            }

            // Create order
            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerEmail = req.CustomerEmail,
                CustomerName = req.CustomerName,
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount,
                ShippingAddress = req.ShippingAddress,
                CreatedAt = DateTime.UtcNow
            };

            var insertedOrder = await _db.InsertAsync(order);

            // Create order lines
            foreach (var line in orderLines)
            {
                line.OrderId = insertedOrder.Id;
                await _db.InsertAsync(line);
            }

            // Update inventory - use UpdateAsync to properly invalidate cache
            foreach (var lineReq in req.Lines)
            {
                var product = productDict[lineReq.ProductId];
                product.StockQuantity -= lineReq.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                await _db.UpdateAsync(product);
            }

            await scope.CompleteAsync();

            var response = new CreateOrderResponse
            {
                Id = insertedOrder.Id,
                OrderNumber = insertedOrder.OrderNumber,
                TotalAmount = insertedOrder.TotalAmount,
                CreatedAt = insertedOrder.CreatedAt
            };

            await HttpContext.Response.SendCreatedAtAsync<GetOrderEndpoint>(
                new { Id = insertedOrder.Id },
                response,
                cancellation: ct);
        }
        catch (Exception)
        {
            // Transaction will be rolled back automatically
            throw;
        }
    }
}
```

### Step 5.3: Create Category Endpoints

Create `WebVella.App.Seed.Api/Endpoints/Categories/GetAllCategoriesEndpoint.cs`:

```csharp
using FastEndpoints;
using WebVella.App.Seed.Api.Entities;
using WebVella.Database;

namespace WebVella.App.Seed.Api.Endpoints.Categories;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class GetAllCategoriesResponse
{
    public List<CategoryDto> Categories { get; set; } = [];
}

public class GetAllCategoriesEndpoint : EndpointWithoutRequest<GetAllCategoriesResponse>
{
    private readonly IDbService _db;

    public GetAllCategoriesEndpoint(IDbService db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/api/categories");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all categories";
            s.Description = "Returns all categories. Results are cached for 10 minutes.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // GetListAsync uses cache automatically for [Cacheable] entities
        var categories = await _db.GetListAsync<Category>();

        var response = new GetAllCategoriesResponse
        {
            Categories = categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ParentId = c.ParentId,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive
                }).ToList()
        };

        await SendAsync(response, cancellation: ct);
    }
}
```

---

## Part 6: Configure and Run the Application

### Step 6.1: Configure Program.cs

Replace the content of `WebVella.App.Seed.Api/Program.cs`:

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using WebVella.Database;
using WebVella.Database.Migrations;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// Configure Services
// =============================================

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add Swagger documentation
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "WebVella.App.Seed API";
        s.Version = "v1";
        s.Description = "A sample API built with FastEndpoints and WebVella.Database";
    };
});

// Add WebVella.Database with caching enabled
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);

// Add database migrations
builder.Services.AddWebVellaDatabaseMigrations();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// =============================================
// Run Database Migrations
// =============================================

using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();
    
    try
    {
        var currentVersion = await migrationService.GetCurrentDbVersionAsync();
        app.Logger.LogInformation("Current database version: {Version}", currentVersion ?? "None");
        
        await migrationService.ExecutePendingMigrationsAsync();
        
        var newVersion = await migrationService.GetCurrentDbVersionAsync();
        app.Logger.LogInformation("Database migrated to version: {Version}", newVersion);
    }
    catch (DbMigrationException ex)
    {
        app.Logger.LogError(ex, "Database migration failed");
        
        foreach (var log in ex.MigrationLogs)
        {
            var status = log.Success ? "✓" : "✗";
            app.Logger.LogError("[{Status}] Version {Version}: {Statement}", 
                status, log.Version, log.Statement);
            
            if (!log.Success && log.SqlError != null)
            {
                app.Logger.LogError("  Error: {Error}", log.SqlError);
            }
        }
        
        throw;
    }
}

// =============================================
// Configure Middleware Pipeline
// =============================================

app.UseCors();

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = null; // Use PascalCase
});

app.UseSwaggerGen();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
```

### Step 6.2: Create Docker Compose for Local Development

Create `docker-compose.yml` in the solution root:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: webvella-seed-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: webvella_seed_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

### Step 6.3: Run the Application

Start PostgreSQL:

```powershell
docker-compose up -d
```

Run the API:

```powershell
cd WebVella.App.Seed.Api
dotnet run
```

Access the API:
- Swagger UI: `https://localhost:5001/swagger`
- Health check: `https://localhost:5001/health`
- Products: `https://localhost:5001/api/products`

---

## Part 7: Create the Unit Test Project

### Step 7.1: Add Required NuGet Packages

Navigate to the test project and add packages:

```powershell
cd WebVella.App.Seed.Tests

# Testing frameworks
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions

# Docker management
dotnet add package Testcontainers
dotnet add package Testcontainers.PostgreSql

# Additional utilities
dotnet add package Respawn
```

### Step 7.2: Update the Test Project File

Edit `WebVella.App.Seed.Tests/WebVella.App.Seed.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="Respawn" Version="6.*" />
    <PackageReference Include="Testcontainers" Version="4.*" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WebVella.App.Seed.Api\WebVella.App.Seed.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
  </ItemGroup>

</Project>
```

---

## Part 8: Configure Docker Integration for Tests

### Step 8.1: Create the PostgreSQL Test Container Fixture

Create `WebVella.App.Seed.Tests/Infrastructure/PostgreSqlContainerFixture.cs`:

```csharp
using Testcontainers.PostgreSql;

namespace WebVella.App.Seed.Tests.Infrastructure;

/// <summary>
/// xUnit collection fixture that manages a PostgreSQL Docker container.
/// The container is created once and shared across all tests in the collection.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("webvella_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

### Step 8.2: Create the Collection Definition

Create `WebVella.App.Seed.Tests/Infrastructure/PostgreSqlCollection.cs`:

```csharp
namespace WebVella.App.Seed.Tests.Infrastructure;

/// <summary>
/// Defines a test collection that shares the PostgreSQL container fixture.
/// All test classes in this collection will use the same database container.
/// </summary>
[CollectionDefinition(Name)]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL";
}
```

### Step 8.3: Create the Custom WebApplicationFactory

Create `WebVella.App.Seed.Tests/Infrastructure/CustomWebApplicationFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;

namespace WebVella.App.Seed.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that configures the API for integration testing
/// with a Docker-hosted PostgreSQL database.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private Respawner? _respawner;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
    }

    /// <summary>
    /// Initializes the Respawner for database cleanup between tests.
    /// Should be called once after the factory is created.
    /// </summary>
    public async Task InitializeRespawnerAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            // Exclude migration tracking tables from cleanup
            TablesToIgnore = ["_db_version"]
        });
    }

    /// <summary>
    /// Resets the database to a clean state by removing all data
    /// except for the migration tracking tables.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException(
                "Respawner not initialized. Call InitializeRespawnerAsync first.");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
```

### Step 8.4: Create a Base Test Class

Create `WebVella.App.Seed.Tests/Infrastructure/IntegrationTestBase.cs`:

```csharp
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database;

namespace WebVella.App.Seed.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides common functionality
/// including HTTP client, database access, and test data helpers.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainerFixture PostgresFixture;
    protected CustomWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;
    protected IServiceScope ServiceScope = null!;
    protected IDbService DbService = null!;

    protected IntegrationTestBase(PostgreSqlContainerFixture postgresFixture)
    {
        PostgresFixture = postgresFixture;
    }

    public virtual async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory(PostgresFixture.ConnectionString);
        
        // Force the application to start and run migrations
        Client = Factory.CreateClient();

        // Initialize respawner after migrations have run
        await Factory.InitializeRespawnerAsync();

        // Create a service scope for database access in tests
        ServiceScope = Factory.Services.CreateScope();
        DbService = ServiceScope.ServiceProvider.GetRequiredService<IDbService>();

        // Reset database to clean state
        await Factory.ResetDatabaseAsync();
        
        // Seed test data if needed
        await SeedTestDataAsync();
    }

    public virtual async Task DisposeAsync()
    {
        ServiceScope?.Dispose();
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Override to seed test data before each test.
    /// </summary>
    protected virtual Task SeedTestDataAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to send a POST request with JSON content.
    /// </summary>
    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T content)
    {
        return await Client.PostAsJsonAsync(url, content);
    }

    /// <summary>
    /// Helper method to send a PUT request with JSON content.
    /// </summary>
    protected async Task<HttpResponseMessage> PutAsync<T>(string url, T content)
    {
        return await Client.PutAsJsonAsync(url, content);
    }

    /// <summary>
    /// Helper method to get and deserialize a response.
    /// </summary>
    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

---

## Part 9: Write Integration Tests

### Step 9.1: Create Product Endpoint Tests

Create `WebVella.App.Seed.Tests/Endpoints/ProductEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using WebVella.App.Seed.Api.Endpoints.Products;
using WebVella.App.Seed.Api.Entities;
using WebVella.App.Seed.Tests.Infrastructure;

namespace WebVella.App.Seed.Tests.Endpoints;

[Collection(PostgreSqlCollection.Name)]
public class ProductEndpointTests : IntegrationTestBase
{
    public ProductEndpointTests(PostgreSqlContainerFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync()
    {
        // Seed a test category
        await DbService.ExecuteAsync("""
            INSERT INTO categories (id, name, sort_order, is_active, created_at)
            VALUES ('11111111-1111-1111-1111-111111111111', 'Test Category', 1, true, CURRENT_TIMESTAMP)
            """);

        // Seed test products
        await DbService.ExecuteAsync("""
            INSERT INTO products (id, sku, name, description, price, stock_quantity, status, category_id, created_at)
            VALUES 
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'TEST-001', 'Test Product 1', 'Description 1', 19.99, 100, 1, '11111111-1111-1111-1111-111111111111', CURRENT_TIMESTAMP),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'TEST-002', 'Test Product 2', 'Description 2', 29.99, 50, 1, '11111111-1111-1111-1111-111111111111', CURRENT_TIMESTAMP),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'TEST-003', 'Inactive Product', 'Description 3', 39.99, 0, 0, NULL, CURRENT_TIMESTAMP)
            """);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsAllProducts()
    {
        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllProducts_WithStatusFilter_ReturnsFilteredProducts()
    {
        // Act
        var response = await Client.GetAsync("/api/products?status=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(2);
        result.Products.Should().OnlyContain(p => p.Status == "Active");
    }

    [Fact]
    public async Task GetAllProducts_WithCategoryFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response = await Client.GetAsync($"/api/products?categoryId={categoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(2);
        result.Products.Should().OnlyContain(p => p.CategoryId == categoryId);
    }

    [Fact]
    public async Task GetAllProducts_WithPagination_ReturnsPagedResults()
    {
        // Act
        var response = await Client.GetAsync("/api/products?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var response = await Client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Sku.Should().Be("TEST-001");
        result.Name.Should().Be("Test Product 1");
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/products/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Sku = "NEW-001",
            Name = "New Product",
            Description = "A brand new product",
            Price = 49.99m,
            StockQuantity = 25,
            Status = ProductStatus.Active,
            CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Metadata = new ProductMetadata
            {
                Brand = "TestBrand",
                Tags = ["new", "featured"]
            }
        };

        // Act
        var response = await PostAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Sku.Should().Be("NEW-001");
        result.Name.Should().Be("New Product");

        // Verify in database
        var product = await DbService.GetAsync<Product>(result.Id);
        product.Should().NotBeNull();
        product!.Metadata.Should().NotBeNull();
        product.Metadata!.Brand.Should().Be("TestBrand");
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Sku = "TEST-001", // Already exists
            Name = "Duplicate Product",
            Price = 19.99m,
            StockQuantity = 10,
            Status = ProductStatus.Draft
        };

        // Act
        var response = await PostAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Sku = "NEW-002",
            Name = "Product with invalid category",
            Price = 19.99m,
            StockQuantity = 10,
            CategoryId = Guid.NewGuid() // Non-existent category
        };

        // Act
        var response = await PostAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        // Arrange
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var request = new UpdateProductRequest
        {
            Id = productId,
            Name = "Updated Product Name",
            Description = "Updated description",
            Price = 24.99m,
            StockQuantity = 150,
            Status = ProductStatus.Active,
            CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        // Act
        var response = await PutAsync($"/api/products/{productId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Product Name");
        result.Price.Should().Be(24.99m);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateProductRequest
        {
            Id = nonExistentId,
            Name = "Updated Name",
            Price = 19.99m,
            StockQuantity = 10,
            Status = ProductStatus.Active
        };

        // Act
        var response = await PutAsync($"/api/products/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var productId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // Act
        var response = await Client.DeleteAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var product = await DbService.GetAsync<Product>(productId);
        product.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Step 9.2: Create Order Endpoint Tests

Create `WebVella.App.Seed.Tests/Endpoints/OrderEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using WebVella.App.Seed.Api.Endpoints.Orders;
using WebVella.App.Seed.Api.Entities;
using WebVella.App.Seed.Tests.Infrastructure;

namespace WebVella.App.Seed.Tests.Endpoints;

[Collection(PostgreSqlCollection.Name)]
public class OrderEndpointTests : IntegrationTestBase
{
    public OrderEndpointTests(PostgreSqlContainerFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync()
    {
        // Seed test products
        await DbService.ExecuteAsync("""
            INSERT INTO products (id, sku, name, price, stock_quantity, status, created_at)
            VALUES 
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'PROD-001', 'Product 1', 10.00, 100, 1, CURRENT_TIMESTAMP),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'PROD-002', 'Product 2', 20.00, 50, 1, CURRENT_TIMESTAMP),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'PROD-003', 'Inactive Product', 30.00, 10, 0, CURRENT_TIMESTAMP)
            """);

        // Seed a test order with lines
        await DbService.ExecuteAsync("""
            INSERT INTO orders (id, order_number, customer_email, customer_name, status, total_amount, created_at)
            VALUES ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'ORD-TEST-001', 'test@example.com', 'Test Customer', 0, 50.00, CURRENT_TIMESTAMP)
            """);

        await DbService.ExecuteAsync("""
            INSERT INTO order_lines (id, order_id, product_id, product_name, product_sku, quantity, unit_price, discount_percent)
            VALUES 
                ('11111111-1111-1111-1111-111111111111', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Product 1', 'PROD-001', 2, 10.00, 0),
                ('22222222-2222-2222-2222-222222222222', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Product 2', 'PROD-002', 1, 20.00, 10)
            """);
    }

    [Fact]
    public async Task GetOrder_WithValidId_ReturnsOrderWithLines()
    {
        // Arrange
        var orderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        // Act
        var response = await Client.GetAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.OrderNumber.Should().Be("ORD-TEST-001");
        result.CustomerEmail.Should().Be("test@example.com");
        result.Lines.Should().HaveCount(2);

        // Verify line totals are calculated correctly
        var line1 = result.Lines.First(l => l.ProductSku == "PROD-001");
        line1.Quantity.Should().Be(2);
        line1.UnitPrice.Should().Be(10.00m);
        line1.LineTotal.Should().Be(20.00m); // 2 * 10.00 * (1 - 0/100)

        var line2 = result.Lines.First(l => l.ProductSku == "PROD-002");
        line2.DiscountPercent.Should().Be(10m);
        line2.LineTotal.Should().Be(18.00m); // 1 * 20.00 * (1 - 10/100)
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/orders/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreatedAndUpdatesInventory()
    {
        // Arrange
        var product1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var product2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            ShippingAddress = new ShippingAddress
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "CA",
                PostalCode = "12345",
                Country = "USA"
            },
            Lines =
            [
                new CreateOrderLineRequest { ProductId = product1Id, Quantity = 5, DiscountPercent = 0 },
                new CreateOrderLineRequest { ProductId = product2Id, Quantity = 2, DiscountPercent = 5 }
            ]
        };

        // Get initial stock quantities
        var initialProduct1 = await DbService.GetAsync<Product>(product1Id);
        var initialProduct2 = await DbService.GetAsync<Product>(product2Id);

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.OrderNumber.Should().StartWith("ORD-");

        // Calculate expected total: (5 * 10.00) + (2 * 20.00 * 0.95) = 50.00 + 38.00 = 88.00
        result.TotalAmount.Should().Be(88.00m);

        // Verify inventory was updated
        var updatedProduct1 = await DbService.GetAsync<Product>(product1Id);
        var updatedProduct2 = await DbService.GetAsync<Product>(product2Id);

        updatedProduct1!.StockQuantity.Should().Be(initialProduct1!.StockQuantity - 5);
        updatedProduct2!.StockQuantity.Should().Be(initialProduct2!.StockQuantity - 2);
    }

    [Fact]
    public async Task CreateOrder_WithInactiveProduct_ReturnsBadRequest()
    {
        // Arrange
        var inactiveProductId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            Lines =
            [
                new CreateOrderLineRequest { ProductId = inactiveProductId, Quantity = 1, DiscountPercent = 0 }
            ]
        };

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"); // Has 50 in stock

        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            Lines =
            [
                new CreateOrderLineRequest { ProductId = productId, Quantity = 100, DiscountPercent = 0 }
            ]
        };

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentProduct_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            Lines =
            [
                new CreateOrderLineRequest { ProductId = nonExistentProductId, Quantity = 1, DiscountPercent = 0 }
            ]
        };

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyLines_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            Lines = []
        };

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = "invalid-email",
            CustomerName = "John Doe",
            Lines =
            [
                new CreateOrderLineRequest
                {
                    ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Quantity = 1,
                    DiscountPercent = 0
                }
            ]
        };

        // Act
        var response = await PostAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### Step 9.3: Create Category Endpoint Tests

Create `WebVella.App.Seed.Tests/Endpoints/CategoryEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using WebVella.App.Seed.Api.Endpoints.Categories;
using WebVella.App.Seed.Tests.Infrastructure;

namespace WebVella.App.Seed.Tests.Endpoints;

[Collection(PostgreSqlCollection.Name)]
public class CategoryEndpointTests : IntegrationTestBase
{
    public CategoryEndpointTests(PostgreSqlContainerFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    protected override async Task SeedTestDataAsync()
    {
        await DbService.ExecuteAsync("""
            INSERT INTO categories (id, name, description, sort_order, is_active, created_at)
            VALUES 
                ('11111111-1111-1111-1111-111111111111', 'Electronics', 'Electronic devices', 1, true, CURRENT_TIMESTAMP),
                ('22222222-2222-2222-2222-222222222222', 'Clothing', 'Apparel and fashion', 2, true, CURRENT_TIMESTAMP),
                ('33333333-3333-3333-3333-333333333333', 'Books', 'Books and publications', 3, true, CURRENT_TIMESTAMP)
            """);
    }

    [Fact]
    public async Task GetAllCategories_ReturnsAllCategoriesOrdered()
    {
        // Act
        var response = await Client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();
        result.Should().NotBeNull();
        result!.Categories.Should().HaveCount(3);

        // Verify ordering by sort_order
        result.Categories[0].Name.Should().Be("Electronics");
        result.Categories[1].Name.Should().Be("Clothing");
        result.Categories[2].Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetAllCategories_ReturnsCachedResults()
    {
        // This test verifies caching behavior by checking that
        // subsequent calls return the same data quickly

        // Act
        var response1 = await Client.GetAsync("/api/categories");
        var response2 = await Client.GetAsync("/api/categories");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await response1.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();

        result1!.Categories.Should().BeEquivalentTo(result2!.Categories);
    }
}
```

### Step 9.4: Create Health Check Test

Create `WebVella.App.Seed.Tests/Endpoints/HealthCheckTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using WebVella.App.Seed.Tests.Infrastructure;

namespace WebVella.App.Seed.Tests.Endpoints;

[Collection(PostgreSqlCollection.Name)]
public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(PostgreSqlContainerFixture postgresFixture)
        : base(postgresFixture)
    {
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Healthy");
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    private class HealthCheckResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
```

---

## Part 10: Running and Verifying

### Step 10.1: Run All Tests

From the solution root directory:

```powershell
# Run all tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProductEndpointTests"

# Run a single test
dotnet test --filter "FullyQualifiedName~ProductEndpointTests.GetAllProducts_ReturnsAllProducts"
```

### Step 10.2: Verify Docker Container Lifecycle

You can observe the Docker container lifecycle during test execution:

```powershell
# In a separate terminal, watch containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Watch container logs (while tests are running)
docker logs -f <container-id>
```

### Step 10.3: Run the API Locally

```powershell
# Start PostgreSQL
docker-compose up -d

# Run the API
cd WebVella.App.Seed.Api
dotnet run

# Or with hot reload
dotnet watch run
```

### Step 10.4: Test API Manually with curl

```powershell
# Health check
curl http://localhost:5000/health

# Get all products
curl http://localhost:5000/api/products

# Get all categories
curl http://localhost:5000/api/categories

# Create a product
curl -X POST http://localhost:5000/api/products `
  -H "Content-Type: application/json" `
  -d '{"sku":"MANUAL-001","name":"Manual Test Product","price":99.99,"stockQuantity":10,"status":1}'

# Create an order
curl -X POST http://localhost:5000/api/orders `
  -H "Content-Type: application/json" `
  -d '{"customerEmail":"test@test.com","customerName":"Test User","lines":[{"productId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","quantity":2,"discountPercent":0}]}'
```

---

## Complete Project Structure

```
WebVella.App.Seed/
├── WebVella.App.Seed.sln
├── docker-compose.yml
│
├── WebVella.App.Seed.Api/
│   ├── WebVella.App.Seed.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   │
│   ├── Endpoints/
│   │   ├── Categories/
│   │   │   └── GetAllCategoriesEndpoint.cs
│   │   ├── Orders/
│   │   │   ├── GetOrderEndpoint.cs
│   │   │   └── CreateOrderEndpoint.cs
│   │   └── Products/
│   │       ├── GetAllProductsEndpoint.cs
│   │       ├── GetProductEndpoint.cs
│   │       ├── CreateProductEndpoint.cs
│   │       ├── UpdateProductEndpoint.cs
│   │       └── DeleteProductEndpoint.cs
│   │
│   ├── Entities/
│   │   ├── Category.cs
│   │   ├── Order.cs
│   │   └── Product.cs
│   │
│   └── Migrations/
│       ├── M_1_0_0_0_InitialSchema.cs
│       ├── M_1_0_0_0_InitialSchema.Script.sql
│       └── M_1_0_1_0_SeedData.cs
│
└── WebVella.App.Seed.Tests/
    ├── WebVella.App.Seed.Tests.csproj
    │
    ├── Infrastructure/
    │   ├── PostgreSqlContainerFixture.cs
    │   ├── PostgreSqlCollection.cs
    │   ├── CustomWebApplicationFactory.cs
    │   └── IntegrationTestBase.cs
    │
    └── Endpoints/
        ├── CategoryEndpointTests.cs
        ├── HealthCheckTests.cs
        ├── OrderEndpointTests.cs
        └── ProductEndpointTests.cs
```

---

## Troubleshooting

### Common Issues and Solutions

#### 1. Docker Container Fails to Start

**Problem:** Testcontainers cannot start the PostgreSQL container.

**Solution:**
- Ensure Docker Desktop is running
- Check Docker has sufficient resources allocated
- Verify no conflicting containers are running on port 5432

```powershell
# Check Docker status
docker info

# Remove any conflicting containers
docker stop webvella-seed-postgres
docker rm webvella-seed-postgres
```

#### 2. Migration Fails

**Problem:** Database migration fails during application startup.

**Solution:**
- Check the connection string in `appsettings.json`
- Verify PostgreSQL is accessible
- Check migration SQL for syntax errors

```powershell
# Test connection manually
docker exec -it webvella-seed-postgres psql -U postgres -d webvella_seed_dev -c "SELECT 1"
```

#### 3. Tests Fail Intermittently

**Problem:** Tests pass sometimes but fail other times.

**Solution:**
- Ensure `ResetDatabaseAsync` is called before each test
- Check for test interdependencies
- Verify seed data is correctly inserted

#### 4. Package Restore Fails

**Problem:** NuGet packages fail to restore.

**Solution:**
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

#### 5. FastEndpoints Not Found

**Problem:** Endpoints not discovered at runtime.

**Solution:**
- Ensure `AddFastEndpoints()` is called in Program.cs
- Verify endpoint classes are public
- Check that endpoint classes inherit from `Endpoint<TRequest>` or similar

---

## Summary

This tutorial covered:

1. **Solution Setup** - Created a .NET 10 solution with API and test projects
2. **API Configuration** - Configured FastEndpoints, WebVella.Database, and Swagger
3. **Entity Models** - Created entities with proper attributes for WebVella.Database
4. **Database Migrations** - Implemented version-controlled schema migrations
5. **CRUD Endpoints** - Built complete REST endpoints for Products, Orders, and Categories
6. **Docker Integration** - Configured Testcontainers for automated PostgreSQL container management
7. **Integration Tests** - Created comprehensive tests with automatic database cleanup

Key technologies used:
- **.NET 10** - Latest framework version
- **FastEndpoints** - Minimal API endpoint framework
- **WebVella.Database** - PostgreSQL data access with Dapper
- **Testcontainers** - Docker container management for tests
- **Respawn** - Database cleanup between tests
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library

The result is a production-ready API template with automated testing infrastructure that spins up and tears down Docker containers automatically.

---

## Integration Test Infrastructure Explained

This section provides a detailed explanation of how the integration testing infrastructure works, using the `CategoryEndpointTests` as an example.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Collection                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         PostgreSqlContainerFixture                   │   │
│  │    (Shared PostgreSQL Docker container)              │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│           ┌───────────────┼───────────────┐                │
│           ▼               ▼               ▼                │
│   ┌─────────────┐ ┌─────────────┐ ┌─────────────┐         │
│   │  Test Class │ │  Test Class │ │  Test Class │         │
│   │  (isolated) │ │  (isolated) │ │  (isolated) │         │
│   └─────────────┘ └─────────────┘ └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

### Test Execution Flow

When running integration tests, the following sequence occurs:

| Step | Component | Action |
|------|-----------|--------|
| 1 | `PostgreSqlContainerFixture` | Docker pulls postgres:16-alpine image (if not cached) |
| 2 | `PostgreSqlContainerFixture` | Container starts and PostgreSQL initializes |
| 3 | `IntegrationTestBase.InitializeAsync()` | Creates `CustomWebApplicationFactory` with test connection string |
| 4 | `CustomWebApplicationFactory` | Starts ASP.NET Core test server |
| 5 | Application Startup | Database migrations execute automatically |
| 6 | `IntegrationTestBase.InitializeAsync()` | Initializes Respawner for database cleanup |
| 7 | `IntegrationTestBase.InitializeAsync()` | Resets database to clean state |
| 8 | `IntegrationTestBase.InitializeAsync()` | Calls `SeedTestDataAsync()` to insert test data |
| 9 | Test Method | Executes test logic |
| 10 | `IntegrationTestBase.DisposeAsync()` | Cleans up resources for next test |

### Sample Code Walkthrough: CategoryEndpointTests

Let's examine each part of the `CategoryEndpointTests` class:

#### Step 1: Class Declaration and Collection Attribute

```csharp
[Collection(PostgreSqlCollection.Name)]
public class CategoryEndpointTests : IntegrationTestBase
```

**What it does:**
- `[Collection(PostgreSqlCollection.Name)]` associates this test class with the "PostgreSQL" collection
- All test classes in the same collection share the same `PostgreSqlContainerFixture` instance
- This means the Docker container starts once and is reused across all tests in the collection
- Inheriting from `IntegrationTestBase` provides access to `Client`, `DbService`, and helper methods

#### Step 2: Constructor with Fixture Injection

```csharp
public CategoryEndpointTests(PostgreSqlContainerFixture postgresFixture)
    : base(postgresFixture)
{
}
```

**What it does:**
- xUnit automatically injects the shared `PostgreSqlContainerFixture` instance
- The fixture is passed to the base class for use during initialization
- The connection string from the fixture is used to configure the test server

#### Step 3: Seeding Test Data

```csharp
protected override async Task SeedTestDataAsync()
{
    await DbService.ExecuteAsync("""
        INSERT INTO categories (id, name, description, sort_order, is_active, created_at)
        VALUES 
            ('11111111-1111-1111-1111-111111111111', 'Electronics', 'Electronic devices', 1, true, CURRENT_TIMESTAMP),
            ('22222222-2222-2222-2222-222222222222', 'Clothing', 'Apparel and fashion', 2, true, CURRENT_TIMESTAMP),
            ('33333333-3333-3333-3333-333333333333', 'Books', 'Books and publications', 3, true, CURRENT_TIMESTAMP)
        """);
}
```

**What it does:**
- Called automatically before each test runs (after database reset)
- Uses raw SQL to insert test data directly into the database
- Predictable GUIDs (`11111111-...`) make assertions easier
- Each category has a different `sort_order` value to test ordering
- The `"""..."""` syntax is a C# 11+ raw string literal for cleaner SQL

**Why this approach:**
- Direct SQL is faster than going through the API
- Bypasses validation logic that might prevent certain test scenarios
- Ensures exact control over test data state

#### Step 4: Test Method - GetAllCategories_ReturnsAllCategoriesOrdered

```csharp
[Fact]
public async Task GetAllCategories_ReturnsAllCategoriesOrdered()
{
    // Act
    var response = await Client.GetAsync("/api/categories");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();
    result.Should().NotBeNull();
    result!.Categories.Should().HaveCount(3);

    // Verify ordering by sort_order
    result.Categories[0].Name.Should().Be("Electronics");
    result.Categories[1].Name.Should().Be("Clothing");
    result.Categories[2].Name.Should().Be("Books");
}
```

**What it does:**
1. **Act**: Sends an HTTP GET request to `/api/categories` using the test HTTP client
2. **Assert**: Verifies the response status code is 200 OK
3. **Assert**: Deserializes the JSON response to `GetAllCategoriesResponse`
4. **Assert**: Checks that exactly 3 categories are returned
5. **Assert**: Verifies categories are ordered by `sort_order` (Electronics=1, Clothing=2, Books=3)

**What it tests:**
- ✅ Endpoint is accessible and returns HTTP 200
- ✅ Response body deserializes correctly
- ✅ All seeded categories are returned
- ✅ Categories are sorted by `sort_order` column

#### Step 5: Test Method - GetAllCategories_ReturnsCachedResults

```csharp
[Fact]
public async Task GetAllCategories_ReturnsCachedResults()
{
    // Act
    var response1 = await Client.GetAsync("/api/categories");
    var response2 = await Client.GetAsync("/api/categories");

    // Assert
    response1.StatusCode.Should().Be(HttpStatusCode.OK);
    response2.StatusCode.Should().Be(HttpStatusCode.OK);

    var result1 = await response1.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();
    var result2 = await response2.Content.ReadFromJsonAsync<GetAllCategoriesResponse>();

    result1!.Categories.Should().BeEquivalentTo(result2!.Categories);
}
```

**What it does:**
1. **Act**: Makes two identical GET requests to the categories endpoint
2. **Assert**: Both responses return HTTP 200
3. **Assert**: Both responses contain equivalent data

**What it tests:**
- ✅ Multiple requests succeed
- ✅ Caching (if enabled) returns consistent data
- ✅ Response data integrity across multiple calls

**Note**: The `Category` entity has `[Cacheable(DurationSeconds = 600, SlidingExpiration = true)]`, so the second request should hit the cache rather than the database.

### Key Components Deep Dive

#### PostgreSqlContainerFixture

```csharp
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")  // Lightweight Alpine-based image
            .WithDatabase("webvella_test")    // Database name
            .WithUsername("postgres")         // PostgreSQL username
            .WithPassword("postgres")         // PostgreSQL password
            .WithCleanUp(true)                // Auto-remove container on dispose
            .Build();
    }
}
```

**Lifecycle:**
- `InitializeAsync()`: Starts the container when the first test in the collection runs
- `DisposeAsync()`: Stops and removes the container after all tests complete
- Container is shared across all test classes in the collection

#### IntegrationTestBase

**Key responsibilities:**
- Creates `CustomWebApplicationFactory` with the test connection string
- Initializes the HTTP client for API calls
- Sets up Respawner for database cleanup
- Provides `DbService` for direct database access in tests
- Resets the database and seeds data before each test

**Why Respawner:**
- Efficiently truncates all tables between tests
- Preserves the database schema (no need to re-run migrations)
- Much faster than dropping/recreating the database
- Excludes migration tracking tables (`_db_version`)

### Writing New Integration Tests

To add a new integration test class:

1. **Create the test class** inheriting from `IntegrationTestBase`:

```csharp
[Collection(PostgreSqlCollection.Name)]
public class MyNewEndpointTests : IntegrationTestBase
{
    public MyNewEndpointTests(PostgreSqlContainerFixture postgresFixture)
        : base(postgresFixture)
    {
    }
}
```

2. **Override `SeedTestDataAsync()`** if you need specific test data:

```csharp
protected override async Task SeedTestDataAsync()
{
    await DbService.ExecuteAsync("""
        INSERT INTO my_table (id, name)
        VALUES ('...', 'Test Data')
        """);
}
```

3. **Write test methods** using `Client` for HTTP and `DbService` for database:

```csharp
[Fact]
public async Task MyEndpoint_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var request = new MyRequest { /* ... */ };

    // Act
    var response = await PostAsync("/api/my-endpoint", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    // Verify database state
    var entity = await DbService.GetAsync<MyEntity>(expectedId);
    entity.Should().NotBeNull();
}
```

### Best Practices

| Practice | Reason |
|----------|--------|
| Use predictable GUIDs | Makes assertions easier and debugging clearer |
| Keep seed data minimal | Only seed what the specific test needs |
| Test one behavior per test | Easier to identify failures |
| Use FluentAssertions | More readable and descriptive test failures |
| Verify database state | Ensures side effects occurred correctly |
| Test both success and failure paths | Complete coverage of endpoint behavior |
