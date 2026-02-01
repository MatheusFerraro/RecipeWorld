# Recipe World - ASP.NET Core MVC Recipe Management System

A full-featured recipe management web application built with ASP.NET Core 8.0, featuring user authentication, recipe management, ingredient tracking, and admin approval workflows.

## Features

- **User Authentication & Authorization**
  # Recipe World - ASP.NET Core MVC Recipe Management System

  A full-featured recipe management web application built with ASP.NET Core (.NET 8), featuring user authentication, recipe management, ingredient tracking, and admin approval workflows.

  ## Features

  - **User Authentication & Authorization**
    - ASP.NET Core Identity with role-based access
    - User registration and login
    - Email confirmation
    - Two-factor authentication support

  - **Recipe Management**
    - Create, read, update, and delete recipes
    - Recipe status workflow (Draft, Public, Private, Pending)
    - Rich recipe details (prep time, cook time, servings, temperature)
    - Step-by-step cooking instructions
    - Recipe categorization
    - Image support (upload recipe photos or use image URLs)

  - **Ingredient Management**
    - Ingredient database
    - Link ingredients to recipes with amounts and units
    - Admin approval system for new ingredients

  - **Search & Filter**
    - Search recipes by name or description
    - Filter by category and status
    - Pagination support

  - **Responsive Design**
    - Mobile-friendly interface
    - Bootstrap-based UI

  ## Technologies Used

  - **Backend**: ASP.NET Core MVC (.NET 8)
  - **Database**: SQL Server + Entity Framework Core
  - **Authentication**: ASP.NET Core Identity
  - **Frontend**: Razor Views + Bootstrap
  - **API Documentation**: Swagger/OpenAPI
  - **Languages**: C#, HTML, CSS, JavaScript

  ## Prerequisites

  - .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
  - SQL Server (LocalDB, Express, or full): https://www.microsoft.com/sql-server/sql-server-downloads
  - Visual Studio 2022 or VS Code

  ## Setup Instructions

  ### 1) Clone the repository

  ```bash
  git clone <your-repository-url>
  cd RecipeWorld
  ```

  ### 2) Configure your database

  This project uses the `RecipeWorldConnectionString` from `src/worldRecipeMvc/appsettings.json`.

  If you're using SQL Server LocalDB, update it to something like:

  ```json
  {
    "ConnectionStrings": {
      "RecipeWorldConnectionString": "Server=(localdb)\\mssqllocaldb;Database=WorldRecipeDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
  }
  ```

  ### 3) Apply migrations

  ```bash
  cd src/worldRecipeMvc
  dotnet ef database update
  ```

  This will create the database, apply migrations, and seed sample data (categories, ingredients, recipes, and demo accounts).

  ### 4) Configure image uploads (optional)

  Recipe image uploads are stored in an external folder and served at `/images/recipes/*`.

  Update the `ImageUpload:StoragePath` setting in `src/worldRecipeMvc/appsettings.json` to match your machine:

  ```json
  {
    "ImageUpload": {
      "StoragePath": "C:\\PATH\\TO\\RecipeWorld\\img\\worldRecipeMvc\\wwwroot\\images\\recipes"
    }
  }
  ```

  ### 5) Run the app

  From `src/worldRecipeMvc`:

  ```bash
  dotnet run
  ```

  Or from the repo root:

  ```bash
  dotnet run --project src/worldRecipeMvc/worldRecipeMvc.csproj
  ```

  In Development, Swagger is available at `/swagger`.

  ## Demo Accounts

  For testing purposes, the following accounts are seeded:

  - Regular user: `demo@recipeworld.com` / `Demo123!`
  - Admin user: `admin@recipeworld.com` / `Admin123!`

  Note: change these credentials in production.

  ## Sample Data

  Seeded recipes include:

  1. Brigadeiro - Traditional Brazilian chocolate truffle dessert
  2. Spaghetti Carbonara - Classic Italian pasta dish
  3. Chicken Tikka Masala - Popular Indian curry
  4. Margherita Pizza - Traditional Italian pizza
  5. Classic Pancakes - American breakfast favorite
  6. Caesar Salad - Fresh and crispy salad

  ## Project Structure

  ```
  RecipeWorld/
    sql/
    src/
      worldRecipeMvc/
        Areas/
          Identity/
        Controllers/
        Data/
        Models/
        Services/
        Views/
        wwwroot/
    img/
      worldRecipeMvc/wwwroot/images/recipes/
  ```

  ## Troubleshooting

  ### Database connection errors

  - Ensure your SQL Server instance is running.
  - Confirm `RecipeWorldConnectionString` points to a reachable SQL Server.
  - Reset the database (destructive):

  ```bash
  dotnet ef database drop
  dotnet ef database update
  ```

  ### Email confirmation in development

  `RequireConfirmedAccount = true` is enabled. Use the seeded demo accounts, or configure an email sender for registration/confirmation.

  ## Contributing

  This is a portfolio project, but suggestions and feedback are welcome.

  ## Contact

  - GitHub: https://github.com/MatheusFerraro
  - LinkedIn: https://www.linkedin.com/in/mcamiloferraro/
  - Email: mailto:matheus.ferraro@gmail.com
1. Ensure SQL Server is running

2. Verify the connection string in `appsettings.json`

3. Try resetting the database:
