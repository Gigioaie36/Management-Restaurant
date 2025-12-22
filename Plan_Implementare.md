# Implementation Plan - Restaurant Management System

## User Review Required
> [!IMPORTANT]
> **Technology Decisions - Please Confirm**:
> 1.  **Framework**: I have chosen **WPF** because the requirements explicitly mention "MVVM Focus".
> 2.  **Database**: I plan to use **SQL Server (LocalDB)** as it is the standard for .NET/Windows environments.
> 3.  **Data Access**: I will use **Entity Framework Core (Code First)** as requested.
>     *Reasoning*: Matches the course requirements. We will use a `DbContext` to manage SQLite/SQLServer interactions.

## Proposed Changes

### 1. Database Schema (EF Core Code First)
We will define C# Models and let EF Core generate the database `RestaurantDB`.

#### Tables (Entities)
- **Category**: `Id`, `Name`
- **MenuItem**: `Id`, `Name`, `Description`, `Price`, `CategoryId (FK)`
- **Ingredient**: `Id`, `Name`, `StockQuantity`, `Unit`
- **RecipeItem**: `Id`, `MenuItemId (FK)`, `IngredientId (FK)`, `QuantityRequired`
- **RestaurantTable**: `Id`, `TableNumber`, `Status` (Enum: Free, Occupied, WaitingPayment), `Capacity`
- **Order**: `Id`, `TableId (FK)`, `OrderDate`, `Status` (Enum: New, Preparing, Served, Paid), `TotalAmount`, `PaymentMethod`
- **OrderItem**: `Id`, `OrderId (FK)`, `MenuItemId (FK)`, `Quantity`, `PriceAtOrder`

#### Core Logic
- **Business Logic Layer**: Services that wrap `DbContext` to perform operations like `CalculateTotal`, `DeductStock`, etc.
- **Reporting**: LINQ queries to generate the required reports.

### 2. C# Application Structure (WPF + MVVM)
The solution will be named `RestaurantManager`.

#### Core Components
- **Data Layer**:
    - `RestaurantDbContext`: Inherits from `DbContext`. Configures relationships.
- **Models**: POCO classes with Data Annotations.
- **ViewModels (MVVM)**:
    - `MainViewModel`: Navigation.
    - `MenuViewModel`: CRUD using `DbContext`.
    - `OrderViewModel`: Order processing.
    - `KitchenViewModel`: Status updates.
    - `ReportsViewModel`: LINQ projections for reports.
- **Views (XAML)**:
    - `MenuView`, `TablesView`, `OrderView`, `ReportsView`.
- **Utilities**:
    - `RelayCommand`: Implementation of `ICommand`.
    - `ViewModelBase`: Implementation of `INotifyPropertyChanged`.

### 3. Application Flow
1.  **Launch**: Checks for DB existence, initializes Schema if missing (First Run experience).
2.  **Main Window**: Sidebar navigation (Menu, Tables, Orders, Reports).

## Verification Plan

### Automated Verification
- We will try to create a basic Unit Test project for the **Total Calculation** logic (Price * Quantity + VAT).

### Manual Verification Checklist
- [ ] **Database Connection**: Verify app connects to LocalDB.
- [ ] **Menu CRUD**: Create "Pizza", Edit Price, Delete "Soup".
- [ ] **Inventory**: Create "Flour", assign to "Pizza".
- [ ] **Order Flow**: Open Table 1 -> Add Pizza -> Send Order -> Stock Deducted -> Close Order -> Table Freed.
- [ ] **Reports**: Generate "Top 5" and verify the Pizza shows up.
